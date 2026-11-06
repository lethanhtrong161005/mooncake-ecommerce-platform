namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.DTOs.Orders.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.WebApi.Filters;

/// <summary>Customer, supplier, and administrator bulk order workflow endpoints.</summary>
[Authorize]
[OrderApi]
[Route("api/v1/orders")]
public sealed class OrdersController(IOrderService orderService) : BaseApiController
{
    /// <summary>Calculates an authoritative bulk price quote without creating an order.</summary>
    [AllowAnonymous]
    [HttpPost("quote-preview")]
    public async Task<IActionResult> PreviewQuoteAsync([FromBody] OrderQuotePreviewRequest request,
        CancellationToken cancellationToken) =>
        Success(await orderService.PreviewQuoteAsync(request, cancellationToken),
            Application.Common.Constant.B2BOrderingMessages.QuoteCalculated);

    /// <summary>Alias endpoint for authoritative checkout price and policy preview.</summary>
    [AllowAnonymous]
    [HttpPost("preview")]
    public async Task<IActionResult> PreviewAsync([FromBody] OrderQuotePreviewRequest request,
        CancellationToken cancellationToken) =>
        Success(await orderService.PreviewQuoteAsync(request, cancellationToken),
            Application.Common.Constant.B2BOrderingMessages.QuoteCalculated);

    /// <summary>Returns the shop's public bulk order deposit, VAT, and regional shipping policy.</summary>
    [AllowAnonymous]
    [HttpGet("shops/{shopId:guid}/policy")]
    public async Task<IActionResult> GetShopPolicyAsync(Guid shopId,
        [FromServices] IDirectBulkOrderService enterpriseService, CancellationToken cancellationToken) =>
        Success(await enterpriseService.GetShopOrderPolicyAsync(shopId, cancellationToken),
            "Shop bulk order policy retrieved successfully.");

