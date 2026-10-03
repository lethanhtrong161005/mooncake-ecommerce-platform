namespace Mooncake.EcommercePlatform.Application.DTOs.Shops.Responses;

/// <summary>Basic shop projection returned in listings.</summary>
public record ShopResponse(
    long Id,
    long SupplierId,
    string SupplierBusinessName,
    long? TemplateId,
    string? TemplateName,
    string Name,
    string Slug,
    string? Description,
    string? LogoUrl,
    string? BannerUrl,
    bool IsActive,
    DateTime CreatedAtUtc
);
