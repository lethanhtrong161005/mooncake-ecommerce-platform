namespace Mooncake.EcommercePlatform.Application.DTOs.Catalog.Responses;

/// <summary>Paginated product search results.</summary>
public sealed record ProductSearchResponse(IReadOnlyList<ProductResponse> Items, int TotalCount, int Page, int PageSize);
