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
using Mooncake.EcommercePlatform.Application.DTOs.Products.Responses;
using Mooncake.EcommercePlatform.Application.DTOs.Wishlist.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Wishlist.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.WebApi.Controllers;
using Mooncake.EcommercePlatform.WebApi.Middleware;
using Xunit;

public sealed class WishlistControllerHttpTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private readonly Mock<IWishlistService> wishlistService = new();

    private HttpClient CreateClient(bool authenticated = true)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        builder.Services.AddControllers()
            .AddApplicationPart(typeof(WishlistController).Assembly)
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            });

        builder.Services.AddSingleton(wishlistService.Object);
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
                    new Claim(ClaimTypes.Role, "Customer"),
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

    [Fact]
    public async Task GetPage_RequiresAuthentication()
    {
        var client = CreateClient(authenticated: false);
        var response = await client.GetAsync("/api/v1/wishlist");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetPage_UsesDefaultQuery_WhenNoQueryParametersProvided()
    {
        wishlistService.Setup(s => s.GetPageAsync(
                UserId,
                It.Is<WishlistPageQuery>(q => q.PageSize == 12 && q.Cursor == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WishlistPageResponse([], false, null, 0));

        var client = CreateClient();
        var response = await client.GetAsync("/api/v1/wishlist");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetPage_ReturnsCursorPagePayload()
    {
        var productId = Guid.NewGuid();
        var item = new WishlistItemResponse(
            Guid.NewGuid(),
            productId,
            "Hộp Trăng Rằm Thượng Hạng",
            "https://res.cloudinary.com/demo/mooncake.png",
            520000m,
            520000m,
            780000m,
            10,
            "hộp",
            true,
            ProductStatus.Active,
            new ProductShopSummaryResponse(Guid.NewGuid(), "kinh-do", "Xưởng Kinh Đô", 96m, true, "HCM"),
            new ProductCategorySummaryResponse(Guid.NewGuid(), "Hộp quà VIP"),
            4.8m,
            12,
            DateTime.UtcNow);

        wishlistService.Setup(s => s.GetPageAsync(
                UserId,
                It.Is<WishlistPageQuery>(q => q.PageSize == 6 && q.Cursor == "cur-1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WishlistPageResponse([item], true, "cur-2", 9));

        var client = CreateClient();
        var response = await client.GetAsync("/api/v1/wishlist?pageSize=6&cursor=cur-1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = json.GetProperty("data");
        Assert.True(data.GetProperty("hasMore").GetBoolean());
        Assert.Equal("cur-2", data.GetProperty("nextCursor").GetString());
        Assert.Equal(9, data.GetProperty("totalCount").GetInt32());
        Assert.Equal(productId.ToString(), data.GetProperty("items")[0].GetProperty("productId").GetString());
    }

    [Theory]
    [InlineData("/api/v1/wishlist?pageSize=0")]
    [InlineData("/api/v1/wishlist?pageSize=51")]
    [InlineData("/api/v1/wishlist?cursor=")]
    [InlineData("/api/v1/wishlist?cursor=%20%20")]
    public async Task GetPage_Returns400_WhenDtoValidationFails(string requestUri)
    {
        var client = CreateClient();
        var response = await client.GetAsync(requestUri);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        wishlistService.Verify(
            s => s.GetPageAsync(It.IsAny<Guid>(), It.IsAny<WishlistPageQuery>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Toggle_ReturnsSavedState_AndRejectsEmptyGuid()
    {
        var productId = Guid.NewGuid();
        wishlistService.Setup(s => s.ToggleAsync(UserId, productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ToggleWishlistResponse(productId, true, 4));

        var client = CreateClient();
        var okResponse = await client.PostAsync($"/api/v1/wishlist/{productId}", null);

        Assert.Equal(HttpStatusCode.OK, okResponse.StatusCode);
        var json = await okResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("data").GetProperty("isSaved").GetBoolean());
        Assert.Equal(4, json.GetProperty("data").GetProperty("totalCount").GetInt32());

        var badResponse = await client.PostAsync($"/api/v1/wishlist/{Guid.Empty}", null);
        Assert.Equal(HttpStatusCode.BadRequest, badResponse.StatusCode);
        wishlistService.Verify(
            s => s.ToggleAsync(UserId, Guid.Empty, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CheckBatch_ParsesIds_AndRejectsNullEmptyMalformedOrOverLimit_AtDtoLayer()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        wishlistService.Setup(s => s.CheckBatchAsync(
                UserId,
                It.Is<WishlistCheckQuery>(q =>
                    q.ToDistinctProductIds().Count == 2 &&
                    q.ToDistinctProductIds().Contains(p1) &&
                    q.ToDistinctProductIds().Contains(p2)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WishlistCheckResponse([p2]));

        var client = CreateClient();
        var commaResponse = await client.GetAsync($"/api/v1/wishlist/check?ids={p1},{p2}");
        Assert.Equal(HttpStatusCode.OK, commaResponse.StatusCode);
        var commaJson = await commaResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(p2.ToString(), commaJson.GetProperty("data").GetProperty("savedIds")[0].GetString());

        var repeatedResponse = await client.GetAsync($"/api/v1/wishlist/check?ids={p1}&ids={p2}");
        Assert.Equal(HttpStatusCode.OK, repeatedResponse.StatusCode);

        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/wishlist/check")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/wishlist/check?ids=")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/wishlist/check?ids=not-a-guid")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/v1/wishlist/check?ids={Guid.Empty}")).StatusCode);

        var tooManyQuery = string.Join(',', Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/v1/wishlist/check?ids={tooManyQuery}")).StatusCode);
    }
}
