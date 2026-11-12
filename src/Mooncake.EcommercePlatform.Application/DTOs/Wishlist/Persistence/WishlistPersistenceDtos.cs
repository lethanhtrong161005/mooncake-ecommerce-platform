namespace Mooncake.EcommercePlatform.Application.DTOs.Wishlist.Persistence;

using Mooncake.EcommercePlatform.Application.DTOs.Wishlist.Responses;

/// <summary>Opaque keyset cursor payload bound to a specific user and wishlist record.</summary>
public sealed record WishlistCursorDto(
    Guid UserId,
    Guid WishlistId,
    DateTime SavedAtUtc);

/// <summary>Repository query output containing bounded wishlist rows and total count.</summary>
public sealed record WishlistPageResultDto(
    IReadOnlyList<WishlistItemResponse> Items,
    bool HasMore,
    WishlistCursorDto? NextCursor,
    int TotalCount);

