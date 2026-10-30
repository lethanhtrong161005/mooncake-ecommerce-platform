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
using Mooncake.EcommercePlatform.Application.DTOs.Ai.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Ai.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.WebApi.Controllers;
using Mooncake.EcommercePlatform.WebApi.Middleware;
using Xunit;

public sealed class AiControllersHttpTests
{
    private static readonly Guid AdminUserId = Guid.NewGuid();
    private static readonly Guid SupplierUserId = Guid.NewGuid();
    private static readonly Guid CustomerUserId = Guid.NewGuid();
    private static readonly Guid ReportId = Guid.NewGuid();

    private readonly Mock<IAiChatService> aiChatService = new();
    private readonly Mock<IAiAnalyticsService> aiAnalyticsService = new();

    private HttpClient CreateClient(string role = "Admin")
    {
        var userId = role == "Admin" ? AdminUserId : role == "Supplier" ? SupplierUserId : CustomerUserId;

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        builder.Services.AddControllers()
            .AddApplicationPart(typeof(AiChatController).Assembly)
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            });

        builder.Services.AddSingleton(aiChatService.Object);
        builder.Services.AddSingleton(aiAnalyticsService.Object);
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
    public async Task PostSessions_Returns200_WithSessionToken()
    {
        aiChatService.Setup(s => s.StartSessionAsync(It.IsAny<Guid?>(), It.IsAny<StartAiSessionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiSessionResponse
            {
                ConversationId = Guid.NewGuid(),
                SessionToken = "aic-session-token-123",
                ConversationType = AiConversationType.ProductInfo,
                CreatedAt = DateTime.UtcNow
            });

        using var client = CreateClient("Customer");
        var response = await client.PostAsJsonAsync("/api/v1/ai/chat/sessions", new StartAiSessionRequest
        {
            ConversationType = AiConversationType.ProductInfo
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("aic-session-token-123", body);
    }

    [Fact]
    public async Task PostMessages_Returns200_WithAssistantResponse()
    {
        aiChatService.Setup(s => s.ProcessMessageAsync(It.IsAny<Guid?>(), It.IsAny<SendAiMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiMessageResponse
            {
                MessageId = Guid.NewGuid(),
                Role = AiMessageRole.Assistant,
                Content = "Chào bạn! Với ngân sách 500k, xưởng Tràng An có set bánh rất đẹp.",
                TokensUsed = 280,
                ModelUsed = "gemini-flash-latest",
                CreatedAt = DateTime.UtcNow,
                RecommendedProducts = new List<AiProductRecommendationDto>
                {
                    new()
                    {
                        ProductId = Guid.NewGuid(),
                        ProductName = "Hộp Bát Bửu Thượng Hạng",
                        ShopName = "Xưởng Tràng An",
                        BasePrice = 480000m,
                        MinOrderQty = 10,
                        ReputationBadgeTitle = "Đối tác Vàng"
                    }
                }
            });

        using var client = CreateClient("Customer");
        var response = await client.PostAsJsonAsync("/api/v1/ai/chat/messages", new SendAiMessageRequest
        {
            SessionToken = "aic-session-token-123",
            Content = "Tư vấn set quà 500k",
            TargetBudget = 500000m
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Chào bạn", body);
        Assert.Contains("Hộp Bát Bửu Thượng Hạng", body);
    }

    [Fact]
    public async Task GetHistory_Returns200_WithTurnList()
    {
        aiChatService.Setup(s => s.GetHistoryAsync("aic-session-token-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiConversationHistoryResponse
            {
                ConversationId = Guid.NewGuid(),
                SessionToken = "aic-session-token-123",
                ConversationType = AiConversationType.ProductInfo,
                Messages =
                [
                    new AiHistoryItemDto
                    {
                        MessageId = Guid.NewGuid(),
                        Role = AiMessageRole.User,
                        Content = "Tư vấn bánh 500k",
                        CreatedAt = DateTime.UtcNow.AddMinutes(-2)
                    },
                    new AiHistoryItemDto
                    {
                        MessageId = Guid.NewGuid(),
                        Role = AiMessageRole.Assistant,
                        Content = "Chào bạn! Đây là gợi ý set bánh.",
                        CreatedAt = DateTime.UtcNow.AddMinutes(-1)
                    }
                ]
            });

        using var client = CreateClient("Customer");
        var response = await client.GetAsync("/api/v1/ai/chat/sessions/aic-session-token-123/history");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(200, json.GetProperty("status").GetInt32());
        Assert.Equal(2, json.GetProperty("data").GetProperty("messages").GetArrayLength());
    }

    [Fact]
    public async Task DeleteSession_Returns200()
    {
        aiChatService.Setup(s => s.EndSessionAsync("aic-session-token-123", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        using var client = CreateClient("Customer");
        var response = await client.DeleteAsync("/api/v1/ai/chat/sessions/aic-session-token-123");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GenerateReport_Admin_Returns201Created()
    {
        aiAnalyticsService.Setup(s => s.GenerateReportAsync(AdminUserId, true, It.IsAny<GenerateAiAnalyticsReportRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiAnalyticsReportResponse
            {
                ReportId = ReportId,
                ReportType = AiReportType.SalesTrend,
                Status = AiReportStatus.Completed,
                CreatedAt = DateTime.UtcNow,
                Result = new AiReportResultDto
                {
                    ExecutiveSummary = "Doanh thu toàn sàn tăng trưởng 18.4% trong tháng 9.",
                    KeyHighlights = ["Dòng bánh VIP doanh nghiệp chiếm 65% GMV"]
                }
            });

        using var client = CreateClient("Admin");
        var response = await client.PostAsJsonAsync("/api/v1/ai/analytics/generate", new GenerateAiAnalyticsReportRequest
        {
            ReportType = AiReportType.SalesTrend
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(201, json.GetProperty("status").GetInt32());
        Assert.Equal(ReportId.ToString(), json.GetProperty("data").GetProperty("reportId").GetString());
    }

    [Fact]
    public async Task GenerateReport_Customer_Returns403Forbidden()
    {
        using var client = CreateClient("Customer");
        var response = await client.PostAsJsonAsync("/api/v1/ai/analytics/generate", new GenerateAiAnalyticsReportRequest
        {
            ReportType = AiReportType.SalesTrend
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ListReports_Supplier_Returns200()
    {
        aiAnalyticsService.Setup(s => s.ListReportsAsync(SupplierUserId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new AiAnalyticsReportResponse
                {
                    ReportId = ReportId,
                    ReportType = AiReportType.SupplierPerformance,
                    Status = AiReportStatus.Completed,
                    CreatedAt = DateTime.UtcNow
                }
            ]);

        using var client = CreateClient("Supplier");
        var response = await client.GetAsync("/api/v1/ai/analytics/reports");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(200, json.GetProperty("status").GetInt32());
        Assert.Equal(1, json.GetProperty("data").GetArrayLength());
    }

    [Fact]
    public async Task ListReports_Customer_Returns403Forbidden()
    {
        using var client = CreateClient("Customer");
        var response = await client.GetAsync("/api/v1/ai/analytics/reports");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetReportDetails_Admin_Returns200()
    {
        aiAnalyticsService.Setup(s => s.GetReportDetailsAsync(AdminUserId, true, ReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiAnalyticsReportResponse
            {
                ReportId = ReportId,
                ReportType = AiReportType.DemandForecast,
                Status = AiReportStatus.Completed,
                CreatedAt = DateTime.UtcNow,
                Result = new AiReportResultDto
                {
                    ExecutiveSummary = "Dự báo nhu cầu đặt bánh tăng 35% trong tuần lễ cao điểm.",
                    KeyHighlights = ["Khuyến nghị nhà xưởng chuẩn bị tồn kho trứng muối"]
                }
            });

        using var client = CreateClient("Admin");
        var response = await client.GetAsync($"/api/v1/ai/analytics/reports/{ReportId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(200, json.GetProperty("status").GetInt32());
        Assert.Equal(ReportId.ToString(), json.GetProperty("data").GetProperty("reportId").GetString());
    }

    [Fact]
    public async Task GetReportDetails_Customer_Returns403Forbidden()
    {
        using var client = CreateClient("Customer");
        var response = await client.GetAsync($"/api/v1/ai/analytics/reports/{ReportId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetDashboardSummary_Returns200_ForAdmin()
    {
        aiAnalyticsService.Setup(s => s.GetDashboardSummaryAsync(It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiDashboardSummaryResponse
            {
                TotalGmv = 48500000000m,
                GmvGrowthRate = 18.4m,
                TotalOrders = 142,
                TotalContracts = 58,
                OnTimeDeliveryRate = 98.2m,
                RfqWinRate = 76.5m
            });

        using var client = CreateClient("Admin");
        var response = await client.GetAsync("/api/v1/ai/analytics/dashboard-summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("48500000000", body);
        Assert.Contains("98.2", body);
    }

    [Fact]
    public async Task GetDashboardSummary_Customer_Returns403Forbidden()
    {
        using var client = CreateClient("Customer");
        var response = await client.GetAsync("/api/v1/ai/analytics/dashboard-summary");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
