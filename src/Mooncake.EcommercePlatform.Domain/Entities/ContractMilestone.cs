using Mooncake.EcommercePlatform.Domain.Common;
using Mooncake.EcommercePlatform.Domain.Enums;
using System;
using System.Collections.Generic;

namespace Mooncake.EcommercePlatform.Domain.Entities;

public class ContractMilestone : BaseEntity
{
    public long ContractId { get; set; }
    public short MilestoneNo { get; set; }
    public string Name { get; set; } = null!;
    public MilestoneType MilestoneType { get; set; }
    public decimal Amount { get; set; }
    public DateOnly? DueDate { get; set; }
    public MilestoneStatus Status { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Contract Contract { get; set; } = null!;
    public ICollection<Payment> Payments { get; set; } = [];
}
