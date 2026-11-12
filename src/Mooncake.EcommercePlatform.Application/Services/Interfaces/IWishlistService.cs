namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Wishlist.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Wishlist.Responses;

/// <summary>Application service orchestrating customer product wishlists.</summary>
public interface IWishlistService
{
    Task<WishlistPageResponse> GetPageAsync(
        Guid userId,
        WishlistPageQuery query,
        CancellationToken ct = default);

    Task<ToggleWishlistResponse> ToggleAsync(
        Guid userId,
        Guid productId,
        CancellationToken ct = default);

    Task<WishlistCheckResponse> CheckBatchAsync(
        Guid userId,
        WishlistCheckQuery query,
        CancellationToken ct = default);
}
