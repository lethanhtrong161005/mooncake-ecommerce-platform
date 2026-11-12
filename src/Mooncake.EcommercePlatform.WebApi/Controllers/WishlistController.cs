namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.DTOs.Wishlist.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.WebApi.Extensions;

/// <summary>Customer product wishlist endpoints supporting cursor pagination and batch checks.</summary>
[Authorize]
[Route("api/v1/wishlist")]
public sealed class WishlistController(IWishlistService wishlistService) : BaseApiController
{
    /// <summary>Returns a cursor-paginated page of products saved by the current user.</summary>
    [HttpGet]
    public async Task<IActionResult> GetPageAsync(
        [FromQuery] WishlistPageQuery query,
        CancellationToken cancellationToken = default) =>
        Success(
            await wishlistService.GetPageAsync(User.GetRequiredUserId(), query, cancellationToken),
            WishlistConstants.ListedSuccess);

    /// <summary>Toggles a product in the authenticated user's wishlist (adds or removes atomically).</summary>
    [HttpPost("{productId:guid}")]
    public async Task<IActionResult> ToggleAsync(
        [FromRoute, NonEmptyGuid(ErrorMessage = WishlistConstants.InvalidProductId)] Guid productId,
        CancellationToken cancellationToken = default)
    {
        var result = await wishlistService.ToggleAsync(User.GetRequiredUserId(), productId, cancellationToken);
        return Success(
            result,
            result.IsSaved ? WishlistConstants.AddedSuccess : WishlistConstants.RemovedSuccess);
    }

    /// <summary>Batch-checks which of the requested product IDs are currently saved by the user.</summary>
    [HttpGet("check")]
    public async Task<IActionResult> CheckBatchAsync(
        [FromQuery] WishlistCheckQuery query,
        CancellationToken cancellationToken = default) =>
        Success(
            await wishlistService.CheckBatchAsync(User.GetRequiredUserId(), query, cancellationToken),
            WishlistConstants.BatchCheckedSuccess);
}
