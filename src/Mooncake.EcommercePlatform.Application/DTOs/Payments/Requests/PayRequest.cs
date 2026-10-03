namespace Mooncake.EcommercePlatform.Application.DTOs.Payments.Requests;

using System.ComponentModel.DataAnnotations;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Request to simulate / process payment for an Order or Contract Milestone.</summary>
public record PayRequest
{
    public long? OrderId { get; init; }

    public long? MilestoneId { get; init; }

    [Required]
    public PaymentType PaymentType { get; init; } = PaymentType.Deposit;

    [Required]
    [Range(0.01, 10000000000)]
    public decimal Amount { get; init; }

    [Required]
    public PaymentMethod Method { get; init; } = PaymentMethod.BankTransfer;

    /// <summary>Unique transaction reference for anti-collision idempotency check.</summary>
    [MaxLength(100)]
    public string? TransactionRef { get; init; }
}

/// <summary>Request to refund an existing payment.</summary>
public record RefundRequest
{
    [Required]
    public long PaymentId { get; init; }

    [Required]
    [Range(0.01, 10000000000)]
    public decimal Amount { get; init; }

    public string? Reason { get; init; }
}
