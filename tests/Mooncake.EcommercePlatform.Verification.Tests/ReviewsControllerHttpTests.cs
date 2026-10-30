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
using Mooncake.EcommercePlatform.Application.DTOs.Reviews.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Reviews.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.WebApi.Controllers;
using Mooncake.EcommercePlatform.WebApi.Middleware;
using Xunit;

public sealed class ReviewsControllerHttpTests
{
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid SupplierId = Guid.NewGuid();
    private static readonly Guid AdminId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();
    private static readonly Guid ReviewId = Guid.NewGuid();

    private readonly Mock<IReviewService> reviewService = new();

    private HttpClient CreateClient(string role = "Customer")
    {
        var userId = role == "Admin" ? AdminId : role == "Supplier" ? SupplierId : CustomerId;

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        builder.Services.AddControllers()
            .AddApplicationPart(typeof(ReviewsController).Assembly)
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            });

        builder.Services.AddSingleton(reviewService.Object);
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

    [Fact]
    public async Task CreateTransactionReview_Returns201Created()
    {
        var client = CreateClient("Customer");
        var request = new CreateTransactionReviewRequest
        {
            OrderId = Guid.NewGuid(),
            OverallRating = 5,
            Comment = "Bánh rất ngon và giao đúng hẹn!",
            IsAnonymous = false
        };

        reviewService.Setup(s => s.CreateTransactionReviewAsync(CustomerId, It.IsAny<CreateTransactionReviewRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransactionReviewResponse
            {
                Id = ReviewId,
                CustomerId = CustomerId,
                CustomerDisplayName = "Công ty FPT",
                OverallRating = 5,
                ReputationScoreDelta = 3.0m,
                NewSupplierReputationScore = 85.0m,
                CreatedAt = DateTime.UtcNow
            });

        var response = await client.PostAsJsonAsync("/api/v1/reviews/transaction", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(201, json.GetProperty("status").GetInt32());
        Assert.Equal(ReviewId.ToString(), json.GetProperty("data").GetProperty("id").GetString());
        Assert.Equal(5, json.GetProperty("data").GetProperty("overallRating").GetInt16());
    }

    [Fact]
    public async Task GetEligibleOrdersForReview_Returns200WithList()
    {
        var client = CreateClient("Customer");

        reviewService.Setup(s => s.GetEligibleOrdersForReviewAsync(CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new EligibleOrderDto
                {
                    OrderId = Guid.NewGuid(),
                    OrderNumber = "ORD-202610-001",
                    SupplierId = SupplierId,
                    SupplierName = "Xưởng Bánh Tràng An Nghệ Nhân",
                    TotalAmount = 50000000m,
                    DeliveredAt = DateTime.UtcNow
                }
            ]);

        var response = await client.GetAsync("/api/v1/reviews/transaction/eligible-orders");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(200, json.GetProperty("status").GetInt32());
        Assert.Equal(1, json.GetProperty("data").GetArrayLength());
        Assert.Equal("ORD-202610-001", json.GetProperty("data")[0].GetProperty("orderNumber").GetString());
    }

    [Fact]
    public async Task ReplyTransactionReview_Supplier_Returns200()
    {
        var client = CreateClient("Supplier");
        var request = new SupplierReplyRequest
        {
            ReplyContent = "Cảm ơn quý khách đã tin dùng sản phẩm của chúng tôi."
        };

        reviewService.Setup(s => s.ReplyTransactionReviewAsync(SupplierId, ReviewId, It.IsAny<SupplierReplyRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransactionReviewResponse
            {
                Id = ReviewId,
                CustomerId = CustomerId,
                SupplierId = SupplierId,
                SupplierReply = request.ReplyContent,
                SupplierRepliedAt = DateTime.UtcNow
            });

        var response = await client.PostAsJsonAsync($"/api/v1/reviews/transaction/{ReviewId}/reply", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(200, json.GetProperty("status").GetInt32());
        Assert.Equal(request.ReplyContent, json.GetProperty("data").GetProperty("supplierReply").GetString());
    }

    [Fact]
    public async Task ReplyTransactionReview_Customer_Returns403Forbidden()
    {
        var client = CreateClient("Customer");
        var request = new SupplierReplyRequest
        {
            ReplyContent = "Khách hàng không được phép gửi phản hồi chính thức."
        };

        var response = await client.PostAsJsonAsync($"/api/v1/reviews/transaction/{ReviewId}/reply", request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetShopReviews_Returns200WithShopReviewSummary()
    {
        var client = CreateClient("Customer");

        reviewService.Setup(s => s.GetShopReviewsAsync("trang-an-bakery", null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShopReviewSummaryResponse
            {
                SupplierId = SupplierId,
                ShopSlug = "trang-an-bakery",
                AverageRating = 4.9m,
                TotalReviews = 42,
                QualityAverage = 4.9m,
                DeliveryAverage = 4.8m,
                ServiceAverage = 5.0m
            });

        var response = await client.GetAsync("/api/v1/reviews/shops/trang-an-bakery");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(200, json.GetProperty("status").GetInt32());
        Assert.Equal(42, json.GetProperty("data").GetProperty("totalReviews").GetInt32());
    }

    [Fact]
    public async Task GetSupplierReputation_Returns200WithScoreAndBadge()
    {
        var client = CreateClient("Customer");

        reviewService.Setup(s => s.GetSupplierReputationAsync(SupplierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SupplierReputationResponse
            {
                SupplierId = SupplierId,
                CompanyName = "Xưởng Bánh Tràng An",
                ReputationScore = 85.0m,
                BadgeTier = "GoldPartner",
                BadgeTitle = "Đối tác Vàng",
                BadgeColor = "amber"
            });

        var response = await client.GetAsync($"/api/v1/reviews/suppliers/{SupplierId}/reputation");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(200, json.GetProperty("status").GetInt32());
        Assert.Equal("GoldPartner", json.GetProperty("data").GetProperty("badgeTier").GetString());
        Assert.Equal("Đối tác Vàng", json.GetProperty("data").GetProperty("badgeTitle").GetString());
    }

    [Fact]
    public async Task AdminAdjustReputation_Admin_Returns200()
    {
        var client = CreateClient("Admin");
        var request = new AdminAdjustReputationRequest
        {
            EventType = ReputationEvent.AdminBonus,
            ScoreDelta = 5.0m,
            Reason = "Khen thưởng hoàn thành xuất sắc đơn hàng lớn mùa Trung Thu."
        };

        reviewService.Setup(s => s.AdminAdjustReputationAsync(AdminId, SupplierId, It.IsAny<AdminAdjustReputationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SupplierReputationResponse
            {
                SupplierId = SupplierId,
                CompanyName = "Xưởng Bánh Tràng An",
                ReputationScore = 90.0m,
                BadgeTier = "GoldPartner",
                BadgeTitle = "Đối tác Vàng",
                BadgeColor = "amber"
            });

        var response = await client.PostAsJsonAsync($"/api/v1/reviews/admin/suppliers/{SupplierId}/reputation/adjustment", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(200, json.GetProperty("status").GetInt32());
        Assert.Equal(90.0m, json.GetProperty("data").GetProperty("reputationScore").GetDecimal());
    }

    [Fact]
    public async Task AdminAdjustReputation_Customer_Returns403Forbidden()
    {
        var client = CreateClient("Customer");
        var request = new AdminAdjustReputationRequest
        {
            EventType = ReputationEvent.AdminPenalty,
            ScoreDelta = -10.0m,
            Reason = "Vi phạm"
        };

        var response = await client.PostAsJsonAsync($"/api/v1/reviews/admin/suppliers/{SupplierId}/reputation/adjustment", request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ModerateReview_Admin_Returns200()
    {
        var client = CreateClient("Admin");
        var request = new ModerateReviewRequest
        {
            IsHidden = true,
            Reason = "Nội dung chứa từ ngữ vi phạm tiêu chuẩn cộng đồng."
        };

        reviewService.Setup(s => s.ModerateReviewAsync(AdminId, ReviewId, true, It.IsAny<ModerateReviewRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var response = await client.PatchAsJsonAsync($"/api/v1/reviews/admin/{ReviewId}/moderate?isProductReview=true", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(200, json.GetProperty("status").GetInt32());
        Assert.True(json.GetProperty("data").GetProperty("isSuccess").GetBoolean());
    }

    [Fact]
    public async Task ModerateReview_Supplier_Returns403Forbidden()
    {
        var client = CreateClient("Supplier");
        var request = new ModerateReviewRequest
        {
            IsHidden = true,
            Reason = "Nội dung vi phạm"
        };

        var response = await client.PatchAsJsonAsync($"/api/v1/reviews/admin/{ReviewId}/moderate?isProductReview=true", request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetProductReviewsFeed_Returns200WithFeedAndHistogram()
    {
        var client = CreateClient("Customer");

        reviewService.Setup(s => s.GetProductReviewsFeedAsync(ProductId, It.IsAny<Guid?>(), null, null, null, 1, 10, "most_helpful", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductReviewFeedResponse
            {
                ProductId = ProductId,
                ProductName = "Bánh Thập Cẩm Bát Bửu",
                Summary = new ProductReviewSummaryDto
                {
                    AverageRating = 4.8m,
                    TotalReviews = 10,
                    VerifiedPurchaseCount = 10,
                    StarDistribution = new StarDistributionDto { Star5 = 8, Star4 = 2 }
                },
                TotalCount = 10,
                PageNumber = 1,
                PageSize = 10
            });

        var response = await client.GetAsync($"/api/v1/products/{ProductId}/reviews");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(200, json.GetProperty("status").GetInt32());
        Assert.Equal("Bánh Thập Cẩm Bát Bửu", json.GetProperty("data").GetProperty("productName").GetString());
        Assert.Equal(8, json.GetProperty("data").GetProperty("summary").GetProperty("starDistribution").GetProperty("star5").GetInt32());
    }

    [Fact]
    public async Task GetEligibleOrderItems_Returns200WithList()
    {
        var client = CreateClient("Customer");

        reviewService.Setup(s => s.GetEligibleOrderItemsAsync(CustomerId, ProductId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new EligibleOrderItemDto
                {
                    OrderItemId = Guid.NewGuid(),
                    OrderId = Guid.NewGuid(),
                    OrderNumber = "ORD-202610-001",
                    ProductId = ProductId,
                    ProductName = "Bánh Thập Cẩm Bát Bửu",
                    VariantName = "Hộp 4 Bánh 180g",
                    DeliveredAt = DateTime.UtcNow
                }
            ]);

        var response = await client.GetAsync($"/api/v1/products/{ProductId}/eligible-order-items");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(200, json.GetProperty("status").GetInt32());
        Assert.Equal(1, json.GetProperty("data").GetArrayLength());
        Assert.Equal("Hộp 4 Bánh 180g", json.GetProperty("data")[0].GetProperty("variantName").GetString());
    }

    [Fact]
    public async Task CreateProductReview_Returns201Created()
    {
        var client = CreateClient("Customer");
        var request = new CreateProductReviewRequest
        {
            OrderItemId = Guid.NewGuid(),
            Rating = 5,
            Title = "Bánh rất ngon",
            Content = "Bánh nướng đậm vị truyền thống, giao hàng đúng hẹn.",
            MediaUrls = []
        };

        reviewService.Setup(s => s.CreateProductReviewAsync(CustomerId, ProductId, It.IsAny<CreateProductReviewRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductReviewItemDto
            {
                Id = ReviewId,
                AuthorName = "Khách Hàng Thân Thiết",
                IsVerifiedPurchase = true,
                Rating = 5,
                Title = request.Title,
                Content = request.Content,
                CreatedAt = DateTime.UtcNow
            });

        var response = await client.PostAsJsonAsync($"/api/v1/products/{ProductId}/reviews", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(201, json.GetProperty("status").GetInt32());
        Assert.Equal(ReviewId.ToString(), json.GetProperty("data").GetProperty("id").GetString());
        Assert.True(json.GetProperty("data").GetProperty("isVerifiedPurchase").GetBoolean());
    }

    [Fact]
    public async Task ToggleProductReviewLike_Returns200WithToggleState()
    {
        var client = CreateClient("Customer");

        reviewService.Setup(s => s.ToggleProductReviewLikeAsync(CustomerId, ReviewId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ToggleReviewLikeResponse
            {
                ReviewId = ReviewId,
                IsLiked = true,
                LikeCount = 29
            });

        var response = await client.PostAsync($"/api/v1/products/reviews/{ReviewId}/like", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(200, json.GetProperty("status").GetInt32());
        Assert.True(json.GetProperty("data").GetProperty("isLiked").GetBoolean());
        Assert.Equal(29, json.GetProperty("data").GetProperty("likeCount").GetInt32());
    }

    [Fact]
    public async Task ReplyProductReview_Supplier_Returns200()
    {
        var client = CreateClient("Supplier");
        var request = new SupplierReplyRequest
        {
            ReplyContent = "Cảm ơn quý khách đã khen ngợi bánh của chúng tôi!"
        };

        reviewService.Setup(s => s.ReplyProductReviewAsync(SupplierId, ReviewId, It.IsAny<SupplierReplyRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductReviewItemDto
            {
                Id = ReviewId,
                AuthorName = "Khách Hàng",
                Rating = 5,
                Content = "Bánh ngon",
                SupplierReply = request.ReplyContent,
                SupplierRepliedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });

        var response = await client.PostAsJsonAsync($"/api/v1/products/reviews/{ReviewId}/reply", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(200, json.GetProperty("status").GetInt32());
        Assert.Equal(request.ReplyContent, json.GetProperty("data").GetProperty("supplierReply").GetString());
    }

    [Fact]
    public async Task ReplyProductReview_Customer_Returns403Forbidden()
    {
        var client = CreateClient("Customer");
        var request = new SupplierReplyRequest
        {
            ReplyContent = "Phản hồi trái phép."
        };

        var response = await client.PostAsJsonAsync($"/api/v1/products/reviews/{ReviewId}/reply", request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
