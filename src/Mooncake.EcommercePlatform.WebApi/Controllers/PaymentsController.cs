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

    [Authorize(Roles = "Customer,Supplier")]
    [HttpGet("mine")]
    public async Task<IActionResult> GetMineAsync(CancellationToken cancellationToken) =>
        Success(await paymentService.GetMineAsync(CurrentUserId, User.IsInRole("Supplier"), cancellationToken), "Payments retrieved successfully.");

    [Authorize(Roles = "Customer")]
    [HttpPost("vnpay/create-url")]
    public async Task<IActionResult> CreateVnPayUrlAsync([FromBody] CreateVnPayUrlRequest request,
        CancellationToken cancellationToken) =>
        Success(await paymentService.CreateVnPayUrlAsync(CurrentUserId, request,
            HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken),
            "VNPay payment URL generated successfully.");

    [Authorize(Roles = "Customer")]
    [HttpPost("contracts/vnpay-url")]
    public async Task<IActionResult> CreateContractVnPayUrlAsync([FromBody] CreateVnPayUrlRequest request,
        CancellationToken cancellationToken) =>
        Success(await paymentService.CreateVnPayUrlAsync(CurrentUserId, request,
            HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken),
            "VNPay contract payment URL generated successfully.");

    [Authorize(Roles = "Customer")]
    [HttpPost("orders/vnpay-url")]
    public async Task<IActionResult> CreateOrderVnPayUrlAsync([FromBody] CreateOrderVnPayUrlRequest request,
        CancellationToken cancellationToken) =>
        Success(await paymentService.CreateOrderVnPayUrlAsync(CurrentUserId, request,
            HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken),
            "VNPay order payment URL generated successfully.");

    [Authorize(Roles = "Customer")]
    [HttpPost("bank-transfer")]
    public async Task<IActionResult> CreateBankTransferAsync([FromBody] CreateBankTransferRequest request,
        CancellationToken cancellationToken) =>
        Success(await paymentService.CreateBankTransferAsync(CurrentUserId, request, cancellationToken),
            "VietQR bank transfer instructions generated successfully.");

    [Authorize(Roles = "Customer")]
    [HttpPost("{paymentId:guid}/evidence")]
    public async Task<IActionResult> UploadEvidenceAsync(Guid paymentId, [FromBody] UploadPaymentEvidenceRequest request,
        CancellationToken cancellationToken) =>
        Success(await paymentService.UploadEvidenceAsync(paymentId, CurrentUserId, request, cancellationToken),
            "Payment transfer evidence attached successfully.");

    [AllowAnonymous]
    [HttpGet("vnpay/return-callback")]
    public async Task<IActionResult> VerifyReturnCallbackAsync(CancellationToken cancellationToken)
    {
        var parameters = Request.Query.ToDictionary(item => item.Key, item => item.Value.ToString(),
            StringComparer.Ordinal);
        var result = await paymentService.VerifyReturnCallbackAsync(parameters, cancellationToken);
        return Success(result, "VNPay return callback verified successfully.");
    }

    [Authorize(Roles = "Admin,Supplier")]
    [HttpGet("escrow/summary")]
    public async Task<IActionResult> GetEscrowSummaryAsync(CancellationToken cancellationToken) =>
        Success(await paymentService.GetEscrowSummaryAsync(cancellationToken), "Escrow summary retrieved successfully.");

    [AllowAnonymous]
    [HttpGet("vnpay/ipn")]
    public async Task<IActionResult> ProcessVnPayIpnAsync(CancellationToken cancellationToken)
    {
        var parameters = Request.Query.ToDictionary(item => item.Key, item => item.Value.ToString(),
            StringComparer.Ordinal);
        var result = await paymentService.ProcessVnPayIpnAsync(parameters, cancellationToken);
        return Ok(new { RspCode = result.Code, Message = result.Message });
    }

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
