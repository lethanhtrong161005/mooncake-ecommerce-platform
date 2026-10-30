namespace Mooncake.EcommercePlatform.Application.DTOs.Payments.Responses;

using Mooncake.EcommercePlatform.Domain.Enums;

public sealed record PaymentResponse(Guid Id, string PaymentNumber, Guid? OrderId, Guid? ContractId, Guid? MilestoneId,
    decimal Amount, string Currency, PaymentMethod Method, PaymentStatus Status, DateTime? PaidAt, DateTime? ExpiresAt,
    string? EvidenceUrl = null, decimal RefundedAmount = 0m, string? RefundReason = null);
