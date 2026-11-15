namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Reports;

/// <summary>Application service for executive platform reports, conversion funnels, top suppliers, and exports.</summary>
public interface IReportService
{
    Task<AdminDashboardMetricsResponse> GetDashboardMetricsAsync(string timeframe = "this_month", CancellationToken cancellationToken = default);
    Task<ConversionFunnelResponse> GetConversionFunnelAsync(string timeframe = "this_month", CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TopSupplierReportResponse>> GetTopSuppliersAsync(CancellationToken cancellationToken = default);
    Task<ReportExportFileResponse> ExportContractsCsvAsync(string format = "csv", CancellationToken cancellationToken = default);
}
