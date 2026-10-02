using Mooncake.EcommercePlatform.Domain.Common;
using Mooncake.EcommercePlatform.Domain.Enums;
using System;

namespace Mooncake.EcommercePlatform.Domain.Entities;

public class Payment : BaseEntity
{
    public long? OrderId { get; set; }
    public long? MilestoneId { get; set; }
    public PaymentType PaymentType { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; }
    public string? TransactionRef { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Order? Order { get; set; }
    public ContractMilestone? Milestone { get; set; }
}
