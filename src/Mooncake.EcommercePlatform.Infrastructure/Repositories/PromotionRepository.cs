namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Promotions.Models;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

/// <summary>EF Core persistence for supplier-owned promotions.</summary>
public sealed class PromotionRepository(ApplicationDbContext context) : IPromotionRepository
{
    public Task<Guid?> GetVerifiedShopIdAsync(Guid supplierUserId, CancellationToken cancellationToken = default) =>
        (from shop in context.Shops.AsNoTracking()
         join profile in context.SupplierProfiles.AsNoTracking() on shop.SupplierId equals profile.Id
         where profile.UserId == supplierUserId && profile.VerificationStatus == SupplierVerificationStatus.Verified
               && shop.Status == ShopStatus.Active && !profile.IsDeleted && !shop.IsDeleted
         select (Guid?)shop.Id).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<PromotionData>> GetForShopAsync(Guid shopId, CancellationToken cancellationToken = default) =>
        (await context.PromotionRules.AsNoTracking().Where(promotion => promotion.ShopId == shopId && !promotion.IsDeleted)
            .OrderByDescending(promotion => promotion.CreatedAt).ToListAsync(cancellationToken)).Select(ToData).ToList();

    public async Task<PromotionData?> GetAsync(Guid promotionId, CancellationToken cancellationToken = default) =>
        ToDataOrNull(await context.PromotionRules.AsNoTracking().FirstOrDefaultAsync(promotion => promotion.Id == promotionId && !promotion.IsDeleted, cancellationToken));

    public Task<bool> ProductBelongsToShopAsync(Guid productId, Guid shopId, CancellationToken cancellationToken = default) =>
        context.Products.AnyAsync(product => product.Id == productId && product.ShopId == shopId && !product.IsDeleted, cancellationToken);

    public Task<bool> NameExistsAsync(Guid shopId, string name, Guid? exceptPromotionId = null, CancellationToken cancellationToken = default) =>
        context.PromotionRules.AnyAsync(promotion => promotion.ShopId == shopId && promotion.Name.ToLower() == name.ToLower()
            && !promotion.IsDeleted && (exceptPromotionId == null || promotion.Id != exceptPromotionId), cancellationToken);

    public Task<bool> HasOverlappingRangeAsync(Guid shopId, Guid? productId, int minQty, int? maxQty,
        DateTime? startAtUtc, DateTime? endAtUtc, Guid? exceptPromotionId = null,
        CancellationToken cancellationToken = default)
    {
        var maximumQuantity = maxQty ?? int.MaxValue;
        var start = startAtUtc ?? DateTime.UnixEpoch;
        var end = endAtUtc ?? DateTime.MaxValue;
        return context.PromotionRules.AnyAsync(promotion => promotion.ShopId == shopId &&
            promotion.ProductId == productId && promotion.IsActive && !promotion.IsDeleted &&
            (exceptPromotionId == null || promotion.Id != exceptPromotionId) &&
            promotion.MinQty <= maximumQuantity && (promotion.MaxQty == null || promotion.MaxQty >= minQty) &&
            (promotion.StartDate == null || promotion.StartDate < end) &&
            (promotion.EndDate == null || promotion.EndDate > start), cancellationToken);
    }

    public async Task SaveAsync(PromotionData data, CancellationToken cancellationToken = default)
    {
        var promotion = await context.PromotionRules.FirstOrDefaultAsync(p => p.Id == data.Id, cancellationToken);
        if (promotion is null)
        {
            promotion = new PromotionRule { Id = data.Id, CreatedAt = data.CreatedAtUtc, CreatedBy = data.CreatedBy };
            context.PromotionRules.Add(promotion);
        }
        promotion.ShopId = data.ShopId; promotion.ProductId = data.ProductId; promotion.Name = data.Name;
        promotion.Description = data.Description; promotion.MinQty = data.MinQty; promotion.MaxQty = data.MaxQty;
        promotion.DiscountType = data.DiscountType; promotion.DiscountValue = data.DiscountValue;
        promotion.StartDate = data.StartDateUtc; promotion.EndDate = data.EndDateUtc;
        promotion.IsActive = data.IsActive; promotion.UpdatedBy = data.UpdatedBy; promotion.UpdatedAt = data.UpdatedAtUtc;
        await context.SaveChangesAsync(cancellationToken);
    }

    private static PromotionData ToData(PromotionRule p) => new(p.Id, p.ShopId, p.ProductId, p.Name, p.Description,
        p.MinQty, p.MaxQty, p.DiscountType, p.DiscountValue, p.StartDate, p.EndDate, p.IsActive,
        p.CreatedBy, p.UpdatedBy, p.CreatedAt, p.UpdatedAt);
    private static PromotionData? ToDataOrNull(PromotionRule? p) => p is null ? null : ToData(p);
}
