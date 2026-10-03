namespace Mooncake.EcommercePlatform.Application.DTOs.Products.Responses;

/// <summary>Product summary returned in search and catalog listings.</summary>
public record ProductResponse(
    long Id,
    long ShopId,
    string ShopName,
    long? CategoryId,
    string? CategoryName,
    string Name,
    string? Description,
    decimal? CustomPackagingFee,
    bool IsActive,
    decimal MinPrice,
    decimal MaxPrice,
    string? PrimaryImageUrl,
    int VariantsCount,
    DateTime CreatedAtUtc
);
