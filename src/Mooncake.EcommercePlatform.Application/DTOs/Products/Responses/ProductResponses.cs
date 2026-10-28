namespace Mooncake.EcommercePlatform.Application.DTOs.Products.Responses;

using Mooncake.EcommercePlatform.Domain.Enums;

public sealed record ProductPageResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
public sealed record ProductVariantOwnerResponse(Guid Id, string Name, string Sku, string? CrustType, string? Filling,
    string? SizeLabel, int? WeightGram, bool IsGiftSet, decimal PriceAdjustment, decimal Price, int StockQty,
    long InventoryVersion, Guid? ImageAssetId, string? ImageUrl);
public sealed record ProductImageResponse(Guid Id, Guid? MediaAssetId, string ImageUrl, bool IsPrimary, int SortOrder);
public sealed record ProductOwnerResponse(Guid ProductId, Guid ShopId, long Version, string Name, string? Description, string? Ingredients,
    Guid CategoryId, decimal BasePrice, int MinOrderQty, int? MaxOrderQty, string Unit,
    bool SupportsCustomPackaging, bool IsPublished, ProductStatus EffectiveStatus,
    IReadOnlyList<ProductVariantOwnerResponse> Variants, IReadOnlyList<ProductImageResponse> Images,
    IReadOnlyList<string> CompletenessIssues, DateTime CreatedAtUtc, DateTime UpdatedAtUtc, bool IdempotentReplay = false);
public sealed record ProductOwnerCardResponse(Guid ProductId, long Version, string Name, Guid CategoryId,
    string? PrimaryImageUrl, decimal MinPrice, decimal MaxPrice, ProductStatus EffectiveStatus,
    int VariantCount, int TotalStockQty, DateTime UpdatedAtUtc);
public sealed record InventoryAdjustmentResponse(Guid MovementId, Guid ProductId, Guid VariantId, int PreviousStockQty,
    int Delta, int CurrentStockQty, long InventoryVersion, long ProductVersion, ProductStatus EffectiveStatus,
    bool IdempotentReplay = false);
public sealed record DeleteProductResponse(Guid ProductId, long Version, bool Deleted, bool IdempotentReplay = false);

public sealed record ProductShopSummaryResponse(Guid ShopId, string Slug, string Name, decimal ReputationScore,
    bool IsVerified, string? RegionCode);
public sealed record ProductCategorySummaryResponse(Guid Id, string Name);
public sealed record ProductVariantPublicResponse(Guid Id, string Name, string Sku, string? CrustType, string? Filling,
    string? SizeLabel, int? WeightGram, bool IsGiftSet, decimal Price, bool IsAvailable, string? ImageUrl);
public sealed record ProductPublicDetailResponse(Guid ProductId, string Name, string? Description, string? Ingredients,
    ProductCategorySummaryResponse Category, ProductShopSummaryResponse Shop, decimal BasePrice, int MinOrderQty,
    int? MaxOrderQty, string Unit, bool SupportsCustomPackaging, ProductStatus EffectiveStatus,
    IReadOnlyList<ProductVariantPublicResponse> Variants, IReadOnlyList<ProductImageResponse> Images,
    decimal? AverageRating, int ReviewCount, DateTime PublishedAtUtc);
public sealed record ProductSearchCardResponse(Guid ProductId, string Name, ProductShopSummaryResponse Shop,
    ProductCategorySummaryResponse Category, string? PrimaryImageUrl, decimal MinPrice, decimal MaxPrice,
    int MinOrderQty, string Unit, bool SupportsCustomPackaging, ProductStatus EffectiveStatus,
    decimal? AverageRating, int ReviewCount, DateTime PublishedAtUtc);
public sealed record ProductFacetValueResponse(string Value, string Label, int Count);
public sealed record ProductSearchFacetsResponse(IReadOnlyList<ProductFacetValueResponse> Categories,
    IReadOnlyList<ProductFacetValueResponse> Regions, decimal? MinPrice, decimal? MaxPrice,
    int CustomPackagingCount, int InStockCount);
public sealed record ProductSearchResponse(IReadOnlyList<ProductSearchCardResponse> Items, int Page, int PageSize,
    int TotalCount, ProductSearchFacetsResponse Facets, string AppliedSort, DateTime ProjectionAsOfUtc);
public sealed record ProductAutocompleteResponse(Guid TargetId, string Type, string Label, string? SecondaryLabel, string TargetPath);
public sealed record CategoryCatalogResponse(Guid Id, Guid? ParentId, string Name, string? Description,
    string? IconUrl, int SortOrder);
public sealed record RegionCatalogResponse(string Code, string Label);
