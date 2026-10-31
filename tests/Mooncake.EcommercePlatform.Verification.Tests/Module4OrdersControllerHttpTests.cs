namespace Mooncake.EcommercePlatform.Verification.Tests;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.DTOs.Orders.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Orders.Responses;
using Mooncake.EcommercePlatform.Application.DTOs.Packaging.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Packaging.Responses;
using Mooncake.EcommercePlatform.Application.DTOs.Promotions.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Promotions.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.WebApi.Controllers;
using Mooncake.EcommercePlatform.WebApi.Middleware;
using Xunit;

public sealed class Module4OrdersControllerHttpTests
{
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid SupplierId = Guid.NewGuid();
    private static readonly Guid AdminId = Guid.NewGuid();
    private static readonly Guid ShopId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();
    private static readonly Guid VariantId = Guid.NewGuid();
    private static readonly Guid OrderId = Guid.NewGuid();
    private static readonly Guid PackagingId = Guid.NewGuid();
    private static readonly Guid RevisionId = Guid.NewGuid();

    private readonly Mock<IOrderService> orderService = new();
    private readonly Mock<IPromotionService> promotionService = new();
    private readonly Mock<ICustomPackagingService> packagingService = new();

    private static OrderResponse CreateOrder(OrderStatus status) => new(
        OrderId,
        "MC-20261029-TEST01",
        CustomerId,
        ShopId,
        status,
        13000000m,
        1300000m,
        0m,
        0m,
        0m,
        11700000m,
        "Tòa nhà Landmark 81, TP. HCM",
        "Nguyễn Văn Doanh Nghiệp",
        "0901234567",
        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
        "Giao giờ hành chính",
        DateTime.UtcNow,
        [
            new OrderItemResponse(
                ProductId,
                VariantId,
                "Bánh Trung Thu Hoàng Kim",
                "Thập cẩm gà quay 2 trứng (200g)",
                "SKU-TC-200",
                100,
                130000m,
                13000m,
                11700000m)
        ],
        new CustomPackagingSummaryResponse(
            PackagingId,
            "Công ty Công nghệ Việt",
            "https://example.com/logo.svg",
            "https://example.com/mockup.png",
            "lacquer-4",
            CustomPackagingStatus.Pending,
            1));

    private static OrderQuotePreviewResponse CreateQuoteResponse() => new(
        13000000m,
        1300000m,
        11700000m,
        10m,
        [
            new OrderQuoteLineResponse(
                ProductId,
                VariantId,
                "Bánh Trung Thu Hoàng Kim",
                "Thập cẩm gà quay 2 trứng (200g)",
                "SKU-TC-200",
                100,
                130000m,
                117000m,
                13000000m,
                1300000m,
                11700000m,
                new AppliedPromotionResponse(
                    Guid.NewGuid(),
                    "Chiết khấu doanh nghiệp 10%",
                    DiscountType.Percentage,
                    10m))
        ]);

    private static CustomPackagingResponse CreatePackaging(CustomPackagingStatus status) => new(
        PackagingId,
        OrderId,
        "Công ty Công nghệ Việt",
        "https://example.com/logo.svg",
        "https://example.com/mockup.png",
        "lacquer-4",
        "{\"primary\":\"#8c1524\"}",
        "Kính chúc vạn sự như ý",
        "Ép kim nhũ vàng",
        status,
        DateTime.UtcNow,
        [
            new CustomPackagingRevisionResponse(
                RevisionId,
                1,
                "https://example.com/mockup-v1.png",
                "Phối cảnh hộp sơn mài 4 bánh ép kim",
                null,
                500000m,
                status,
                DateTime.UtcNow,
                null)
        ]);

