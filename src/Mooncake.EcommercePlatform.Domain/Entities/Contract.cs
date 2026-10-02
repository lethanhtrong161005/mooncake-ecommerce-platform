using Mooncake.EcommercePlatform.Domain.Common;
using Mooncake.EcommercePlatform.Domain.Enums;
using System;
using System.Collections.Generic;

namespace Mooncake.EcommercePlatform.Domain.Entities;

public class Contract : BaseEntity
{
    public long QuotationId { get; set; }
    public long CustomerId { get; set; }
    public long SupplierId { get; set; }
    public ContractStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DepositPercent { get; set; }
    public DateOnly DeliveryDeadline { get; set; }
    public decimal LatePenaltyPercentPerDay { get; set; }
    public decimal MaxPenaltyPercent { get; set; }
    public string? Terms { get; set; }
    public DateTime? CustomerSignedAtUtc { get; set; }
    public DateTime? SupplierSignedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Quotation Quotation { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
    public Supplier Supplier { get; set; } = null!;
    
    public ICollection<ContractMilestone> ContractMilestones { get; set; } = [];
    public ICollection<Delivery> Deliveries { get; set; } = [];
    public ICollection<Review> Reviews { get; set; } = [];
    public ICollection<ReputationLog> ReputationLogs { get; set; } = [];
}
