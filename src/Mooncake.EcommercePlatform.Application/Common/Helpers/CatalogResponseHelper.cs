namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Application.DTOs.Catalog.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Maps catalog entities to API response records.</summary>
public static class CatalogResponseHelper
{
    public static ProductResponse ToResponse(Product product, IReadOnlyList<ProductVariant> variants, IReadOnlyList<ProductImage> images) =>
        new(product.Id, product.ShopId, product.CategoryId, product.Name, product.Description, product.BasePrice,
            product.MinOrderQty, product.MaxOrderQty, product.Unit, product.Status,
            variants.Select(variant => new ProductVariantResponse(variant.Id, variant.Name, variant.Sku, variant.Flavor,
                variant.Filling, variant.SizeLabel, variant.WeightGram, variant.PriceAdjustment, variant.StockQty, variant.ImageUrl)).ToList(),
            images.Select(image => new ProductImageResponse(image.Id, image.ImageUrl, image.IsPrimary, image.SortOrder)).ToList());

    public static ShopResponse ToResponse(Shop shop) =>
        new(shop.Id, shop.SupplierId, shop.Name, shop.Slug, shop.Description, shop.BannerUrl, shop.LogoUrl, shop.Status);
}
