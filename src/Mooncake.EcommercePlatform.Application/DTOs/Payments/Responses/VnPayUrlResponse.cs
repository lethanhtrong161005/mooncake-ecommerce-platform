namespace Mooncake.EcommercePlatform.Application.DTOs.Payments.Responses;

/// <summary>VNPay checkout address for one pending payment.</summary>
public sealed record VnPayUrlResponse(Guid PaymentId, string PaymentNumber, decimal Amount, string PaymentUrl);
