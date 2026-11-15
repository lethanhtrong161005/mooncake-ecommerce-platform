namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Reports.Models;

/// <summary>Data access contract for platform reporting and analytics aggregations.</summary>
public interface IReportRepository
{
    Task<AdminReportSnapshot> GetAdminReportSnapshotAsync(CancellationToken cancellationToken = default);
    Task<ConversionFunnelRawCounts> GetConversionFunnelCountsAsync(CancellationToken cancellationToken = default);
    Task<TopSuppliersReportSnapshot> GetTopSuppliersSnapshotAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ContractExportRow>> GetRecentContractsForExportAsync(int limit, CancellationToken cancellationToken = default);
}
