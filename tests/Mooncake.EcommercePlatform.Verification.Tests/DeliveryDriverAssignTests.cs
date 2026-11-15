namespace Mooncake.EcommercePlatform.Verification.Tests;

using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.DTOs;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Deliveries.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Deliveries.Responses;
using Mooncake.EcommercePlatform.Application.Services.Implementations;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Services;
using Mooncake.EcommercePlatform.WebApi.Controllers;
using Xunit;

public sealed class DeliveryDriverAssignTests
{
    private static readonly Guid SupplierId = Guid.NewGuid();
    private static readonly Guid OtherSupplierId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid DeliveryId = Guid.NewGuid();
    private static readonly Guid OrderId = Guid.NewGuid();
    private static readonly Guid ContractId = Guid.NewGuid();

    private readonly Mock<IDeliveryRepository> repository = new();
    private readonly Mock<IDateTimeProvider> clock = new();
    private readonly Mock<INotificationService> notificationService = new();
    private readonly DateTime fixedNow = new(2026, 11, 16, 8, 0, 0, DateTimeKind.Utc);

    public DeliveryDriverAssignTests()
    {
        clock.SetupGet(c => c.UtcNow).Returns(fixedNow);
    }

    [Fact]
    public async Task SubmitPublicProof_WhenTripNotClaimed_Throws409Conflict()
    {
        var service = CreateService(out var delivery);
        delivery.DriverClaimedAt = null;

        var ex = await Assert.ThrowsAsync<HttpException>(() =>
            service.SubmitPublicProofAsync("sample-token", new CreateDeliveryProofRequest
            {
                ProofType = ProofType.Delivery,
                PhotoUrls = ["https://res.cloudinary.com/mooncake/image/upload/pod/test.jpg"]
            }));

        Assert.Equal(409, ex.StatusCode);
        Assert.Equal("Hãy nhận chuyến trước", ex.Message);
    }

    [Fact]
    public async Task EnsurePublicTokenSubmittable_WhenTripNotClaimed_Throws409Conflict()
    {
        var service = CreateService(out var delivery);
        delivery.DriverClaimedAt = null;

        var ex = await Assert.ThrowsAsync<HttpException>(() =>
            service.EnsurePublicTokenSubmittableAsync("sample-token"));

        Assert.Equal(409, ex.StatusCode);
        Assert.Equal("Hãy nhận chuyến trước", ex.Message);
    }

    [Fact]
    public async Task GetPublicTrip_WhenUnclaimed_CanSubmitProofTrueAndRequiresClaimTrue()
    {
        var service = CreateService(out var delivery);
        delivery.DriverClaimedAt = null;
        delivery.Status = DeliveryStatus.InTransit;

        var response = await service.GetPublicByTokenAsync("sample-token");

        Assert.NotNull(response);
        Assert.True(response.CanSubmitProof);
        Assert.True(response.RequiresClaim);
    }

    [Fact]
    public async Task GetPublicTrip_WhenClaimed_RequiresClaimFalse()
    {
        var service = CreateService(out var delivery);
        delivery.DriverClaimedAt = fixedNow;
        delivery.Status = DeliveryStatus.InTransit;

        var response = await service.GetPublicByTokenAsync("sample-token");

        Assert.NotNull(response);
        Assert.True(response.CanSubmitProof);
        Assert.False(response.RequiresClaim);
    }

    [Fact]
    public async Task GetPublicTrip_WhenDelivered_CanSubmitProofFalse()
    {
        var service = CreateService(out var delivery);
        delivery.DriverClaimedAt = fixedNow;
        delivery.Status = DeliveryStatus.Delivered;

        var response = await service.GetPublicByTokenAsync("sample-token");

        Assert.NotNull(response);
        Assert.False(response.CanSubmitProof);
        Assert.False(response.RequiresClaim);
    }

