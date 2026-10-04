namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Represents the Payment domain entity.</summary>
public class Payment : BaseEntity
{
    public Guid? OrderId { get; set; }

    public Guid? ContractId { get; set; }

    public Guid? MilestoneId { get; set; }

    public Guid CustomerId { get; set; }

    public string PaymentNumber { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string? TransactionId { get; set; }

    public DateTime? PaidAt { get; set; }

    public PaymentMethod Method { get; set; } = PaymentMethod.BankTransfer;

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
}
