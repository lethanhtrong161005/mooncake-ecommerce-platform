namespace Mooncake.EcommercePlatform.Verification.Tests;

using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Mooncake.EcommercePlatform.Application.DTOs.Ai.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Ai.Responses;
using Mooncake.EcommercePlatform.Application.DTOs.Products.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Products.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;
using Mooncake.EcommercePlatform.WebApi.Controllers;
using Mooncake.EcommercePlatform.WebApi.Middleware;
using Xunit;

public sealed class ReportsAuthorizationTests
{
    private static readonly Guid AdminUserId = Guid.NewGuid();
    private static readonly Guid SupplierUserId = Guid.NewGuid();
    private static readonly Guid CustomerUserId = Guid.NewGuid();

    private readonly Mock<IAiAnalyticsService> aiAnalyticsService = new();
    private readonly Mock<IProductCatalogService> productService = new();

    private HttpClient CreateClient(string role = "Admin")
    {
        var userId = role == "Admin" ? AdminUserId : role == "Supplier" ? SupplierUserId : CustomerUserId;

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        builder.Services.AddScoped(_ => new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().Options));
        builder.Services.AddControllers()
            .AddApplicationPart(typeof(ReportsController).Assembly)
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            });

        builder.Services.AddSingleton(aiAnalyticsService.Object);
        builder.Services.AddSingleton(productService.Object);
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

    [Fact]
    public async Task GetDashboardMetrics_Customer_Returns403Forbidden()
    {
        using var client = CreateClient("Customer");
        var response = await client.GetAsync("/api/v1/reports/dashboard-metrics");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetDashboardMetrics_Supplier_Returns403Forbidden()
    {
        using var client = CreateClient("Supplier");
        var response = await client.GetAsync("/api/v1/reports/dashboard-metrics");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetConversionFunnel_Customer_Returns403Forbidden()
    {
        using var client = CreateClient("Customer");
        var response = await client.GetAsync("/api/v1/reports/conversion-funnel");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetTopSuppliers_Customer_Returns403Forbidden()
    {
        using var client = CreateClient("Customer");
        var response = await client.GetAsync("/api/v1/reports/top-suppliers");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ExportReport_Customer_Returns403Forbidden()
    {
        using var client = CreateClient("Customer");
        var response = await client.GetAsync("/api/v1/reports/export");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GenerateAiSummary_Customer_Returns403Forbidden()
    {
        using var client = CreateClient("Customer");
        var response = await client.PostAsJsonAsync("/api/v1/reports/ai-summary", new GenerateAiAnalyticsReportRequest
        {
            ReportType = AiReportType.SalesTrend
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GenerateAiSummary_Supplier_Returns200Ok()
    {
        aiAnalyticsService.Setup(s => s.GenerateReportAsync(SupplierUserId, false, It.IsAny<GenerateAiAnalyticsReportRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiAnalyticsReportResponse
            {
                ReportId = Guid.NewGuid(),
                ReportType = AiReportType.SalesTrend,
                Status = AiReportStatus.Completed
            });

        using var client = CreateClient("Supplier");
        var response = await client.PostAsJsonAsync("/api/v1/reports/ai-summary", new GenerateAiAnalyticsReportRequest
        {
            ReportType = AiReportType.SalesTrend
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SupplierProducts_Customer_Returns403Forbidden()
    {
        using var client = CreateClient("Customer");
        var response = await client.GetAsync("/api/v1/suppliers/me/products");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SupplierProducts_Admin_Returns403Forbidden()
    {
        using var client = CreateClient("Admin");
        var response = await client.GetAsync("/api/v1/suppliers/me/products");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SupplierProducts_Supplier_Returns200Ok()
    {
        var page = new ProductPageResponse<ProductOwnerCardResponse>([], 1, 20, 0);
        productService.Setup(s => s.ListMineAsync(SupplierUserId, It.IsAny<OwnerProductQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        using var client = CreateClient("Supplier");
        var response = await client.GetAsync("/api/v1/suppliers/me/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
