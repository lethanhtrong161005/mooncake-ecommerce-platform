namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

public sealed class AdminPlatformRepository(ApplicationDbContext context) : IAdminPlatformRepository
{
    public async Task<AdminPlatformMetrics> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        var topSuppliers = await context.Orders.AsNoTracking().Where(order => !order.IsDeleted && order.Status != OrderStatus.Cancelled)
            .Join(context.Shops.AsNoTracking().Where(shop => !shop.IsDeleted), order => order.ShopId, shop => shop.Id,
                (order, shop) => new { shop.SupplierId, order.TotalAmount })
            .GroupBy(row => row.SupplierId).Select(group => new { SupplierId = group.Key, Sales = group.Sum(row => row.TotalAmount) })
            .OrderByDescending(row => row.Sales).Take(5)
            .Join(context.SupplierProfiles.AsNoTracking().Where(profile => !profile.IsDeleted), row => row.SupplierId, profile => profile.UserId,
                (row, profile) => new AdminTopSupplierMetric(row.SupplierId, profile.CompanyName, row.Sales)).ToListAsync(cancellationToken);

        var topProducts = await context.OrderItems.AsNoTracking().Where(item => !item.IsDeleted)
            .Join(context.Orders.AsNoTracking().Where(order => !order.IsDeleted && order.Status != OrderStatus.Cancelled), item => item.OrderId, order => order.Id,
                (item, _) => item)
            .GroupBy(item => new { item.ProductId, item.ProductNameSnapshot })
            .Select(group => new
            {
                ProductId = group.Key.ProductId,
                ProductName = group.Key.ProductNameSnapshot,
                UnitsSold = group.Sum(item => item.Quantity),
                SalesAmount = group.Sum(item => item.Subtotal)
            })
            .OrderByDescending(row => row.SalesAmount).Take(5)
            .Select(row => new AdminTopProductMetric(row.ProductId, row.ProductName, row.UnitsSold, row.SalesAmount))
            .ToListAsync(cancellationToken);

        return new AdminPlatformMetrics(
            await context.Users.CountAsync(user => !user.IsDeleted, cancellationToken),
            await context.Users.CountAsync(user => !user.IsDeleted && user.IsActive, cancellationToken),
            await context.SupplierProfiles.CountAsync(profile => !profile.IsDeleted && profile.VerificationStatus == SupplierVerificationStatus.Pending, cancellationToken),
            await context.Shops.CountAsync(shop => !shop.IsDeleted && shop.Status == ShopStatus.Active, cancellationToken),
            await context.Orders.CountAsync(order => !order.IsDeleted, cancellationToken),
            await context.Payments.Where(payment => !payment.IsDeleted && payment.Status == PaymentStatus.Completed).SumAsync(payment => (decimal?)payment.Amount, cancellationToken) ?? 0m,
            await context.Payments.CountAsync(payment => !payment.IsDeleted && payment.Status == PaymentStatus.Pending, cancellationToken),
            await context.Contracts.CountAsync(contract => !contract.IsDeleted && contract.Status == ContractStatus.Disputed, cancellationToken),
            topSuppliers, topProducts);
    }

    public async Task<IReadOnlyList<SystemConfig>> GetSystemConfigsAsync(CancellationToken cancellationToken = default) =>
        await context.SystemConfigs.AsNoTracking().Where(config => !config.IsDeleted).OrderBy(config => config.Key).ToListAsync(cancellationToken);

    public Task<SystemConfig?> GetSystemConfigAsync(string key, CancellationToken cancellationToken = default) =>
        context.SystemConfigs.FirstOrDefaultAsync(config => config.Key == key && !config.IsDeleted, cancellationToken);

    public async Task SaveSystemConfigAsync(SystemConfig config, CancellationToken cancellationToken = default)
    {
        if (config.Id == Guid.Empty) context.SystemConfigs.Add(config);
        else context.SystemConfigs.Update(config);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLog>> GetAuditLogsAsync(int page, int pageSize, CancellationToken cancellationToken = default) =>
        await context.AuditLogs.AsNoTracking().Where(log => !log.IsDeleted)
            .OrderByDescending(log => log.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

    public Task<User?> GetUserForUpdateAsync(Guid userId, CancellationToken cancellationToken = default) =>
        context.Users.FirstOrDefaultAsync(user => user.Id == userId && !user.IsDeleted, cancellationToken);

    public Task<bool> IsSupplierVerifiedAsync(Guid userId, CancellationToken cancellationToken = default) =>
        context.SupplierProfiles.AnyAsync(profile => profile.UserId == userId && !profile.IsDeleted && profile.VerificationStatus == SupplierVerificationStatus.Verified, cancellationToken);

    public async Task SaveUserAndAuditAsync(User user, AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        context.Users.Update(user);
        context.AuditLogs.Add(auditLog);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task SaveAuditAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        context.AuditLogs.Add(auditLog);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Shop>> GetShopsAsync(CancellationToken cancellationToken = default) =>
        await context.Shops.AsNoTracking().Where(shop => !shop.IsDeleted).OrderBy(shop => shop.Name).ToListAsync(cancellationToken);

    public Task<Shop?> GetShopForUpdateAsync(Guid shopId, CancellationToken cancellationToken = default) =>
        context.Shops.FirstOrDefaultAsync(shop => shop.Id == shopId && !shop.IsDeleted, cancellationToken);

    public Task<int> GetShopProductCountAsync(Guid shopId, CancellationToken cancellationToken = default) =>
        context.Products.CountAsync(product => product.ShopId == shopId && !product.IsDeleted, cancellationToken);

    public async Task SaveShopAsync(Shop shop, CancellationToken cancellationToken = default)
    {
        context.Shops.Update(shop);
        await context.SaveChangesAsync(cancellationToken);
    }
}
