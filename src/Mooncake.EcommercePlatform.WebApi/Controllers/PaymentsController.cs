namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.DTOs.Payments.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;

[Route("api/v1/payments")]
[Authorize]
public sealed class PaymentsController(IPaymentService paymentService) : BaseApiController
{
    [Authorize(Roles = "Customer")]
    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromBody] CreatePaymentRequest request, CancellationToken cancellationToken) =>
        Created(await paymentService.CreateAsync(CurrentUserId, idempotencyKey, request, cancellationToken), "Payment created successfully.");

    [Authorize(Roles = "Customer")]
    [HttpPost("vnpay/checkout")]
    public async Task<IActionResult> CreateVnPayCheckoutAsync([FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromBody] CreatePaymentRequest request, CancellationToken cancellationToken) =>
        Created(await paymentService.CreateVnPayPaymentAsync(CurrentUserId, idempotencyKey, request,
            HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken), "VNPAY checkout created successfully.");

    [AllowAnonymous]
    [HttpGet("vnpay/ipn")]
    public async Task<IActionResult> VnPayIpnAsync(CancellationToken cancellationToken)
    {
        var parameters = Request.Query.ToDictionary(pair => pair.Key, pair => pair.Value.ToString(), StringComparer.Ordinal);
        var result = await paymentService.ProcessVnPayCallbackAsync(parameters, cancellationToken);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("vnpay/return")]
    public IActionResult VnPayReturn()
    {
        var parameters = Request.Query.ToDictionary(pair => pair.Key, pair => pair.Value.ToString(), StringComparer.Ordinal);
        return Ok(new
        {
            valid = paymentService.ValidateVnPayReturn(parameters),
            paymentNumber = parameters.GetValueOrDefault("vnp_TxnRef"),
            responseCode = parameters.GetValueOrDefault("vnp_ResponseCode")
        });
    }

    [Authorize(Roles = "Customer,Supplier")]
    [HttpGet("mine")]
    public async Task<IActionResult> GetMineAsync(CancellationToken cancellationToken) =>
        Success(await paymentService.GetMineAsync(CurrentUserId, User.IsInRole("Supplier"), cancellationToken), "Payments retrieved successfully.");

    [Authorize(Roles = "Admin")]
    [HttpPost("{paymentId:guid}/confirm")]
    public async Task<IActionResult> ConfirmAsync(Guid paymentId, [FromBody] ConfirmPaymentRequest request, CancellationToken cancellationToken) =>
        Success(await paymentService.ConfirmAsync(paymentId, CurrentUserId, request.Reason, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken), "Payment confirmed and recorded.");

    [Authorize(Roles = "Admin")]
    [HttpPost("{paymentId:guid}/refund")]
    public async Task<IActionResult> RefundAsync(Guid paymentId, [FromBody] RefundPaymentRequest request, CancellationToken cancellationToken) =>
        Success(await paymentService.RefundAsync(paymentId, CurrentUserId, request.Reason, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken), "Payment refund recorded.");

    private Guid CurrentUserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
        ? userId : throw new UnauthorizedAccessException("A valid user identity is required.");
}
