namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.DTOs.Orders.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;

/// <summary>Customer and supplier bulk order endpoints.</summary>
[Authorize]
[Route("api/v1/orders")]
public sealed class OrdersController(IOrderService orderService) : BaseApiController
{
    /// <summary>Places a bulk order and reserves its variant inventory.</summary>
    [Authorize(Roles = "Customer")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync([FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromBody] CreateOrderRequest request, CancellationToken cancellationToken) =>
        Created(await orderService.CreateAsync(GetCurrentUserId(), idempotencyKey, request, cancellationToken), "Order placed successfully.");

    /// <summary>Returns orders placed by the authenticated customer.</summary>
    [Authorize(Roles = "Customer")]
    [HttpGet("mine")]
    public async Task<IActionResult> GetCustomerOrdersAsync(CancellationToken cancellationToken) =>
        Success(await orderService.GetMyOrdersAsync(GetCurrentUserId(), false, cancellationToken), "Orders retrieved successfully.");

    /// <summary>Returns orders received by the authenticated supplier.</summary>
    [Authorize(Policy = "SupplierExistingFulfillment")]
    [HttpGet("supplier")]
    public async Task<IActionResult> GetSupplierOrdersAsync(CancellationToken cancellationToken) =>
        Success(await orderService.GetMyOrdersAsync(GetCurrentUserId(), true, cancellationToken), "Supplier orders retrieved successfully.");

    /// <summary>Returns an order visible to the authenticated customer or supplier.</summary>
    [Authorize(Roles = "Customer,Supplier,Admin")]
    [HttpGet("{orderId:guid}")]
    public async Task<IActionResult> GetAsync(Guid orderId, CancellationToken cancellationToken) =>
        Success(await orderService.GetAsync(GetCurrentUserId(), User.IsInRole("Supplier") || User.HasClaim("supplier_fulfillment", "true"), orderId,
            User.IsInRole("Admin"), cancellationToken), "Order retrieved successfully.");

    /// <summary>Cancels a pending customer order and releases its inventory reservation.</summary>
    [Authorize(Roles = "Customer")]
    [HttpPost("{orderId:guid}/cancel")]
    public async Task<IActionResult> CancelAsync(Guid orderId, [FromBody] CancelOrderRequest request, CancellationToken cancellationToken) =>
        Success(await orderService.CancelAsync(GetCurrentUserId(), orderId, request, cancellationToken), "Order cancelled successfully.");

    /// <summary>Confirms a pending order received by the authenticated supplier.</summary>
    [Authorize(Roles = "Supplier")]
    [HttpPost("{orderId:guid}/confirm")]
    public async Task<IActionResult> ConfirmAsync(Guid orderId, CancellationToken cancellationToken) =>
        Success(await orderService.ConfirmAsync(GetCurrentUserId(), orderId, cancellationToken), "Order confirmed successfully.");

    /// <summary>Rejects a pending order and releases its reserved inventory.</summary>
    [Authorize(Roles = "Supplier")]
    [HttpPost("{orderId:guid}/reject")]
    public async Task<IActionResult> RejectAsync(Guid orderId, [FromBody] CancelOrderRequest request, CancellationToken cancellationToken) =>
        Success(await orderService.RejectAsync(GetCurrentUserId(), orderId, request, cancellationToken), "Order rejected successfully.");

    /// <summary>Moves a confirmed order into production.</summary>
    [Authorize(Policy = "SupplierExistingFulfillment")]
    [HttpPost("{orderId:guid}/processing")]
    public async Task<IActionResult> StartProcessingAsync(Guid orderId, CancellationToken cancellationToken) =>
        Success(await orderService.StartProcessingAsync(GetCurrentUserId(), orderId, cancellationToken), "Order processing started.");

    /// <summary>Marks a processing order as shipped.</summary>
    [Authorize(Policy = "SupplierExistingFulfillment")]
    [HttpPost("{orderId:guid}/ship")]
    public async Task<IActionResult> MarkShippedAsync(Guid orderId, CancellationToken cancellationToken) =>
        Success(await orderService.MarkShippedAsync(GetCurrentUserId(), orderId, cancellationToken), "Order marked as shipped.");

    /// <summary>Confirms receipt of a shipped order.</summary>
    [Authorize(Roles = "Customer")]
    [HttpPost("{orderId:guid}/delivery/confirm")]
    public async Task<IActionResult> ConfirmDeliveryAsync(Guid orderId, CancellationToken cancellationToken) =>
        Success(await orderService.ConfirmDeliveryAsync(GetCurrentUserId(), orderId, cancellationToken), "Order delivery confirmed.");

    private Guid GetCurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
