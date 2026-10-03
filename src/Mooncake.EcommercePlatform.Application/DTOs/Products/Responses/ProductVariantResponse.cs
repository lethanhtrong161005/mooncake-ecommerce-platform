namespace Mooncake.EcommercePlatform.Application.DTOs.Products.Responses;

/// <summary>Variant of a mooncake product (flavor, size, packaging count).</summary>
public record ProductVariantResponse(
    long Id,
    long ProductId,
    string? Sku,
    string Name,
    decimal Price,
    int StockQuantity,
    int MinOrderQuantity,
    bool IsActive
);
