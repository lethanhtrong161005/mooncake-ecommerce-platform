namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.DTOs.Reviews.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;

[Route("api/v1/products")]
public sealed class ProductReviewsController(IReviewService reviewService) : BaseApiController
{
    [AllowAnonymous]
    [HttpGet("{productId:guid}/reviews")]
    public async Task<IActionResult> GetReviewsFeedAsync(
        Guid productId,
        [FromQuery] Guid? variantId,
        [FromQuery] int? rating,
        [FromQuery] bool? hasMedia,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = "most_helpful",
        CancellationToken ct = default)
    {
        Guid? currentUserId = null;
        if (User.Identity?.IsAuthenticated == true && Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsedUserId))
        {
            currentUserId = parsedUserId;
        }

        var result = await reviewService.GetProductReviewsFeedAsync(productId, currentUserId, variantId, rating, hasMedia, pageNumber, pageSize, sortBy, ct);
        return Success(result, "Lấy luồng đánh giá sản phẩm thành công.");
    }

    [HttpGet("{productId:guid}/eligible-order-items")]
    [Authorize]
    public async Task<IActionResult> GetEligibleOrderItemsAsync(Guid productId, CancellationToken ct)
    {
        var result = await reviewService.GetEligibleOrderItemsAsync(CurrentUserId, productId, ct);
        return Success(result, "Lấy danh sách sản phẩm đủ điều kiện đánh giá thành công.");
    }

    [HttpPost("{productId:guid}/reviews")]
    [Authorize]
    public async Task<IActionResult> CreateProductReviewAsync(Guid productId, [FromBody] CreateProductReviewRequest request, CancellationToken ct)
    {
        var result = await reviewService.CreateProductReviewAsync(CurrentUserId, productId, request, ct);
        return Created(result, "Đăng đánh giá sản phẩm thành công.");
    }

    [HttpPost("reviews/{reviewId:guid}/like")]
    [Authorize]
    public async Task<IActionResult> ToggleLikeAsync(Guid reviewId, CancellationToken ct)
    {
        var result = await reviewService.ToggleProductReviewLikeAsync(CurrentUserId, reviewId, ct);
        return Success(result, "Thao tác thích đánh giá thành công.");
    }

    [HttpPost("reviews/{reviewId:guid}/reply")]
    [Authorize(Roles = "Supplier,Admin")]
    public async Task<IActionResult> ReplyProductReviewAsync(Guid reviewId, [FromBody] SupplierReplyRequest request, CancellationToken ct)
    {
        var result = await reviewService.ReplyProductReviewAsync(CurrentUserId, reviewId, request, ct);
        return Success(result, "Gửi phản hồi đánh giá sản phẩm thành công.");
    }

    private Guid CurrentUserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
        ? userId : throw new UnauthorizedAccessException("Yêu cầu phiên làm việc hợp lệ.");
}

