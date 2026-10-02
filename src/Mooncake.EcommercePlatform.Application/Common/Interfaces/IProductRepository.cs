namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Products.Requests;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Data access contract for Product aggregate, variants, and gallery images.</summary>
public interface IProductRepository
{
    Task<(IEnumerable<Product> Products, int TotalCount)> SearchProductsAsync(ProductQueryParameters parameters, CancellationToken cancellationToken = default);
    Task<Product?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Product> CreateAsync(Product product, CancellationToken cancellationToken = default);
    Task<Product> UpdateAsync(Product product, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);

    Task<IEnumerable<ProductVariant>> GetVariantsByProductIdAsync(long productId, CancellationToken cancellationToken = default);
    Task<ProductVariant?> GetVariantByIdAsync(long variantId, CancellationToken cancellationToken = default);
    Task<ProductVariant> CreateVariantAsync(ProductVariant variant, CancellationToken cancellationToken = default);
    Task<ProductVariant> UpdateVariantAsync(ProductVariant variant, CancellationToken cancellationToken = default);
    Task DeleteVariantAsync(long variantId, CancellationToken cancellationToken = default);

    Task<ProductImage> AddImageAsync(ProductImage image, CancellationToken cancellationToken = default);
    Task DeleteImageAsync(long imageId, CancellationToken cancellationToken = default);
    Task SetPrimaryImageAsync(long productId, long imageId, CancellationToken cancellationToken = default);
}
