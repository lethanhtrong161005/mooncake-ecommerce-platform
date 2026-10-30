namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Ai.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Ai.Responses;

/// <summary>Service for AI Sales Analytics and Executive Market Intelligence.</summary>
public interface IAiAnalyticsService
{
    Task<AiAnalyticsReportResponse> GenerateReportAsync(Guid requestedBy, bool isAdmin, GenerateAiAnalyticsReportRequest request, CancellationToken cancellationToken = default);

    Task<List<AiAnalyticsReportResponse>> ListReportsAsync(Guid requestedBy, bool isAdmin, CancellationToken cancellationToken = default);

    Task<AiAnalyticsReportResponse> GetReportDetailsAsync(Guid requestedBy, bool isAdmin, Guid reportId, CancellationToken cancellationToken = default);

    Task<AiDashboardSummaryResponse> GetDashboardSummaryAsync(Guid? shopId, CancellationToken cancellationToken = default);
}

