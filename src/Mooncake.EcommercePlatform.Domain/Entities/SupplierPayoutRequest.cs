namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>Tracks a supplier's request to disburse eligible funds for a delivered order in sandbox simulation mode.</summary>
public sealed class SupplierPayoutRequest : BaseEntity
{
    public Guid OrderId { get; set; }

    public Guid ShopId { get; set; }

    public Guid SupplierUserId { get; set; }

    public Guid PayoutAccountId { get; set; }

    public string RequestNumber { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Status { get; set; } = "Pending";

    public bool SandboxSimulated { get; set; } = true;

    public string BankSnapshotJson { get; set; } = "{}";

    public Guid? ProcessedByAdminId { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public string? AdminNotes { get; set; }

    public string? PayoutReferenceCode { get; set; }
}

