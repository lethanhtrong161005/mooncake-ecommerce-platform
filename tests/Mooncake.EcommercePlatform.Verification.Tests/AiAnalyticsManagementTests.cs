namespace Mooncake.EcommercePlatform.Verification.Tests;

using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Ai.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Ai.Responses;
using Mooncake.EcommercePlatform.Application.Services.Implementations;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Xunit;

public sealed class AiAnalyticsManagementTests
{
    private readonly Mock<IAiAnalyticsReportRepository> reportRepo = new();
    private readonly Mock<IGeminiGateway> geminiGateway = new();

    private readonly AiAnalyticsService analyticsService;
    private readonly Guid requestedBy = Guid.NewGuid();
    private readonly Guid reportId = Guid.NewGuid();

    public AiAnalyticsManagementTests()
    {
        analyticsService = new AiAnalyticsService(
            reportRepo.Object,
            geminiGateway.Object,
            NullLogger<AiAnalyticsService>.Instance);
    }

    [Fact]
    public async Task GenerateReportAsync_ReturnsCachedReport_WhenRecentReportExistsWithin15Minutes()
    {
        var cachedResult = new AiReportResultDto
        {
            ExecutiveSummary = "Doanh thu B2B đạt 50 tỷ VND với tốc độ tăng trưởng vượt bậc.",
            KeyHighlights = new List<string> { "Dòng bánh nướng truyền thống dẫn đầu thị phần" },
            MetricsSummary = new AiReportMetricsSummaryDto { TotalGmv = 50000000000m }
        };

        var cachedEntity = new AiAnalyticsReport
        {
            Id = reportId,
            RequestedBy = requestedBy,
            ReportType = AiReportType.SalesTrend,
            Status = AiReportStatus.Completed,
            ResultData = JsonSerializer.Serialize(cachedResult),
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            CompletedAt = DateTime.UtcNow.AddMinutes(-5)
        };

        reportRepo.Setup(r => r.FindRecentReportAsync(requestedBy, AiReportType.SalesTrend, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedEntity);

        var response = await analyticsService.GenerateReportAsync(requestedBy, true, new GenerateAiAnalyticsReportRequest
        {
            ReportType = AiReportType.SalesTrend
        });

        Assert.NotNull(response);
        Assert.Equal(reportId, response.ReportId);
        Assert.Equal("Doanh thu B2B đạt 50 tỷ VND với tốc độ tăng trưởng vượt bậc.", response.Result?.ExecutiveSummary);
        geminiGateway.Verify(g => g.GenerateContentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GenerateReportAsync_AggregatesMetrics_AndCallsGemini_WhenNoCache()
    {
        reportRepo.Setup(r => r.FindRecentReportAsync(requestedBy, AiReportType.SalesTrend, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiAnalyticsReport?)null);

        var metrics = new AiReportMetricsSummaryDto
        {
            TotalGmv = 48500000000m,
            GmvGrowthRate = 18.4m,
            TotalOrders = 142,
            TotalContracts = 58,
            OnTimeDeliveryRate = 98.2m,
            RfqWinRate = 76.5m
        };

        reportRepo.Setup(r => r.AggregateMetricsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        var geminiJson = """
            {
              "executiveSummary": "Thị trường B2B ghi nhận sức mua tăng trưởng ổn định trong mùa cao điểm.",
              "keyHighlights": ["Doanh thu tăng 18.4%", "Tỷ lệ thầu RFQ đạt 76.5%"],
              "riskAlerts": [{"severity": "Medium", "description": "Công suất lò nướng cao", "potentialImpact": "Chậm đơn phát sinh"}],
              "strategicRecommendations": [{"priority": "High", "action": "Đẩy mạnh combo trà cao cấp", "expectedOutcome": "Tăng AOV 15%"}]
            }
            """;

        geminiGateway.Setup(g => g.GenerateContentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(geminiJson);

        reportRepo.Setup(r => r.CreateReportAsync(It.IsAny<AiAnalyticsReport>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiAnalyticsReport r, CancellationToken _) => r);

        var response = await analyticsService.GenerateReportAsync(requestedBy, true, new GenerateAiAnalyticsReportRequest
        {
            ReportType = AiReportType.SalesTrend
        });

        Assert.NotNull(response);
        Assert.NotNull(response.Result);
        Assert.Equal("Thị trường B2B ghi nhận sức mua tăng trưởng ổn định trong mùa cao điểm.", response.Result.ExecutiveSummary);
        Assert.Equal(48500000000m, response.Result.MetricsSummary.TotalGmv);
    }

    [Fact]
    public async Task GenerateReportAsync_FallsBackGracefully_WhenGeminiFails()
    {
        reportRepo.Setup(r => r.FindRecentReportAsync(requestedBy, AiReportType.SalesTrend, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiAnalyticsReport?)null);

        var metrics = new AiReportMetricsSummaryDto
        {
            TotalGmv = 30000000000m,
            GmvGrowthRate = 12.0m,
            TotalOrders = 100,
            TotalContracts = 40,
            OnTimeDeliveryRate = 97.0m,
            RfqWinRate = 70.0m
        };

        reportRepo.Setup(r => r.AggregateMetricsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        geminiGateway.Setup(g => g.GenerateContentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty); // Gemini down / empty

        reportRepo.Setup(r => r.CreateReportAsync(It.IsAny<AiAnalyticsReport>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiAnalyticsReport r, CancellationToken _) => r);

        var response = await analyticsService.GenerateReportAsync(requestedBy, true, new GenerateAiAnalyticsReportRequest
        {
            ReportType = AiReportType.SalesTrend
        });

        Assert.NotNull(response);
        Assert.NotNull(response.Result);
        Assert.Contains("Thị trường B2B ghi nhận sức mua", response.Result.ExecutiveSummary);
        Assert.NotEmpty(response.Result.StrategicRecommendations);
    }

    [Fact]
    public async Task GetReportDetailsAsync_ThrowsNotFound_WhenReportDoesNotExist()
    {
        reportRepo.Setup(r => r.GetByIdAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiAnalyticsReport?)null);

        await Assert.ThrowsAsync<HttpException>(() =>
            analyticsService.GetReportDetailsAsync(requestedBy, true, reportId));
    }

    [Fact]
    public async Task GetReportDetailsAsync_ThrowsForbidden_WhenSupplierAccessesOthersReport()
    {
        var otherUser = Guid.NewGuid();
        reportRepo.Setup(r => r.GetByIdAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiAnalyticsReport
            {
                Id = reportId,
                RequestedBy = otherUser,
                Status = AiReportStatus.Completed
            });

        await Assert.ThrowsAsync<HttpException>(() =>
            analyticsService.GetReportDetailsAsync(requestedBy, false, reportId));
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_ReturnsAggregatedMetrics()
    {
        reportRepo.Setup(r => r.GetDashboardSummaryMetricsAsync(It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiDashboardSummaryResponse
            {
                TotalGmv = 48500000000m,
                GmvGrowthRate = 18.4m,
                TotalOrders = 142,
                TotalContracts = 58,
                OnTimeDeliveryRate = 98.2m,
                RfqWinRate = 76.5m,
                ActiveProductsCount = 24,
                ActiveSuppliersCount = 12
            });

        var summary = await analyticsService.GetDashboardSummaryAsync(null);

        Assert.NotNull(summary);
        Assert.Equal(48500000000m, summary.TotalGmv);
        Assert.Equal(142, summary.TotalOrders);
        Assert.Equal(98.2m, summary.OnTimeDeliveryRate);
    }
}

