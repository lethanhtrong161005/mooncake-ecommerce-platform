namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>Represents the OrderItem domain entity.</summary>
public class OrderItem : BaseEntity
{
    public Guid OrderId { get; set; }

    public Guid ProductId { get; set; }

    public Guid VariantId { get; set; }

    public Guid? PromotionRuleId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal Subtotal { get; set; }
}
