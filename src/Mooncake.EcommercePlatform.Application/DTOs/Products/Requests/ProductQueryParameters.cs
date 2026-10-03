namespace Mooncake.EcommercePlatform.Application.DTOs.Products.Requests;

/// <summary>Query parameters for catalog search and filtering.</summary>
public record ProductQueryParameters
{
    public string? Keyword { get; init; }
    public long? CategoryId { get; init; }
    public long? ShopId { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public string? SortBy { get; init; } // "price_asc", "price_desc", "name_asc", "newest"
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
