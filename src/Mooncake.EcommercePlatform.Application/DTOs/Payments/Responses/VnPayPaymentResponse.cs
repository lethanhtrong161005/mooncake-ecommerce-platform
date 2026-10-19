namespace Mooncake.EcommercePlatform.Application.DTOs.Payments.Responses;

public sealed record VnPayPaymentResponse(Guid PaymentId, string PaymentNumber, string PaymentUrl,
    decimal Amount, DateTime ExpiresAtUtc);

public sealed record VnPayIpnResponse(string RspCode, string Message);
