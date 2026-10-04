namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>Represents the ProductVariant domain entity.</summary>
public class ProductVariant : BaseEntity
{
    public Guid ProductId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Sku { get; set; } = string.Empty;

    public string? Flavor { get; set; }

    public string? Filling { get; set; }

    public string? SizeLabel { get; set; }

    public int? WeightGram { get; set; }

    public decimal PriceAdjustment { get; set; }

    public int StockQty { get; set; }

    public string? ImageUrl { get; set; }
}
