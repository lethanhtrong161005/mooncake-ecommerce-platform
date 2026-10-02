using Mooncake.EcommercePlatform.Domain.Common;
using Mooncake.EcommercePlatform.Domain.Enums;
using System.Collections.Generic;

namespace Mooncake.EcommercePlatform.Domain.Entities;

public class Order : BaseEntity
{
    public long CustomerId { get; set; }
    public long ShopId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DepositRequired { get; set; }
    public string ReceiverName { get; set; } = default!;
    public string ReceiverPhone { get; set; } = default!;
    public string ShippingAddress { get; set; } = default!;
    public DateOnly? RequiredDeliveryDate { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Customer? Customer { get; set; }
    public Shop? Shop { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; } = [];
    public ICollection<Payment> Payments { get; set; } = [];
    public ICollection<Delivery> Deliveries { get; set; } = [];
    public ICollection<Review> Reviews { get; set; } = [];
}
