namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Catalog.Responses;
using Mooncake.EcommercePlatform.Application.DTOs.Catalog.Requests;
using Mooncake.EcommercePlatform.Domain.Enums;

public interface ICatalogService
{
    Task<CatalogSearchResponse> SearchAsync(string? search, Guid? categoryId, Guid? shopId, decimal? minPrice,
        decimal? maxPrice, int? minOrderQty, bool? supportsCustomPackaging, string? sortBy,
        int page, int pageSize, CancellationToken cancellationToken = default);
    Task<CatalogProductResponse> GetProductAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CategoryResponse>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CatalogProductResponse>> GetFeaturedAsync(int limit, CancellationToken cancellationToken = default);
    Task<PublicShopResponse> GetShopAsync(string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CatalogSuggestionResponse>> GetSuggestionsAsync(string query, int limit, CancellationToken cancellationToken = default);
    Task<ProductResponse> SaveSupplierProductAsync(Guid supplierUserId, Guid? productId, SaveProductRequest request,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductResponse>> GetSupplierProductsAsync(Guid supplierUserId, CancellationToken cancellationToken = default);
    Task<ProductResponse> AdjustStockAsync(Guid supplierUserId, Guid productId, AdjustProductStockRequest request,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShopTemplateResponse>> GetActiveShopTemplatesAsync(CancellationToken cancellationToken = default);
    Task<SupplierShopResponse> GetSupplierShopAsync(Guid supplierUserId, CancellationToken cancellationToken = default);
    Task<SupplierShopResponse> SaveSupplierShopAsync(Guid supplierUserId, SaveShopRequest request,
        CancellationToken cancellationToken = default);
    Task<SupplierShopResponse> SetSupplierShopStatusAsync(Guid supplierUserId, ShopStatus status,
        CancellationToken cancellationToken = default);
}
