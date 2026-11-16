namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.DTOs.Orders.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.WebApi.Filters;

/// <summary>Supplier self-service configuration for bulk order policies, payout bank accounts, and simulated financial ledgers.</summary>
[Authorize(Policy = "SupplierExistingFulfillment")]
[OrderApi]
[Route("api/v1/suppliers/me")]
public sealed class SupplierOrderOperationsController(IDirectBulkOrderService enterpriseService) : BaseApiController
{
    /// <summary>Returns the supplier shop's deposit, VAT, and regional shipping rate configuration.</summary>
    [HttpGet("order-policy")]
    public async Task<IActionResult> GetOrderPolicyAsync([FromQuery] Guid? shopId, CancellationToken cancellationToken) =>
        Success(await enterpriseService.GetMyShopOrderPolicyAsync(CurrentUserId, shopId, cancellationToken),
            "Supplier bulk order policy retrieved successfully.");

    /// <summary>Updates the supplier shop's deposit, VAT, and regional shipping rate configuration.</summary>
    [HttpPut("order-policy")]
    public async Task<IActionResult> UpsertOrderPolicyAsync([FromBody] UpsertShopOrderPolicyRequest request,
        CancellationToken cancellationToken) =>
        Success(await enterpriseService.UpsertMyShopOrderPolicyAsync(CurrentUserId, request, cancellationToken),
            "Supplier bulk order policy updated successfully.");

    /// <summary>Returns the supplier's configured bank account for simulated order payouts.</summary>
    [HttpGet("payout-account")]
    public async Task<IActionResult> GetPayoutAccountAsync(CancellationToken cancellationToken) =>
        Success(await enterpriseService.GetMyPayoutAccountAsync(CurrentUserId, cancellationToken),
            "Supplier payout account retrieved successfully.");

    /// <summary>Creates or updates the supplier's bank account for simulated order payouts.</summary>
    [HttpPut("payout-account")]
    public async Task<IActionResult> UpsertPayoutAccountAsync([FromBody] UpsertSupplierPayoutAccountRequest request,
        CancellationToken cancellationToken) =>
        Success(await enterpriseService.UpsertMyPayoutAccountAsync(CurrentUserId, request, cancellationToken),
            "Supplier payout account saved successfully.");

    /// <summary>Returns the supplier's aggregated simulated order balances (Held, Disputed, Refunded, Eligible, PaidOutSimulated).</summary>
    [HttpGet("financial-overview")]
    public async Task<IActionResult> GetFinancialOverviewAsync(CancellationToken cancellationToken) =>
        Success(await enterpriseService.GetMyFinancialOverviewAsync(CurrentUserId, cancellationToken),
            "Supplier financial overview retrieved successfully.");

    /// <summary>Submits a supplier rebuttal for a reported delivery dispute within 48h.</summary>
    [HttpPost("orders/{orderId:guid}/delivery-disputes/{disputeId:guid}/rebuttal")]
    public async Task<IActionResult> SubmitDeliveryDisputeRebuttalAsync(
        [FromRoute] Guid orderId,
        [FromRoute] Guid disputeId,
        [FromBody] SubmitSupplierDeliveryDisputeRebuttalRequest request,
        CancellationToken cancellationToken) =>
        Success(await enterpriseService.SubmitDeliveryDisputeRebuttalAsync(CurrentUserId, orderId, disputeId, request, cancellationToken),
            "Supplier dispute rebuttal submitted successfully.");

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

