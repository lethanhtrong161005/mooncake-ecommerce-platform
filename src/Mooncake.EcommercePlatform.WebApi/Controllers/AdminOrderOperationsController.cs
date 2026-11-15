namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Orders.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.WebApi.Filters;

/// <summary>Administrator operations for Direct Bulk Order refund cases, simulated payouts, and delivery disputes.</summary>
[Authorize(Roles = "Admin")]
[OrderApi]
[Route("api/v1/admin/orders")]
public sealed class AdminOrderOperationsController(IDirectBulkOrderService enterpriseService) : BaseApiController
{
    private const long MaxRefundProofBytes = 10 * 1024 * 1024;

    /// <summary>Uploads a manual bank transfer proof receipt image for refund reconciliation.</summary>
    [HttpPost("refund-proof/upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadRefundProofAsync(IFormFile? file,
        [FromServices] ICloudinaryStorageService? cloudinaryStorage, CancellationToken cancellationToken)
    {
        var upload = await UploadProofImageCoreAsync(file, cloudinaryStorage, cancellationToken);
        return Success(new { url = upload.Url, publicId = upload.PublicId }, "Refund transfer proof uploaded successfully.");
    }

    /// <summary>Uploads a manual bank transfer proof receipt image (PoD) for supplier payout reconciliation.</summary>
    [HttpPost("payout-proof/upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadPayoutProofAsync(IFormFile? file,
        [FromServices] ICloudinaryStorageService? cloudinaryStorage, CancellationToken cancellationToken)
    {
        var upload = await UploadProofImageCoreAsync(file, cloudinaryStorage, cancellationToken);
        return Success(new { url = upload.Url, publicId = upload.PublicId }, "Payout transfer proof uploaded successfully.");
    }

    private static async Task<Mooncake.EcommercePlatform.Application.DTOs.Files.Responses.CloudinaryUploadResponse> UploadProofImageCoreAsync(
        IFormFile? file, ICloudinaryStorageService? cloudinaryStorage, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            throw new HttpException(400, "Please select a transfer proof image file.", OrderErrorCodes.ValidationError);
        if (file.Length > MaxRefundProofBytes)
            throw new HttpException(400, "Transfer proof image size must not exceed 10MB.", OrderErrorCodes.ValidationError);
        if (cloudinaryStorage is null)
            throw new HttpException(503, B2BOrderingMessages.LogoStorageUnavailable, OrderErrorCodes.FeatureDisabled);

        await using var stream = file.OpenReadStream();
        return await cloudinaryStorage.UploadImageAsync(stream, file.FileName, false, cancellationToken);
    }
    /// <summary>Returns all pending and recent Direct Bulk Order refund cases, simulated payout requests, and delivery disputes.</summary>
    [HttpGet("operations")]
    public async Task<IActionResult> GetOperationsOverviewAsync(CancellationToken cancellationToken) =>
        Success(await enterpriseService.GetAdminOperationsOverviewAsync(cancellationToken),
            "Administrator bulk order operations retrieved successfully.");

    [HttpGet("operations/summary")]
    public async Task<IActionResult> GetOperationsSummaryAsync(CancellationToken cancellationToken)
    {
        var overview = await enterpriseService.GetAdminOperationsOverviewAsync(cancellationToken);
        return Success(new { overview.SandboxSimulationOnly, overview.PlatformHeldAmount,
            overview.PlatformDisputedAmount, overview.PlatformEligibleAmount, overview.PlatformRefundedSimulatedAmount,
            overview.PlatformPaidOutSimulatedAmount }, "Administrator order finance summary retrieved successfully.");
    }

    [HttpGet("operations/refund-cases")]
    public async Task<IActionResult> GetRefundCasesAsync([FromQuery] AdminOperationPageQueryRequest query,
        CancellationToken cancellationToken) =>
        Success(await enterpriseService.GetAdminRefundCasesPageAsync(query, cancellationToken),
            "Order refund cases retrieved successfully.");

    [HttpGet("operations/payout-requests")]
    public async Task<IActionResult> GetPayoutRequestsAsync([FromQuery] AdminOperationPageQueryRequest query,
        CancellationToken cancellationToken) =>
        Success(await enterpriseService.GetAdminPayoutRequestsPageAsync(query, cancellationToken),
            "Supplier payout requests retrieved successfully.");

    [HttpGet("operations/delivery-disputes")]
    public async Task<IActionResult> GetDeliveryDisputesAsync([FromQuery] AdminOperationPageQueryRequest query,
        CancellationToken cancellationToken) =>
        Success(await enterpriseService.GetAdminDeliveryDisputesPageAsync(query, cancellationToken),
            "Order delivery disputes retrieved successfully.");

    /// <summary>Approves or rejects a sandbox order refund case and updates the financial ledger.</summary>
    [HttpPost("refund-cases/{refundCaseId:guid}/process")]
    public async Task<IActionResult> ProcessRefundCaseAsync(Guid refundCaseId,
        [FromBody] ProcessOrderRefundCaseRequest request, CancellationToken cancellationToken) =>
        Success(await enterpriseService.ProcessRefundCaseAsync(CurrentUserId, refundCaseId, request, cancellationToken),
            "Order refund case processed successfully.");

    /// <summary>Approves or rejects a supplier simulated payout request in sandbox mode.</summary>
    [HttpPost("payout-requests/{payoutRequestId:guid}/process")]
    public async Task<IActionResult> ProcessPayoutRequestAsync(Guid payoutRequestId,
        [FromBody] ProcessOrderPayoutRequest request, CancellationToken cancellationToken) =>
        Success(await enterpriseService.ProcessPayoutRequestAsync(CurrentUserId, payoutRequestId, request, cancellationToken),
            "Simulated payout request processed successfully.");

    /// <summary>Resolves an open delivery dispute by releasing payout, issuing a partial refund, or issuing a full refund.</summary>
    [HttpPost("delivery-disputes/{disputeId:guid}/resolve")]
    public async Task<IActionResult> ResolveDeliveryDisputeAsync(Guid disputeId,
        [FromBody] ResolveOrderDeliveryDisputeRequest request, CancellationToken cancellationToken) =>
        Success(await enterpriseService.ResolveDeliveryDisputeAsync(CurrentUserId, disputeId, request, cancellationToken),
            "Delivery dispute resolved successfully.");

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

