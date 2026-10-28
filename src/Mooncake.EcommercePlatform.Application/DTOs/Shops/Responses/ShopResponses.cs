namespace Mooncake.EcommercePlatform.Application.DTOs.Shops.Responses;

using Mooncake.EcommercePlatform.Application.DTOs.Shops.Persistence;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Requests;
using Mooncake.EcommercePlatform.Domain.Enums;

public sealed record ShopPageResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
public sealed record ShopCursorPageResponse<T>(IReadOnlyList<T> Items, bool HasMore, string? NextCursor);
public sealed record ShopAppearanceResponse(Guid TemplateId, Guid TemplateRevisionId, string TemplateCode,
    string TemplateName, string RendererKey, int SchemaVersion, int Revision, bool IsSelectable, bool IsPremium,
    ThemeOverridesRequest Defaults, ThemeOverridesRequest Overrides, ThemeOverridesRequest EffectiveTokens,
    TemplateLayoutRequest Layout, long Version);
public sealed record OwnerShopResponse(Guid ShopId, Guid SupplierId, long Version, string Name, string Slug,
    string? Description, ShopStatus Status, Guid? LogoAssetId, Guid? BannerAssetId, string? LogoUrl,
    string? BannerUrl, string? PublicContactEmail, string? PublicContactPhone, string? PublicAddress, string? RegionCode,
    string? SuspensionReason, ShopAppearanceResponse Appearance, IReadOnlyList<string> AllowedActions,
    bool IdempotentReplay = false);
public sealed record PublicShopResponse(Guid ShopId, string Name, string Slug, string? Description,
    string? LogoUrl, string? BannerUrl, string? PublicContactEmail, string? PublicContactPhone,
    string? PublicAddress, string? RegionCode, bool IsVerified, decimal ReputationScore, int ReviewCount, decimal? AverageRating,
    ShopAppearanceResponse Appearance);
public sealed record OwnerShopPreviewResponse(OwnerShopResponse Shop, IReadOnlyList<ShopProductCardResponse> Products);
public sealed record ShopProductCardResponse(Guid Id, string Name, Guid CategoryId, string? ImageUrl,
    decimal MinPrice, decimal MaxPrice, bool IsInStock, int MinOrderQty, DateTime CreatedAtUtc);
public sealed record ShopReviewResponse(Guid Id, short? OverallRating, string? Comment, DateTime CreatedAtUtc);
public sealed record ShopReviewStatsDto(int Count, decimal? Average);
public sealed record ShopCategoryResponse(Guid Id, string Name);
public sealed record AdminShopSummaryResponse(Guid ShopId, Guid SupplierId, string Name, string Slug,
    ShopStatus Status, long Version, int ProductCount, DateTime CreatedAtUtc);
public sealed record ShopMediaResponse(Guid MediaAssetId, string Purpose, string Status, string? Url,
    string? ContentType, long ByteCount, int Width, int Height);
public sealed record ShopTemplateResponse(Guid Id, string Code, string Name, string? Description,
    long Version, bool IsActive, bool IsPremium, int SortOrder, Guid? PreviewAssetId, string? PreviewImageUrl,
    TemplateRevisionDto? CurrentRevision, bool IdempotentReplay = false);
