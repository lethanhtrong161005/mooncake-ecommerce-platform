namespace Mooncake.EcommercePlatform.Application.DTOs.Products.Responses;

using Mooncake.EcommercePlatform.Application.DTOs.Promotions.Responses;

/// <summary>Detailed product projection with variants, images, and applicable promotion rules.</summary>
public record ProductDetailResponse(
    long Id,
    long ShopId,
    string ShopName,
    string ShopSlug,
    long? CategoryId,
    string? CategoryName,
    string Name,
    string? Description,
    decimal? CustomPackagingFee,
    bool IsActive,
    DateTime CreatedAtUtc,
    List<ProductVariantResponse> Variants,
    List<ProductImageResponse> Images,
    List<PromotionRuleResponse> PromotionRules
);
