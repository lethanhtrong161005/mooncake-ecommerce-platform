namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>Tracks customer refund requests and simulated/admin refund reconciliation for direct bulk orders.</summary>
public sealed class OrderRefundCase : BaseEntity
{
    public Guid OrderId { get; set; }

    public Guid CustomerId { get; set; }

    public Guid ShopId { get; set; }

    public Guid? CancellationRequestId { get; set; }

    public string CaseNumber { get; set; } = string.Empty;

    public decimal RequestedAmount { get; set; }

    public decimal ApprovedAmount { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string Status { get; set; } = "Pending";

    public bool SandboxSimulated { get; set; } = true;

    public Guid? ProcessedByAdminId { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public string? AdminNotes { get; set; }

    public string? RefundReferenceCode { get; set; }
}

