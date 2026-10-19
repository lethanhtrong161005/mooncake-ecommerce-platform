namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

public interface ICatalogRepository
{
    Task<(IReadOnlyList<CatalogProductRecord> Items, int TotalCount)> SearchAsync(CatalogSearchFilter filter,
        CancellationToken cancellationToken = default);
    Task<CatalogProductRecord?> GetProductAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CatalogProductRecord>> GetFeaturedAsync(int limit, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CatalogProductRecord>> GetShopProductsAsync(Guid shopId, CancellationToken cancellationToken = default);
    Task<ShopCatalogRecord?> GetShopBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CatalogSuggestionRecord>> GetSuggestionsAsync(string query, int limit, CancellationToken cancellationToken = default);
    Task<Shop?> GetVerifiedShopForSupplierAsync(Guid supplierUserId, CancellationToken cancellationToken = default);
    Task<bool> CategoryExistsAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<Product?> GetSupplierProductAsync(Guid productId, Guid supplierUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> GetSupplierProductsAsync(Guid supplierUserId, CancellationToken cancellationToken = default);
    Task<CatalogProductRecord?> GetSupplierProductRecordAsync(Guid productId, Guid supplierUserId, CancellationToken cancellationToken = default);
    Task<bool> SkuExistsAsync(string sku, Guid? exceptVariantId = null, CancellationToken cancellationToken = default);
    Task<bool> SaveProductAsync(Product product, IReadOnlyList<ProductVariant> variants, IReadOnlyList<ProductImage> images,
        Guid supplierUserId, CancellationToken cancellationToken = default);
    Task<bool> AdjustStockAsync(Guid productId, Guid variantId, int delta, Guid supplierUserId, string reason,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShopTemplate>> GetActiveShopTemplatesAsync(CancellationToken cancellationToken = default);
    Task<SupplierShopRecord?> GetSupplierShopAsync(Guid supplierUserId, CancellationToken cancellationToken = default);
    Task<bool> SaveSupplierShopAsync(Shop shop, Guid supplierUserId, CancellationToken cancellationToken = default);
    Task<bool> SetSupplierShopStatusAsync(Guid supplierUserId, ShopStatus status, CancellationToken cancellationToken = default);
}

public sealed record CatalogSearchFilter(string? Search, Guid? CategoryId, Guid? ShopId, decimal? MinPrice,
    decimal? MaxPrice, int? MinOrderQty, bool? SupportsCustomPackaging, string? SortBy, int Page, int PageSize);
public sealed record CatalogProductRecord(Product Product, Shop Shop, SupplierProfile Supplier,
    IReadOnlyList<ProductVariant> Variants, IReadOnlyList<ProductImage> Images, decimal? AverageRating, int ReviewCount);
public sealed record ShopCatalogRecord(Shop Shop, SupplierProfile Supplier);
public sealed record CatalogSuggestionRecord(Guid ProductId, string ProductName, Guid ShopId, string? ShopName);
public sealed record SupplierShopRecord(Shop Shop, ShopTemplate? Template);
