namespace Mooncake.EcommercePlatform.Application.DTOs.Catalog.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>Product data including sellable variants and image references.</summary>
public sealed record SaveProductRequest
{
    [Required, MaxLength(255)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(5000)]
    public string? Description { get; init; }

    [Range(typeof(decimal), "0.01", "9999999999")]
    public decimal BasePrice { get; init; }

    [Range(1, 1_000_000)]
    public int MinOrderQty { get; init; } = 1;

    [Range(1, 1_000_000)]
    public int? MaxOrderQty { get; init; }

    [MaxLength(50)]
    public string? Unit { get; init; }

    [Required]
    public Guid CategoryId { get; init; }

    public bool SupportsCustomPackaging { get; init; }

    [Required, MinLength(1), MaxLength(100)]
    public List<SaveProductVariantRequest> Variants { get; init; } = [];

    [MaxLength(20)]
    public List<SaveProductImageRequest> Images { get; init; } = [];
}

/// <summary>Sellable product variant and opening inventory.</summary>
public sealed record SaveProductVariantRequest
{
    public Guid? Id { get; init; }

    [Required, MaxLength(255)]
    public string Name { get; init; } = string.Empty;

    [Required, MaxLength(100)]
    public string Sku { get; init; } = string.Empty;

    [MaxLength(100)]
    public string? Flavor { get; init; }

    [MaxLength(100)]
    public string? Filling { get; init; }

    [MaxLength(100)]
    public string? SizeLabel { get; init; }

    [Range(1, 1_000_000)]
    public int? WeightGram { get; init; }

    [Range(typeof(decimal), "-9999999999", "9999999999")]
    public decimal PriceAdjustment { get; init; }

    [Range(0, 1_000_000)]
    public int StockQty { get; init; }

    [Url, MaxLength(1000)]
    public string? ImageUrl { get; init; }
}

/// <summary>Product image reference.</summary>
public sealed record SaveProductImageRequest
{
    [Required, Url, MaxLength(1000)]
    public string ImageUrl { get; init; } = string.Empty;

    public bool IsPrimary { get; init; }

    [Range(0, 1000)]
    public int SortOrder { get; init; }
}
