namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Catalog.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Catalog.Responses;

/// <summary>Catalog discovery and supplier catalog management use cases.</summary>
public interface ICatalogService
{
    Task<IReadOnlyList<CategoryResponse>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShopTemplateResponse>> GetTemplatesAsync(CancellationToken cancellationToken = default);
    Task<CategoryResponse> CreateCategoryAsync(Guid adminUserId, SaveCategoryRequest request, CancellationToken cancellationToken = default);
    Task<CategoryResponse> UpdateCategoryAsync(Guid adminUserId, Guid categoryId, SaveCategoryRequest request, CancellationToken cancellationToken = default);
    Task DeleteCategoryAsync(Guid adminUserId, Guid categoryId, CancellationToken cancellationToken = default);
    Task<ShopTemplateResponse> CreateShopTemplateAsync(Guid adminUserId, SaveShopTemplateRequest request, CancellationToken cancellationToken = default);
    Task<ShopTemplateResponse> UpdateShopTemplateAsync(Guid adminUserId, Guid templateId, SaveShopTemplateRequest request, CancellationToken cancellationToken = default);
    Task DeleteShopTemplateAsync(Guid adminUserId, Guid templateId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductResponse>> SearchProductsAsync(string? search, Guid? categoryId, Guid? shopId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ProductResponse> GetProductAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<ShopResponse> GetShopAsync(string slug, CancellationToken cancellationToken = default);
    Task<ShopResponse> SaveMyShopAsync(Guid userId, SaveShopRequest request, CancellationToken cancellationToken = default);
    Task<ProductResponse> CreateProductAsync(Guid userId, SaveProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductResponse> UpdateProductAsync(Guid userId, Guid productId, SaveProductRequest request, CancellationToken cancellationToken = default);
    Task DeleteProductAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default);
}
