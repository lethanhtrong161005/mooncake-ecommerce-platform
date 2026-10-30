namespace Mooncake.EcommercePlatform.Application.DTOs.Payments.Responses;

/// <summary>Response from verifying VNPay return callback query parameters.</summary>
public sealed record VnPayReturnCallbackResponse(
    string PaymentNumber,
    Guid? OrderId,
    Guid? ContractId,
    Guid? MilestoneId,
    decimal Amount,
    bool IsSuccess,
    string ResponseCode,
    string? BankCode,
    string? TransactionNo,
    DateTime? PaidAt);

