namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Products.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Products.Responses;

/// <summary>Contract for managing products, variants, and product images.</summary>
public interface IProductService
{
    Task<(IEnumerable<ProductResponse> Items, int TotalCount, int Page, int PageSize)> SearchProductsAsync(ProductQueryParameters parameters, CancellationToken cancellationToken = default);
    Task<ProductDetailResponse> GetProductByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<ProductResponse> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductResponse> UpdateProductAsync(long id, UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task DeleteProductAsync(long id, CancellationToken cancellationToken = default);

    Task<IEnumerable<ProductVariantResponse>> GetVariantsAsync(long productId, CancellationToken cancellationToken = default);
    Task<ProductVariantResponse> GetVariantByIdAsync(long variantId, CancellationToken cancellationToken = default);
    Task<ProductVariantResponse> CreateVariantAsync(long productId, CreateProductVariantRequest request, CancellationToken cancellationToken = default);
    Task<ProductVariantResponse> UpdateVariantAsync(long variantId, UpdateProductVariantRequest request, CancellationToken cancellationToken = default);
    Task<ProductVariantResponse> UpdateStockAsync(long variantId, UpdateStockRequest request, CancellationToken cancellationToken = default);
    Task DeleteVariantAsync(long variantId, CancellationToken cancellationToken = default);

    Task<ProductImageResponse> AddImageAsync(long productId, CreateProductImageRequest request, CancellationToken cancellationToken = default);
    Task DeleteImageAsync(long imageId, CancellationToken cancellationToken = default);
    Task SetPrimaryImageAsync(long productId, long imageId, CancellationToken cancellationToken = default);
}
