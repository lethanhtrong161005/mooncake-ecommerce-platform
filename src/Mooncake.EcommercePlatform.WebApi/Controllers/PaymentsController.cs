namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.DTOs.Payments.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;

/// <summary>Payment processing, anti-collision idempotency simulation, milestone/order settlement, and refunds.</summary>
[Route("api/v1/payments")]
public class PaymentsController(IPaymentService paymentService) : BaseApiController
{
    /// <summary>Processes payment for an Order or Contract Milestone with anti-collision idempotency check.</summary>
    [HttpPost("process")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ProcessPaymentAsync([FromBody] PayRequest request, CancellationToken cancellationToken)
    {
        var result = await paymentService.ProcessPaymentAsync(request, cancellationToken);
        return Success(result, "Payment processed successfully.");
    }

    /// <summary>Refunds a previously completed payment transaction.</summary>
    [HttpPost("refund")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RefundPaymentAsync([FromBody] RefundRequest request, CancellationToken cancellationToken)
    {
        var result = await paymentService.RefundPaymentAsync(request, cancellationToken);
        return Success(result, "Payment refunded successfully.");
    }

    /// <summary>Lists payments with optional Order or Contract Milestone filters.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaymentsAsync(
        [FromQuery] long? orderId,
        [FromQuery] long? milestoneId,
        CancellationToken cancellationToken)
    {
        var payments = await paymentService.GetPaymentsAsync(orderId, milestoneId, cancellationToken);
        return Success(payments, "Payments retrieved successfully.");
    }

    /// <summary>Gets single payment details by ID.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentByIdAsync(long id, CancellationToken cancellationToken)
    {
        var payment = await paymentService.GetPaymentByIdAsync(id, cancellationToken);
        if (payment == null)
        {
            return NotFound($"Payment with ID {id} was not found.");
        }

        return Success(payment, "Payment details retrieved successfully.");
    }
}