    private HttpClient CreateClient(string? role, Guid? explicitUserId = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        builder.Services.AddControllers()
            .AddApplicationPart(typeof(OrdersController).Assembly)
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            });

        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddProblemDetails();
        builder.Services.AddSingleton(orderService.Object);
        builder.Services.AddSingleton(promotionService.Object);
        builder.Services.AddSingleton(packagingService.Object);

        builder.Services.AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("SupplierExistingFulfillment", policy =>
                policy.RequireAuthenticatedUser().RequireAssertion(ctx =>
                    ctx.User.IsInRole("Supplier") || ctx.User.HasClaim("supplier_fulfillment", "true")));
        });

        var app = builder.Build();
        app.UseExceptionHandler();

        if (role != null)
        {
            var userId = explicitUserId ?? (role switch
            {
                "Customer" => CustomerId,
                "Supplier" => SupplierId,
                "Admin" => AdminId,
                _ => CustomerId
            });

            app.Use(async (context, next) =>
            {
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, userId.ToString()),
                    new(ClaimTypes.Role, role),
                };

                if (role == "Supplier")
                {
                    claims.Add(new Claim("supplier_fulfillment", "true"));
                }

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
    // FEATURE #7: ĐẶT HÀNG SỐ LƯỢNG LỚN (BULK ORDERS & LIFECYCLE)
    // =========================================================================

    [Fact]
    public async Task QuotePreview_AllowsAnonymous_Returns200OK()
    {
        var client = CreateClient(role: null);

        orderService.Setup(s => s.PreviewQuoteAsync(It.IsAny<OrderQuotePreviewRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateQuoteResponse());

        var request = new OrderQuotePreviewRequest
        {
            ShopId = ShopId,
            Items = [new CreateOrderItemRequest { ProductId = ProductId, VariantId = VariantId, Quantity = 100 }]
        };

        var response = await client.PostAsJsonAsync("/api/v1/orders/quote-preview", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(11700000m, json.GetProperty("data").GetProperty("finalTotal").GetDecimal());
    }

    [Fact]
    public async Task CreateOrder_Customer_Returns201Created()
    {
        var client = CreateClient("Customer");
        var key = Guid.NewGuid().ToString();
        client.DefaultRequestHeaders.Add("Idempotency-Key", key);

        orderService.Setup(s => s.CreateAsync(CustomerId, key, It.IsAny<CreateOrderRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateOrder(OrderStatus.Pending));

        var request = new CreateOrderRequest
        {
            ShopId = ShopId,
            Items = [new CreateOrderItemRequest { ProductId = ProductId, VariantId = VariantId, Quantity = 100 }],
            DeliveryAddress = "Tòa nhà Landmark 81, TP. HCM",
            RecipientName = "Nguyễn Văn Doanh Nghiệp",
            RecipientPhone = "0901234567"
        };

        var response = await client.PostAsJsonAsync("/api/v1/orders", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("MC-20261029-TEST01", json.GetProperty("data").GetProperty("orderNumber").GetString());
    }

    [Fact]
    public async Task CreateOrder_WithoutCustomerRole_ReturnsUnauthorizedOrForbidden()
    {
        var client = CreateClient(role: null);

        var request = new CreateOrderRequest
        {
            ShopId = ShopId,
            Items = [new CreateOrderItemRequest { ProductId = ProductId, VariantId = VariantId, Quantity = 100 }],
            DeliveryAddress = "Tòa nhà Landmark 81, TP. HCM",
            RecipientName = "Nguyễn Văn Doanh Nghiệp",
            RecipientPhone = "0901234567"
        };

        var response = await client.PostAsJsonAsync("/api/v1/orders", request);
        Assert.True(response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateOrder_SupplierRole_Returns403Forbidden()
    {
        var client = CreateClient("Supplier");

        var request = new CreateOrderRequest
        {
            ShopId = ShopId,
            Items = [new CreateOrderItemRequest { ProductId = ProductId, VariantId = VariantId, Quantity = 100 }],
            DeliveryAddress = "Tòa nhà Landmark 81, TP. HCM",
            RecipientName = "Nguyễn Văn Doanh Nghiệp",
            RecipientPhone = "0901234567"
        };

        var response = await client.PostAsJsonAsync("/api/v1/orders", request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_InvalidModel_Returns400BadRequest()
    {
        var client = CreateClient("Customer");

        // Missing items and recipient name
        var invalid = new CreateOrderRequest
        {
            ShopId = ShopId,
            Items = [],
            DeliveryAddress = "Tòa nhà Landmark 81, TP. HCM",
            RecipientName = "",
            RecipientPhone = "0901234567"
        };

        var response = await client.PostAsJsonAsync("/api/v1/orders", invalid);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetCustomerOrders_Customer_Returns200OK()
    {
        var client = CreateClient("Customer");

        orderService.Setup(s => s.GetMyOrdersAsync(CustomerId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateOrder(OrderStatus.Pending)]);

        var response = await client.GetAsync("/api/v1/orders/mine");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, json.GetProperty("data").GetArrayLength());
    }

    [Fact]
    public async Task GetSupplierOrders_Supplier_Returns200OK()
    {
        var client = CreateClient("Supplier");

        orderService.Setup(s => s.GetMyOrdersAsync(SupplierId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateOrder(OrderStatus.Pending)]);

        var response = await client.GetAsync("/api/v1/orders/supplier");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, json.GetProperty("data").GetArrayLength());
    }

    [Fact]
    public async Task GetSupplierOrders_Customer_Returns403Forbidden()
    {
        var client = CreateClient("Customer");

        var response = await client.GetAsync("/api/v1/orders/supplier");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetOrder_Customer_Returns200OK()
    {
        var client = CreateClient("Customer");

        orderService.Setup(s => s.GetAsync(CustomerId, false, OrderId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateOrder(OrderStatus.Pending));

        var response = await client.GetAsync($"/api/v1/orders/{OrderId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(OrderId, json.GetProperty("data").GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task GetOrder_Supplier_Returns200OK()
    {
        var client = CreateClient("Supplier");

        orderService.Setup(s => s.GetAsync(SupplierId, true, OrderId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateOrder(OrderStatus.Confirmed));

        var response = await client.GetAsync($"/api/v1/orders/{OrderId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(OrderStatus.Confirmed.ToString(), json.GetProperty("data").GetProperty("status").GetString());
    }

    [Fact]
    public async Task GetOrder_Admin_Returns200OK()
    {
        var client = CreateClient("Admin");

        orderService.Setup(s => s.GetAsync(AdminId, false, OrderId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateOrder(OrderStatus.Processing));

        var response = await client.GetAsync($"/api/v1/orders/{OrderId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(OrderStatus.Processing.ToString(), json.GetProperty("data").GetProperty("status").GetString());
    }

    [Fact]
    public async Task CancelOrder_Customer_Returns200OK()
    {
        var client = CreateClient("Customer");

        orderService.Setup(s => s.CancelAsync(CustomerId, OrderId, It.IsAny<CancelOrderRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateOrder(OrderStatus.Cancelled));

        var request = new CancelOrderRequest { Reason = "Thay đổi kế hoạch tổ chức tiệc Trung Thu" };
        var response = await client.PostAsJsonAsync($"/api/v1/orders/{OrderId}/cancel", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(OrderStatus.Cancelled.ToString(), json.GetProperty("data").GetProperty("status").GetString());
    }

    [Fact]
    public async Task CancelOrder_MissingReason_Returns400BadRequest()
    {
        var client = CreateClient("Customer");

        var invalid = new CancelOrderRequest { Reason = "AB" }; // MinLength 3
        var response = await client.PostAsJsonAsync($"/api/v1/orders/{OrderId}/cancel", invalid);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ConfirmOrder_Supplier_Returns200OK()
    {
        var client = CreateClient("Supplier");

        orderService.Setup(s => s.ConfirmAsync(SupplierId, OrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateOrder(OrderStatus.Confirmed));

        var response = await client.PostAsync($"/api/v1/orders/{OrderId}/confirm", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(OrderStatus.Confirmed.ToString(), json.GetProperty("data").GetProperty("status").GetString());
    }

    [Fact]
    public async Task ConfirmOrder_Customer_Returns403Forbidden()
    {
        var client = CreateClient("Customer");

        var response = await client.PostAsync($"/api/v1/orders/{OrderId}/confirm", null);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RejectOrder_Supplier_Returns200OK()
    {
        var client = CreateClient("Supplier");

        orderService.Setup(s => s.RejectAsync(SupplierId, OrderId, It.IsAny<CancelOrderRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateOrder(OrderStatus.Cancelled));

        var request = new CancelOrderRequest { Reason = "Xưởng đã kín lịch sản xuất cho đợt này" };
        var response = await client.PostAsJsonAsync($"/api/v1/orders/{OrderId}/reject", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(OrderStatus.Cancelled.ToString(), json.GetProperty("data").GetProperty("status").GetString());
    }

    [Fact]
    public async Task StartProcessing_Supplier_Returns200OK()
    {
        var client = CreateClient("Supplier");

        orderService.Setup(s => s.StartProcessingAsync(SupplierId, OrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateOrder(OrderStatus.Processing));

        var response = await client.PostAsync($"/api/v1/orders/{OrderId}/processing", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(OrderStatus.Processing.ToString(), json.GetProperty("data").GetProperty("status").GetString());
    }

    [Fact]
    public async Task MarkShipped_Supplier_Returns200OK()
    {
        var client = CreateClient("Supplier");

        orderService.Setup(s => s.MarkShippedAsync(SupplierId, OrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateOrder(OrderStatus.Shipped));

        var response = await client.PostAsync($"/api/v1/orders/{OrderId}/ship", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(OrderStatus.Shipped.ToString(), json.GetProperty("data").GetProperty("status").GetString());
    }

    [Fact]
    public async Task ConfirmDelivery_Customer_Returns200OK()
    {
        var client = CreateClient("Customer");

        orderService.Setup(s => s.ConfirmDeliveryAsync(CustomerId, OrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateOrder(OrderStatus.Delivered));

        var response = await client.PostAsync($"/api/v1/orders/{OrderId}/delivery/confirm", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(OrderStatus.Delivered.ToString(), json.GetProperty("data").GetProperty("status").GetString());
    }

    // =========================================================================
    // FEATURE #8: KHUYẾN MÃI CHIẾT KHẤU BẬC THANG (VOLUME PROMOTIONS)
    // =========================================================================

    [Fact]
    public async Task GetMinePromotions_Supplier_Returns200OK()
    {
        var client = CreateClient("Supplier");
        var promotionId = Guid.NewGuid();

        promotionService.Setup(s => s.GetMineAsync(SupplierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new PromotionResponse(
                    promotionId,
                    ShopId,
                    ProductId,
                    "Chiết khấu doanh nghiệp số lượng lớn",
                    "Giảm 10% khi đặt từ 100 hộp",
                    100,
                    500,
                    DiscountType.Percentage,
                    10m,
                    DateTime.UtcNow.AddDays(-1),
                    DateTime.UtcNow.AddMonths(1),
                    true)
            ]);

        var response = await client.GetAsync("/api/v1/supplier/promotions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, json.GetProperty("data").GetArrayLength());
    }

    [Fact]
    public async Task GetMinePromotions_Customer_Returns403Forbidden()
    {
        var client = CreateClient("Customer");

        var response = await client.GetAsync("/api/v1/supplier/promotions");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreatePromotion_Supplier_Returns201Created()
    {
        var client = CreateClient("Supplier");
        var promotionId = Guid.NewGuid();

        promotionService.Setup(s => s.CreateAsync(SupplierId, It.IsAny<SavePromotionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PromotionResponse(
                promotionId,
                ShopId,
                ProductId,
                "Chiết khấu VIP",
                null,
                50,
                null,
                DiscountType.Percentage,
                15m,
                null,
                null,
                true));

        var request = new SavePromotionRequest
        {
            Name = "Chiết khấu VIP",
            ProductId = ProductId,
            MinQty = 50,
            DiscountType = DiscountType.Percentage,
            DiscountValue = 15m
        };

        var response = await client.PostAsJsonAsync("/api/v1/supplier/promotions", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(promotionId, json.GetProperty("data").GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task UpdatePromotion_Supplier_Returns200OK()
    {
        var client = CreateClient("Supplier");
        var promotionId = Guid.NewGuid();

        promotionService.Setup(s => s.UpdateAsync(SupplierId, promotionId, It.IsAny<SavePromotionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PromotionResponse(
                promotionId,
                ShopId,
                ProductId,
                "Chiết khấu VIP cập nhật",
                null,
                60,
                null,
                DiscountType.Percentage,
                20m,
                null,
                null,
                true));

        var request = new SavePromotionRequest
        {
            Name = "Chiết khấu VIP cập nhật",
            ProductId = ProductId,
            MinQty = 60,
            DiscountType = DiscountType.Percentage,
            DiscountValue = 20m
        };

        var response = await client.PutAsJsonAsync($"/api/v1/supplier/promotions/{promotionId}", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(20m, json.GetProperty("data").GetProperty("discountValue").GetDecimal());
    }

    [Fact]
    public async Task DeactivatePromotion_Supplier_Returns200OK()
    {
        var client = CreateClient("Supplier");
        var promotionId = Guid.NewGuid();

        promotionService.Setup(s => s.DeactivateAsync(SupplierId, promotionId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var response = await client.DeleteAsync($"/api/v1/supplier/promotions/{promotionId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Promotion deactivated successfully.", json.GetProperty("message").GetString());
    }

    // =========================================================================
    // FEATURE #9: ĐÓNG GÓI & THIẾT KẾ IN LOGO (CUSTOM PACKAGING)
    // =========================================================================

    [Fact]
    public async Task CreatePackaging_Customer_Returns201Created()
    {
        var client = CreateClient("Customer");

        packagingService.Setup(s => s.CreateAsync(CustomerId, OrderId, It.IsAny<CreateCustomPackagingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePackaging(CustomPackagingStatus.Pending));

        var request = new CreateCustomPackagingRequest
        {
            CompanyName = "Công ty Công nghệ Việt",
            CompanyLogoUrl = "https://example.com/logo.svg",
            PackagingType = "lacquer-4",
            ColorScheme = "{\"primary\":\"#8c1524\"}",
            SpecialMessage = "Kính chúc vạn sự như ý"
        };

        var response = await client.PostAsJsonAsync($"/api/v1/orders/{OrderId}/custom-packaging", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Công ty Công nghệ Việt", json.GetProperty("data").GetProperty("companyName").GetString());
    }

    [Fact]
    public async Task CreatePackaging_SupplierRole_Returns403Forbidden()
    {
        var client = CreateClient("Supplier");

        var request = new CreateCustomPackagingRequest
        {
            CompanyName = "Công ty Công nghệ Việt",
            CompanyLogoUrl = "https://example.com/logo.svg",
            PackagingType = "lacquer-4"
        };

        var response = await client.PostAsJsonAsync($"/api/v1/orders/{OrderId}/custom-packaging", request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetPackaging_Customer_Returns200OK()
    {
        var client = CreateClient("Customer");

        packagingService.Setup(s => s.GetAsync(CustomerId, false, OrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePackaging(CustomPackagingStatus.DesignReview));

        var response = await client.GetAsync($"/api/v1/orders/{OrderId}/custom-packaging");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(PackagingId, json.GetProperty("data").GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task SubmitRevision_Supplier_Returns201Created()
    {
        var client = CreateClient("Supplier");

        packagingService.Setup(s => s.SubmitRevisionAsync(SupplierId, OrderId, It.IsAny<SubmitPackagingRevisionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePackaging(CustomPackagingStatus.DesignReview));

        var request = new SubmitPackagingRevisionRequest
        {
            MockupUrl = "https://example.com/mockup-v2.png",
            SupplierNotes = "Bản phối cảnh mockup v2 điều chỉnh kích thước logo",
            DesignFee = 500000m
        };

        var response = await client.PostAsJsonAsync($"/api/v1/orders/{OrderId}/custom-packaging/revisions", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(CustomPackagingStatus.DesignReview.ToString(), json.GetProperty("data").GetProperty("status").GetString());
    }

    [Fact]
    public async Task ReviewRevision_Customer_Returns200OK()
    {
        var client = CreateClient("Customer");

        packagingService.Setup(s => s.ReviewRevisionAsync(CustomerId, OrderId, RevisionId, It.IsAny<ReviewPackagingRevisionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PackagingReviewResponse(
                CustomPackagingStatus.Approved,
                "https://example.com/mockup-approved.png",
                DateTime.UtcNow));

        var request = new ReviewPackagingRevisionRequest
        {
            Action = "Approve",
            Notes = "Đồng ý bản thiết kế phối cảnh v2"
        };

        var response = await client.PostAsJsonAsync($"/api/v1/orders/{OrderId}/custom-packaging/revisions/{RevisionId}/review", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(CustomPackagingStatus.Approved.ToString(), json.GetProperty("data").GetProperty("packagingStatus").GetString());
    }
}
