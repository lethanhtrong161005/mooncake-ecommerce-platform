namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Data access contract for PromotionRule aggregate.</summary>
public interface IPromotionRuleRepository
{
    Task<IEnumerable<PromotionRule>> GetByShopIdAsync(long shopId, bool? activeOnly = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<PromotionRule>> GetActiveRulesForProductAsync(long shopId, long? productId, CancellationToken cancellationToken = default);
    Task<PromotionRule?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<PromotionRule> CreateAsync(PromotionRule rule, CancellationToken cancellationToken = default);
    Task<PromotionRule> UpdateAsync(PromotionRule rule, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}
