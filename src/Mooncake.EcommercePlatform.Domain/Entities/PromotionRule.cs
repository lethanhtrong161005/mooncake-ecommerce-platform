using Mooncake.EcommercePlatform.Domain.Common;
using Mooncake.EcommercePlatform.Domain.Enums;

namespace Mooncake.EcommercePlatform.Domain.Entities;

public class PromotionRule : BaseEntity
{
    public long ShopId { get; set; }
    public long? ProductId { get; set; }
    public string Name { get; set; } = default!;
    public DiscountType DiscountType { get; set; }
    public int MinQuantity { get; set; }
    public decimal? DiscountPercent { get; set; }
    public decimal? DiscountAmount { get; set; }
    public int? FreeQuantity { get; set; }
    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Shop? Shop { get; set; }
    public Product? Product { get; set; }
}
