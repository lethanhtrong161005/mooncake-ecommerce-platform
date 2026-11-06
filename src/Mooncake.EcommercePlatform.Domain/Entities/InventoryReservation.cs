namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>Tracks active, committed, or released inventory reservations for an order line variant.</summary>
public sealed class InventoryReservation : BaseEntity
{
    public Guid OrderId { get; set; }

    public Guid ProductId { get; set; }

    public Guid VariantId { get; set; }

    public Guid ShopId { get; set; }

    public int Quantity { get; set; }

    public string Status { get; set; } = "Active";

    public DateTime? ExpiresAt { get; set; }

    public DateTime? ReleasedAt { get; set; }

    public string? ReleaseReason { get; set; }
}

