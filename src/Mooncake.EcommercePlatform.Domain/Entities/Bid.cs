namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Represents the Bid domain entity.</summary>
public class Bid : BaseEntity
{
    public Guid RfqId { get; set; }

    public Guid SupplierId { get; set; }

    public string BidNumber { get; set; } = string.Empty;

    public decimal? TotalPrice { get; set; }

    public int? EstimatedDeliveryDays { get; set; }

    public string? Notes { get; set; }

    public int ValidityDays { get; set; }

    public DateTime SubmittedAt { get; set; }

    public BidStatus Status { get; set; } = BidStatus.Submitted;
}
