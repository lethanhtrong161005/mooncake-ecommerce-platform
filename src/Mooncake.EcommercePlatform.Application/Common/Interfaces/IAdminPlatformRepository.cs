namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Domain.Entities;

public interface IAdminPlatformRepository
{
    Task<AdminPlatformMetrics> GetMetricsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SystemConfig>> GetSystemConfigsAsync(CancellationToken cancellationToken = default);
    Task<SystemConfig?> GetSystemConfigAsync(string key, CancellationToken cancellationToken = default);
    Task SaveSystemConfigAsync(SystemConfig config, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLog>> GetAuditLogsAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<User?> GetUserForUpdateAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> IsSupplierVerifiedAsync(Guid userId, CancellationToken cancellationToken = default);
    Task SaveUserAndAuditAsync(User user, AuditLog auditLog, CancellationToken cancellationToken = default);
    Task SaveAuditAsync(AuditLog auditLog, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Shop>> GetShopsAsync(CancellationToken cancellationToken = default);
    Task<Shop?> GetShopForUpdateAsync(Guid shopId, CancellationToken cancellationToken = default);
    Task<int> GetShopProductCountAsync(Guid shopId, CancellationToken cancellationToken = default);
    Task SaveShopAsync(Shop shop, CancellationToken cancellationToken = default);
}

public sealed record AdminTopSupplierMetric(Guid SupplierId, string? CompanyName, decimal SalesAmount);
public sealed record AdminTopProductMetric(Guid ProductId, string ProductName, int UnitsSold, decimal SalesAmount);
public sealed record AdminPlatformMetrics(int TotalUsers, int ActiveUsers, int PendingSuppliers, int ActiveShops,
    int TotalOrders, decimal PaidGmv, int PendingPayments, int DisputedContracts,
    IReadOnlyList<AdminTopSupplierMetric> TopSuppliers, IReadOnlyList<AdminTopProductMetric> TopProducts);
