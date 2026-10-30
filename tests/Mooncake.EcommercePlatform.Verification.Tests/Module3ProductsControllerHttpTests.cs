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
using Mooncake.EcommercePlatform.Application.DTOs.Products.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Products.Responses;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.WebApi.Controllers;
using Mooncake.EcommercePlatform.WebApi.Middleware;
using Xunit;

public sealed class Module3ProductsControllerHttpTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ShopId = Guid.NewGuid();
    private static readonly Guid CategoryId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();
    private static readonly Guid VariantId = Guid.NewGuid();
    private static readonly Guid MediaAssetId = Guid.NewGuid();

    private readonly Mock<IProductCatalogService> productService = new();

    private static ProductOwnerResponse CreateOwnerProduct(long version = 1, bool isPublished = false) => new(
        ProductId,
        ShopId,
        version,
        "Bánh Nướng Thập Cẩm Bát Bửu",
        "Sản phẩm quà tặng cao cấp mùa Trăng 2026",
        "Bột mì, lạp xưởng, hạt sen, mỡ đường, trứng muối",
        CategoryId,
        85000m,
        50,
        5000,
        "piece",
        true,
        isPublished,
        isPublished ? ProductStatus.Active : ProductStatus.Inactive,
        [
            new ProductVariantOwnerResponse(
                VariantId, "Thập cẩm 1 trứng 150g", "SKU-TC-150", "Baked", "Mixed Nuts",
                "150g", 150, false, 0m, 85000m, 500, 1, MediaAssetId, "https://example.com/v1.png")
        ],
        [
            new ProductImageResponse(Guid.NewGuid(), MediaAssetId, "https://example.com/img1.png", true, 0)
        ],
        [],
        DateTime.UtcNow,
        DateTime.UtcNow);

    private HttpClient CreateClient(string role = "Supplier", bool authenticated = true)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        builder.Services.AddControllers()
            .AddApplicationPart(typeof(SupplierProductsController).Assembly)
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            });

        builder.Services.AddSingleton(productService.Object);
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
    // FEATURE #5: QUẢN LÝ SẢN PHẨM & BIẾN THỂ (SUPPLIER PRODUCTS)
    // =========================================================================

    [Fact]
    public async Task ListMine_Returns200OK_ForOwner()
    {
        var client = CreateClient("Supplier");

        var cardList = new List<ProductOwnerCardResponse>
        {
            new(ProductId, 1, "Bánh Nướng Thập Cẩm", CategoryId, "https://example.com/img1.png", 85000m, 120000m, ProductStatus.Active, 1, 500, DateTime.UtcNow)
        };
        var page = new ProductPageResponse<ProductOwnerCardResponse>(cardList, 1, 20, 1);

        productService.Setup(s => s.ListMineAsync(UserId, It.IsAny<OwnerProductQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var response = await client.GetAsync("/api/v1/suppliers/me/products?page=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, json.GetProperty("data").GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task CreateProduct_Returns201Created_OnValidData()
    {
        var client = CreateClient("Supplier");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var request = new CreateProductRequest
        {
            Name = "Bánh Nướng Thập Cẩm Bát Bửu",
            Description = "Bánh nướng truyền thống",
            Ingredients = "Bột nếp, lạp xưởng, hạt điều",
            CategoryId = CategoryId,
            BasePrice = 85000m,
            MinOrderQty = 50,
            MaxOrderQty = 5000,
            Unit = "piece",
            SupportsCustomPackaging = true,
            Variants =
            [
                new CreateProductVariantInput
                {
                    Name = "Thập cẩm 150g",
                    Sku = "SKU-TC-150",
                    CrustType = "Baked",
                    Filling = "Mixed Nuts",
                    SizeLabel = "150g",
                    WeightGram = 150,
                    IsGiftSet = false,
                    PriceAdjustment = 0m,
                    InitialStockQty = 500
                }
            ],
            Images =
            [
                new ProductImageInput
                {
                    MediaAssetId = MediaAssetId,
                    SortOrder = 0,
                    IsPrimary = true
                }
            ]
        };

        var createdProduct = CreateOwnerProduct(1, false);
        productService.Setup(s => s.CreateAsync(UserId, It.IsAny<string>(), It.IsAny<CreateProductRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdProduct);

        var response = await client.PostAsJsonAsync("/api/v1/suppliers/me/products", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(ProductId.ToString(), json.GetProperty("data").GetProperty("productId").GetString());
        Assert.Equal("Bánh Nướng Thập Cẩm Bát Bửu", json.GetProperty("data").GetProperty("name").GetString());
    }

    [Fact]
    public async Task CreateProduct_Returns409Conflict_WhenSkuAlreadyExists()
    {
        var client = CreateClient("Supplier");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var request = new CreateProductRequest
        {
            Name = "Bánh Nướng Thập Cẩm Bát Bửu",
            CategoryId = CategoryId,
            BasePrice = 85000m,
            MinOrderQty = 50,
            Unit = "piece",
            SupportsCustomPackaging = true,
            Variants =
            [
                new CreateProductVariantInput { Name = "V1", Sku = "SKU-TC-150", IsGiftSet = false, PriceAdjustment = 0m, InitialStockQty = 100 }
            ],
            Images =
            [
                new ProductImageInput { MediaAssetId = MediaAssetId, SortOrder = 0, IsPrimary = true }
            ]
        };

        productService.Setup(s => s.CreateAsync(UserId, It.IsAny<string>(), It.IsAny<CreateProductRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpException(409, "SKU already exists in shop."));

        var response = await client.PostAsJsonAsync("/api/v1/suppliers/me/products", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetMine_Returns200OK_ForOwnerProduct()
    {
        var client = CreateClient("Supplier");

        productService.Setup(s => s.GetMineAsync(UserId, ProductId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateOwnerProduct());

        var response = await client.GetAsync($"/api/v1/suppliers/me/products/{ProductId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(ProductId.ToString(), json.GetProperty("data").GetProperty("productId").GetString());
    }

    [Fact]
    public async Task UpdateCore_Returns200OK_OnSuccess()
    {
        var client = CreateClient("Supplier");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var request = new UpdateProductCoreRequest
        {
            ExpectedVersion = 1,
            Name = "Bánh Nướng Thập Cẩm Thượng Hạng",
            CategoryId = CategoryId,
            BasePrice = 90000m,
            MinOrderQty = 50,
            Unit = "piece",
            SupportsCustomPackaging = true
        };

        var updated = CreateOwnerProduct(2);
        productService.Setup(s => s.UpdateAsync(UserId, ProductId, It.IsAny<string>(), It.IsAny<UpdateProductCoreRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);

        var response = await client.PutAsJsonAsync($"/api/v1/suppliers/me/products/{ProductId}", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, json.GetProperty("data").GetProperty("version").GetInt64());
    }

    [Fact]
    public async Task AddVariant_Returns201Created()
    {
        var client = CreateClient("Supplier");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var request = new CreateProductVariantRequest
        {
            ExpectedVersion = 2,
            Name = "Thập cẩm 2 trứng 200g",
            Sku = "SKU-TC-200",
            CrustType = "Baked",
            Filling = "Mixed Nuts",
            SizeLabel = "200g",
            WeightGram = 200,
            IsGiftSet = false,
            PriceAdjustment = 20000m,
            InitialStockQty = 300
        };

        var productWithNewVariant = CreateOwnerProduct(3);
        productService.Setup(s => s.AddVariantAsync(UserId, ProductId, It.IsAny<string>(), It.IsAny<CreateProductVariantRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(productWithNewVariant);

        var response = await client.PostAsJsonAsync($"/api/v1/suppliers/me/products/{ProductId}/variants", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(3, json.GetProperty("data").GetProperty("version").GetInt64());
    }

    [Fact]
    public async Task UpdateVariant_Returns200OK()
    {
        var client = CreateClient("Supplier");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var request = new UpdateProductVariantRequest
        {
            ExpectedVersion = 3,
            Name = "Thập cẩm 1 trứng đặc biệt 150g",
            Sku = "SKU-TC-150",
            IsGiftSet = false,
            PriceAdjustment = 5000m
        };

        var updated = CreateOwnerProduct(4);
        productService.Setup(s => s.UpdateVariantAsync(UserId, ProductId, VariantId, It.IsAny<string>(), It.IsAny<UpdateProductVariantRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);

        var response = await client.PutAsJsonAsync($"/api/v1/suppliers/me/products/{ProductId}/variants/{VariantId}", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteVariant_Returns200OK()
    {
        var client = CreateClient("Supplier");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var updated = CreateOwnerProduct(5);
        productService.Setup(s => s.DeleteVariantAsync(UserId, ProductId, VariantId, 4, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);

        var response = await client.DeleteAsync($"/api/v1/suppliers/me/products/{ProductId}/variants/{VariantId}?expectedVersion=4");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AdjustInventory_Returns200OK_WithNewStock()
    {
        var client = CreateClient("Supplier");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var request = new AdjustVariantInventoryRequest
        {
            ExpectedVersion = 5,
            Delta = 100,
            Reason = "Nhập thêm 100 hộp từ xưởng sản xuất"
        };

        var adjResponse = new InventoryAdjustmentResponse(
            Guid.NewGuid(), ProductId, VariantId, 500, 100, 600, 2, 5, ProductStatus.Active);

        productService.Setup(s => s.AdjustInventoryAsync(UserId, ProductId, VariantId, It.IsAny<string>(), It.IsAny<AdjustVariantInventoryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(adjResponse);

        var response = await client.PostAsJsonAsync($"/api/v1/suppliers/me/products/{ProductId}/variants/{VariantId}/inventory-adjustments", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(600, json.GetProperty("data").GetProperty("currentStockQty").GetInt32());
    }

    [Fact]
    public async Task ReplaceGallery_Returns200OK()
    {
        var client = CreateClient("Supplier");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var request = new ReplaceProductGalleryRequest
        {
            ExpectedVersion = 5,
            Images =
            [
                new ProductImageInput { MediaAssetId = MediaAssetId, SortOrder = 0, IsPrimary = true }
            ]
        };

        var updated = CreateOwnerProduct(6);
        productService.Setup(s => s.ReplaceGalleryAsync(UserId, ProductId, It.IsAny<string>(), It.IsAny<ReplaceProductGalleryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);

        var response = await client.PutAsJsonAsync($"/api/v1/suppliers/me/products/{ProductId}/gallery", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SetPublication_Returns200OK_WhenPublishingProduct()
    {
        var client = CreateClient("Supplier");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var request = new SetProductPublicationRequest
        {
            ExpectedVersion = 6,
            IsPublished = true
        };

        var publishedProduct = CreateOwnerProduct(7, true);
        productService.Setup(s => s.SetPublicationAsync(UserId, ProductId, It.IsAny<string>(), It.IsAny<SetProductPublicationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(publishedProduct);

        var response = await client.PatchAsJsonAsync($"/api/v1/suppliers/me/products/{ProductId}/publication", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("data").GetProperty("isPublished").GetBoolean());
    }

    [Fact]
    public async Task DeleteProduct_Returns200OK_SoftDeleted()
    {
        var client = CreateClient("Supplier");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var deleteResult = new DeleteProductResponse(ProductId, 8, true);
        productService.Setup(s => s.DeleteAsync(UserId, ProductId, 7, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(deleteResult);

        var response = await client.DeleteAsync($"/api/v1/suppliers/me/products/{ProductId}?expectedVersion=7");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("data").GetProperty("deleted").GetBoolean());
    }

    [Fact]
    public async Task UploadMedia_Returns201Created_OnProductImage()
    {
        var client = CreateClient("Supplier");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("fake-image-bytes"));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(fileContent, "File", "product.jpg");
        content.Add(new StringContent("ProductGallery"), "Purpose");

        var mediaResponse = new ShopMediaResponse(
            MediaAssetId, "ProductGallery", "Ready", "https://example.com/p.jpg", "image/jpeg", 16, 800, 800);

        productService.Setup(s => s.UploadMediaAsync(UserId, It.IsAny<string>(), "ProductGallery", It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaResponse);

        var response = await client.PostAsync("/api/v1/suppliers/me/product-media", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(MediaAssetId.ToString(), json.GetProperty("data").GetProperty("mediaAssetId").GetString());
    }

    // =========================================================================
    // FEATURE #6: TÌM KIẾM & KHÁM PHÁ SẢN PHẨM (PUBLIC DISCOVERY)
    // =========================================================================

    [Fact]
    public async Task Search_Returns200OK_WithFacetsAndItems()
    {
        var client = CreateClient(authenticated: false);

        var shopSummary = new ProductShopSummaryResponse(ShopId, "an-nhien", "An Nhiên", 95m, true, "VN-HN");
        var catSummary = new ProductCategorySummaryResponse(CategoryId, "Bánh nướng");

        var card = new ProductSearchCardResponse(
            ProductId, "Bánh Thập Cẩm Bát Bửu", shopSummary, catSummary, "https://example.com/p1.png",
            85000m, 120000m, 50, "piece", true, ProductStatus.Active, 4.9m, 28, DateTime.UtcNow);

        var facets = new ProductSearchFacetsResponse(
            [new ProductFacetValueResponse(CategoryId.ToString(), "Bánh nướng", 1)],
            [new ProductFacetValueResponse("VN-HN", "Hà Nội", 1)],
            85000m, 120000m, 1, 1);

        var searchResponse = new ProductSearchResponse([card], 1, 24, 1, facets, "relevance", DateTime.UtcNow);

        productService.Setup(s => s.SearchAsync(It.IsAny<ProductSearchQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResponse);

        var response = await client.GetAsync("/api/v1/products?q=thap+cam&minPrice=50000&maxPrice=200000&pageSize=24");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, json.GetProperty("data").GetProperty("totalCount").GetInt32());
        Assert.Equal("Bánh Thập Cẩm Bát Bửu", json.GetProperty("data").GetProperty("items")[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task Autocomplete_Returns200OK_WithSuggestions()
    {
        var client = CreateClient(authenticated: false);

        var suggestions = new List<ProductAutocompleteResponse>
        {
            new(ProductId, "Product", "Bánh Nướng Thập Cẩm Bát Bửu", "An Nhiên Bakery", $"/products/{ProductId}"),
            new(ShopId, "Shop", "An Nhiên Bakery", "Hà Nội", "/shops/an-nhien")
        };

        productService.Setup(s => s.AutocompleteAsync(It.IsAny<ProductAutocompleteQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(suggestions);

        var response = await client.GetAsync("/api/v1/products/autocomplete?q=thap&limit=5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, json.GetProperty("data").GetArrayLength());
    }

    [Fact]
    public async Task PublicDetail_Returns200OK_WhenProductPublished()
    {
        var client = CreateClient(authenticated: false);

        var detail = new ProductPublicDetailResponse(
            ProductId, "Bánh Thập Cẩm Bát Bửu", "Mô tả chi tiết", "Nguyên liệu cao cấp",
            new ProductCategorySummaryResponse(CategoryId, "Bánh nướng"),
            new ProductShopSummaryResponse(ShopId, "an-nhien", "An Nhiên", 95m, true, "VN-HN"),
            85000m, 50, 5000, "piece", true, ProductStatus.Active,
            [
                new ProductVariantPublicResponse(VariantId, "150g", "SKU-TC-150", "Baked", "Nuts", "150g", 150, false, 85000m, true, null)
            ],
            [
                new ProductImageResponse(Guid.NewGuid(), MediaAssetId, "https://example.com/p1.png", true, 0)
            ],
            4.9m, 28, DateTime.UtcNow);

        productService.Setup(s => s.PublicAsync(ProductId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(detail);

        var response = await client.GetAsync($"/api/v1/products/{ProductId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Bánh Thập Cẩm Bát Bửu", json.GetProperty("data").GetProperty("name").GetString());
        Assert.True(json.GetProperty("data").GetProperty("supportsCustomPackaging").GetBoolean());
    }

    [Fact]
    public async Task PublicDetail_Returns404NotFound_WhenProductUnpublishedOrMissing()
    {
        var client = CreateClient(authenticated: false);

        productService.Setup(s => s.PublicAsync(ProductId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpException(404, "Product not found or not published."));

        var response = await client.GetAsync($"/api/v1/products/{ProductId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Related_Returns200OK_WithRelatedProducts()
    {
        var client = CreateClient(authenticated: false);

        var relatedCards = new List<ProductSearchCardResponse>
        {
            new(Guid.NewGuid(), "Bánh Dẻo Đậu Xanh",
                new ProductShopSummaryResponse(ShopId, "an-nhien", "An Nhiên", 95m, true, "VN-HN"),
                new ProductCategorySummaryResponse(CategoryId, "Bánh dẻo"),
                "https://example.com/p2.png", 65000m, 95000m, 50, "piece", false, ProductStatus.Active, 4.7m, 12, DateTime.UtcNow)
        };

        productService.Setup(s => s.RelatedAsync(ProductId, 12, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(relatedCards);

        var response = await client.GetAsync($"/api/v1/products/{ProductId}/related?limit=12");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, json.GetProperty("data").GetArrayLength());
    }

    [Fact]
    public async Task Recommendations_Returns200OK()
    {
        var client = CreateClient(authenticated: false);

        var recCards = new List<ProductSearchCardResponse>
        {
            new(Guid.NewGuid(), "Hộp Quà Trung Thu VIP Trăng Vàng",
                new ProductShopSummaryResponse(ShopId, "an-nhien", "An Nhiên", 95m, true, "VN-HN"),
                new ProductCategorySummaryResponse(CategoryId, "Hộp quà VIP"),
                "https://example.com/vip.png", 450000m, 850000m, 10, "box", true, ProductStatus.Active, 5.0m, 45, DateTime.UtcNow)
        };

        productService.Setup(s => s.RecommendationsAsync(20, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(recCards);

        var response = await client.GetAsync("/api/v1/products/recommendations?limit=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, json.GetProperty("data").GetArrayLength());
    }

    [Fact]
    public async Task RecordView_Returns202Accepted()
    {
        var client = CreateClient(authenticated: false);
        var request = new RecordProductViewRequest
        {
            SessionId = Guid.NewGuid(),
            Source = "Search"
        };

        productService.Setup(s => s.RecordViewAsync(ProductId, It.IsAny<Guid?>(), It.IsAny<RecordProductViewRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var response = await client.PostAsJsonAsync($"/api/v1/products/{ProductId}/views", request);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task CatalogMetadata_Categories_Returns200OK()
    {
        var client = CreateClient(authenticated: false);

        var categories = new List<CategoryCatalogResponse>
        {
            new(CategoryId, null, "Bánh Nướng", "Bánh nướng truyền thống", "https://example.com/icon1.svg", 1),
            new(Guid.NewGuid(), null, "Bánh Dẻo", "Bánh dẻo cổ truyền", "https://example.com/icon2.svg", 2)
        };

        productService.Setup(s => s.CategoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        var response = await client.GetAsync("/api/v1/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, json.GetProperty("data").GetArrayLength());
    }

    [Fact]
    public async Task CatalogMetadata_Regions_Returns200OK()
    {
        var client = CreateClient(authenticated: false);

        var regions = new List<RegionCatalogResponse>
        {
            new("VN-HN", "Hà Nội & Miền Bắc"),
            new("VN-SG", "TP. Hồ Chí Minh & Miền Nam")
        };

        productService.Setup(s => s.RegionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(regions);

        var response = await client.GetAsync("/api/v1/regions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, json.GetProperty("data").GetArrayLength());
    }
}
