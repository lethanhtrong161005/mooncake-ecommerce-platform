namespace Mooncake.EcommercePlatform.Verification.Tests;

using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Mooncake.EcommercePlatform.Application.DTOs.Deliveries.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Deliveries.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.WebApi.Controllers;
using Mooncake.EcommercePlatform.WebApi.Middleware;
using Xunit;

public sealed class DeliveriesControllerHttpTests
{
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid SupplierId = Guid.NewGuid();
    private static readonly Guid AdminId = Guid.NewGuid();
    private static readonly Guid DeliveryId = Guid.NewGuid();
    private static readonly Guid ContractId = Guid.NewGuid();
    private static readonly Guid OrderId = Guid.NewGuid();
    private static readonly Guid ProofId = Guid.NewGuid();

    private readonly Mock<IDeliveryService> deliveryService = new();

    private HttpClient CreateClient(string role = "Supplier")
    {
        var userId = role switch
        {
            "Admin" => AdminId,
            "Customer" => CustomerId,
            _ => SupplierId
        };

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        builder.Services.AddControllers()
            .AddApplicationPart(typeof(DeliveriesController).Assembly)
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            });

        builder.Services.AddSingleton(deliveryService.Object);
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddProblemDetails();

        builder.Services.AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
        builder.Services.AddAuthorization();

        var app = builder.Build();
        app.UseExceptionHandler();
        app.Use(async (context, next) =>
        {
            var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim("supplier_fulfillment", role == "Supplier" ? "true" : "false")
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

    private static DeliveryResponse CreateSampleDelivery() => new()
    {
        Id = DeliveryId,
        ContractId = ContractId,
        OrderId = OrderId,
        DeliveryNumber = "DEL-202610-001",
        DeliveryType = DeliveryType.SelfArranged,
        Status = DeliveryStatus.InTransit,
        ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
        RecipientName = "Bác Nguyễn Văn Hùng (Thủ kho)",
        RecipientPhone = "0912345678",
        FromAddress = "Kho Xưởng Tràng An, Lô B4 Cụm CN Bát Tràng",
        ToAddress = "Tổng kho Vingroup, KCN Quang Minh",
        CreatedAt = DateTime.UtcNow
    };

    private static DeliveryProofResponse CreateSampleProof() => new()
    {
        Id = ProofId,
        DeliveryId = DeliveryId,
        ProofType = ProofType.Delivery,
        PhotoUrl = "https://images.unsplash.com/photo-1549465220-1a8b9238cd48?w=800",
        SignatureUrl = "https://upload.wikimedia.org/wikipedia/commons/f/f8/Signature_sample.svg",
        TakenBy = "Bác Nguyễn Văn Hùng (Thủ kho)",
        TakenAt = DateTime.UtcNow,
        LocationLat = 21.0168m,
        LocationLng = 105.7838m,
        TamperProofHash = new string('f', 64),
        IsVerified = true,
        CreatedAt = DateTime.UtcNow
    };

    // =========================================================================
    // FEATURE #19: QUẢN LÝ ĐƠN VẬN CHUYỂN & GIAO HÀNG (DELIVERY MANAGEMENT)
    // =========================================================================

    [Fact]
    public async Task GetMine_AsSupplier_Returns200WithDeliveryList()
    {
        deliveryService.Setup(s => s.GetMineAsync(SupplierId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateSampleDelivery()]);

        using var client = CreateClient("Supplier");
        var response = await client.GetAsync("/api/v1/deliveries");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("DEL-202610-001", json);
    }

    [Fact]
    public async Task GetMine_AsCustomer_Returns200WithDeliveryList()
    {
        deliveryService.Setup(s => s.GetMineAsync(CustomerId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateSampleDelivery()]);

        using var client = CreateClient("Customer");
        var response = await client.GetAsync("/api/v1/deliveries");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("DEL-202610-001", json);
    }

    [Fact]
    public async Task GetById_Returns200WithDeliveryDetail()
    {
        var detail = new DeliveryDetailResponse
        {
            Id = DeliveryId,
            ContractId = ContractId,
            DeliveryNumber = "DEL-202610-001",
            Status = DeliveryStatus.InTransit,
            RecipientName = "Bác Nguyễn Văn Hùng (Thủ kho)",
            Proofs = [CreateSampleProof()],
            CreatedAt = DateTime.UtcNow
        };

        deliveryService.Setup(s => s.GetAsync(CustomerId, DeliveryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(detail);

        using var client = CreateClient("Customer");
        var response = await client.GetAsync($"/api/v1/deliveries/{DeliveryId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("DEL-202610-001", json);
    }

    [Fact]
    public async Task GetByContract_Returns200WithList()
    {
        deliveryService.Setup(s => s.GetByContractAsync(CustomerId, ContractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateSampleDelivery()]);

        using var client = CreateClient("Customer");
        var response = await client.GetAsync($"/api/v1/deliveries/contract/{ContractId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("DEL-202610-001", json);
    }

    [Fact]
    public async Task GetByOrder_Returns200WithList()
    {
        deliveryService.Setup(s => s.GetByOrderAsync(CustomerId, OrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateSampleDelivery()]);

        using var client = CreateClient("Customer");
        var response = await client.GetAsync($"/api/v1/deliveries/order/{OrderId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("DEL-202610-001", json);
    }

    [Fact]
    public async Task Create_AsSupplier_Returns201Created()
    {
        var request = new CreateDeliveryRequest
        {
            ContractId = ContractId,
            FromAddress = "Kho Xưởng Tràng An, Gia Lâm, Hà Nội",
            ToAddress = "Tổng kho Vingroup, Mê Linh, Hà Nội",
            RecipientName = "Bác Nguyễn Văn Hùng (Thủ kho)",
            RecipientPhone = "0912345678"
        };

        deliveryService.Setup(s => s.CreateAsync(SupplierId, It.IsAny<CreateDeliveryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSampleDelivery());

        using var client = CreateClient("Supplier");
        var response = await client.PostAsJsonAsync("/api/v1/deliveries", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("DEL-202610-001", json);
    }

    [Fact]
    public async Task Create_AsAdmin_Returns201Created()
    {
        var request = new CreateDeliveryRequest
        {
            ContractId = ContractId,
            FromAddress = "Kho Xưởng Tràng An",
            ToAddress = "Tổng kho Vingroup",
            RecipientName = "Thủ kho",
            RecipientPhone = "0912345678"
        };

        deliveryService.Setup(s => s.CreateAsync(AdminId, It.IsAny<CreateDeliveryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSampleDelivery());

        using var client = CreateClient("Admin");
        var response = await client.PostAsJsonAsync("/api/v1/deliveries", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_AsCustomer_Returns403Forbidden()
    {
        var request = new CreateDeliveryRequest
        {
            ContractId = ContractId,
            FromAddress = "Kho",
            ToAddress = "Điểm Giao",
            RecipientName = "Người Nhận",
            RecipientPhone = "0912345678"
        };

        using var client = CreateClient("Customer");
        var response = await client.PostAsJsonAsync("/api/v1/deliveries", request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_Returns200Ok()
    {
        var request = new UpdateDeliveryStatusRequest
        {
            Status = DeliveryStatus.InTransit,
            Notes = "Xe xuất bến"
        };

        deliveryService.Setup(s => s.UpdateStatusAsync(SupplierId, DeliveryId, It.IsAny<UpdateDeliveryStatusRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSampleDelivery());

        using var client = CreateClient("Supplier");
        var response = await client.PutAsJsonAsync($"/api/v1/deliveries/{DeliveryId}/status", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateCarrierInfo_AsSupplier_Returns200Ok()
    {
        var request = new UpdateCarrierInfoRequest
        {
            TrackingCode = "VNPOST-999888",
            RecipientPhone = "0987654321"
        };

        deliveryService.Setup(s => s.UpdateCarrierInfoAsync(SupplierId, DeliveryId, It.IsAny<UpdateCarrierInfoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSampleDelivery());

        using var client = CreateClient("Supplier");
        var response = await client.PutAsJsonAsync($"/api/v1/deliveries/{DeliveryId}/carrier-info", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateCarrierInfo_AsCustomer_Returns403Forbidden()
    {
        var request = new UpdateCarrierInfoRequest { TrackingCode = "VNPOST-999888" };

        using var client = CreateClient("Customer");
        var response = await client.PutAsJsonAsync($"/api/v1/deliveries/{DeliveryId}/carrier-info", request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // =========================================================================
    // FEATURE #20: BẰNG CHỨNG GIAO NHẬN ĐIỆN TỬ (PROOF OF DELIVERY - PoD)
    // =========================================================================

    [Fact]
    public async Task SubmitProof_Returns201Created()
    {
        var request = new CreateDeliveryProofRequest
        {
            ProofType = ProofType.Delivery,
            PhotoUrl = "https://images.unsplash.com/photo-1549465220-1a8b9238cd48?w=800",
            SignatureUrl = "https://upload.wikimedia.org/wikipedia/commons/f/f8/Signature_sample.svg",
            PhotoBase64 = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==",
            SignatureBase64 = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==",
            TakenBy = "Bác Nguyễn Văn Hùng (Thủ kho)",
            LocationLat = 21.0168m,
            LocationLng = 105.7838m,
            Notes = "Đã nhận đủ 1.500 hộp nguyên vẹn."
        };

        deliveryService.Setup(s => s.SubmitProofAsync(SupplierId, DeliveryId, It.IsAny<CreateDeliveryProofRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSampleProof());

        using var client = CreateClient("Supplier");
        var response = await client.PostAsJsonAsync($"/api/v1/deliveries/{DeliveryId}/proofs", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains(ProofId.ToString(), json);
    }

    [Fact]
    public async Task GetProofs_Returns200Ok()
    {
        deliveryService.Setup(s => s.GetProofsAsync(CustomerId, DeliveryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateSampleProof()]);

        using var client = CreateClient("Customer");
        var response = await client.GetAsync($"/api/v1/deliveries/{DeliveryId}/proofs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains(ProofId.ToString(), json);
    }

    [Fact]
    public async Task GetProofDetail_Returns200Ok()
    {
        deliveryService.Setup(s => s.GetProofDetailAsync(CustomerId, ProofId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSampleProof());

        using var client = CreateClient("Customer");
        var response = await client.GetAsync($"/api/v1/deliveries/proofs/{ProofId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains(ProofId.ToString(), json);
    }

    [Fact]
    public async Task VerifyProofIntegrity_Returns200Ok()
    {
        deliveryService.Setup(s => s.VerifyProofIntegrityAsync(ProofId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        using var client = CreateClient("Customer");
        var response = await client.GetAsync($"/api/v1/deliveries/proofs/{ProofId}/verify");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("true", json);
    }
}
