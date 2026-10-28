namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Rebuildable public search projection; canonical product tables remain source of truth.</summary>
public sealed class ProductSearchDocument
{
    public Guid ProductId { get; set; }
    public Guid ShopId { get; set; }
    public Guid SupplierProfileId { get; set; }
    public Guid CategoryId { get; set; }
    public long SourceVersion { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public string ShopSlug { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string SearchText { get; set; } = string.Empty;
    public string? PrimaryImageUrl { get; set; }
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public int MinOrderQty { get; set; }
    public string? Unit { get; set; }
    public bool SupportsCustomPackaging { get; set; }
    public ProductStatus EffectiveStatus { get; set; }
    public decimal ReputationScore { get; set; }
    public decimal? AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public long SoldCount { get; set; }
    public string? RegionCode { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime ProjectedAtUtc { get; set; }
}
