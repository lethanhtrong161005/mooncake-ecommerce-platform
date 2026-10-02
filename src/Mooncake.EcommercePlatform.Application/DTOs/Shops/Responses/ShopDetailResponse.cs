namespace Mooncake.EcommercePlatform.Application.DTOs.Shops.Responses;

using Mooncake.EcommercePlatform.Application.DTOs.Products.Responses;
using Mooncake.EcommercePlatform.Application.DTOs.Promotions.Responses;

/// <summary>Detailed shop storefront projection returned to customers browsing the shop.</summary>
public record ShopDetailResponse(
    long Id,
    long SupplierId,
    string SupplierBusinessName,
    long? TemplateId,
    string? TemplateName,
    string? TemplateConfig,
    string TemplateOverrides,
    string Name,
    string Slug,
    string? Description,
    string? LogoUrl,
    string? BannerUrl,
    bool IsActive,
    DateTime CreatedAtUtc,
    List<ProductResponse> Products,
    List<PromotionRuleResponse> PromotionRules
);
