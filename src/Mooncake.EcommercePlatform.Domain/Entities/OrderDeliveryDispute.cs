namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>Records a customer's delivery discrepancy or damage claim that locks order payout until resolved.</summary>
public sealed class OrderDeliveryDispute : BaseEntity
{
    public Guid OrderId { get; set; }

    public Guid? DeliveryId { get; set; }

    public Guid CustomerId { get; set; }

    public Guid ShopId { get; set; }

    public string IssueType { get; set; } = "DamagedGoods";

    public string Description { get; set; } = string.Empty;

    public string? EvidenceUrlsJson { get; set; }

    public string Status { get; set; } = "Open";

    public string? ResolutionNotes { get; set; }

    public Guid? ResolvedByAdminId { get; set; }

    public DateTime? ResolvedAt { get; set; }
}