    [Theory]
    [InlineData(DeliveryStatus.InTransit, null, true, true)]
    [InlineData(DeliveryStatus.Pending, null, true, true)]
    [InlineData(DeliveryStatus.PickedUp, null, true, true)]
    [InlineData(DeliveryStatus.InTransit, "2026-11-16T08:00:00Z", true, false)]
    [InlineData(DeliveryStatus.Delivered, null, false, false)]
    [InlineData(DeliveryStatus.Delivered, "2026-11-16T08:00:00Z", false, false)]
    [InlineData(DeliveryStatus.Returned, null, false, false)]
    [InlineData(DeliveryStatus.Returned, "2026-11-16T08:00:00Z", false, false)]
    public void ToPublicDriverResponse_SetsCanSubmitProofAndRequiresClaim_Correctly(
        DeliveryStatus status,
        string? claimedAtString,
        bool expectedCanSubmitProof,
        bool expectedRequiresClaim)
    {
        var delivery = new Delivery
        {
            Id = DeliveryId,
            Status = status,
            DriverClaimedAt = claimedAtString is not null ? DateTime.Parse(claimedAtString).ToUniversalTime() : null
        };

        var response = Mooncake.EcommercePlatform.Application.Common.Helpers.DeliveryResponseHelper.ToPublicDriverResponse(delivery, 0);

        Assert.Equal(expectedCanSubmitProof, response.CanSubmitProof);
        Assert.Equal(expectedRequiresClaim, response.RequiresClaim);
    }

