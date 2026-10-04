namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>Represents the BidItem domain entity.</summary>
public class BidItem : BaseEntity
{
    public Guid BidId { get; set; }

    public Guid RfqItemId { get; set; }

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public decimal Subtotal { get; set; }

    public string? Notes { get; set; }
}
