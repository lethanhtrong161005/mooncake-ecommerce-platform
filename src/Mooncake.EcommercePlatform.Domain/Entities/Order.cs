namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Represents the Order domain entity.</summary>
public class Order : BaseEntity
{
    public Guid CustomerId { get; set; }

    public Guid ShopId { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public decimal Subtotal { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal ShippingFee { get; set; }

    public decimal TotalAmount { get; set; }

    public string? Notes { get; set; }

    public string? DeliveryAddress { get; set; }

    public DateOnly? DeliveryDateExpected { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;
}