    [Fact]
    public async Task ClaimTrip_WhenFirstClaim_SavesDriverAndClaimedAtAndWorkflowEventAndNotifications()
    {
        var service = CreateService(out var delivery);
        delivery.DriverClaimedAt = null;

        WorkflowEvent? recordedEvent = null;
        repository.Setup(r => r.AddWorkflowEventAsync(It.IsAny<WorkflowEvent>(), It.IsAny<CancellationToken>()))
            .Callback<WorkflowEvent, CancellationToken>((we, _) => recordedEvent = we)
            .Returns(Task.CompletedTask);

        repository.Setup(r => r.GetSupplierUserIdForDeliveryAsync(delivery.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SupplierId);
        repository.Setup(r => r.GetCustomerUserIdForDeliveryAsync(delivery.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CustomerId);

        var request = new ClaimDeliveryTripRequest
        {
            DriverName = "Nguyễn Văn Tài",
            DriverPhone = "0912345678",
            VehiclePlate = "29A-12345"
        };

        var response = await service.ClaimTripAsync("sample-token", request);

        Assert.NotNull(response);
        Assert.Equal("Nguyễn Văn Tài", delivery.DriverName);
        Assert.Equal("0912345678", delivery.DriverPhone);
        Assert.Equal("29A-12345", delivery.VehiclePlate);
        Assert.Equal(fixedNow, delivery.DriverClaimedAt);
        Assert.True(response.CanSubmitProof);
        Assert.False(response.RequiresClaim);

        Assert.NotNull(recordedEvent);
        Assert.Equal("Delivery", recordedEvent!.AggregateType);
        Assert.Equal("DriverClaimed", recordedEvent.EventType);
        Assert.Contains("Nguyễn Văn Tài", recordedEvent.Reason);

        notificationService.Verify(n => n.DispatchNotificationAsync(
            SupplierId,
            "delivery.driver_claimed",
            It.IsAny<string>(),
            It.Is<string>(msg => msg.Contains("Tài xế Nguyễn Văn Tài – 0912345678 – 29A-12345 đã nhận chuyến")),
            "Delivery",
            delivery.Id,
            null,
            "medium",
            It.IsAny<CancellationToken>()), Times.Once);

        notificationService.Verify(n => n.DispatchNotificationAsync(
            CustomerId,
            "delivery.driver_claimed",
            It.IsAny<string>(),
            It.Is<string>(msg => msg.Contains("Tài xế Nguyễn Văn Tài – 0912345678 – 29A-12345 đã nhận chuyến")),
            "Delivery",
            delivery.Id,
            null,
            "medium",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ClaimTrip_WhenSamePhone_IsIdempotentAndUpdatesPlate()
    {
        var service = CreateService(out var delivery);
        delivery.DriverName = "Nguyễn Văn Tài";
        delivery.DriverPhone = "0912345678";
        delivery.VehiclePlate = "29A-12345";
        delivery.DriverClaimedAt = fixedNow.AddHours(-1);

        var request = new ClaimDeliveryTripRequest
        {
            DriverName = "Nguyễn Văn Tài Đổi Xe",
            DriverPhone = "+84912345678", // Same normalized phone
            VehiclePlate = "29A-99999"
        };

        var response = await service.ClaimTripAsync("sample-token", request);

        Assert.NotNull(response);
        Assert.Equal("Nguyễn Văn Tài Đổi Xe", delivery.DriverName);
        Assert.Equal("29A-99999", delivery.VehiclePlate);
        Assert.Equal(fixedNow.AddHours(-1), delivery.DriverClaimedAt);
    }

    [Fact]
    public async Task ClaimTrip_WhenDifferentPhone_Throws409Conflict()
    {
        var service = CreateService(out var delivery);
        delivery.DriverName = "Bác Hùng";
        delivery.DriverPhone = "0912345678";
        delivery.VehiclePlate = "29A-12345";
        delivery.DriverClaimedAt = fixedNow.AddHours(-1);

        var request = new ClaimDeliveryTripRequest
        {
            DriverName = "Bác Tuấn",
            DriverPhone = "0987654321", // Different phone
            VehiclePlate = "30B-55555"
        };

        var ex = await Assert.ThrowsAsync<HttpException>(() =>
            service.ClaimTripAsync("sample-token", request));

        Assert.Equal(409, ex.StatusCode);
        Assert.Equal("Chuyến đã có tài xế nhận", ex.Message);
    }

    [Fact]
    public async Task PublicResponse_ContainsOrderCodeAndItems_AndDoesNotExposePrice()
    {
        var service = CreateService(out var delivery);
        delivery.OrderId = OrderId;
        delivery.DriverClaimedAt = fixedNow;

        repository.Setup(r => r.GetDeliveryItemsAsync(delivery.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new DeliveryItem
                {
                    Id = Guid.NewGuid(),
                    DeliveryId = delivery.Id,
                    ProductName = "Hộp Bánh Trung Thu Trăng Vàng",
                    Quantity = 10,
                    Unit = "hộp"
                }
            ]);

        repository.Setup(r => r.GetPartyContextsAsync(It.IsAny<IReadOnlyCollection<Delivery>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, DeliveryPartyContext>
            {
                [delivery.Id] = new DeliveryPartyContext("ORD-202611-001", null, "Công ty ABC", "Xưởng Bánh Kinh Đô", "Doanh nghiệp ABC")
            });

        var response = await service.GetPublicByTokenAsync("sample-token");

        Assert.NotNull(response);
        Assert.Equal("ORD-202611-001", response.OrderNumber);
        Assert.Equal("BulkOrder", response.OrderType);
        Assert.Equal("Doanh nghiệp ABC", response.RecipientCompanyName);
        Assert.Single(response.Items);
        Assert.Equal("Hộp Bánh Trung Thu Trăng Vàng", response.Items[0].ProductName);
        Assert.Equal(10, response.Items[0].Quantity);

        // Security check: Verify no price or financial fields exposed
        var driverProps = typeof(PublicDriverDeliveryResponse).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var itemProps = typeof(DeliveryItemResponse).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var priceRelatedKeywords = new[] { "Price", "Cost", "Subtotal", "TotalValue", "Amount" };

        foreach (var keyword in priceRelatedKeywords)
        {
            Assert.DoesNotContain(driverProps, p => p.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(itemProps, p => p.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public async Task AssignDriver_GeneratesValidTokenAndShareCode_AndCanBeLookedUp()
    {
        var service = CreateService(out var delivery);
        repository.Setup(r => r.CanManageDeliveryAsync(delivery.Id, SupplierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository.Setup(r => r.GetCustomerUserIdForDeliveryAsync(delivery.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CustomerId);

        var assignRequest = new AssignDeliveryDriverRequest
        {
            DriverName = "Nguyễn Văn Tài",
            DriverPhone = "0912345678",
            VehiclePlate = "29A-12345"
        };

        var assignResult = await service.AssignDriverAsync(SupplierId, delivery.Id, assignRequest);

        Assert.NotNull(assignResult);
        Assert.NotEmpty(assignResult.Token);
        Assert.Equal(8, assignResult.ShareCode.Length);
        Assert.Equal("Nguyễn Văn Tài", assignResult.Driver.DriverName);
        Assert.Equal("0912345678", assignResult.Driver.DriverPhone);
        Assert.Equal("29A-12345", assignResult.Driver.VehiclePlate);

        // Customer notified
        notificationService.Verify(n => n.DispatchNotificationAsync(
            CustomerId,
            "delivery.driver_assigned",
            It.IsAny<string>(),
            It.Is<string>(msg => msg.Contains("Nguyễn Văn Tài") && msg.Contains("0912345678") && msg.Contains("29A-12345")),
            "Delivery",
            delivery.Id,
            null,
            "medium",
            It.IsAny<CancellationToken>()), Times.Once);

        // Look up by share code
        repository.Setup(r => r.GetByShareCodeAsync(assignResult.ShareCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(delivery);

        var codeLookup = await service.GetPublicByShareCodeAsync(assignResult.ShareCode);
        Assert.NotNull(codeLookup);
        Assert.Equal(assignResult.Token, codeLookup.Token);
        Assert.Equal(assignResult.ShareCode, codeLookup.ShareCode);
        Assert.Equal("Nguyễn Văn Tài", codeLookup.DriverName);
    }

    [Fact]
    public async Task ReassignDriver_RevokesOldTokenAndCode_OldLookupReturns404()
    {
        var service = CreateService(out var delivery);
        repository.Setup(r => r.CanManageDeliveryAsync(delivery.Id, SupplierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Initial assignment
        var firstAssign = await service.AssignDriverAsync(SupplierId, delivery.Id, new AssignDeliveryDriverRequest
        {
            DriverName = "Tài xế 1",
            DriverPhone = "0912345678"
        });

        var oldToken = firstAssign.Token;
        var oldCode = firstAssign.ShareCode;

        // Simulate driver claimed
        delivery.DriverClaimedAt = fixedNow;

        // Reassign with new driver
        var secondAssign = await service.AssignDriverAsync(SupplierId, delivery.Id, new AssignDeliveryDriverRequest
        {
            DriverName = "Tài xế 2",
            DriverPhone = "0987654321"
        });

        // DriverClaimedAt must be reset on reassign
        Assert.Null(delivery.DriverClaimedAt);
        Assert.NotEqual(oldToken, secondAssign.Token);
        Assert.NotEqual(oldCode, secondAssign.ShareCode);

        // Old token and code now return 404
        repository.Setup(r => r.GetByShareTokenAsync(oldToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Delivery?)null);
        repository.Setup(r => r.GetByShareCodeAsync(oldCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Delivery?)null);

        var tokenEx = await Assert.ThrowsAsync<HttpException>(() => service.GetPublicByTokenAsync(oldToken));
        Assert.Equal(404, tokenEx.StatusCode);

        var codeEx = await Assert.ThrowsAsync<HttpException>(() => service.GetPublicByShareCodeAsync(oldCode));
        Assert.Equal(404, codeEx.StatusCode);
    }

    [Fact]
    public async Task LookupByShareCode_IsCaseInsensitive()
    {
        var service = CreateService(out var delivery);
        delivery.ShareCode = "ABC23456";

        repository.Setup(r => r.GetByShareCodeAsync("abc23456", It.IsAny<CancellationToken>()))
            .ReturnsAsync(delivery);

        var result = await service.GetPublicByShareCodeAsync("abc23456");
        Assert.NotNull(result);
        Assert.Equal("ABC23456", result.ShareCode);
    }

    [Fact]
    public async Task LookupByShareCode_WhenExpiredOrRevoked_Returns404()
    {
        var service = CreateService(out var delivery);
        delivery.ShareCode = "EXPIRED1";
        delivery.ShareTokenExpiresAtUtc = fixedNow.AddMinutes(-5);

        repository.Setup(r => r.GetByShareCodeAsync("EXPIRED1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(delivery);

        var expiredEx = await Assert.ThrowsAsync<HttpException>(() => service.GetPublicByShareCodeAsync("EXPIRED1"));
        Assert.Equal(404, expiredEx.StatusCode);
        Assert.Equal(DeliveryMessages.ShareLinkExpiredError, expiredEx.Message);

        delivery.ShareTokenExpiresAtUtc = fixedNow.AddHours(5);
        delivery.ShareTokenRevokedAtUtc = fixedNow.AddMinutes(-1);

        var revokedEx = await Assert.ThrowsAsync<HttpException>(() => service.GetPublicByShareCodeAsync("EXPIRED1"));
        Assert.Equal(404, revokedEx.StatusCode);
        Assert.Equal(DeliveryMessages.ShareLinkRevokedError, revokedEx.Message);
    }

    [Fact]
    public async Task AssignDriver_WhenOtherSupplier_Returns403Or404()
    {
        var service = CreateService(out var delivery);
        repository.Setup(r => r.CanManageDeliveryAsync(delivery.Id, OtherSupplierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var forbiddenEx = await Assert.ThrowsAsync<HttpException>(() =>
            service.AssignDriverAsync(OtherSupplierId, delivery.Id, new AssignDeliveryDriverRequest
            {
                DriverName = "Nguyễn Văn Tài",
                DriverPhone = "0912345678"
            }));
        Assert.Equal(403, forbiddenEx.StatusCode);

        var nonExistentId = Guid.NewGuid();
        repository.Setup(r => r.GetByIdAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Delivery?)null);

        var notFoundEx = await Assert.ThrowsAsync<HttpException>(() =>
            service.AssignDriverAsync(SupplierId, nonExistentId, new AssignDeliveryDriverRequest
            {
                DriverName = "Nguyễn Văn Tài",
                DriverPhone = "0912345678"
            }));
        Assert.Equal(404, notFoundEx.StatusCode);
    }

    [Fact]
    public async Task UnassignDriver_ClearsDriverAndRevokesToken()
    {
        var service = CreateService(out var delivery);
        delivery.DriverName = "Nguyễn Văn Tài";
        delivery.DriverPhone = "0912345678";
        delivery.VehiclePlate = "29A-12345";
        delivery.DriverClaimedAt = fixedNow;
        delivery.ShareToken = "active-token";

        repository.Setup(r => r.CanManageDeliveryAsync(delivery.Id, SupplierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await service.RevokeDriverAssignmentAsync(SupplierId, delivery.Id);

        Assert.Null(delivery.DriverName);
        Assert.Null(delivery.DriverPhone);
        Assert.Null(delivery.VehiclePlate);
        Assert.Null(delivery.DriverClaimedAt);
        Assert.Equal(fixedNow, delivery.ShareTokenRevokedAtUtc);
    }

    [Fact]
    public async Task CreateShareLink_WithoutDriver_GeneratesShareCode()
    {
        var service = CreateService(out var delivery);
        repository.Setup(r => r.CanManageDeliveryAsync(delivery.Id, SupplierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var linkResponse = await service.CreateOrReshareLinkAsync(SupplierId, delivery.Id, new CreateDeliveryShareLinkRequest
        {
            ExpiresInHours = 24
        });

        Assert.NotNull(linkResponse);
        Assert.NotEmpty(linkResponse.Token!);
        Assert.NotNull(linkResponse.ShareCode);
        Assert.Equal(8, linkResponse.ShareCode!.Length);
    }

    [Fact]
    public async Task Controller_HttpEndpoints_ExecuteCorrectly()
    {
        var mockService = new Mock<IDeliveryService>();
        var samplePublic = new PublicDriverDeliveryResponse
        {
            DeliveryId = DeliveryId,
            DeliveryNumber = "DEL-202611-001",
            Token = "code-token",
            ShareCode = "CODE1234",
            CanSubmitProof = true,
            RequiresClaim = true
        };

        mockService.Setup(s => s.GetPublicByShareCodeAsync("CODE1234", It.IsAny<CancellationToken>()))
            .ReturnsAsync(samplePublic);

        mockService.Setup(s => s.ClaimTripAsync("sample-token", It.IsAny<ClaimDeliveryTripRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(samplePublic);

        mockService.Setup(s => s.AssignDriverAsync(SupplierId, DeliveryId, It.IsAny<AssignDeliveryDriverRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssignDriverResponse
            {
                Token = "new-token",
                ShareCode = "NEWCODE1",
                ExpiresAt = fixedNow.AddHours(24),
                Driver = new DriverInfoDto { DriverName = "Tài xế Test", DriverPhone = "0912345678" }
            });

        using var client = CreateTestHttpClient(mockService.Object);

        // Test GET public/code/{shareCode}
        var codeRes = await client.GetAsync("/api/v1/deliveries/public/code/CODE1234");
        Assert.Equal(HttpStatusCode.OK, codeRes.StatusCode);
        var json = await codeRes.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var data = json.GetProperty("data");
        Assert.True(data.GetProperty("canSubmitProof").GetBoolean());
        Assert.True(data.GetProperty("requiresClaim").GetBoolean());

        // Test POST public/{token}/claim
        var claimRes = await client.PostAsJsonAsync("/api/v1/deliveries/public/sample-token/claim", new ClaimDeliveryTripRequest
        {
            DriverName = "Tài xế Test",
            DriverPhone = "0912345678",
            VehiclePlate = "29A-12345"
        });
        Assert.Equal(HttpStatusCode.OK, claimRes.StatusCode);

        // Test PUT {id}/driver
        var putRes = await client.PutAsJsonAsync($"/api/v1/deliveries/{DeliveryId}/driver", new AssignDeliveryDriverRequest
        {
            DriverName = "Tài xế Test",
            DriverPhone = "0912345678"
        });
        Assert.Equal(HttpStatusCode.OK, putRes.StatusCode);

        // Test DELETE {id}/driver
        var delRes = await client.DeleteAsync($"/api/v1/deliveries/{DeliveryId}/driver");
        Assert.Equal(HttpStatusCode.OK, delRes.StatusCode);
    }

    private DeliveryService CreateService(out Delivery delivery)
    {
        var localDelivery = new Delivery
        {
            Id = DeliveryId,
            OrderId = OrderId,
            DeliveryNumber = "DEL-202611-001",
            Status = DeliveryStatus.InTransit,
            ShareToken = "sample-token",
            ShareCode = "ABCD2345",
            ShareTokenCreatedAtUtc = fixedNow,
            ShareTokenExpiresAtUtc = fixedNow.AddHours(24),
            CreatedBy = SupplierId,
            CreatedAt = fixedNow
        };
        delivery = localDelivery;

        repository.Setup(r => r.GetByIdAsync(DeliveryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(localDelivery);
        repository.Setup(r => r.GetByShareTokenAsync("sample-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(localDelivery);
        repository.Setup(r => r.UpdateAsync(It.IsAny<Delivery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Delivery d, CancellationToken _) => d);
        repository.Setup(r => r.GetProofsAsync(localDelivery.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        repository.Setup(r => r.GetDeliveryItemsAsync(localDelivery.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        repository.Setup(r => r.GetReferenceCodeAsync(localDelivery, It.IsAny<CancellationToken>()))
            .ReturnsAsync("REF-123");

        return new DeliveryService(repository.Object, clock.Object, null, notificationService.Object);
    }

    private static HttpClient CreateTestHttpClient(IDeliveryService deliveryService)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        builder.Services.AddControllers()
            .AddApplicationPart(typeof(DeliveriesController).Assembly)
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            });

        builder.Services.AddSingleton(deliveryService);
        builder.Services.AddSingleton<ICloudinaryStorageService>(new Mock<ICloudinaryStorageService>().Object);
        builder.Services.AddSingleton<ILogoImageSanitizer>(new LogoImageSanitizer());

        builder.Services.AddRateLimiter(options =>
        {
            options.AddPolicy(DeliveriesController.PublicDriverRatePolicy, context =>
            {
                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var token = context.Request.RouteValues.TryGetValue("token", out var routeVal)
                    ? routeVal?.ToString() ?? string.Empty
                    : string.Empty;
                return RateLimitPartition.GetFixedWindowLimiter(
                    $"{ip}:{token}",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    });
            });
        });

        builder.Services.AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
        builder.Services.AddAuthorization();

        var app = builder.Build();
        app.Use(async (context, next) =>
        {
            var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, SupplierId.ToString()),
                new Claim(ClaimTypes.Role, "Supplier"),
                new Claim("supplier_fulfillment", "true")
            ], "Test");

            context.User = new ClaimsPrincipal(identity);
            await next();
        });

        app.UseAuthentication();
        app.UseRateLimiter();
        app.UseAuthorization();
        app.MapControllers();
        app.StartAsync().GetAwaiter().GetResult();

        return app.GetTestClient();
    }

    private sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthHandler(
            Microsoft.Extensions.Options.IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            System.Text.Encodings.Web.UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, SupplierId.ToString()),
                new Claim(ClaimTypes.Role, "Supplier")
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
