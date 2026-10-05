namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Persistence operations for public catalog and supplier-owned catalog data.</summary>
public interface ICatalogRepository
{
    Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShopTemplate>> GetActiveTemplatesAsync(CancellationToken cancellationToken = default);
    Task<SupplierProfile?> GetSupplierProfileAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Shop?> GetShopBySupplierAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Shop?> GetShopBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<Shop?> GetShopAsync(Guid shopId, CancellationToken cancellationToken = default);
    Task<bool> IsSlugTakenAsync(string slug, Guid? exceptShopId = null, CancellationToken cancellationToken = default);
    Task<bool> IsTemplateActiveAsync(Guid templateId, CancellationToken cancellationToken = default);
    Task<bool> CategoryExistsAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<Category?> GetCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<bool> IsCategoryNameTakenAsync(string name, Guid? exceptCategoryId = null, CancellationToken cancellationToken = default);
    Task<bool> HasCategoryChildrenAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<bool> HasProductsInCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<bool> WouldCreateCategoryCycleAsync(Guid categoryId, Guid parentCategoryId, CancellationToken cancellationToken = default);
    Task SaveCategoryAsync(Category category, CancellationToken cancellationToken = default);
    Task<bool> DeleteCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<ShopTemplate?> GetShopTemplateAsync(Guid templateId, CancellationToken cancellationToken = default);
    Task<bool> IsTemplateNameTakenAsync(string name, Guid? exceptTemplateId = null, CancellationToken cancellationToken = default);
    Task<bool> IsTemplateInUseAsync(Guid templateId, CancellationToken cancellationToken = default);
    Task SaveShopTemplateAsync(ShopTemplate template, CancellationToken cancellationToken = default);
    Task<bool> DeleteShopTemplateAsync(Guid templateId, CancellationToken cancellationToken = default);
    Task SaveShopAsync(Shop shop, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> SearchProductsAsync(string? search, Guid? categoryId, Guid? shopId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Product?> GetProductAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductVariant>> GetVariantsAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductImage>> GetImagesAsync(Guid productId, CancellationToken cancellationToken = default);
    Task SaveProductAsync(Product product, IReadOnlyList<ProductVariant> variants, IReadOnlyList<ProductImage> images, CancellationToken cancellationToken = default);
    Task<bool> DeleteProductAsync(Guid productId, CancellationToken cancellationToken = default);
}
