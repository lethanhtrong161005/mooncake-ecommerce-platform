namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Ai.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Repository for AI sales and marketplace analytics reports.</summary>
public interface IAiAnalyticsReportRepository
{
    Task<AiAnalyticsReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AiAnalyticsReport?> FindRecentReportAsync(Guid requestedBy, AiReportType reportType, TimeSpan withinDuration, CancellationToken cancellationToken = default);

    Task<List<AiAnalyticsReport>> ListReportsAsync(Guid requestedBy, AiReportType? reportType, int limit = 20, CancellationToken cancellationToken = default);

    Task<AiAnalyticsReport> CreateReportAsync(AiAnalyticsReport report, CancellationToken cancellationToken = default);

    Task UpdateReportAsync(AiAnalyticsReport report, CancellationToken cancellationToken = default);

    Task<AiReportMetricsSummaryDto> AggregateMetricsAsync(DateTime from, DateTime to, Guid? shopId, CancellationToken cancellationToken = default);

    Task<AiDashboardSummaryResponse> GetDashboardSummaryMetricsAsync(Guid? shopId, CancellationToken cancellationToken = default);
}
