namespace Mooncake.EcommercePlatform.Application.DTOs.Wishlist.Responses;

using Mooncake.EcommercePlatform.Application.DTOs.Products.Responses;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Saved wishlist product card payload aligned with storefront catalog fields.</summary>
public sealed record WishlistItemResponse(
    Guid WishlistId,
    Guid ProductId,
    string Name,
    string? PrimaryImageUrl,
    decimal BasePrice,
    decimal MinPrice,
    decimal MaxPrice,
    int MinOrderQty,
    string Unit,
    bool SupportsCustomPackaging,
    ProductStatus EffectiveStatus,
    ProductShopSummaryResponse Shop,
    ProductCategorySummaryResponse Category,
    decimal? AverageRating,
    int ReviewCount,
    DateTime SavedAtUtc);

/// <summary>Cursor-paginated wishlist page response supporting Load More UI.</summary>
public sealed record WishlistPageResponse(
    IReadOnlyList<WishlistItemResponse> Items,
    bool HasMore,
    string? NextCursor,
    int TotalCount);

/// <summary>Toggle result indicating the current saved state and total saved count.</summary>
public sealed record ToggleWishlistResponse(
    Guid ProductId,
    bool IsSaved,
    int TotalCount);

/// <summary>Batch lookup response of saved product IDs for the authenticated user.</summary>
public sealed record WishlistCheckResponse(
    IReadOnlyList<Guid> SavedIds);

