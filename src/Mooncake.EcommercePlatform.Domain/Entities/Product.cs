using Mooncake.EcommercePlatform.Domain.Common;
using System.Collections.Generic;

namespace Mooncake.EcommercePlatform.Domain.Entities;

public class Product : BaseEntity
{
    public long ShopId { get; set; }
    public long? CategoryId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public decimal? CustomPackagingFee { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Shop? Shop { get; set; }
    public Category? Category { get; set; }
    public ICollection<ProductVariant> ProductVariants { get; set; } = [];
    public ICollection<ProductImage> ProductImages { get; set; } = [];
    public ICollection<PromotionRule> PromotionRules { get; set; } = [];
    public ICollection<RfqItem> RfqItems { get; set; } = [];
}
