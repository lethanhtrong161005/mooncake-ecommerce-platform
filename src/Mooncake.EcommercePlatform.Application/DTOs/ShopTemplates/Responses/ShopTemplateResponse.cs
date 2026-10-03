namespace Mooncake.EcommercePlatform.Application.DTOs.ShopTemplates.Responses;

/// <summary>Shop template projection returned to callers.</summary>
public record ShopTemplateResponse(
    long Id,
    string Name,
    string? Description,
    string? PreviewUrl,
    string Config,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc
);
