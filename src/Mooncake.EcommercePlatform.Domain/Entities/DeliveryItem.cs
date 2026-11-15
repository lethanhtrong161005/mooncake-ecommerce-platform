namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>Represents an item snapshot within a delivery shipment.</summary>
public class DeliveryItem : BaseEntity
{
    public Guid DeliveryId { get; set; }

    public Guid? OrderItemId { get; set; }

    public Guid? RfqItemId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string? Unit { get; set; }

    public int Quantity { get; set; }
}
