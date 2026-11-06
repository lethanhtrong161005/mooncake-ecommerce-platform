namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.DTOs;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Admin;
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
            .Join(context.SupplierProfiles.AsNoTracking().Where(profile => !profile.IsDeleted), row => row.SupplierId, profile => profile.Id,
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
            await context.SupplierProfiles.CountAsync(profile => !profile.IsDeleted && profile.CurrentSubmissionId != null &&
                profile.VerificationStatus == SupplierVerificationStatus.Pending, cancellationToken),
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

    public async Task<PagedResult<AuditLog>> GetAuditLogsPageAsync(AuditLogPageQueryRequest query,
        CancellationToken cancellationToken = default)
    {
        IQueryable<AuditLog> rows = context.AuditLogs.AsNoTracking().Where(log => !log.IsDeleted);
        if (!string.IsNullOrWhiteSpace(query.Action)) rows = rows.Where(log => log.Action == query.Action);
        if (query.ActorUserId.HasValue) rows = rows.Where(log => log.ActorUserId == query.ActorUserId.Value);
        if (!string.IsNullOrWhiteSpace(query.EntityName)) rows = rows.Where(log => log.EntityName == query.EntityName);
        if (query.From.HasValue) rows = rows.Where(log => log.CreatedAt >= query.From.Value);
        if (query.To.HasValue) rows = rows.Where(log => log.CreatedAt <= query.To.Value);
        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var term = $"%{query.Q.Trim()}%";
            rows = rows.Where(log => EF.Functions.ILike(log.Action, term)
                || EF.Functions.ILike(log.EntityName, term));
        }
        var total = await rows.CountAsync(cancellationToken);
        rows = query.Sort == "createdAtAsc"
            ? rows.OrderBy(log => log.CreatedAt).ThenBy(log => log.Id)
            : rows.OrderByDescending(log => log.CreatedAt).ThenByDescending(log => log.Id);
        var items = await PagedQueryHelper.ApplyPaging(rows, query).ToListAsync(cancellationToken);
        return new PagedResult<AuditLog>(items, query.Page, query.PageSize, total);
    }

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

    public async Task<IReadOnlyList<ContractDepositRule>> GetContractDepositRulesAsync(CancellationToken cancellationToken = default) =>
        await context.ContractDepositRules.AsNoTracking().Where(rule => !rule.IsDeleted)
            .OrderBy(rule => rule.MinContractValue).ToListAsync(cancellationToken);

    public Task<ContractDepositRule?> GetContractDepositRuleAsync(Guid ruleId, CancellationToken cancellationToken = default) =>
        context.ContractDepositRules.FirstOrDefaultAsync(rule => rule.Id == ruleId && !rule.IsDeleted, cancellationToken);

    public async Task SaveContractDepositRuleAndAuditAsync(ContractDepositRule rule, AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        if (rule.Id == Guid.Empty) context.ContractDepositRules.Add(rule);
        else context.ContractDepositRules.Update(rule);
        context.AuditLogs.Add(auditLog);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
