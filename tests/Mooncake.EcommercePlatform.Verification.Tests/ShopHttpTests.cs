namespace Mooncake.EcommercePlatform.Verification.Tests;

using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SkiaSharp;
using Xunit;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.Services.Implementations;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Infrastructure.Repositories;
using Mooncake.EcommercePlatform.Infrastructure.Services;
using Mooncake.EcommercePlatform.Infrastructure.Options;
using Mooncake.EcommercePlatform.Application.DTOs.Files.Responses;
using Mooncake.EcommercePlatform.WebApi.Configurations;
using Mooncake.EcommercePlatform.WebApi.Controllers;
using Mooncake.EcommercePlatform.WebApi.Middleware;

/// <summary>Real HTTP model binding and PostgreSQL writes on a disposable loopback server; no project credentials.</summary>
public sealed class ShopHttpTests(PostgresVerificationFixture fixture) : IClassFixture<PostgresVerificationFixture>
{
    [PostgresFact]
    public async Task HttpWorkflowAndValidationUseRealPersistence()
    {
        var (owner, admin) = await new ShopPostgresTests(fixture).SeedAsync();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddControllers().AddApplicationPart(typeof(ShopsController).Assembly)
            .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        builder.Services.AddAuthorization();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>(); builder.Services.AddProblemDetails();
        builder.Services.AddShopFeature();
        var cleanup = builder.Services.First(x => x.ServiceType == typeof(IHostedService) &&
            x.ImplementationType == typeof(Mooncake.EcommercePlatform.WebApi.Background.ShopMediaCleanupWorker));
        builder.Services.Remove(cleanup);
        builder.Services.AddScoped(_ => fixture.Create());
        builder.Services.AddScoped<IShopRepository, ShopRepository>();
        var clock = new Mock<IDateTimeProvider>(); clock.SetupGet(x => x.UtcNow).Returns(() => DateTime.UtcNow);
        builder.Services.AddSingleton(clock.Object);
        builder.Services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
        builder.Services.AddSingleton<IShopCursorProtector, ShopCursorProtector>();
        builder.Services.AddScoped<IShopService, ShopService>();
        var storage = new Mock<ICloudinaryStorageService>();
        storage.Setup(x => x.UploadImageAsync(It.IsAny<Stream>(), It.IsAny<string>(), false, It.IsAny<CancellationToken>()))
            .Returns((Stream content, string _, bool _, CancellationToken _) => Task.FromResult(new CloudinaryUploadResponse(
                "https://example.test/branding.png", $"public-images/{Guid.NewGuid():N}", "image", "upload", content.Length)));
        builder.Services.AddSingleton<IShopMediaGateway>(new ShopMediaGateway(storage.Object,
            Mock.Of<CloudinaryDotNet.ICloudinaryUploadApi>(), Options.Create(new CloudinaryOptions { PublicImagesFolder = "public-images" }),
            NullLogger<ShopMediaGateway>.Instance));
        await using var app = builder.Build();
        app.UseExceptionHandler(); app.UseRouting();
        app.Use(async (context, next) => {
            var role = context.Request.Headers["X-Test-Role"].ToString();
            context.User = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, role == "Admin" ? admin.ToString() : owner.ToString()),
                new Claim(ClaimTypes.Role, role == "Admin" ? "Admin" : "Supplier")
            ], "TestOnly"));
            await next();
        });
        app.UseRateLimiter(); app.UseAuthorization(); app.MapControllers(); await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        using var client = new HttpClient { BaseAddress = new(address) };
        async Task<HttpResponseMessage> Send(HttpMethod method, string path, string body, string? key = null) {
            var request = new HttpRequestMessage(method, path) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
            request.Headers.Add("Idempotency-Key", key ?? Guid.NewGuid().ToString());
            return await client.SendAsync(request);
        }
        async Task<Guid> UploadImage(string purpose, int width, int height) {
            using var bitmap = new SKBitmap(width, height); bitmap.Erase(SKColors.Coral);
            using var image = SKImage.FromBitmap(bitmap); using var encoded = image.Encode(SKEncodedImageFormat.Png, 90);
            using var form = new MultipartFormDataContent();
            form.Add(new ByteArrayContent(encoded.ToArray()), "file", "branding.png");
            form.Add(new StringContent(purpose), "purpose");
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/shops/me/media") { Content = form };
            request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
            using var result = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Created, result.StatusCode);
            var data = JsonDocument.Parse(await result.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
            Assert.Equal("Ready", data.GetProperty("status").GetString());
            return data.GetProperty("mediaAssetId").GetGuid();
        }
        // No scanner is registered: creating a shop with branding must work while ClamAV is stopped.
        var logoAssetId = await UploadImage("Logo", 128, 128);
        var bannerAssetId = await UploadImage("Banner", 640, 200);
        var slug = $"http-bakery-{Guid.NewGuid():N}";
        var json = JsonSerializer.Serialize(new { name = "HTTP Bakery", slug, logoAssetId, bannerAssetId,
            templateId = "90000000-0000-4000-8000-000000000001", templateRevisionId = "91000000-0000-4000-8000-000000000001" });
        var key = Guid.NewGuid().ToString();
        var created = await Send(HttpMethod.Post, "/api/v1/shops/me", json, key);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var ownerResult = JsonDocument.Parse(await created.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
        var id = ownerResult.GetProperty("shopId").GetGuid(); Assert.Equal("Inactive", ownerResult.GetProperty("status").GetString());
        Assert.Equal(logoAssetId, ownerResult.GetProperty("logoAssetId").GetGuid());
        Assert.Equal(bannerAssetId, ownerResult.GetProperty("bannerAssetId").GetGuid());
        storage.Verify(x => x.UploadImageAsync(It.IsAny<Stream>(), It.IsAny<string>(), false, It.IsAny<CancellationToken>()), Times.Exactly(2));
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/shops/{slug}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/shops/me/preview")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/shop-templates?pageSize=5")).StatusCode);
        foreach (var invalid in new[] {
            json.Replace("\"name\":\"HTTP Bakery\"", "\"name\":\"HTTP Bakery\",\"Name\":\"Other\""),
            json[..^1] + ",\"ownerUserId\":\"11111111-1111-4111-8111-111111111111\"}",
            "{", "null" }) {
            Assert.Equal(HttpStatusCode.BadRequest, (await Send(HttpMethod.Post, "/api/v1/shops/me", invalid)).StatusCode);
        }
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(HttpMethod.Put, "/api/v1/shops/me/appearance",
            "{\"expectedVersion\":1,\"overrides\":{\"primaryColor\":null}}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(HttpMethod.Put, "/api/v1/shops/me/appearance",
            "{\"expectedVersion\":1,\"templateId\":\"90000000-0000-4000-8000-000000000001\",\"templateRevisionId\":\"91000000-0000-4000-8000-000000000001\",\"overrides\":{\"customCss\":\"url(https://invalid.test)\"}}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(HttpMethod.Patch, "/api/v1/shops/me/visibility", "{\"status\":\"Active\"}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/v1/shops/{slug}/products?sort=newest&unexpected=true")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/v1/shops/{slug}/products?pageSize=5&pageSize=6")).StatusCode);
        var activated = await Send(HttpMethod.Patch, "/api/v1/shops/me/visibility", "{\"expectedVersion\":1,\"status\":\"Active\"}");
        Assert.Equal(HttpStatusCode.OK, activated.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/shops/{slug}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/shops/{slug}/products?sort=priceAsc&pageSize=12&inStockOnly=false")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/shops/{slug}/categories?pageSize=100")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/shops/{slug}/reviews?rating=5&pageSize=10")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Send(HttpMethod.Put, "/api/v1/shops/me", "{\"name\":\"Stale update\",\"expectedVersion\":1}")).StatusCode);
        client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/admin/shops?page=1&pageSize=20&sort=createdAtDesc")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Send(HttpMethod.Patch, $"/api/v1/admin/shops/{id}/status",
            "{\"action\":\"Suspend\",\"expectedVersion\":2,\"reason\":\"Suspended for HTTP verification.\"}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/shops/{slug}/products")).StatusCode);
        var definition = new { code = $"http-template-{Guid.NewGuid():N}", name = "HTTP Template", sortOrder = 12,
            rendererKey = "royal-gift", schemaVersion = 1,
            defaults = new { primaryColor = "#8C1524", backgroundColor = "#FAF8F6", textColor = "#222222",
                surfaceColor = "#FFFFFF", buttonTextColor = "#FFFFFF", borderRadiusPx = 12, fontScale = 1 },
            layout = new { sectionOrder = new[] { "Hero", "Products", "Contact" }, productColumnsDesktop = 3 } };
        var createdTemplate = await Send(HttpMethod.Post, "/api/v1/admin/shop-templates", JsonSerializer.Serialize(definition));
        Assert.Equal(HttpStatusCode.Created, createdTemplate.StatusCode);
        var templateId = JsonDocument.Parse(await createdTemplate.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await Send(HttpMethod.Patch, $"/api/v1/admin/shop-templates/{templateId}/availability",
            "{\"expectedVersion\":1,\"isActive\":true}")).StatusCode);
        var publish = JsonSerializer.Serialize(new { definition.name, definition.sortOrder, definition.rendererKey,
            definition.schemaVersion, definition.defaults, definition.layout, expectedVersion = 2 });
        Assert.Equal(HttpStatusCode.OK, (await Send(HttpMethod.Put, $"/api/v1/admin/shop-templates/{templateId}", publish)).StatusCode);
        using var multipart = new MultipartFormDataContent();
        multipart.Add(new StringContent("Logo"), "purpose"); // Missing file must fail MVC validation before opening the stream.
        var uploadRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/shops/me/media") { Content = multipart };
        uploadRequest.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.SendAsync(uploadRequest)).StatusCode);
        await app.StopAsync();
    }
}
