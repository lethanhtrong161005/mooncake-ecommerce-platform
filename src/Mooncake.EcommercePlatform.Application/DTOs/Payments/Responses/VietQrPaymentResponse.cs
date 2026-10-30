namespace Mooncake.EcommercePlatform.Application.DTOs.Payments.Responses;

/// <summary>Response containing VietQR bank transfer instructions.</summary>
public sealed record VietQrPaymentResponse(
    Guid PaymentId,
    string PaymentNumber,
    decimal Amount,
    string BankName,
    string BankBin,
    string AccountNumber,
    string AccountName,
    string TransferMemo,
    string QrCodeUrl,
    DateTime ExpiresAt);

