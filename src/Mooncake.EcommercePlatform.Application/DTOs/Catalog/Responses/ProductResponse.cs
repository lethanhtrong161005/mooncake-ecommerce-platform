namespace Mooncake.EcommercePlatform.Application.DTOs.Catalog.Responses;

using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Product details exposed by catalog APIs.</summary>
public sealed record ProductResponse(
    Guid Id,
    Guid ShopId,
    Guid CategoryId,
    string Name,
    string? Description,
    decimal BasePrice,
    int MinOrderQty,
    int? MaxOrderQty,
    string? Unit,
    ProductStatus Status,
    IReadOnlyList<ProductVariantResponse> Variants,
    IReadOnlyList<ProductImageResponse> Images);

/// <summary>Variant information and currently available stock.</summary>
public sealed record ProductVariantResponse(Guid Id, string Name, string Sku, string? Flavor, string? Filling, string? SizeLabel, int? WeightGram, decimal PriceAdjustment, int StockQty, string? ImageUrl);

/// <summary>Product image and display order.</summary>
public sealed record ProductImageResponse(Guid Id, string ImageUrl, bool IsPrimary, int SortOrder);
