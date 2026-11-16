namespace Mooncake.EcommercePlatform.Verification.Tests;

using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Mooncake.EcommercePlatform.Application.Common.DTOs;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.Common.Utils;
using Mooncake.EcommercePlatform.Application.DTOs.Auth.Models;
using Mooncake.EcommercePlatform.Application.DTOs.Authen.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Authen.Responses;
using Mooncake.EcommercePlatform.Application.DTOs.Deliveries.Responses;
using Mooncake.EcommercePlatform.Application.DTOs.Drivers.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Drivers.Responses;
using Mooncake.EcommercePlatform.Application.Services.Implementations;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.WebApi.Controllers;
using Mooncake.EcommercePlatform.WebApi.Middleware;
using Xunit;

public sealed class DriverAccountTests
{
    private static readonly Guid DriverUserId = Guid.NewGuid();
    private static readonly Guid OtherDriverUserId = Guid.NewGuid();
    private static readonly Guid CustomerUserId = Guid.NewGuid();
    private static readonly Guid DeliveryId = Guid.NewGuid();

    private readonly Mock<IDriverRepository> driverRepository = new();
    private readonly Mock<IDeliveryRepository> deliveryRepository = new();
    private readonly Mock<IUserRepository> userRepository = new();
    private readonly Mock<IAuthService> authService = new();
    private readonly Mock<IDateTimeProvider> clock = new();
    private readonly Mock<INotificationService> notificationService = new();
    private readonly DateTime fixedNow = new(2026, 11, 16, 8, 0, 0, DateTimeKind.Utc);

    public DriverAccountTests()
    {
        clock.SetupGet(c => c.UtcNow).Returns(fixedNow);
    }

