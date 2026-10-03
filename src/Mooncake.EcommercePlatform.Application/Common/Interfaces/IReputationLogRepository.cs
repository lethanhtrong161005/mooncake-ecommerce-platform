namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Data access contract for supplier reputation audit logs.</summary>
public interface IReputationLogRepository
{
    Task<ReputationLog> CreateAsync(ReputationLog log, CancellationToken cancellationToken = default);
    Task<IEnumerable<ReputationLog>> GetBySupplierIdAsync(long supplierId, CancellationToken cancellationToken = default);
}
