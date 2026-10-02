namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Application.DTOs.Shops.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Maps Shop entities to response DTOs.</summary>
public class ShopHelper(IProductHelper productHelper, IPromotionRuleHelper promotionRuleHelper) : IShopHelper
{
    public ShopResponse ToResponse(Shop shop) =>
        new(
            shop.Id,
            shop.SupplierId,
            shop.Supplier?.BusinessName ?? string.Empty,
            shop.TemplateId,
            shop.Template?.Name,
            shop.Name,
            shop.Slug,
            shop.Description,
            shop.LogoUrl,
            shop.BannerUrl,
            shop.IsActive,
            shop.CreatedAtUtc
        );

    public ShopDetailResponse ToDetailResponse(Shop shop)
    {
        var products = shop.Products?.Where(p => p.IsActive).Select(productHelper.ToResponse).ToList() ?? [];
        var promoRules = shop.PromotionRules?.Where(r => r.IsActive).Select(promotionRuleHelper.ToResponse).ToList() ?? [];

        return new ShopDetailResponse(
            shop.Id,
            shop.SupplierId,
            shop.Supplier?.BusinessName ?? string.Empty,
            shop.TemplateId,
            shop.Template?.Name,
            shop.Template?.Config,
            shop.TemplateOverrides,
            shop.Name,
            shop.Slug,
            shop.Description,
            shop.LogoUrl,
            shop.BannerUrl,
            shop.IsActive,
            shop.CreatedAtUtc,
            products,
            promoRules
        );
    }
}
