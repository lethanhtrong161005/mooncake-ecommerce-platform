namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Application.DTOs.Products.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Maps Product entities and related children to response DTOs.</summary>
public class ProductHelper(IPromotionRuleHelper promotionRuleHelper) : IProductHelper
{
    public ProductResponse ToResponse(Product product)
    {
        var activeVariants = product.ProductVariants?.Where(v => v.IsActive).ToList() ?? [];
        var minPrice = activeVariants.Count != 0 ? activeVariants.Min(v => v.Price) : 0;
        var maxPrice = activeVariants.Count != 0 ? activeVariants.Max(v => v.Price) : 0;
        var primaryImage = product.ProductImages?.FirstOrDefault(img => img.IsPrimary)?.Url
                           ?? product.ProductImages?.OrderBy(img => img.SortOrder).FirstOrDefault()?.Url;

        return new ProductResponse(
            product.Id,
            product.ShopId,
            product.Shop?.Name ?? string.Empty,
            product.CategoryId,
            product.Category?.Name,
            product.Name,
            product.Description,
            product.CustomPackagingFee,
            product.IsActive,
            minPrice,
            maxPrice,
            primaryImage,
            activeVariants.Count,
            product.CreatedAtUtc
        );
    }

    public ProductDetailResponse ToDetailResponse(Product product)
    {
        var variants = product.ProductVariants?.Select(ToVariantResponse).ToList() ?? [];
        var images = product.ProductImages?.OrderBy(img => img.SortOrder).Select(ToImageResponse).ToList() ?? [];
        var promoRules = product.PromotionRules?.Where(r => r.IsActive).Select(promotionRuleHelper.ToResponse).ToList() ?? [];

        return new ProductDetailResponse(
            product.Id,
            product.ShopId,
            product.Shop?.Name ?? string.Empty,
            product.Shop?.Slug ?? string.Empty,
            product.CategoryId,
            product.Category?.Name,
            product.Name,
            product.Description,
            product.CustomPackagingFee,
            product.IsActive,
            product.CreatedAtUtc,
            variants,
            images,
            promoRules
        );
    }

    public ProductVariantResponse ToVariantResponse(ProductVariant variant) =>
        new(
            variant.Id,
            variant.ProductId,
            variant.Sku,
            variant.Name,
            variant.Price,
            variant.StockQuantity,
            variant.MinOrderQuantity,
            variant.IsActive
        );

    public ProductImageResponse ToImageResponse(ProductImage image) =>
        new(
            image.Id,
            image.ProductId,
            image.Url,
            image.SortOrder,
            image.IsPrimary
        );
}