    /// <summary>Places a bulk order and reserves its variant inventory atomically.</summary>
    [Authorize(Roles = "Customer")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync([FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromBody] CreateOrderRequest request, CancellationToken cancellationToken) =>
        Created(await orderService.CreateAsync(GetCurrentUserId(), idempotencyKey, request, cancellationToken), "Order placed successfully.");

    /// <summary>Returns orders placed by the authenticated customer.</summary>
    [Authorize(Roles = "Customer")]
    [HttpGet("mine")]
    public async Task<IActionResult> GetCustomerOrdersAsync([FromQuery] OrderPageQueryRequest query,
        CancellationToken cancellationToken) =>
        Success<object>(Request.Query.ContainsKey("page")
            ? await HttpContext.RequestServices.GetRequiredService<IDirectBulkOrderService>()
                .GetOrdersPageAsync(GetCurrentUserId(), false, query, cancellationToken)
            : await orderService.GetMyOrdersAsync(GetCurrentUserId(), false, cancellationToken),
            "Orders retrieved successfully.");

    /// <summary>Returns cursor-paginated orders placed by the authenticated customer.</summary>
    [Authorize(Roles = "Customer")]
    [HttpGet("mine/paged")]
    public async Task<IActionResult> GetCustomerOrdersPagedAsync([FromQuery] OrderPageQueryRequest query,
        [FromServices] IDirectBulkOrderService enterpriseService, CancellationToken cancellationToken) =>
        Success(await enterpriseService.GetOrdersPageAsync(GetCurrentUserId(), false, query, cancellationToken),
            "Orders retrieved successfully.");

    /// <summary>Returns orders received by the authenticated supplier.</summary>
    [Authorize(Policy = "SupplierExistingFulfillment")]
    [HttpGet("supplier")]
    public async Task<IActionResult> GetSupplierOrdersAsync([FromQuery] OrderPageQueryRequest query,
        CancellationToken cancellationToken) =>
        Success<object>(Request.Query.ContainsKey("page")
            ? await HttpContext.RequestServices.GetRequiredService<IDirectBulkOrderService>()
                .GetOrdersPageAsync(GetCurrentUserId(), true, query, cancellationToken)
            : await orderService.GetMyOrdersAsync(GetCurrentUserId(), true, cancellationToken),
            "Supplier orders retrieved successfully.");

    /// <summary>Returns cursor-paginated orders received by the authenticated supplier.</summary>
    [Authorize(Policy = "SupplierExistingFulfillment")]
    [HttpGet("supplier/paged")]
    public async Task<IActionResult> GetSupplierOrdersPagedAsync([FromQuery] OrderPageQueryRequest query,
        [FromServices] IDirectBulkOrderService enterpriseService, CancellationToken cancellationToken) =>
        Success(await enterpriseService.GetOrdersPageAsync(GetCurrentUserId(), true, query, cancellationToken),
            "Supplier orders retrieved successfully.");

    [Authorize(Policy = "SupplierExistingFulfillment")]
    [HttpGet("supplier/dashboard-summary")]
    public async Task<IActionResult> GetSupplierDashboardSummaryAsync(
        [FromServices] IDirectBulkOrderService enterpriseService, CancellationToken cancellationToken) =>
        Success(await enterpriseService.GetSupplierOrderDashboardSummaryAsync(GetCurrentUserId(), cancellationToken),
            "Supplier order dashboard summary retrieved successfully.");

    /// <summary>Returns an order visible to the authenticated customer, supplier, or administrator.</summary>
    [Authorize(Roles = "Customer,Supplier,Admin")]
    [HttpGet("{orderId:guid}")]
    public async Task<IActionResult> GetAsync(Guid orderId, CancellationToken cancellationToken) =>
        Success(await orderService.GetAsync(GetCurrentUserId(), IsSupplierActor(), orderId,
            User.IsInRole("Admin"), cancellationToken), "Order retrieved successfully.");

    /// <summary>Cancels a pending or unpaid confirmed customer order and releases its inventory reservation.</summary>
    [Authorize(Roles = "Customer")]
    [HttpPost("{orderId:guid}/cancel")]
    public async Task<IActionResult> CancelAsync(Guid orderId, [FromBody] CancelOrderRequest request, CancellationToken cancellationToken) =>
        Success(await orderService.CancelAsync(GetCurrentUserId(), orderId, request, cancellationToken), "Order cancelled successfully.");

    /// <summary>Confirms a pending order received by the authenticated supplier and generates its payment schedule.</summary>
    [Authorize(Policy = "SupplierExistingFulfillment")]
    [HttpPost("{orderId:guid}/confirm")]
    public async Task<IActionResult> ConfirmAsync(Guid orderId, CancellationToken cancellationToken) =>
        Success(await orderService.ConfirmAsync(GetCurrentUserId(), orderId, cancellationToken), "Order confirmed successfully.");

    /// <summary>Rejects a pending order and releases its reserved inventory.</summary>
    [Authorize(Policy = "SupplierExistingFulfillment")]
    [HttpPost("{orderId:guid}/reject")]
    public async Task<IActionResult> RejectAsync(Guid orderId, [FromBody] CancelOrderRequest request, CancellationToken cancellationToken) =>
        Success(await orderService.RejectAsync(GetCurrentUserId(), orderId, request, cancellationToken), "Order rejected successfully.");

    /// <summary>Proposes an alternate delivery date when the requested date cannot be met within capacity.</summary>
    [Authorize(Policy = "SupplierExistingFulfillment")]
    [HttpPost("{orderId:guid}/propose-delivery-date")]
    public async Task<IActionResult> ProposeDeliveryDateAsync(Guid orderId, [FromBody] ProposeOrderDeliveryDateRequest request,
        [FromServices] IDirectBulkOrderService enterpriseService, CancellationToken cancellationToken) =>
        Success(await enterpriseService.ProposeDeliveryDateAsync(GetCurrentUserId(), orderId, request, cancellationToken),
            "Alternate delivery date proposed successfully.");

    /// <summary>Approves or declines a supplier's proposed delivery date.</summary>
    [Authorize(Roles = "Customer")]
    [HttpPost("{orderId:guid}/delivery-proposal/respond")]
    public async Task<IActionResult> RespondDeliveryProposalAsync(Guid orderId, [FromBody] RespondOrderDeliveryProposalRequest request,
        [FromServices] IDirectBulkOrderService enterpriseService, CancellationToken cancellationToken) =>
        Success(await enterpriseService.RespondDeliveryProposalAsync(GetCurrentUserId(), orderId, request, cancellationToken),
            request.Approve ? "Proposed delivery date approved." : "Order cancelled after declining delivery date proposal.");

    /// <summary>Submits a cancellation request according to the enterprise order cancellation matrix.</summary>
    [Authorize(Roles = "Customer,Supplier")]
    [HttpPost("{orderId:guid}/cancellation-requests")]
    public async Task<IActionResult> RequestCancellationAsync(Guid orderId, [FromBody] CreateOrderCancellationRequest request,
        [FromServices] IDirectBulkOrderService enterpriseService, CancellationToken cancellationToken) =>
        Success(await enterpriseService.RequestCancellationAsync(GetCurrentUserId(), IsSupplierActor(), orderId, request, cancellationToken),
            "Cancellation request processed.");

    /// <summary>Approves or rejects a pending mutual cancellation request.</summary>
    [Authorize(Roles = "Customer,Supplier,Admin")]
    [HttpPost("{orderId:guid}/cancellation-requests/{requestId:guid}/respond")]
    public async Task<IActionResult> RespondCancellationRequestAsync(Guid orderId, Guid requestId,
        [FromBody] RespondOrderCancellationRequest request, [FromServices] IDirectBulkOrderService enterpriseService,
        CancellationToken cancellationToken) =>
        Success(await enterpriseService.RespondCancellationRequestAsync(GetCurrentUserId(), IsSupplierActor(),
            User.IsInRole("Admin"), orderId, requestId, request, cancellationToken),
            "Cancellation request decision recorded.");

    /// <summary>Moves a confirmed order into production once pre-production payment and packaging gates are satisfied.</summary>
    [Authorize(Policy = "SupplierExistingFulfillment")]
    [HttpPost("{orderId:guid}/processing")]
    public async Task<IActionResult> StartProcessingAsync(Guid orderId, CancellationToken cancellationToken) =>
        Success(await orderService.StartProcessingAsync(GetCurrentUserId(), orderId, cancellationToken), "Order processing started.");

    /// <summary>Marks a processing order as shipped once 100% of required payment obligations are settled.</summary>
    [Authorize(Policy = "SupplierExistingFulfillment")]
    [HttpPost("{orderId:guid}/ship")]
    public async Task<IActionResult> MarkShippedAsync(Guid orderId, CancellationToken cancellationToken) =>
        Success(await orderService.MarkShippedAsync(GetCurrentUserId(), orderId, cancellationToken), "Order marked as shipped.");

    /// <summary>Generates a VNPay redirect URL for a specific order payment part (capped at 100,000,000 VND per part).</summary>
    [Authorize(Roles = "Customer")]
    [HttpPost("{orderId:guid}/payment-parts/{partId:guid}/vnpay-url")]
    public async Task<IActionResult> CreatePartVnPayUrlAsync(Guid orderId, Guid partId,
        [FromBody] CreateOrderPartVnPayUrlRequest? request, [FromServices] IDirectBulkOrderService enterpriseService,
        CancellationToken cancellationToken) =>
        Success(await enterpriseService.CreatePartVnPayUrlAsync(GetCurrentUserId(), orderId, partId,
            request ?? new CreateOrderPartVnPayUrlRequest(), HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken),
            "VNPay payment URL generated for order payment part.");

    /// <summary>Returns the order's payment schedule, 100M-capped parts, and production/shipping gates.</summary>
    [Authorize(Roles = "Customer,Supplier,Admin")]
    [HttpGet("{orderId:guid}/payment-summary")]
    public async Task<IActionResult> GetPaymentSummaryAsync(Guid orderId,
        [FromServices] IDirectBulkOrderService enterpriseService, CancellationToken cancellationToken) =>
        Success(await enterpriseService.GetPaymentSummaryAsync(GetCurrentUserId(), IsSupplierActor(),
            User.IsInRole("Admin"), orderId, cancellationToken),
            "Order payment summary retrieved successfully.");

    /// <summary>Returns the order's simulated platform ledger (Held, Disputed, Refunded, Eligible, PaidOutSimulated).</summary>
    [Authorize(Roles = "Customer,Supplier,Admin")]
    [HttpGet("{orderId:guid}/financial-summary")]
    public async Task<IActionResult> GetFinancialSummaryAsync(Guid orderId,
        [FromServices] IDirectBulkOrderService enterpriseService, CancellationToken cancellationToken) =>
        Success(await enterpriseService.GetFinancialSummaryAsync(GetCurrentUserId(), IsSupplierActor(),
            User.IsInRole("Admin"), orderId, cancellationToken),
            "Order financial summary retrieved successfully.");

    [Authorize(Roles = "Customer,Supplier,Admin")]
    [HttpGet("{orderId:guid}/financial-ledger")]
    public async Task<IActionResult> GetFinancialLedgerAsync(Guid orderId,
        [FromQuery] OrderLedgerPageQueryRequest query,
        [FromServices] IDirectBulkOrderService enterpriseService, CancellationToken cancellationToken) =>
        Success(await enterpriseService.GetOrderLedgerPageAsync(GetCurrentUserId(), IsSupplierActor(),
            User.IsInRole("Admin"), orderId, query, cancellationToken),
            "Order financial ledger retrieved successfully.");

    /// <summary>Confirms receipt of a shipped order after Proof-of-Delivery photos are uploaded.</summary>
    [Authorize(Roles = "Customer")]
    [HttpPost("{orderId:guid}/delivery/confirm")]
    public async Task<IActionResult> ConfirmDeliveryAsync(Guid orderId, CancellationToken cancellationToken) =>
        Success(await orderService.ConfirmDeliveryAsync(GetCurrentUserId(), orderId, cancellationToken), "Order delivery confirmed.");

    /// <summary>Confirms receipt of a shipped order with optional shipment reference and notes, unlocking Held funds to Eligible.</summary>
    [Authorize(Roles = "Customer")]
    [HttpPost("{orderId:guid}/delivery/confirm-receipt")]
    public async Task<IActionResult> ConfirmDeliveryReceiptAsync(Guid orderId,
        [FromBody] ConfirmOrderDeliveryReceiptRequest? request, [FromServices] IDirectBulkOrderService enterpriseService,
        CancellationToken cancellationToken) =>
        Success(await enterpriseService.ConfirmDeliveryReceiptAsync(GetCurrentUserId(), orderId, request, cancellationToken),
            "Order delivery confirmed and funds unlocked to eligible balance.");

    /// <summary>Reports a delivery issue (missing, damaged, or wrong items) and freezes payout eligibility.</summary>
    [Authorize(Roles = "Customer")]
    [HttpPost("{orderId:guid}/delivery/dispute")]
    public async Task<IActionResult> ReportDeliveryDisputeAsync(Guid orderId,
        [FromBody] ReportOrderDeliveryDisputeRequest request, [FromServices] IDirectBulkOrderService enterpriseService,
        CancellationToken cancellationToken) =>
        Success(await enterpriseService.ReportDeliveryDisputeAsync(GetCurrentUserId(), orderId, request, cancellationToken),
            "Delivery dispute submitted and order funds locked for administrator review.");

    /// <summary>Submits a customer refund claim for a cancelled, expired, or disputed order.</summary>
    [Authorize(Roles = "Customer")]
    [HttpPost("{orderId:guid}/refund-requests")]
    public async Task<IActionResult> RequestRefundAsync(Guid orderId,
        [FromBody] CreateOrderRefundRequest request, [FromServices] IDirectBulkOrderService enterpriseService,
        CancellationToken cancellationToken) =>
        Success(await enterpriseService.RequestRefundAsync(GetCurrentUserId(), orderId, request, cancellationToken),
            "Refund claim submitted successfully.");

    /// <summary>Submits a supplier simulated payout request for an order whose Held balance has unlocked to Eligible.</summary>
    [Authorize(Policy = "SupplierExistingFulfillment")]
    [HttpPost("{orderId:guid}/payout-requests")]
    public async Task<IActionResult> CreatePayoutRequestAsync(Guid orderId,
        [FromBody] CreateOrderPayoutRequest? request, [FromServices] IDirectBulkOrderService enterpriseService,
        CancellationToken cancellationToken) =>
        Success(await enterpriseService.CreatePayoutRequestAsync(GetCurrentUserId(), orderId,
            request ?? new CreateOrderPayoutRequest(), cancellationToken),
            "Simulated payout request submitted successfully.");

    private bool IsSupplierActor() => User.IsInRole("Supplier") || User.HasClaim("supplier_fulfillment", "true");

    private Guid GetCurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
