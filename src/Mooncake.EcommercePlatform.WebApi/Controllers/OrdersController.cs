namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.DTOs.Orders.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Order placement, price calculation, multi-shop checkout, and status lifecycle endpoints.</summary>
[Route("api/v1/orders")]
public class OrdersController(IOrderService orderService) : BaseApiController
{
    /// <summary>Calculates item line totals, promotion discounts, and required deposits across shops before checkout.</summary>
    [HttpPost("calculate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CalculateAsync([FromBody] CalculateOrderRequest request, CancellationToken cancellationToken)
    {
        var calculation = await orderService.CalculateAsync(request, cancellationToken);
        return Success(calculation, "Order calculations completed successfully.");
    }

    /// <summary>Submits cart checkout with automatic multi-shop order splitting and stock deduction.</summary>
    [HttpPost("checkout")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckoutAsync([FromBody] CheckoutOrderRequest request, CancellationToken cancellationToken)
    {
        var checkoutResult = await orderService.CheckoutAsync(request, cancellationToken);
        return Created(checkoutResult, "Order(s) placed successfully.");
    }

    /// <summary>Lists orders with optional filters by customer, shop, or status.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrdersAsync([FromQuery] long? customerId, [FromQuery] long? shopId, [FromQuery] OrderStatus? status, CancellationToken cancellationToken)
    {
        var orders = await orderService.GetOrdersAsync(customerId, shopId, status, cancellationToken);
        return Success(orders, "Orders retrieved successfully.");
    }

    /// <summary>Gets single order details with item breakdowns.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        var order = await orderService.GetOrderByIdAsync(id, cancellationToken);
        return Success(order, "Order details retrieved successfully.");
    }

    /// <summary>Updates order status along its lifecycle (Pending -> Confirmed -> Preparing -> Shipping -> Delivered -> Completed / Cancelled).</summary>
    [HttpPut("{id:long}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatusAsync(long id, [FromBody] UpdateOrderStatusRequest request, CancellationToken cancellationToken)
    {
        var order = await orderService.UpdateStatusAsync(id, request, cancellationToken);
        return Success(order, "Order status updated successfully.");
    }

    /// <summary>Confirms deposit payment for high-value or bulk orders in AwaitingDeposit status.</summary>
    [HttpPost("{id:long}/deposit")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PayDepositAsync(long id, [FromBody] PayDepositRequest request, CancellationToken cancellationToken)
    {
        var order = await orderService.PayDepositAsync(id, request, cancellationToken);
        return Success(order, "Deposit payment confirmed successfully.");
    }
}
