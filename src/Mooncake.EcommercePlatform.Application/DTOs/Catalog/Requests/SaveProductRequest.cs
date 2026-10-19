namespace Mooncake.EcommercePlatform.Application.DTOs.Catalog.Requests;

using System.ComponentModel.DataAnnotations;
using Mooncake.EcommercePlatform.Domain.Enums;

public sealed record SaveProductRequest
{
    [Required] public Guid CategoryId { get; init; }
    [Required, MaxLength(255)] public string Name { get; init; } = string.Empty;
    [MaxLength(5000)] public string? Description { get; init; }
    [Range(typeof(decimal), "0.01", "999999999999")] public decimal BasePrice { get; init; }
    [Range(1, 1_000_000)] public int MinOrderQty { get; init; } = 1;
    [Range(1, 1_000_000)] public int? MaxOrderQty { get; init; }
    [MaxLength(50)] public string? Unit { get; init; }
    public bool SupportsCustomPackaging { get; init; }
    public ProductStatus Status { get; init; } = ProductStatus.Active;
    [Required, MinLength(1), MaxLength(100)] public List<SaveProductVariantRequest> Variants { get; init; } = [];
    [MaxLength(50)] public List<SaveProductImageRequest> Images { get; init; } = [];
}

public sealed record SaveProductVariantRequest
{
    public Guid? Id { get; init; }
    [Required, MaxLength(255)] public string Name { get; init; } = string.Empty;
    [Required, MaxLength(100)] public string Sku { get; init; } = string.Empty;
    [MaxLength(100)] public string? Flavor { get; init; }
    [MaxLength(100)] public string? Filling { get; init; }
    [MaxLength(50)] public string? SizeLabel { get; init; }
    [Range(1, 100_000)] public int? WeightGram { get; init; }
    [Range(typeof(decimal), "-999999999999", "999999999999")] public decimal PriceAdjustment { get; init; }
    [Range(0, 100_000_000)] public int InitialStockQty { get; init; }
    [Url, MaxLength(1000)] public string? ImageUrl { get; init; }
}

public sealed record SaveProductImageRequest
{
    [Required, Url, MaxLength(1000)] public string ImageUrl { get; init; } = string.Empty;
    public bool IsPrimary { get; init; }
    [Range(0, 100_000)] public int SortOrder { get; init; }
}

public sealed record AdjustProductStockRequest
{
    [Required] public Guid VariantId { get; init; }
    [Range(-100_000_000, 100_000_000)] public int Delta { get; init; }
    [Required, MinLength(3), MaxLength(1000)] public string Reason { get; init; } = string.Empty;
}
