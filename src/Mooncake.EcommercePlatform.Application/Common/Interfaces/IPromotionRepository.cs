namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Promotions.Models;
using Mooncake.EcommercePlatform.Application.DTOs.Promotions.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Persistence operations for supplier-owned promotions.</summary>
public interface IPromotionRepository
{
    Task<Guid?> GetVerifiedShopIdAsync(Guid supplierUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PromotionResponse>> GetForShopAsync(Guid shopId, CancellationToken cancellationToken = default);
    Task<PromotionData?> GetAsync(Guid promotionId, CancellationToken cancellationToken = default);
    Task<bool> ProductBelongsToShopAsync(Guid productId, Guid shopId, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(Guid shopId, string name, Guid? exceptPromotionId = null, CancellationToken cancellationToken = default);
    Task<bool> HasOverlappingRangeAsync(Guid shopId, Guid? productId, int minQty, int? maxQty,
        DateTime? startAtUtc, DateTime? endAtUtc, Guid? exceptPromotionId = null,
        CancellationToken cancellationToken = default);
    Task SaveAsync(PromotionRule promotion, CancellationToken cancellationToken = default);
}
