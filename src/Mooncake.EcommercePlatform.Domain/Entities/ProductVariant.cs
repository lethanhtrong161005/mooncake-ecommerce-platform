using Mooncake.EcommercePlatform.Domain.Common;

namespace Mooncake.EcommercePlatform.Domain.Entities;

public class ProductVariant : BaseEntity
{
    public long ProductId { get; set; }
    public string? Sku { get; set; }
    public string Name { get; set; } = default!;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public int MinOrderQuantity { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Product? Product { get; set; }
}
