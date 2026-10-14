namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Persistence operations for supplier-owned promotions.</summary>
public interface IPromotionRepository
{
    Task<Shop?> GetVerifiedShopAsync(Guid supplierUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PromotionRule>> GetForShopAsync(Guid shopId, CancellationToken cancellationToken = default);
    Task<PromotionRule?> GetAsync(Guid promotionId, CancellationToken cancellationToken = default);
    Task<bool> ProductBelongsToShopAsync(Guid productId, Guid shopId, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(Guid shopId, string name, Guid? exceptPromotionId = null, CancellationToken cancellationToken = default);
    Task SaveAsync(PromotionRule promotion, CancellationToken cancellationToken = default);
}
