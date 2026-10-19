namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.DTOs.Reviews.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;

[Route("api/v1/reviews")]
public sealed class ReviewsController(IReviewService reviewService) : BaseApiController
{
    [Authorize(Roles = "Customer")]
    [HttpPost]
    public async Task<IActionResult> CreateTransactionReviewAsync([FromBody] CreateReviewRequest request,
        CancellationToken cancellationToken) =>
        Created(await reviewService.CreateTransactionReviewAsync(CurrentUserId, request, cancellationToken),
            "Review created successfully.");

    [AllowAnonymous]
    [HttpGet("suppliers/{supplierId:guid}")]
    public async Task<IActionResult> GetSupplierReviewsAsync(Guid supplierId, CancellationToken cancellationToken) =>
        Success(await reviewService.GetSupplierReviewsAsync(supplierId, cancellationToken), "Supplier reviews retrieved successfully.");

    [Authorize(Roles = "Supplier")]
    [HttpPost("{reviewId:guid}/reply")]
    public async Task<IActionResult> ReplyAsync(Guid reviewId, [FromBody] ReplyToReviewRequest request,
        CancellationToken cancellationToken)
    {
        await reviewService.ReplyToReviewAsync(CurrentUserId, reviewId, request.Reply, cancellationToken);
        return Success("Reply saved successfully.");
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{reviewId:guid}/moderation")]
    public async Task<IActionResult> ModerateAsync(Guid reviewId, [FromBody] ModerateReviewRequest request,
        CancellationToken cancellationToken)
    {
        await reviewService.ModerateReviewAsync(CurrentUserId, reviewId, request, cancellationToken);
        return Success("Review moderation saved successfully.");
    }

    private Guid CurrentUserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
        ? id : throw new UnauthorizedAccessException("A valid user identity is required.");
}

[Route("api/v1/products/{productId:guid}/reviews")]
public sealed class ProductReviewsController(IReviewService reviewService) : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAsync(Guid productId, CancellationToken cancellationToken) =>
        Success(await reviewService.GetProductReviewsAsync(productId, CurrentUserIdOrNull, cancellationToken),
            "Product reviews retrieved successfully.");

    [Authorize(Roles = "Customer")]
    [HttpPost]
    public async Task<IActionResult> CreateAsync(Guid productId, [FromBody] CreateProductReviewRequest request,
        CancellationToken cancellationToken)
    {
        await reviewService.AddProductReviewAsync(CurrentUserId, productId, request, cancellationToken);
        return Success("Product review created successfully.");
    }

    [Authorize(Roles = "Customer")]
    [HttpPut("{reviewId:guid}/like")]
    public async Task<IActionResult> LikeAsync(Guid productId, Guid reviewId, CancellationToken cancellationToken)
    {
        await reviewService.SetProductReviewLikeAsync(CurrentUserId, reviewId, true, cancellationToken);
        return Success("Product review liked successfully.");
    }

    [Authorize(Roles = "Customer")]
    [HttpDelete("{reviewId:guid}/like")]
    public async Task<IActionResult> UnlikeAsync(Guid productId, Guid reviewId, CancellationToken cancellationToken)
    {
        await reviewService.SetProductReviewLikeAsync(CurrentUserId, reviewId, false, cancellationToken);
        return Success("Product review unliked successfully.");
    }

    [Authorize(Roles = "Supplier")]
    [HttpPost("{reviewId:guid}/reply")]
    public async Task<IActionResult> ReplyAsync(Guid productId, Guid reviewId, [FromBody] ReplyToReviewRequest request,
        CancellationToken cancellationToken)
    {
        await reviewService.ReplyToProductReviewAsync(CurrentUserId, reviewId, request.Reply, cancellationToken);
        return Success("Reply saved successfully.");
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{reviewId:guid}/moderation")]
    public async Task<IActionResult> ModerateAsync(Guid productId, Guid reviewId,
        [FromBody] ModerateReviewRequest request, CancellationToken cancellationToken)
    {
        await reviewService.ModerateProductReviewAsync(CurrentUserId, reviewId, request, cancellationToken);
        return Success("Product review moderation saved successfully.");
    }

    private Guid CurrentUserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
        ? id : throw new UnauthorizedAccessException("A valid user identity is required.");

    private Guid? CurrentUserIdOrNull => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
        ? id : null;
}
