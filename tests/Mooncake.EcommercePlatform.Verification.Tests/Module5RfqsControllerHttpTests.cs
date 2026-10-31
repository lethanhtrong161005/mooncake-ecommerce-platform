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
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.DTOs.Rfqs.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Rfqs.Responses;
using Mooncake.EcommercePlatform.Application.DTOs.Contracts.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.WebApi.Controllers;
using Mooncake.EcommercePlatform.WebApi.Middleware;
using Xunit;

public sealed class Module5RfqsControllerHttpTests
{
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid SupplierId = Guid.NewGuid();
    private static readonly Guid RfqId = Guid.NewGuid();
    private static readonly Guid RfqItemId = Guid.NewGuid();
    private static readonly Guid BidId = Guid.NewGuid();

    private readonly Mock<IRfqService> rfqService = new();

    private static RfqResponse CreateRfq(RfqStatus status = RfqStatus.Draft) => new(
        RfqId,
        "RFQ-2026-TEST01",
        "Gói thầu Quà tặng Trung Thu Doanh Nghiệp 2026",
        "Cung cấp 500 hộp bánh trung thu cao cấp cho đối tác VIP",
        "Tòa nhà Bitexco, Q1, TP. HCM",
        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20)),
        150000000m,
        200000000m,
        DateTime.UtcNow.AddDays(10),
        status,
        [
            new RfqItemResponse(
                RfqItemId,
                "Bánh Trung Thu Thập Cẩm Bát Bửu",
                "Hộp 4 bánh 200g, 2 trứng muối",
                500,
                "Hộp",
                350000m,
                "Hạn sử dụng tối thiểu 45 ngày",
                "Hộp sơn mài dập nhũ vàng",
                true)
        ],
        1,
        status == RfqStatus.Draft ? 0 : 1);

    private static BidResponse CreateBid(BidStatus status = BidStatus.Submitted) => new(
        BidId,
        "BID-2026-001",
        SupplierId,
        165000000m,
        7,
        30,
        DateTime.UtcNow,
        false,
        status,
        [
            new BidItemResponse(
                RfqItemId,
                "Bánh Trung Thu Thập Cẩm Bát Bửu",
                500,
                "Hộp",
                330000m,
                165000000m,
                "Chiết khấu 5% cho đơn thanh toán sớm")
        ]);

    private HttpClient CreateClient(string? role, Guid? explicitUserId = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        builder.Services.AddControllers()
            .AddApplicationPart(typeof(RfqsController).Assembly)
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            });

        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddProblemDetails();
        builder.Services.AddSingleton(rfqService.Object);

        builder.Services.AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
        builder.Services.AddAuthorization();

        var app = builder.Build();
        app.UseExceptionHandler();

        if (role != null)
        {
            var userId = explicitUserId ?? (role switch
            {
                "Customer" => CustomerId,
                "Supplier" => SupplierId,
                _ => CustomerId
            });

            app.Use(async (context, next) =>
            {
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, userId.ToString()),
                    new(ClaimTypes.Role, role),
                };

                context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
                await next();
            });
        }

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.StartAsync().GetAwaiter().GetResult();

        return app.GetTestClient();
    }

    // =========================================================================
    // FEATURE #10: KHỞI TẠO RFQ & VÒNG ĐỜI (RFQ CREATION & LIFECYCLE)
    // =========================================================================

    [Fact]
    public async Task CreateRfq_Customer_Returns201Created()
    {
        var client = CreateClient("Customer");

        rfqService.Setup(s => s.CreateAsync(CustomerId, It.IsAny<CreateRfqRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRfq(RfqStatus.Draft));

        var request = new CreateRfqRequest
        {
            Title = "Gói thầu Quà tặng Trung Thu Doanh Nghiệp 2026",
            DeliveryAddress = "Tòa nhà Bitexco, Q1, TP. HCM",
            DeliveryDateRequired = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20)),
            BidDeadline = DateTime.UtcNow.AddDays(10),
            BudgetRangeMin = 150000000m,
            BudgetRangeMax = 200000000m,
            Items =
            [
                new CreateRfqItemRequest
                {
                    ProductName = "Bánh Trung Thu Thập Cẩm Bát Bửu",
                    Quantity = 500,
                    Unit = "Hộp",
                    TargetPrice = 350000m,
                    NeedsCustomPackaging = true
                }
            ]
        };

        var response = await client.PostAsJsonAsync("/api/v1/rfqs", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("RFQ-2026-TEST01", json.GetProperty("data").GetProperty("rfqNumber").GetString());
    }

    [Fact]
    public async Task CreateRfq_WithoutCustomerRole_ReturnsUnauthorizedOrForbidden()
    {
        var client = CreateClient(role: null);

        var request = new CreateRfqRequest
        {
            Title = "Gói thầu Quà tặng Trung Thu",
            DeliveryAddress = "Bitexco, TP. HCM",
            DeliveryDateRequired = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20)),
            BidDeadline = DateTime.UtcNow.AddDays(10),
            Items = [new CreateRfqItemRequest { ProductName = "Bánh nướng", Quantity = 100, Unit = "Hộp" }]
        };

        var response = await client.PostAsJsonAsync("/api/v1/rfqs", request);
        Assert.True(response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateRfq_SupplierRole_Returns403Forbidden()
    {
        var client = CreateClient("Supplier");

        var request = new CreateRfqRequest
        {
            Title = "Gói thầu Quà tặng Trung Thu",
            DeliveryAddress = "Bitexco, TP. HCM",
            DeliveryDateRequired = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20)),
            BidDeadline = DateTime.UtcNow.AddDays(10),
            Items = [new CreateRfqItemRequest { ProductName = "Bánh nướng", Quantity = 100, Unit = "Hộp" }]
        };

        var response = await client.PostAsJsonAsync("/api/v1/rfqs", request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateRfq_MissingTitleOrItems_Returns400BadRequest()
    {
        var client = CreateClient("Customer");

        var invalid = new CreateRfqRequest
        {
            Title = "", // Required
            DeliveryAddress = "Bitexco, TP. HCM",
            DeliveryDateRequired = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20)),
            BidDeadline = DateTime.UtcNow.AddDays(10),
            Items = [] // MinLength(1)
        };

        var response = await client.PostAsJsonAsync("/api/v1/rfqs", invalid);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetMineRfqs_Customer_Returns200OK()
    {
        var client = CreateClient("Customer");

        rfqService.Setup(s => s.GetMineAsync(CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateRfq(RfqStatus.Open)]);

        var response = await client.GetAsync("/api/v1/rfqs/mine");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, json.GetProperty("data").GetArrayLength());
    }

    [Fact]
    public async Task GetMineRfqs_Supplier_Returns403Forbidden()
    {
        var client = CreateClient("Supplier");

        var response = await client.GetAsync("/api/v1/rfqs/mine");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetSupplierInvitations_Supplier_Returns200OK()
    {
        var client = CreateClient("Supplier");

        rfqService.Setup(s => s.GetSupplierInvitationsAsync(SupplierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateRfq(RfqStatus.Open)]);

        var response = await client.GetAsync("/api/v1/rfqs/supplier");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, json.GetProperty("data").GetArrayLength());
    }

    [Fact]
    public async Task GetSupplierInvitations_Customer_Returns403Forbidden()
    {
        var client = CreateClient("Customer");

        var response = await client.GetAsync("/api/v1/rfqs/supplier");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetRfq_Customer_Returns200OK()
    {
        var client = CreateClient("Customer");

        rfqService.Setup(s => s.GetAsync(CustomerId, false, RfqId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRfq(RfqStatus.Open));

        var response = await client.GetAsync($"/api/v1/rfqs/{RfqId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(RfqId, json.GetProperty("data").GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task GetRfq_Supplier_Returns200OK()
    {
        var client = CreateClient("Supplier");

        rfqService.Setup(s => s.GetAsync(SupplierId, true, RfqId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRfq(RfqStatus.Open));

        var response = await client.GetAsync($"/api/v1/rfqs/{RfqId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(RfqId, json.GetProperty("data").GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task PublishRfq_Customer_Returns200OK()
    {
        var client = CreateClient("Customer");

        rfqService.Setup(s => s.PublishAsync(CustomerId, RfqId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRfq(RfqStatus.Open));

        var response = await client.PostAsync($"/api/v1/rfqs/{RfqId}/publish", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(RfqStatus.Open.ToString(), json.GetProperty("data").GetProperty("status").GetString());
    }

    [Fact]
    public async Task PublishRfq_Supplier_Returns403Forbidden()
    {
        var client = CreateClient("Supplier");

        var response = await client.PostAsync($"/api/v1/rfqs/{RfqId}/publish", null);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CancelRfq_Customer_Returns200OK()
    {
        var client = CreateClient("Customer");

        rfqService.Setup(s => s.CancelAsync(CustomerId, RfqId, "Hủy dự án do thay đổi kế hoạch", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var request = new CancelRfqRequest("Hủy dự án do thay đổi kế hoạch");
        var response = await client.PostAsJsonAsync($"/api/v1/rfqs/{RfqId}/cancel", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("RFQ cancelled successfully.", json.GetProperty("message").GetString());
    }

    // =========================================================================
    // FEATURE #11: MỜI & GỢI Ý NHÀ CUNG CẤP (SUPPLIER INVITATION & MATCHING)
    // =========================================================================

    [Fact]
    public async Task GetSuggestedSuppliers_Customer_Returns200OK()
    {
        var client = CreateClient("Customer");

        rfqService.Setup(s => s.GetSuggestedSuppliersAsync(CustomerId, RfqId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new SuggestedSupplierResponse(
                    SupplierId,
                    Guid.NewGuid(),
                    "Xưởng Bánh Hoàng Triều",
                    95m,
                    4.9m,
                    "Quận Bình Thạnh, TP. HCM")
            ]);

        var response = await client.GetAsync($"/api/v1/rfqs/{RfqId}/suggested-suppliers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, json.GetProperty("data").GetArrayLength());
        Assert.Equal("Xưởng Bánh Hoàng Triều", json.GetProperty("data")[0].GetProperty("companyName").GetString());
    }

    [Fact]
    public async Task InviteSuppliers_Customer_Returns200OK()
    {
        var client = CreateClient("Customer");

        rfqService.Setup(s => s.InviteAsync(CustomerId, RfqId, It.IsAny<InviteSuppliersRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRfq(RfqStatus.Open));

        var request = new InviteSuppliersRequest { SupplierUserIds = [SupplierId] };
        var response = await client.PostAsJsonAsync($"/api/v1/rfqs/{RfqId}/invitations", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(RfqId, json.GetProperty("data").GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task InviteSuppliers_EmptyList_Returns400BadRequest()
    {
        var client = CreateClient("Customer");

        var invalid = new InviteSuppliersRequest { SupplierUserIds = [] }; // MinLength(1)
        var response = await client.PostAsJsonAsync($"/api/v1/rfqs/{RfqId}/invitations", invalid);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeclineInvitation_Supplier_Returns200OK()
    {
        var client = CreateClient("Supplier");

        rfqService.Setup(s => s.DeclineAsync(SupplierId, RfqId, It.IsAny<DeclineInvitationRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var request = new DeclineInvitationRequest { Reason = "Xưởng hiện đã hết công suất sản xuất cho đợt rằm" };
        var response = await client.PostAsJsonAsync($"/api/v1/rfqs/{RfqId}/decline", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("RFQ invitation declined successfully.", json.GetProperty("message").GetString());
    }

    [Fact]
    public async Task DeclineInvitation_ReasonTooShort_Returns400BadRequest()
    {
        var client = CreateClient("Supplier");

        var invalid = new DeclineInvitationRequest { Reason = "No" }; // MinLength(3)
        var response = await client.PostAsJsonAsync($"/api/v1/rfqs/{RfqId}/decline", invalid);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // =========================================================================
    // FEATURE #12: BÁO GIÁ & SO SÁNH THẦU (SUPPLIER BIDDING & COMPARISON)
    // =========================================================================

    [Fact]
    public async Task SubmitBid_Supplier_Returns201Created()
    {
        var client = CreateClient("Supplier");

        rfqService.Setup(s => s.SubmitBidAsync(SupplierId, RfqId, It.IsAny<SubmitBidRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateBid(BidStatus.Submitted));

        var request = new SubmitBidRequest
        {
            EstimatedDeliveryDays = 7,
            ValidityDays = 30,
            Notes = "Chiết khấu 5% cho đơn thanh toán sớm",
            Items =
            [
                new SubmitBidItemRequest
                {
                    RfqItemId = RfqItemId,
                    UnitPrice = 330000m,
                    Notes = "Bánh chất lượng tiêu chuẩn xuất khẩu"
                }
            ]
        };

        var response = await client.PostAsJsonAsync($"/api/v1/rfqs/{RfqId}/bids", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("BID-2026-001", json.GetProperty("data").GetProperty("bidNumber").GetString());
    }

    [Fact]
    public async Task SubmitBid_Customer_Returns403Forbidden()
    {
        var client = CreateClient("Customer");

        var request = new SubmitBidRequest
        {
            EstimatedDeliveryDays = 7,
            ValidityDays = 30,
            Items = [new SubmitBidItemRequest { RfqItemId = RfqItemId, UnitPrice = 330000m }]
        };

        var response = await client.PostAsJsonAsync($"/api/v1/rfqs/{RfqId}/bids", request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CompareBids_Customer_Returns200OK()
    {
        var client = CreateClient("Customer");

        rfqService.Setup(s => s.CompareBidsAsync(CustomerId, RfqId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateBid(BidStatus.Submitted)]);

        var response = await client.GetAsync($"/api/v1/rfqs/{RfqId}/bids");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, json.GetProperty("data").GetArrayLength());
    }

    [Fact]
    public async Task CompareBids_Supplier_Returns403Forbidden()
    {
        var client = CreateClient("Supplier");

        var response = await client.GetAsync($"/api/v1/rfqs/{RfqId}/bids");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CancelBid_Customer_Returns200OK()
    {
        var client = CreateClient("Customer");

        rfqService.Setup(s => s.CancelBidAsync(CustomerId, RfqId, BidId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateBid(BidStatus.Cancelled));

        var response = await client.PostAsync($"/api/v1/rfqs/{RfqId}/bids/{BidId}/cancel", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(BidStatus.Cancelled.ToString(), json.GetProperty("data").GetProperty("status").GetString());
    }

    // =========================================================================
    // FEATURE #13: CHỌN THẦU & SINH DỰ THẢO HỢP ĐỒNG (BID AWARDING & CONTRACT)
    // =========================================================================

    [Fact]
    public async Task AwardBid_Customer_Returns200OK()
    {
        var client = CreateClient("Customer");
        var contractId = Guid.NewGuid();

        var contract = new ContractResponse(
            contractId, "HD-2026-TEST01", RfqId, null, null, null, null,
            ContractStatus.Draft, null, null, null);

        rfqService.Setup(s => s.AwardBidAsync(CustomerId, RfqId, BidId, It.IsAny<AwardBidRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);

        var request = new AwardBidRequest { SelectionNote = "Giá tốt và thời gian giao hàng hợp lý" };
        var response = await client.PostAsJsonAsync($"/api/v1/rfqs/{RfqId}/bids/{BidId}/accept", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(contractId, json.GetProperty("data").GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task AwardBid_Supplier_Returns403Forbidden()
    {
        var client = CreateClient("Supplier");

        var request = new AwardBidRequest { SelectionNote = "Supplier cannot award bid" };
        var response = await client.PostAsJsonAsync($"/api/v1/rfqs/{RfqId}/bids/{BidId}/accept", request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

