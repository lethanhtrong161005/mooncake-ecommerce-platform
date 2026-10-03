using Mooncake.EcommercePlatform.Domain.Common;

namespace Mooncake.EcommercePlatform.Domain.Entities;

public class OrderItem : BaseEntity
{
    public long OrderId { get; set; }
    public long VariantId { get; set; }
    public long? PromotionRuleId { get; set; }
    public long? CustomPackagingId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal PackagingFee { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineTotal { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public Order? Order { get; set; }
    public ProductVariant? Variant { get; set; }
    public PromotionRule? PromotionRule { get; set; }
    public CustomPackaging? CustomPackaging { get; set; }
}
