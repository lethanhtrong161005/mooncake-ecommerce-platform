namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Wishlist.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Wishlist.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;

/// <summary>Orchestrates customer product wishlist queries, toggles, and cursor protection.</summary>
public sealed class WishlistService(
    IWishlistRepository repository,
    IWishlistCursorProtector cursors,
    IDateTimeProvider clock) : IWishlistService
{
    public async Task<WishlistPageResponse> GetPageAsync(
        Guid userId,
        WishlistPageQuery query,
        CancellationToken ct = default)
    {
        var after = query.Cursor is null
            ? null
            : cursors.Unprotect(userId, query.Cursor.Trim());

        var page = await repository.GetPageAsync(userId, after, query.PageSize, ct);
        var nextCursor = page.HasMore && page.NextCursor is not null
            ? cursors.Protect(page.NextCursor)
            : null;

        return new WishlistPageResponse(page.Items, page.HasMore, nextCursor, page.TotalCount);
    }

    public Task<ToggleWishlistResponse> ToggleAsync(
        Guid userId,
        Guid productId,
        CancellationToken ct = default) =>
        repository.ToggleAsync(userId, productId, clock.UtcNow, ct);

    public async Task<WishlistCheckResponse> CheckBatchAsync(
        Guid userId,
        WishlistCheckQuery query,
        CancellationToken ct = default)
    {
        var savedIds = await repository.GetSavedProductIdsAsync(
            userId,
            query.ToDistinctProductIds(),
            ct);

        return new WishlistCheckResponse(savedIds);
    }
}
