namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>Represents the ProductVariant domain entity.</summary>
public class ProductVariant : BaseEntity
{
    public Guid ProductId { get; set; }

    public Guid ShopId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Sku { get; set; } = string.Empty;

    public string NormalizedSku { get; set; } = string.Empty;

    public string? CrustType { get; set; }

    public string? Flavor { get; set; }

    public string? Filling { get; set; }

    public string? SizeLabel { get; set; }

    public int? WeightGram { get; set; }

    public bool IsGiftSet { get; set; }

    public decimal PriceAdjustment { get; set; }

    public int StockQty { get; set; }

    public long InventoryVersion { get; set; } = 1;

    public Guid? ImageAssetId { get; set; }

    public string? ImageUrl { get; set; }
}
