namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Wishlist.Persistence;
using Mooncake.EcommercePlatform.Application.DTOs.Wishlist.Responses;

/// <summary>Persistence boundary for customer product wishlist storage and cursor queries.</summary>
public interface IWishlistRepository
{
    Task<WishlistPageResultDto> GetPageAsync(
        Guid userId,
        WishlistCursorDto? after,
        int pageSize,
        CancellationToken ct = default);

    Task<ToggleWishlistResponse> ToggleAsync(
        Guid userId,
        Guid productId,
        DateTime nowUtc,
        CancellationToken ct = default);

    Task<IReadOnlyList<Guid>> GetSavedProductIdsAsync(
        Guid userId,
        IReadOnlyList<Guid> productIds,
        CancellationToken ct = default);
}

