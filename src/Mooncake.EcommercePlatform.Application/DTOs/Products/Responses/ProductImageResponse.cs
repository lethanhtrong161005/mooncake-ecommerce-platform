namespace Mooncake.EcommercePlatform.Application.DTOs.Products.Responses;

/// <summary>Product visual asset.</summary>
public record ProductImageResponse(
    long Id,
    long ProductId,
    string Url,
    int SortOrder,
    bool IsPrimary
);
