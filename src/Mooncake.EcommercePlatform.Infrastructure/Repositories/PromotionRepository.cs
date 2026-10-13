namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

/// <summary>EF Core persistence for supplier-owned promotions.</summary>
public sealed class PromotionRepository(ApplicationDbContext context) : IPromotionRepository
{
    public Task<Shop?> GetVerifiedShopAsync(Guid supplierUserId, CancellationToken cancellationToken = default) =>
        (from shop in context.Shops.AsNoTracking()
         join profile in context.SupplierProfiles.AsNoTracking() on shop.SupplierId equals profile.Id
         where profile.UserId == supplierUserId && profile.VerificationStatus == SupplierVerificationStatus.Verified
               && shop.Status == ShopStatus.Active && !profile.IsDeleted && !shop.IsDeleted
         select shop).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<PromotionRule>> GetForShopAsync(Guid shopId, CancellationToken cancellationToken = default) =>
        await context.PromotionRules.AsNoTracking().Where(promotion => promotion.ShopId == shopId && !promotion.IsDeleted)
            .OrderByDescending(promotion => promotion.CreatedAt).ToListAsync(cancellationToken);

    public Task<PromotionRule?> GetAsync(Guid promotionId, CancellationToken cancellationToken = default) =>
        context.PromotionRules.FirstOrDefaultAsync(promotion => promotion.Id == promotionId && !promotion.IsDeleted, cancellationToken);

    public Task<bool> ProductBelongsToShopAsync(Guid productId, Guid shopId, CancellationToken cancellationToken = default) =>
        context.Products.AnyAsync(product => product.Id == productId && product.ShopId == shopId && !product.IsDeleted, cancellationToken);

    public Task<bool> NameExistsAsync(Guid shopId, string name, Guid? exceptPromotionId = null, CancellationToken cancellationToken = default) =>
        context.PromotionRules.AnyAsync(promotion => promotion.ShopId == shopId && promotion.Name.ToLower() == name.ToLower()
            && !promotion.IsDeleted && (exceptPromotionId == null || promotion.Id != exceptPromotionId), cancellationToken);

    public async Task SaveAsync(PromotionRule promotion, CancellationToken cancellationToken = default)
    {
        if (promotion.Id == Guid.Empty)
            context.PromotionRules.Add(promotion);
        else
            context.PromotionRules.Update(promotion);
        await context.SaveChangesAsync(cancellationToken);
    }
}
