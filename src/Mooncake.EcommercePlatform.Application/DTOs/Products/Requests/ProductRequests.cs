namespace Mooncake.EcommercePlatform.Application.DTOs.Products.Requests;

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Mooncake.EcommercePlatform.Domain.Enums;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record ProductCoreRequest
{
    [Required, StringLength(255, MinimumLength = 2)] public string Name { get; init; } = string.Empty;
    [MaxLength(10_000)] public string? Description { get; init; }
    [MaxLength(10_000)] public string? Ingredients { get; init; }
    [Required] public Guid? CategoryId { get; init; }
    [Required, Range(typeof(decimal), "0", "9999999999.99")] public decimal? BasePrice { get; init; }
    [Required, Range(1, 1_000_000)] public int? MinOrderQty { get; init; }
    [Range(1, 1_000_000)] public int? MaxOrderQty { get; init; }
    [Required, RegularExpression("^(piece|box|set|tray)$")] public string Unit { get; init; } = string.Empty;
    [Required] public bool? SupportsCustomPackaging { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record ProductVariantInput
{
    [Required, StringLength(255, MinimumLength = 1)] public string Name { get; init; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 1), RegularExpression("^[A-Za-z0-9._-]+$")] public string Sku { get; init; } = string.Empty;
    [MaxLength(100)] public string? CrustType { get; init; }
    [MaxLength(100)] public string? Filling { get; init; }
    [MaxLength(50)] public string? SizeLabel { get; init; }
    [Range(1, 100_000)] public int? WeightGram { get; init; }
    [Required] public bool? IsGiftSet { get; init; }
    [Required, Range(typeof(decimal), "-9999999999.99", "9999999999.99")] public decimal? PriceAdjustment { get; init; }
    public Guid? ImageAssetId { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateProductVariantInput : ProductVariantInput
{
    [Required, Range(0, 10_000_000)] public int? InitialStockQty { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ProductImageInput
{
    [Required] public Guid? MediaAssetId { get; init; }
    [Required, Range(0, 9)] public int? SortOrder { get; init; }
    [Required] public bool? IsPrimary { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateProductRequest : ProductCoreRequest
{
    [Required, MinLength(1), MaxLength(100)] public List<CreateProductVariantInput> Variants { get; init; } = [];
    [Required, MinLength(1), MaxLength(10)] public List<ProductImageInput> Images { get; init; } = [];
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record UpdateProductCoreRequest : ProductCoreRequest
{
    [Required, Range(1, long.MaxValue)] public long? ExpectedVersion { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateProductVariantRequest : ProductVariantInput
{
    [Required, Range(0, 10_000_000)] public int? InitialStockQty { get; init; }
    [Required, Range(1, long.MaxValue)] public long? ExpectedVersion { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record UpdateProductVariantRequest : ProductVariantInput
{
    [Required, Range(1, long.MaxValue)] public long? ExpectedVersion { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record AdjustVariantInventoryRequest
{
    [Required, Range(1, long.MaxValue)] public long? ExpectedVersion { get; init; }
    [Required, Range(-1_000_000, 1_000_000)] public int? Delta { get; init; }
    [Required, StringLength(500, MinimumLength = 5)] public string Reason { get; init; } = string.Empty;
    [StringLength(100, MinimumLength = 1), RegularExpression("^[A-Za-z0-9._:-]+$")] public string? ClientReference { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ReplaceProductGalleryRequest
{
    [Required, Range(1, long.MaxValue)] public long? ExpectedVersion { get; init; }
    [Required, MinLength(1), MaxLength(10)] public List<ProductImageInput> Images { get; init; } = [];
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SetProductPublicationRequest
{
    [Required, Range(1, long.MaxValue)] public long? ExpectedVersion { get; init; }
    [Required] public bool? IsPublished { get; init; }
}

public sealed record OwnerProductQuery
{
    [Range(1, 1000)] public int Page { get; init; } = 1;
    [Range(1, 100)] public int PageSize { get; init; } = 20;
    [MaxLength(100)] public string? Q { get; init; }
    public ProductStatus? Status { get; init; }
    public Guid? CategoryId { get; init; }
    [RegularExpression("^(updatedAtDesc|nameAsc|stockAsc)$")] public string Sort { get; init; } = "updatedAtDesc";
}

public sealed record ProductSearchQuery
{
    [Range(1, 1000)] public int Page { get; init; } = 1;
    [Range(1, 50)] public int PageSize { get; init; } = 24;
    [MaxLength(100)] public string? Q { get; init; }
    [MaxLength(20)] public Guid[] CategoryIds { get; init; } = [];
    [Range(typeof(decimal), "0", "9999999999.99")] public decimal? MinPrice { get; init; }
    [Range(typeof(decimal), "0", "9999999999.99")] public decimal? MaxPrice { get; init; }
    [Range(1, 1_000_000)] public int? MaxMinOrderQty { get; init; }
    public bool? SupportsCustomPackaging { get; init; }
    [Range(typeof(decimal), "0", "100")] public decimal? MinReputation { get; init; }
    [MaxLength(20)] public string[] RegionCodes { get; init; } = [];
    public bool InStockOnly { get; init; }
    [RegularExpression("^(relevance|priceAsc|priceDesc|reputation|rating|newest|bestSelling)$")]
    public string Sort { get; init; } = "relevance";
}

public sealed record ProductAutocompleteQuery
{
    [Required, StringLength(100, MinimumLength = 2)] public string Q { get; init; } = string.Empty;
    [Range(1, 10)] public int Limit { get; init; } = 8;
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RecordProductViewRequest
{
    public Guid? SessionId { get; init; }
    [Required, RegularExpression("^(Search|Shop|Related|Direct)$")] public string Source { get; init; } = string.Empty;
}
