namespace Mooncake.EcommercePlatform.Application.DTOs.Payments.Responses;

using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Payment projection returned to callers.</summary>
public record PaymentResponse(
    long Id,
    long? OrderId,
    long? MilestoneId,
    PaymentType PaymentType,
    decimal Amount,
    PaymentMethod Method,
    PaymentStatus Status,
    string? TransactionRef,
    DateTime? PaidAtUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string? OrderReceiverName = null,
    string? MilestoneName = null
);
