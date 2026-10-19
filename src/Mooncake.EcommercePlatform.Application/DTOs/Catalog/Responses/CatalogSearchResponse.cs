namespace Mooncake.EcommercePlatform.Application.DTOs.Catalog.Responses;

public sealed record CatalogSearchResponse(IReadOnlyList<CatalogProductResponse> Items, int Page,
    int PageSize, int TotalCount);

public sealed record CatalogProductResponse(ProductResponse Product, string ShopSlug, string? ShopName,
    string? CompanyName, decimal ReputationScore, decimal? AverageRating, int ReviewCount);

public sealed record CatalogSuggestionResponse(Guid ProductId, string ProductName, Guid ShopId, string? ShopName);

public sealed record PublicShopResponse(ShopResponse Shop, string? CompanyName, decimal ReputationScore,
    IReadOnlyList<ProductResponse> Products);

public sealed record ShopTemplateResponse(Guid Id, string? Name, string? Description, string? PreviewImageUrl,
    string? CssVariables, string? LayoutConfig, int SortOrder);

public sealed record SupplierShopResponse(ShopResponse Shop, string? CustomColors, string? TemplateName);