    [Fact]
    public async Task Register_ValidConditions_CreatesAccountAndReturnsTokens()
    {
        var service = CreateService();
        var token = "valid-trip-token";
        var phone = "0987654321";

        var delivery = new Delivery
        {
            Id = DeliveryId,
            DeliveryNumber = "DEL-001",
            DriverPhone = phone,
            DriverName = "Nguyễn Văn A",
            VehiclePlate = "29A-11111",
            DriverClaimedAt = fixedNow.AddHours(-2),
            ShareToken = token,
            ShareTokenExpiresAtUtc = fixedNow.AddHours(22),
            Status = DeliveryStatus.InTransit,
            CreatedAt = fixedNow.AddHours(-3)
        };

        deliveryRepository.Setup(r => r.GetByShareTokenAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(delivery);
        userRepository.Setup(r => r.GetByPhoneAsync(phone, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthUserData?)null);

        AuthUserData? createdUser = null;
        userRepository.Setup(r => r.CreateAsync(It.IsAny<AuthUserData>(), It.IsAny<CancellationToken>()))
            .Callback<AuthUserData, CancellationToken>((u, _) => createdUser = u)
            .ReturnsAsync((AuthUserData u, CancellationToken _) => u);

        DriverProfile? createdProfile = null;
        driverRepository.Setup(r => r.CreateProfileAsync(It.IsAny<DriverProfile>(), It.IsAny<CancellationToken>()))
            .Callback<DriverProfile, CancellationToken>((p, _) => createdProfile = p)
            .ReturnsAsync((DriverProfile p, CancellationToken _) => p);

        var expectedAuthenResponse = new AuthenResponse(
            "access-token-xyz", "refresh-token-xyz", "Bearer", 3600,
            new AuthenUserResponse(DriverUserId, phone, "Nguyễn Văn A", UserRole.Driver));

        authService.Setup(a => a.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedAuthenResponse);

        var request = new DriverRegisterRequest
        {
            Token = token,
            PhoneNumber = phone,
            Password = "SecurePassword123!",
            FullName = "Nguyễn Văn A",
            VehiclePlate = "29A-22222"
        };

        var response = await service.RegisterAsync(request);

        Assert.NotNull(response);
        Assert.Equal(UserRole.Driver, response.User.Role);
        Assert.NotNull(createdUser);
        Assert.Equal(UserRole.Driver, createdUser.Role);
        Assert.Equal(phone, createdUser.Phone);
        Assert.NotNull(createdProfile);
        Assert.Equal("29A-22222", createdProfile.DefaultVehiclePlate);
    }

    [Fact]
    public async Task Register_MismatchedPhone_Throws400()
    {
        var service = CreateService();
        var token = "trip-token";

        var delivery = new Delivery
        {
            Id = DeliveryId,
            DriverPhone = "0987654321",
            DriverClaimedAt = fixedNow.AddHours(-1),
            ShareToken = token
        };

        deliveryRepository.Setup(r => r.GetByShareTokenAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(delivery);

        var request = new DriverRegisterRequest
        {
            Token = token,
            PhoneNumber = "0912345678",
            Password = "SecurePassword123!",
            FullName = "Tài xế B"
        };

        var ex = await Assert.ThrowsAsync<HttpException>(() => service.RegisterAsync(request));
        Assert.Equal(400, ex.StatusCode);
        Assert.Contains("không trùng khớp", ex.Message);
    }

    [Fact]
    public async Task Register_ClaimedOver24HoursAgo_Throws400()
    {
        var service = CreateService();
        var token = "expired-claim-token";
        var phone = "0987654321";

        var delivery = new Delivery
        {
            Id = DeliveryId,
            DriverPhone = phone,
            DriverClaimedAt = fixedNow.AddHours(-25),
            ShareToken = token
        };

        deliveryRepository.Setup(r => r.GetByShareTokenAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(delivery);

        var request = new DriverRegisterRequest
        {
            Token = token,
            PhoneNumber = phone,
            Password = "SecurePassword123!",
            FullName = "Tài xế A"
        };

        var ex = await Assert.ThrowsAsync<HttpException>(() => service.RegisterAsync(request));
        Assert.Equal(400, ex.StatusCode);
        Assert.Contains("quá 24 giờ", ex.Message);
    }

    [Fact]
    public async Task Register_UnclaimedDelivery_Throws400()
    {
        var service = CreateService();
        var token = "unclaimed-token";
        var phone = "0987654321";

        var delivery = new Delivery
        {
            Id = DeliveryId,
            DriverClaimedAt = null,
            ShareToken = token
        };

        deliveryRepository.Setup(r => r.GetByShareTokenAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(delivery);

        var request = new DriverRegisterRequest
        {
            Token = token,
            PhoneNumber = phone,
            Password = "SecurePassword123!",
            FullName = "Tài xế A"
        };

        var ex = await Assert.ThrowsAsync<HttpException>(() => service.RegisterAsync(request));
        Assert.Equal(400, ex.StatusCode);
        Assert.Contains("chưa được nhận", ex.Message);
    }

    [Fact]
    public async Task Register_PhoneAlreadyHasAccount_Throws409Conflict()
    {
        var service = CreateService();
        var phone = "0987654321";

        userRepository.Setup(r => r.GetByPhoneAsync(phone, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthUserData { Id = Guid.NewGuid(), Phone = phone, Role = UserRole.Customer });

        var request = new DriverRegisterRequest
        {
            Token = "any-token",
            PhoneNumber = phone,
            Password = "SecurePassword123!",
            FullName = "Tài xế A"
        };

        var ex = await Assert.ThrowsAsync<HttpException>(() => service.RegisterAsync(request));
        Assert.Equal(409, ex.StatusCode);
        Assert.Equal("Số điện thoại này đã có tài khoản. Hãy đăng nhập.", ex.Message);
    }

    [Fact]
    public async Task Register_ShortPassword_Throws400()
    {
        var service = CreateService();

        var request = new DriverRegisterRequest
        {
            Token = "token",
            PhoneNumber = "0987654321",
            Password = "short",
            FullName = "Tài xế A"
        };

        var ex = await Assert.ThrowsAsync<HttpException>(() => service.RegisterAsync(request));
        Assert.Equal(400, ex.StatusCode);
        Assert.Contains("8 ký tự", ex.Message);
    }

    [Fact]
    public async Task GetTrips_GroupsByDriverPhone_ReturnsPublicDTOWithoutPrices()
    {
        var service = CreateService();
        var phone = "0987654321";

        driverRepository.Setup(r => r.GetProfileByUserIdAsync(DriverUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DriverProfile
            {
                UserId = DriverUserId,
                Phone = phone,
                FullName = "Tài xế A"
            });

        var trip1 = new Delivery
        {
            Id = Guid.NewGuid(),
            DeliveryNumber = "DEL-001",
            DriverPhone = phone,
            ShareToken = "token-1",
            ShareTokenExpiresAtUtc = fixedNow.AddHours(10),
            Status = DeliveryStatus.InTransit,
            CreatedAt = fixedNow.AddHours(-1)
        };

        var pagedResult = new PagedResult<Delivery>([trip1], 1, 10, 1);

        driverRepository.Setup(r => r.GetDriverTripsPageAsync(phone, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        deliveryRepository.Setup(r => r.GetProofsAsync(trip1.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        deliveryRepository.Setup(r => r.GetDeliveryItemsAsync(trip1.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await service.GetTripsAsync(DriverUserId, null, 1, 10);

        Assert.NotNull(result);
        Assert.Single(result.Items);
        var tripResponse = result.Items[0];
        Assert.Equal(trip1.Id, tripResponse.DeliveryId);
        Assert.Equal("DEL-001", tripResponse.DeliveryNumber);
        Assert.Equal("token-1", tripResponse.Token);
    }

    [Fact]
    public async Task ClaimTrip_WithDriverAccount_ClaimsSuccessfully()
    {
        var service = CreateService();
        var phone = "0987654321";

        driverRepository.Setup(r => r.GetProfileByUserIdAsync(DriverUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DriverProfile
            {
                UserId = DriverUserId,
                Phone = phone,
                FullName = "Nguyễn Văn Tài Xế",
                DefaultVehiclePlate = "51F-99999"
            });

        var delivery = new Delivery
        {
            Id = DeliveryId,
            DeliveryNumber = "DEL-CLAIM-01",
            Status = DeliveryStatus.Confirmed,
            DriverClaimedAt = null,
            ShareToken = "open-token",
            ShareTokenExpiresAtUtc = fixedNow.AddHours(5),
            CreatedAt = fixedNow.AddHours(-1)
        };

        deliveryRepository.Setup(r => r.GetByIdAsync(DeliveryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(delivery);
        deliveryRepository.Setup(r => r.UpdateAsync(It.IsAny<Delivery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Delivery d, CancellationToken _) => d);
        deliveryRepository.Setup(r => r.GetProofsAsync(DeliveryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var response = await service.ClaimTripAsync(DriverUserId, DeliveryId, new DriverClaimTripRequest
        {
            VehiclePlate = "51F-88888"
        });

        Assert.NotNull(response);
        Assert.Equal("Nguyễn Văn Tài Xế", delivery.DriverName);
        Assert.Equal(phone, delivery.DriverPhone);
        Assert.Equal("51F-88888", delivery.VehiclePlate);
        Assert.NotNull(delivery.DriverClaimedAt);
    }

    [Fact]
    public async Task ClaimTrip_WhenAlreadyClaimedByOtherDriver_Throws409Conflict()
    {
        var service = CreateService();

        driverRepository.Setup(r => r.GetProfileByUserIdAsync(DriverUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DriverProfile
            {
                UserId = DriverUserId,
                Phone = "0987654321",
                FullName = "Tài xế A"
            });

        var delivery = new Delivery
        {
            Id = DeliveryId,
            DriverPhone = "0912345678", // Different driver's phone
            DriverClaimedAt = fixedNow.AddHours(-1),
            Status = DeliveryStatus.InTransit,
            ShareToken = "open-token",
            ShareTokenExpiresAtUtc = fixedNow.AddHours(5)
        };

        deliveryRepository.Setup(r => r.GetByIdAsync(DeliveryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(delivery);

        var ex = await Assert.ThrowsAsync<HttpException>(() =>
            service.ClaimTripAsync(DriverUserId, DeliveryId, new DriverClaimTripRequest()));

        Assert.Equal(409, ex.StatusCode);
        Assert.Equal("Chuyến đã có tài xế khác nhận. Liên hệ xưởng nếu cần đổi tài xế.", ex.Message);
    }

    [Fact]
    public async Task ClaimTrip_WhenSupplierRevokedLink_Throws409AndDoesNotReopenLink()
    {
        var service = CreateService();
        driverRepository.Setup(r => r.GetProfileByUserIdAsync(DriverUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DriverProfile { UserId = DriverUserId, Phone = "0987654321", FullName = "Tài xế A" });

        var revokedAt = fixedNow.AddMinutes(-10);
        var delivery = new Delivery
        {
            Id = DeliveryId,
            DriverPhone = "0987654321", // assigned to this driver, but the supplier revoked the link
            Status = DeliveryStatus.Confirmed,
            ShareToken = "revoked-token",
            ShareTokenExpiresAtUtc = fixedNow.AddHours(5),
            ShareTokenRevokedAtUtc = revokedAt
        };
        deliveryRepository.Setup(r => r.GetByIdAsync(DeliveryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(delivery);

        var ex = await Assert.ThrowsAsync<HttpException>(() =>
            service.ClaimTripAsync(DriverUserId, DeliveryId, new DriverClaimTripRequest()));

        Assert.Equal(409, ex.StatusCode);
        Assert.Equal(revokedAt, delivery.ShareTokenRevokedAtUtc);
        Assert.Null(delivery.DriverClaimedAt);
        deliveryRepository.Verify(r => r.UpdateAsync(It.IsAny<Delivery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ClaimTrip_WhenLinkExpiredAndNotAssignedToDriver_Throws409()
    {
        var service = CreateService();
        driverRepository.Setup(r => r.GetProfileByUserIdAsync(DriverUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DriverProfile { UserId = DriverUserId, Phone = "0987654321", FullName = "Tài xế A" });

        var delivery = new Delivery
        {
            Id = DeliveryId,
            Status = DeliveryStatus.Confirmed,
            ShareToken = "old-token",
            ShareTokenExpiresAtUtc = fixedNow.AddHours(-1)
        };
        deliveryRepository.Setup(r => r.GetByIdAsync(DeliveryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(delivery);

        var ex = await Assert.ThrowsAsync<HttpException>(() =>
            service.ClaimTripAsync(DriverUserId, DeliveryId, new DriverClaimTripRequest()));

        Assert.Equal(409, ex.StatusCode);
        Assert.Null(delivery.DriverClaimedAt);
    }

    [Fact]
    public async Task DriverRole_Forbidden_OnCustomerAndSupplierApis()
    {
        var client = CreateHttpClientWithRole("Driver");

        // Customer-only endpoint
        var customerOrdersResponse = await client.GetAsync("/api/v1/orders/mine");
        Assert.Equal(HttpStatusCode.Forbidden, customerOrdersResponse.StatusCode);

        // Supplier-only endpoint
        var supplierOrdersResponse = await client.GetAsync("/api/v1/orders/supplier");
        Assert.Equal(HttpStatusCode.Forbidden, supplierOrdersResponse.StatusCode);
    }

    [Fact]
    public async Task DriverRole_CanAccessDriverEndpoints_CustomerForbidden()
    {
        var driverMock = new Mock<IDriverService>();
        driverMock.Setup(d => d.GetProfileAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DriverProfileResponse(DriverUserId, "Tài xế A", "0987654321", "29A-12345", fixedNow));

        var driverClient = CreateHttpClientWithRole("Driver", driverMock.Object);
        var profileResponse = await driverClient.GetAsync("/api/v1/driver/profile");
        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);

        // Customer trying to access driver endpoint
        var customerClient = CreateHttpClientWithRole("Customer", driverMock.Object);
        var customerAccessResponse = await customerClient.GetAsync("/api/v1/driver/profile");
        Assert.Equal(HttpStatusCode.Forbidden, customerAccessResponse.StatusCode);
    }

    private DriverService CreateService() =>
        new(driverRepository.Object,
            deliveryRepository.Object,
            userRepository.Object,
            authService.Object,
            clock.Object,
            notificationService.Object);

    private HttpClient CreateHttpClientWithRole(string role, IDriverService? customDriverService = null)
    {
        var userId = role == "Driver" ? DriverUserId : CustomerUserId;
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        builder.Services.AddControllers()
            .AddApplicationPart(typeof(DriverController).Assembly)
            .AddApplicationPart(typeof(OrdersController).Assembly)
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            });

        builder.Services.AddSingleton(customDriverService ?? CreateService());

        // Dummy services needed if OrdersController is loaded
        var orderService = new Mock<IOrderService>();
        builder.Services.AddSingleton(orderService.Object);
        var directBulkOrderService = new Mock<IDirectBulkOrderService>();
        builder.Services.AddSingleton(directBulkOrderService.Object);

        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddProblemDetails();

        builder.Services.AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("SupplierExistingFulfillment", policy => policy.RequireRole("Supplier", "Admin"));
        });

        var app = builder.Build();
        app.UseExceptionHandler();
        app.Use(async (context, next) =>
        {
            var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            ], "Test");

            context.User = new ClaimsPrincipal(identity);
            await next();
        });

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.StartAsync().GetAwaiter().GetResult();

        return app.GetTestClient();
    }
}
