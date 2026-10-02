namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IPromotionRuleRepository"/>.</summary>
public class PromotionRuleRepository(ApplicationDbContext context) : IPromotionRuleRepository
{
    public async Task<IEnumerable<PromotionRule>> GetByShopIdAsync(long shopId, bool? activeOnly = null, CancellationToken cancellationToken = default)
    {
        var query = context.PromotionRules.AsNoTracking().Where(r => r.ShopId == shopId);
        if (activeOnly.HasValue && activeOnly.Value)
        {
            var now = DateTime.UtcNow;
            query = query.Where(r => r.IsActive
                && (!r.StartsAt.HasValue || r.StartsAt.Value <= now)
                && (!r.EndsAt.HasValue || r.EndsAt.Value >= now));
        }

        return await query.OrderBy(r => r.MinQuantity).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PromotionRule>> GetActiveRulesForProductAsync(long shopId, long? productId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await context.PromotionRules
            .AsNoTracking()
            .Where(r => r.ShopId == shopId
                && (r.ProductId == null || r.ProductId == productId)
                && r.IsActive
                && (!r.StartsAt.HasValue || r.StartsAt.Value <= now)
                && (!r.EndsAt.HasValue || r.EndsAt.Value >= now))
            .OrderByDescending(r => r.MinQuantity)
            .ToListAsync(cancellationToken);
    }

    public async Task<PromotionRule?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        await context.PromotionRules.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<PromotionRule> CreateAsync(PromotionRule rule, CancellationToken cancellationToken = default)
    {
        context.PromotionRules.Add(rule);
        await context.SaveChangesAsync(cancellationToken);
        return rule;
    }

    public async Task<PromotionRule> UpdateAsync(PromotionRule rule, CancellationToken cancellationToken = default)
    {
        context.PromotionRules.Update(rule);
        await context.SaveChangesAsync(cancellationToken);
        return rule;
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var rule = await context.PromotionRules.FindAsync([id], cancellationToken);
        if (rule is not null)
        {
            context.PromotionRules.Remove(rule);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
