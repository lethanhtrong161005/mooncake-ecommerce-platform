using Mooncake.EcommercePlatform.Domain.Common;
using System.Collections.Generic;

namespace Mooncake.EcommercePlatform.Domain.Entities;

public class Shop : BaseEntity
{
    public long SupplierId { get; set; }
    public long? TemplateId { get; set; }
    public string TemplateOverrides { get; set; } = "{}";
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? BannerUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Supplier? Supplier { get; set; }
    public ShopTemplate? Template { get; set; }
    public ICollection<Product> Products { get; set; } = [];
    public ICollection<PromotionRule> PromotionRules { get; set; } = [];
    public ICollection<Order> Orders { get; set; } = [];
}
