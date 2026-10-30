namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.DTOs.Reviews.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;

[Route("api/v1/reviews")]
public sealed class ReviewsController(IReviewService reviewService) : BaseApiController
{
    [HttpPost("transaction")]
    [Authorize]
    public async Task<IActionResult> CreateTransactionReviewAsync([FromBody] CreateTransactionReviewRequest request, CancellationToken ct)
    {
        var result = await reviewService.CreateTransactionReviewAsync(CurrentUserId, request, ct);
        return Created(result, "Gửi đánh giá giao dịch thành công.");
    }

    [HttpGet("transaction/eligible-orders")]
    [Authorize]
    public async Task<IActionResult> GetEligibleOrdersForReviewAsync(CancellationToken ct)
    {
        var result = await reviewService.GetEligibleOrdersForReviewAsync(CurrentUserId, ct);
        return Success(result, "Lấy danh sách đơn hàng chờ đánh giá thành công.");
    }

    [HttpPost("transaction/{reviewId:guid}/reply")]
    [Authorize(Roles = "Supplier,Admin")]
    public async Task<IActionResult> ReplyTransactionReviewAsync(Guid reviewId, [FromBody] SupplierReplyRequest request, CancellationToken ct)
    {
        var result = await reviewService.ReplyTransactionReviewAsync(CurrentUserId, reviewId, request, ct);
        return Success(result, "Gửi phản hồi đánh giá thành công.");
    }

    [AllowAnonymous]
    [HttpGet("shops/{slug}")]
    public async Task<IActionResult> GetShopReviewsAsync(string slug, [FromQuery] int? rating, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var result = await reviewService.GetShopReviewsAsync(slug, rating, pageNumber, pageSize, ct);
        return Success(result, "Lấy danh sách đánh giá gian hàng thành công.");
    }

    [AllowAnonymous]
    [HttpGet("suppliers/{supplierId:guid}/reputation")]
    public async Task<IActionResult> GetSupplierReputationAsync(Guid supplierId, CancellationToken ct)
    {
        var result = await reviewService.GetSupplierReputationAsync(supplierId, ct);
        return Success(result, "Lấy thông tin điểm uy tín nhà cung cấp thành công.");
    }

    [HttpPost("admin/suppliers/{supplierId:guid}/reputation/adjustment")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdminAdjustReputationAsync(Guid supplierId, [FromBody] AdminAdjustReputationRequest request, CancellationToken ct)
    {
        var result = await reviewService.AdminAdjustReputationAsync(CurrentUserId, supplierId, request, ct);
        return Success(result, "Điều chỉnh điểm uy tín nhà cung cấp thành công.");
    }

    [HttpPatch("admin/{reviewId:guid}/moderate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ModerateReviewAsync(Guid reviewId, [FromQuery] bool isProductReview, [FromBody] ModerateReviewRequest request, CancellationToken ct)
    {
        var result = await reviewService.ModerateReviewAsync(CurrentUserId, reviewId, isProductReview, request, ct);
        return Success(new { isSuccess = result }, "Kiểm duyệt đánh giá thành công.");
    }

    private Guid CurrentUserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
        ? userId : throw new UnauthorizedAccessException("Yêu cầu phiên làm việc hợp lệ.");
}

