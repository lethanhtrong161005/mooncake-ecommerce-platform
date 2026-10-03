namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Products.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Products.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Implements product, variant, and image management.</summary>
public class ProductService(
    IProductRepository productRepository,
    IShopRepository shopRepository,
    ICategoryRepository categoryRepository,
    IProductHelper productHelper) : IProductService
{
    public async Task<(IEnumerable<ProductResponse> Items, int TotalCount, int Page, int PageSize)> SearchProductsAsync(ProductQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        var (products, totalCount) = await productRepository.SearchProductsAsync(parameters, cancellationToken);
        var responses = products.Select(productHelper.ToResponse);
        return (responses, totalCount, parameters.Page, parameters.PageSize);
    }

    public async Task<ProductDetailResponse> GetProductByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var product = await productRepository.GetByIdAsync(id, cancellationToken)
                      ?? throw new HttpException(404, $"Product with id '{id}' was not found.");
        return productHelper.ToDetailResponse(product);
    }

    public async Task<ProductResponse> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var shop = await shopRepository.GetByIdAsync(request.ShopId, cancellationToken)
                   ?? throw new HttpException(404, $"Shop with id '{request.ShopId}' was not found.");

        if (request.CategoryId.HasValue)
        {
            var category = await categoryRepository.GetByIdAsync(request.CategoryId.Value, cancellationToken)
                           ?? throw new HttpException(404, $"Category with id '{request.CategoryId.Value}' was not found.");
        }

        var product = new Product
        {
            ShopId = request.ShopId,
            CategoryId = request.CategoryId,
            Name = request.Name,
            Description = request.Description,
            CustomPackagingFee = request.CustomPackagingFee,
            IsActive = request.IsActive,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var created = await productRepository.CreateAsync(product, cancellationToken);
        var fullProduct = await productRepository.GetByIdAsync(created.Id, cancellationToken);
        return productHelper.ToResponse(fullProduct ?? created);
    }

    public async Task<ProductResponse> UpdateProductAsync(long id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await productRepository.GetByIdAsync(id, cancellationToken)
                      ?? throw new HttpException(404, $"Product with id '{id}' was not found.");

        if (request.CategoryId.HasValue)
        {
            var category = await categoryRepository.GetByIdAsync(request.CategoryId.Value, cancellationToken)
                           ?? throw new HttpException(404, $"Category with id '{request.CategoryId.Value}' was not found.");
        }

        product.CategoryId = request.CategoryId;
        product.Name = request.Name;
        product.Description = request.Description;
        product.CustomPackagingFee = request.CustomPackagingFee;
        product.IsActive = request.IsActive;
        product.UpdatedAtUtc = DateTime.UtcNow;

        var updated = await productRepository.UpdateAsync(product, cancellationToken);
        var fullProduct = await productRepository.GetByIdAsync(updated.Id, cancellationToken);
        return productHelper.ToResponse(fullProduct ?? updated);
    }

    public async Task DeleteProductAsync(long id, CancellationToken cancellationToken = default)
    {
        var product = await productRepository.GetByIdAsync(id, cancellationToken)
                      ?? throw new HttpException(404, $"Product with id '{id}' was not found.");

        await productRepository.DeleteAsync(product.Id, cancellationToken);
    }

    public async Task<IEnumerable<ProductVariantResponse>> GetVariantsAsync(long productId, CancellationToken cancellationToken = default)
    {
        var variants = await productRepository.GetVariantsByProductIdAsync(productId, cancellationToken);
        return variants.Select(productHelper.ToVariantResponse);
    }

    public async Task<ProductVariantResponse> GetVariantByIdAsync(long variantId, CancellationToken cancellationToken = default)
    {
        var variant = await productRepository.GetVariantByIdAsync(variantId, cancellationToken)
                      ?? throw new HttpException(404, $"Product variant with id '{variantId}' was not found.");
        return productHelper.ToVariantResponse(variant);
    }

    public async Task<ProductVariantResponse> CreateVariantAsync(long productId, CreateProductVariantRequest request, CancellationToken cancellationToken = default)
    {
        var product = await productRepository.GetByIdAsync(productId, cancellationToken)
                      ?? throw new HttpException(404, $"Product with id '{productId}' was not found.");

        var variant = new ProductVariant
        {
            ProductId = productId,
            Sku = string.IsNullOrWhiteSpace(request.Sku) ? null : request.Sku.Trim(),
            Name = request.Name,
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            MinOrderQuantity = request.MinOrderQuantity,
            IsActive = request.IsActive,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var created = await productRepository.CreateVariantAsync(variant, cancellationToken);
        return productHelper.ToVariantResponse(created);
    }

    public async Task<ProductVariantResponse> UpdateVariantAsync(long variantId, UpdateProductVariantRequest request, CancellationToken cancellationToken = default)
    {
        var variant = await productRepository.GetVariantByIdAsync(variantId, cancellationToken)
                      ?? throw new HttpException(404, $"Product variant with id '{variantId}' was not found.");

        variant.Sku = string.IsNullOrWhiteSpace(request.Sku) ? null : request.Sku.Trim();
        variant.Name = request.Name;
        variant.Price = request.Price;
        variant.StockQuantity = request.StockQuantity;
        variant.MinOrderQuantity = request.MinOrderQuantity;
        variant.IsActive = request.IsActive;
        variant.UpdatedAtUtc = DateTime.UtcNow;

        var updated = await productRepository.UpdateVariantAsync(variant, cancellationToken);
        return productHelper.ToVariantResponse(updated);
    }

    public async Task<ProductVariantResponse> UpdateStockAsync(long variantId, UpdateStockRequest request, CancellationToken cancellationToken = default)
    {
        var variant = await productRepository.GetVariantByIdAsync(variantId, cancellationToken)
                      ?? throw new HttpException(404, $"Product variant with id '{variantId}' was not found.");

        variant.StockQuantity = request.StockQuantity;
        variant.UpdatedAtUtc = DateTime.UtcNow;

        var updated = await productRepository.UpdateVariantAsync(variant, cancellationToken);
        return productHelper.ToVariantResponse(updated);
    }

    public async Task DeleteVariantAsync(long variantId, CancellationToken cancellationToken = default)
    {
        var variant = await productRepository.GetVariantByIdAsync(variantId, cancellationToken)
                      ?? throw new HttpException(404, $"Product variant with id '{variantId}' was not found.");

        await productRepository.DeleteVariantAsync(variant.Id, cancellationToken);
    }

    public async Task<ProductImageResponse> AddImageAsync(long productId, CreateProductImageRequest request, CancellationToken cancellationToken = default)
    {
        var product = await productRepository.GetByIdAsync(productId, cancellationToken)
                      ?? throw new HttpException(404, $"Product with id '{productId}' was not found.");

        var image = new ProductImage
        {
            ProductId = productId,
            Url = request.Url,
            SortOrder = request.SortOrder,
            IsPrimary = request.IsPrimary,
            CreatedAtUtc = DateTime.UtcNow
        };

        var created = await productRepository.AddImageAsync(image, cancellationToken);
        return productHelper.ToImageResponse(created);
    }

    public async Task DeleteImageAsync(long imageId, CancellationToken cancellationToken = default)
    {
        await productRepository.DeleteImageAsync(imageId, cancellationToken);
    }

    public async Task SetPrimaryImageAsync(long productId, long imageId, CancellationToken cancellationToken = default)
    {
        var product = await productRepository.GetByIdAsync(productId, cancellationToken)
                      ?? throw new HttpException(404, $"Product with id '{productId}' was not found.");

        await productRepository.SetPrimaryImageAsync(productId, imageId, cancellationToken);
    }
}
