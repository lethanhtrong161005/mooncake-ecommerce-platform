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
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.WebApi.Controllers;
using Mooncake.EcommercePlatform.WebApi.Middleware;
using Xunit;

public sealed class Module2ShopAndTemplatesControllerHttpTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ShopId = Guid.NewGuid();
    private static readonly Guid SupplierId = Guid.NewGuid();
    private static readonly Guid TemplateId = Guid.NewGuid();
    private static readonly Guid TemplateRevisionId = Guid.NewGuid();
    private static readonly Guid MediaAssetId = Guid.NewGuid();

    private readonly Mock<IShopService> shopService = new();

    private static readonly ThemeOverridesRequest DefaultTokens = new()
    {
        PrimaryColor = "#8c1524",
        BackgroundColor = "#faf8f6",
        TextColor = "#222222",
        SurfaceColor = "#ffffff",
        ButtonTextColor = "#ffffff",
        BorderRadiusPx = 12,
        FontScale = 1.0m
    };

    private static readonly TemplateLayoutRequest DefaultLayout = new()
    {
        SectionOrder = ["Hero", "Products", "Reviews"],
        ProductColumnsDesktop = 3
    };

    private static ShopAppearanceResponse CreateAppearance(long version = 1) => new(
        TemplateId,
        TemplateRevisionId,
        "heritage",
        "Heritage Classic",
        "heritage",
        1,
        1,
        true,
        false,
        DefaultTokens,
        new ThemeOverridesRequest(),
        DefaultTokens,
        DefaultLayout,
        version);

    private static OwnerShopResponse CreateOwnerShop(long version = 1, ShopStatus status = ShopStatus.Active) => new(
        ShopId,
        SupplierId,
        version,
        "An Nhiên Bakery",
        "an-nhien-bakery",
        "Bánh trung thu nghệ nhân gia truyền",
        status,
        MediaAssetId,
        null,
        "https://example.com/logo.png",
        null,
        "contact@annhien.vn",
        "+84901234567",
        "Hà Nội",
        "HN",
        null,
        CreateAppearance(version),
        ["Update", "SetVisibility", "UpdateAppearance"]);

    private HttpClient CreateClient(string role = "Supplier", bool authenticated = true)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        builder.Services.AddControllers()
            .AddApplicationPart(typeof(ShopsController).Assembly)
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            });

        builder.Services.AddSingleton(shopService.Object);
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddProblemDetails();

        builder.Services.AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
        builder.Services.AddAuthorization();

        var app = builder.Build();
        app.UseExceptionHandler();

        if (authenticated)
        {
            app.Use(async (context, next) =>
            {
                var identity = new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, UserId.ToString()),
                    new Claim(ClaimTypes.Role, role),
                ], "Test");

                context.User = new ClaimsPrincipal(identity);
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
    // FEATURE #3: TẠO & QUẢN LÝ GIAN HÀNG (SHOP MANAGEMENT)
    // =========================================================================

    [Fact]
    public async Task GetMine_Returns200OK_WithShopDetails()
    {
        var client = CreateClient("Supplier");
        var expectedShop = CreateOwnerShop();

        shopService.Setup(s => s.GetMineAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedShop);

        var response = await client.GetAsync("/api/v1/shops/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("An Nhiên Bakery", json.GetProperty("data").GetProperty("name").GetString());
        Assert.Equal("an-nhien-bakery", json.GetProperty("data").GetProperty("slug").GetString());
    }

    [Fact]
    public async Task CreateShop_Returns201Created_OnValidRequest()
    {
        var client = CreateClient("Supplier");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var request = new CreateShopRequest
        {
            Name = "An Nhiên Bakery",
            Slug = "an-nhien-bakery",
            TemplateId = TemplateId,
            TemplateRevisionId = TemplateRevisionId,
            Description = "Bánh trung thu nghệ nhân gia truyền"
        };

        var createdShop = CreateOwnerShop(1);
        shopService.Setup(s => s.CreateAsync(UserId, It.IsAny<string>(), It.IsAny<CreateShopRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdShop);

        var response = await client.PostAsJsonAsync("/api/v1/shops/me", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(ShopId.ToString(), json.GetProperty("data").GetProperty("shopId").GetString());
        Assert.Equal("an-nhien-bakery", json.GetProperty("data").GetProperty("slug").GetString());
    }

    [Fact]
    public async Task CreateShop_Returns409Conflict_WhenSlugAlreadyTaken()
    {
        var client = CreateClient("Supplier");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var request = new CreateShopRequest
        {
            Name = "An Nhiên Bakery",
            Slug = "an-nhien-bakery",
            TemplateId = TemplateId,
            TemplateRevisionId = TemplateRevisionId
        };

        shopService.Setup(s => s.CreateAsync(UserId, It.IsAny<string>(), It.IsAny<CreateShopRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpException(409, "Slug is already registered."));

        var response = await client.PostAsJsonAsync("/api/v1/shops/me", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task UpdateShop_Returns200OK_OnSuccess()
    {
        var client = CreateClient("Supplier");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var request = new UpdateShopRequest
        {
            ExpectedVersion = 1,
            Name = "An Nhiên Bakery Cổ Truyền",
            Description = "Cập nhật mô tả mới"
        };

        var updatedShop = CreateOwnerShop(2);
        shopService.Setup(s => s.UpdateAsync(UserId, It.IsAny<string>(), It.IsAny<UpdateShopRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedShop);

        var response = await client.PutAsJsonAsync("/api/v1/shops/me", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, json.GetProperty("data").GetProperty("version").GetInt64());
    }

    [Fact]
    public async Task SetVisibility_Returns200OK_WhenTogglingStatus()
    {
        var client = CreateClient("Supplier");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var request = new SetMyShopVisibilityRequest
        {
            ExpectedVersion = 2,
            Status = ShopStatus.Inactive
        };

        var inactiveShop = CreateOwnerShop(3, ShopStatus.Inactive);
        shopService.Setup(s => s.VisibilityAsync(UserId, It.IsAny<string>(), It.IsAny<SetMyShopVisibilityRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(inactiveShop);

        var response = await client.PatchAsJsonAsync("/api/v1/shops/me/visibility", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Inactive", json.GetProperty("data").GetProperty("status").GetString());
    }

    [Fact]
    public async Task UploadMedia_Returns201Created_OnLogoUpload()
    {
        var client = CreateClient("Supplier");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("fake-png-content"));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
        content.Add(fileContent, "File", "logo.png");
        content.Add(new StringContent("Logo"), "Purpose");

        var mediaResponse = new ShopMediaResponse(
            MediaAssetId, "Logo", "Ready", "https://example.com/logo.png", "image/png", 16, 512, 512);

        shopService.Setup(s => s.UploadAsync(UserId, false, It.IsAny<string>(), "Logo", It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaResponse);

        var response = await client.PostAsync("/api/v1/shops/me/media", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(MediaAssetId.ToString(), json.GetProperty("data").GetProperty("mediaAssetId").GetString());
        Assert.Equal("Logo", json.GetProperty("data").GetProperty("purpose").GetString());
    }

    [Fact]
    public async Task Preview_Returns200OK_ForOwner()
    {
        var client = CreateClient("Supplier");

        var preview = new OwnerShopPreviewResponse(
            CreateOwnerShop(),
            [new ShopProductCardResponse(Guid.NewGuid(), "Bánh Thập Cẩm", Guid.NewGuid(), "https://example.com/p1.png", 85000, 120000, true, 10, DateTime.UtcNow)]);

        shopService.Setup(s => s.PreviewAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preview);

        var response = await client.GetAsync("/api/v1/shops/me/preview");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, json.GetProperty("data").GetProperty("products").GetArrayLength());
    }

    [Fact]
    public async Task PublicShop_Returns200OK_ForAnonymousUser()
    {
        var client = CreateClient(authenticated: false);

        var publicShop = new PublicShopResponse(
            ShopId, "An Nhiên Bakery", "an-nhien-bakery", "Bánh trung thu nghệ nhân gia truyền",
            "https://example.com/logo.png", null, "contact@annhien.vn", "+84901234567", "Hà Nội", "HN",
            true, 92.5m, 14, 4.8m, CreateAppearance());

        shopService.Setup(s => s.PublicAsync("an-nhien-bakery", It.IsAny<CancellationToken>()))
            .ReturnsAsync(publicShop);

        var response = await client.GetAsync("/api/v1/shops/an-nhien-bakery");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("an-nhien-bakery", json.GetProperty("data").GetProperty("slug").GetString());
        Assert.True(json.GetProperty("data").GetProperty("isVerified").GetBoolean());
    }

    [Fact]
    public async Task PublicShop_Returns404NotFound_WhenSlugDoesNotExist()
    {
        var client = CreateClient(authenticated: false);

        shopService.Setup(s => s.PublicAsync("non-existent-shop", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpException(404, "Shop not found."));

        var response = await client.GetAsync("/api/v1/shops/non-existent-shop");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PublicProducts_Returns200OK_WithProductCards()
    {
        var client = CreateClient(authenticated: false);

        var productList = new List<ShopProductCardResponse>
        {
            new(Guid.NewGuid(), "Bánh Dẻo Hạt Sen", Guid.NewGuid(), "https://example.com/p2.png", 65000, 95000, true, 20, DateTime.UtcNow)
        };
        var cursorPage = new ShopCursorPageResponse<ShopProductCardResponse>(productList, false, null);

        shopService.Setup(s => s.ProductsAsync("an-nhien-bakery", It.IsAny<ShopProductQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cursorPage);

        var response = await client.GetAsync("/api/v1/shops/an-nhien-bakery/products?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, json.GetProperty("data").GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task PublicCategories_Returns200OK()
    {
        var client = CreateClient(authenticated: false);

        var categories = new List<ShopCategoryResponse>
        {
            new(Guid.NewGuid(), "Bánh Nướng Truyền Thống"),
            new(Guid.NewGuid(), "Hộp Quà Doanh Nghiệp VIP")
        };
        var cursorPage = new ShopCursorPageResponse<ShopCategoryResponse>(categories, false, null);

        shopService.Setup(s => s.CategoriesAsync("an-nhien-bakery", It.IsAny<ShopPageQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cursorPage);

        var response = await client.GetAsync("/api/v1/shops/an-nhien-bakery/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, json.GetProperty("data").GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task PublicReviews_Returns200OK()
    {
        var client = CreateClient(authenticated: false);

        var reviews = new List<ShopReviewResponse>
        {
            new(Guid.NewGuid(), 5, "Bánh rất ngon, đóng gói đẹp mắt", DateTime.UtcNow)
        };
        var cursorPage = new ShopCursorPageResponse<ShopReviewResponse>(reviews, false, null);

        shopService.Setup(s => s.ReviewsAsync("an-nhien-bakery", It.IsAny<ShopReviewQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cursorPage);

        var response = await client.GetAsync("/api/v1/shops/an-nhien-bakery/reviews");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, json.GetProperty("data").GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task AdminShops_List_Returns200OK_ForAdmin()
    {
        var client = CreateClient("Admin");

        var summaryList = new List<AdminShopSummaryResponse>
        {
            new(ShopId, SupplierId, "An Nhiên Bakery", "an-nhien-bakery", ShopStatus.Active, 1, 10, DateTime.UtcNow)
        };
        var page = new ShopPageResponse<AdminShopSummaryResponse>(summaryList, 1, 20, 1);

        shopService.Setup(s => s.ListAdminAsync(UserId, It.IsAny<AdminShopQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var response = await client.GetAsync("/api/v1/admin/shops?page=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, json.GetProperty("data").GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task AdminShops_List_Returns403Forbidden_ForCustomer()
    {
        var client = CreateClient("Customer");

        var response = await client.GetAsync("/api/v1/admin/shops");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminShops_Detail_Returns200OK_ForAdmin()
    {
        var client = CreateClient("Admin");

        shopService.Setup(s => s.DetailAdminAsync(UserId, ShopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateOwnerShop());

        var response = await client.GetAsync($"/api/v1/admin/shops/{ShopId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(ShopId.ToString(), json.GetProperty("data").GetProperty("shopId").GetString());
    }

    [Fact]
    public async Task AdminShops_Moderate_Returns200OK_WhenAdminSuspends()
    {
        var client = CreateClient("Admin");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var request = new AdminShopModerationRequest
        {
            ExpectedVersion = 1,
            Action = "Suspend",
            Reason = "Vi phạm chính sách an toàn vệ sinh thực phẩm trên bao bì."
        };

        var suspendedShop = CreateOwnerShop(2, ShopStatus.Suspended);
        shopService.Setup(s => s.ModerateAsync(UserId, ShopId, It.IsAny<string>(), It.IsAny<AdminShopModerationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(suspendedShop);

        var response = await client.PatchAsJsonAsync($"/api/v1/admin/shops/{ShopId}/status", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Suspended", json.GetProperty("data").GetProperty("status").GetString());
    }

    // =========================================================================
    // FEATURE #4: TÙY CHỈNH GIAO DIỆN & TEMPLATES (STOREFRONT CUSTOMIZATION)
    // =========================================================================

    [Fact]
    public async Task Appearance_Get_Returns200OK_ForOwner()
    {
        var client = CreateClient("Supplier");

        shopService.Setup(s => s.GetMineAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateOwnerShop());

        var response = await client.GetAsync("/api/v1/shops/me/appearance");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("heritage", json.GetProperty("data").GetProperty("templateCode").GetString());
    }

    [Fact]
    public async Task Appearance_Save_Returns200OK_OnCustomColors()
    {
        var client = CreateClient("Supplier");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var request = new UpdateShopAppearanceRequest
        {
            ExpectedVersion = 1,
            TemplateId = TemplateId,
            TemplateRevisionId = TemplateRevisionId,
            Overrides = new ThemeOverridesRequest
            {
                PrimaryColor = "#d97706",
                BackgroundColor = "#fffbeb"
            }
        };

        var updatedShop = CreateOwnerShop(2);
        shopService.Setup(s => s.AppearanceAsync(UserId, It.IsAny<string>(), It.IsAny<UpdateShopAppearanceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedShop);

        var response = await client.PutAsJsonAsync("/api/v1/shops/me/appearance", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Templates_Catalog_Returns200OK_ForSupplier()
    {
        var client = CreateClient("Supplier");

        var templateList = new List<ShopTemplateResponse>
        {
            new(TemplateId, "heritage", "Heritage Luxury", "Mẫu giao diện cung đình hoàng gia", 1, true, false, 0, null, null, null)
        };
        var cursorPage = new ShopCursorPageResponse<ShopTemplateResponse>(templateList, false, null);

        shopService.Setup(s => s.CatalogAsync(UserId, It.IsAny<ShopPageQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cursorPage);

        var response = await client.GetAsync("/api/v1/shop-templates");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, json.GetProperty("data").GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task AdminTemplates_List_Returns200OK_ForAdmin()
    {
        var client = CreateClient("Admin");

        var templateList = new List<ShopTemplateResponse>
        {
            new(TemplateId, "royal-gift", "Royal Gift Set", "Mẫu giao diện quà tặng hoàng gia", 1, true, true, 1, null, null, null)
        };
        var page = new ShopPageResponse<ShopTemplateResponse>(templateList, 1, 20, 1);

        shopService.Setup(s => s.TemplatesAdminAsync(UserId, It.IsAny<AdminTemplateQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var response = await client.GetAsync("/api/v1/admin/shop-templates?page=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, json.GetProperty("data").GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task AdminTemplates_Create_Returns201Created_ForAdmin()
    {
        var client = CreateClient("Admin");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var request = new CreateShopTemplateRequest
        {
            Code = "minimal-grid",
            Name = "Minimal Modern Grid",
            Description = "Giao diện lưới tối giản",
            SortOrder = 2,
            RendererKey = "minimal-grid",
            SchemaVersion = 1,
            Defaults = DefaultTokens,
            Layout = DefaultLayout
        };

        var createdTemplate = new ShopTemplateResponse(
            TemplateId, request.Code, request.Name, request.Description, 1, true, false, 2, null, null, null);

        shopService.Setup(s => s.CreateTemplateAsync(UserId, It.IsAny<string>(), It.IsAny<CreateShopTemplateRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdTemplate);

        var response = await client.PostAsJsonAsync("/api/v1/admin/shop-templates", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("minimal-grid", json.GetProperty("data").GetProperty("code").GetString());
    }

    [Fact]
    public async Task AdminTemplates_Availability_Returns200OK()
    {
        var client = CreateClient("Admin");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var request = new SetTemplateAvailabilityRequest
        {
            ExpectedVersion = 1,
            IsActive = false
        };

        var updatedTemplate = new ShopTemplateResponse(
            TemplateId, "heritage", "Heritage", null, 2, false, false, 0, null, null, null);

        shopService.Setup(s => s.TemplateAvailabilityAsync(UserId, TemplateId, It.IsAny<string>(), It.IsAny<SetTemplateAvailabilityRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedTemplate);

        var response = await client.PatchAsJsonAsync($"/api/v1/admin/shop-templates/{TemplateId}/availability", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(json.GetProperty("data").GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task AdminTemplates_Premium_Returns200OK()
    {
        var client = CreateClient("Admin");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var request = new SetTemplatePremiumRequest
        {
            ExpectedVersion = 2,
            IsPremium = true
        };

        var updatedTemplate = new ShopTemplateResponse(
            TemplateId, "heritage", "Heritage", null, 3, true, true, 0, null, null, null);

        shopService.Setup(s => s.TemplatePremiumAsync(UserId, TemplateId, It.IsAny<string>(), It.IsAny<SetTemplatePremiumRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedTemplate);

        var response = await client.PatchAsJsonAsync($"/api/v1/admin/shop-templates/{TemplateId}/premium", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("data").GetProperty("isPremium").GetBoolean());
    }
}

