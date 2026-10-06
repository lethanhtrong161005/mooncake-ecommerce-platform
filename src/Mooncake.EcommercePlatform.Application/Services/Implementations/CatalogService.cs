namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Catalog.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Catalog.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using System.Text.Json;

/// <summary>Implements catalog discovery and supplier catalog management.</summary>
public sealed class CatalogService(ICatalogRepository repository, IDateTimeProvider dateTimeProvider) : ICatalogService
{
    private static readonly HashSet<string> ProductSortOptions = new(StringComparer.OrdinalIgnoreCase)
        { "newest", "oldest", "price_asc", "price_desc", "name" };

    public async Task<IReadOnlyList<CategoryResponse>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var categories = await repository.GetCategoriesAsync(cancellationToken);
        return categories.Select(category => new CategoryResponse(category.Id, category.ParentId, category.Name, category.Description, category.IconUrl, category.SortOrder)).ToList();
    }

    public async Task<IReadOnlyList<ShopTemplateResponse>> GetTemplatesAsync(CancellationToken cancellationToken = default)
    {
        var templates = await repository.GetActiveTemplatesAsync(cancellationToken);
        return templates.Select(template => new ShopTemplateResponse(template.Id, template.Name, template.Description, template.PreviewImageUrl, template.CssVariables, template.LayoutConfig, template.IsPremium)).ToList();
    }

    public async Task<IReadOnlyList<ShopTemplateResponse>> GetAdminTemplatesAsync(CancellationToken cancellationToken = default)
    {
        var templates = await repository.GetAllShopTemplatesAsync(cancellationToken);
        return templates.Select(ToTemplateResponse).ToList();
    }

    public async Task<CategoryResponse> CreateCategoryAsync(Guid adminUserId, SaveCategoryRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateCategoryAsync(request, null, cancellationToken);
        var category = new Category
        {
            Name = request.Name.Trim(), Description = request.Description?.Trim(), IconUrl = request.IconUrl,
            ParentId = request.ParentId, SortOrder = request.SortOrder, CreatedBy = adminUserId
        };
        await repository.SaveCategoryAsync(category, cancellationToken);
        return ToCategoryResponse(category);
    }

    public async Task<CategoryResponse> UpdateCategoryAsync(Guid adminUserId, Guid categoryId, SaveCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = await repository.GetCategoryAsync(categoryId, cancellationToken)
                       ?? throw new HttpException(404, "Category was not found.");
        await ValidateCategoryAsync(request, categoryId, cancellationToken);
        category.Name = request.Name.Trim();
        category.Description = request.Description?.Trim();
        category.IconUrl = request.IconUrl;
        category.ParentId = request.ParentId;
        category.SortOrder = request.SortOrder;
        category.UpdatedBy = adminUserId;
        category.UpdatedAt = dateTimeProvider.UtcNow;
        await repository.SaveCategoryAsync(category, cancellationToken);
        return ToCategoryResponse(category);
    }

    public async Task DeleteCategoryAsync(Guid adminUserId, Guid categoryId, CancellationToken cancellationToken = default)
    {
        if (await repository.HasProductsInCategoryAsync(categoryId, cancellationToken))
            throw new HttpException(409, "A category with products cannot be deleted.");
        if (await repository.HasCategoryChildrenAsync(categoryId, cancellationToken))
            throw new HttpException(409, "A category with child categories cannot be deleted.");
        var category = await repository.GetCategoryAsync(categoryId, cancellationToken)
                       ?? throw new HttpException(404, "Category was not found.");
        category.UpdatedBy = adminUserId;
        category.UpdatedAt = dateTimeProvider.UtcNow;
        if (!await repository.DeleteCategoryAsync(categoryId, cancellationToken))
            throw new HttpException(404, "Category was not found.");
    }

    public async Task<ShopTemplateResponse> CreateShopTemplateAsync(Guid adminUserId, SaveShopTemplateRequest request, CancellationToken cancellationToken = default)
    {
        ValidateJsonObject(request.CssVariables, "CSS variables");
        ValidateJsonObject(request.LayoutConfig, "Layout configuration");
        var name = request.Name.Trim();
        if (await repository.IsTemplateNameTakenAsync(name, cancellationToken: cancellationToken))
            throw new HttpException(409, "A shop template with this name already exists.");
        var template = new ShopTemplate
        {
            Name = name, Description = request.Description?.Trim(), PreviewImageUrl = request.PreviewImageUrl,
            CssVariables = request.CssVariables, LayoutConfig = request.LayoutConfig, SortOrder = request.SortOrder, IsPremium = request.IsPremium,
            IsActive = request.IsActive, CreatedBy = adminUserId
        };
        await repository.SaveShopTemplateAsync(template, cancellationToken);
        return ToTemplateResponse(template);
    }

    public async Task<ShopTemplateResponse> UpdateShopTemplateAsync(Guid adminUserId, Guid templateId, SaveShopTemplateRequest request, CancellationToken cancellationToken = default)
    {
        ValidateJsonObject(request.CssVariables, "CSS variables");
        ValidateJsonObject(request.LayoutConfig, "Layout configuration");
        var template = await repository.GetShopTemplateAsync(templateId, cancellationToken)
                       ?? throw new HttpException(404, "Shop template was not found.");
        var name = request.Name.Trim();
        if (await repository.IsTemplateNameTakenAsync(name, templateId, cancellationToken))
            throw new HttpException(409, "A shop template with this name already exists.");
        template.Name = name;
        template.Description = request.Description?.Trim();
        template.PreviewImageUrl = request.PreviewImageUrl;
        template.CssVariables = request.CssVariables;
        template.LayoutConfig = request.LayoutConfig;
        template.SortOrder = request.SortOrder;
        template.IsActive = request.IsActive;
        template.IsPremium = request.IsPremium;
        template.UpdatedBy = adminUserId;
        template.UpdatedAt = dateTimeProvider.UtcNow;
        await repository.SaveShopTemplateAsync(template, cancellationToken);
        return ToTemplateResponse(template);
    }

    public async Task DeleteShopTemplateAsync(Guid adminUserId, Guid templateId, CancellationToken cancellationToken = default)
    {
        if (await repository.IsTemplateInUseAsync(templateId, cancellationToken))
            throw new HttpException(409, "A template assigned to a shop cannot be deleted.");
        var template = await repository.GetShopTemplateAsync(templateId, cancellationToken)
                       ?? throw new HttpException(404, "Shop template was not found.");
        template.UpdatedBy = adminUserId;
        template.UpdatedAt = dateTimeProvider.UtcNow;
        if (!await repository.DeleteShopTemplateAsync(templateId, cancellationToken))
            throw new HttpException(404, "Shop template was not found.");
    }

    public async Task<ProductSearchResponse> SearchProductsAsync(string? search, Guid? categoryId, Guid? shopId, decimal? minPrice, decimal? maxPrice, string sortBy, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 100)
            throw new HttpException(400, "Page must be positive and page size must be between 1 and 100.");
        if (minPrice < 0 || maxPrice < 0 || minPrice > maxPrice)
            throw new HttpException(400, "Price filters must be non-negative and the minimum cannot exceed the maximum.");
        var normalizedSort = sortBy.Trim().ToLowerInvariant();
        if (!ProductSortOptions.Contains(normalizedSort))
            throw new HttpException(400, "The selected product sort option is unavailable.");
        var pageResult = await repository.SearchProductsAsync(search?.Trim(), categoryId, shopId, minPrice, maxPrice,
            normalizedSort, page, pageSize, cancellationToken);
        var responses = new List<ProductResponse>(pageResult.Products.Count);
        foreach (var product in pageResult.Products)
            responses.Add(await ToResponseAsync(product, cancellationToken));
        return new ProductSearchResponse(responses, pageResult.TotalCount, page, pageSize);
    }

    public async Task<IReadOnlyList<string>> GetProductSuggestionsAsync(string query, CancellationToken cancellationToken = default)
    {
        var normalizedQuery = query.Trim();
        if (normalizedQuery.Length is < 2 or > 100)
            throw new HttpException(400, "Suggestion query must contain between 2 and 100 characters.");
        return await repository.GetProductSuggestionsAsync(normalizedQuery, 10, cancellationToken);
    }

    public async Task<ProductResponse> GetProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var product = await repository.GetProductAsync(productId, cancellationToken)
                      ?? throw new HttpException(404, "Product was not found.");
        if (product.Status != ProductStatus.Active)
            throw new HttpException(404, "Product was not found.");
        return await ToResponseAsync(product, cancellationToken);
    }

    public async Task<ShopResponse> GetShopAsync(string slug, CancellationToken cancellationToken = default)
    {
        var shop = await repository.GetShopBySlugAsync(slug.Trim().ToLowerInvariant(), cancellationToken)
                   ?? throw new HttpException(404, "Shop was not found.");
        return CatalogResponseHelper.ToResponse(shop);
    }

    public async Task<ShopResponse> SaveMyShopAsync(Guid userId, SaveShopRequest request, CancellationToken cancellationToken = default)
    {
        var profile = await GetVerifiedSupplierAsync(userId, cancellationToken);
        var existingShop = await repository.GetShopBySupplierAsync(userId, cancellationToken);
        var slug = request.Slug.Trim().ToLowerInvariant();
        if (await repository.IsSlugTakenAsync(slug, existingShop?.Id, cancellationToken))
            throw new HttpException(409, "The requested shop URL is already in use.");
        if (!await repository.IsTemplateActiveAsync(request.TemplateId, cancellationToken))
            throw new HttpException(400, "The selected shop template is not available.");

        var shop = existingShop ?? new Shop { SupplierId = profile.Id, Status = ShopStatus.Active };
        shop.Name = request.Name.Trim();
        shop.Slug = slug;
        shop.Description = request.Description?.Trim();
        shop.BannerUrl = request.BannerUrl;
        shop.LogoUrl = request.LogoUrl;
        shop.TemplateId = request.TemplateId;
        shop.UpdatedBy = userId;
        await repository.SaveShopAsync(shop, cancellationToken);
        return CatalogResponseHelper.ToResponse(shop);
    }

    public async Task<ProductResponse> CreateProductAsync(Guid userId, SaveProductRequest request, CancellationToken cancellationToken = default)
    {
        var shop = await GetVerifiedShopAsync(userId, cancellationToken);
        await EnsureCategoryExistsAsync(request.CategoryId, cancellationToken);
        var product = BuildProduct(shop.Id, request, userId);
        var variants = BuildVariants(product.Id, request.Variants);
        var images = BuildImages(product.Id, request.Images);
        await repository.SaveProductAsync(product, variants, images, cancellationToken);
        return await ToResponseAsync(product, cancellationToken);
    }

    public async Task<IReadOnlyList<ProductResponse>> GetMyProductsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var shop = await GetVerifiedShopAsync(userId, cancellationToken);
        var products = await repository.GetSupplierProductsAsync(shop.Id, cancellationToken);
        var responses = new List<ProductResponse>(products.Count);
        foreach (var product in products)
            responses.Add(await ToResponseAsync(product, cancellationToken));
        return responses;
    }

    public async Task<ProductResponse> UpdateProductAsync(Guid userId, Guid productId, SaveProductRequest request, CancellationToken cancellationToken = default)
    {
        var shop = await GetVerifiedShopAsync(userId, cancellationToken);
        await EnsureCategoryExistsAsync(request.CategoryId, cancellationToken);
        var validatedProduct = BuildProduct(shop.Id, request, userId);
        var product = await repository.GetSupplierProductAsync(productId, shop.Id, cancellationToken)
                      ?? throw new HttpException(404, "Product was not found.");

        product.Name = validatedProduct.Name;
        product.Description = validatedProduct.Description;
        product.BasePrice = validatedProduct.BasePrice;
        product.MinOrderQty = validatedProduct.MinOrderQty;
        product.MaxOrderQty = validatedProduct.MaxOrderQty;
        product.Unit = validatedProduct.Unit;
        product.CategoryId = validatedProduct.CategoryId;
        product.SupportsCustomPackaging = validatedProduct.SupportsCustomPackaging;
        product.UpdatedBy = userId;
        var variants = BuildVariants(product.Id, request.Variants);
        var images = BuildImages(product.Id, request.Images);
        if (!await repository.SaveProductAsync(product, variants, images, cancellationToken))
            throw new HttpException(409, "A variant with a pending order cannot be removed.");
        return await ToResponseAsync(product, cancellationToken);
    }

    public async Task DeleteProductAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
    {
        var shop = await GetVerifiedShopAsync(userId, cancellationToken);
        var product = await repository.GetSupplierProductAsync(productId, shop.Id, cancellationToken)
                      ?? throw new HttpException(404, "Product was not found.");
        await repository.DeleteProductAsync(productId, cancellationToken);
    }

    public async Task<ProductResponse> AdjustVariantStockAsync(Guid userId, Guid productId, Guid variantId, int quantityDelta, string reason, CancellationToken cancellationToken = default)
    {
        if (quantityDelta == 0)
            throw new HttpException(400, "Stock adjustment must be non-zero.");
        var shop = await GetVerifiedShopAsync(userId, cancellationToken);
        var product = await repository.GetSupplierProductAsync(productId, shop.Id, cancellationToken)
                      ?? throw new HttpException(404, "Product was not found.");
        if (!await repository.AdjustVariantStockAsync(productId, variantId, quantityDelta, userId, reason.Trim(), cancellationToken))
            throw new HttpException(409, "Stock adjustment would result in negative stock or the variant is unavailable.");
        return await ToResponseAsync(product, cancellationToken);
    }

    private async Task<SupplierProfile> GetVerifiedSupplierAsync(Guid userId, CancellationToken cancellationToken) =>
        await repository.GetSupplierProfileAsync(userId, cancellationToken) is { VerificationStatus: SupplierVerificationStatus.Verified } profile
            ? profile
            : throw new HttpException(403, "A verified supplier profile is required for this action.");

    private async Task EnsureCategoryExistsAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        if (!await repository.CategoryExistsAsync(categoryId, cancellationToken))
            throw new HttpException(400, "The selected product category is unavailable.");
    }

    private async Task ValidateCategoryAsync(SaveCategoryRequest request, Guid? exceptCategoryId, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (await repository.IsCategoryNameTakenAsync(name, exceptCategoryId, cancellationToken))
            throw new HttpException(409, "A category with this name already exists.");
        if (request.ParentId.HasValue && request.ParentId == exceptCategoryId)
            throw new HttpException(400, "A category cannot be its own parent.");
        if (request.ParentId.HasValue && !await repository.CategoryExistsAsync(request.ParentId.Value, cancellationToken))
            throw new HttpException(400, "The selected parent category was not found.");
        if (request.ParentId.HasValue && exceptCategoryId.HasValue
            && await repository.WouldCreateCategoryCycleAsync(exceptCategoryId.Value, request.ParentId.Value, cancellationToken))
            throw new HttpException(400, "The category hierarchy cannot contain a cycle.");
    }

    private static void ValidateJsonObject(string? json, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(json))
            return;
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new HttpException(400, $"{fieldName} must be a JSON object.");
        }
        catch (JsonException)
        {
            throw new HttpException(400, $"{fieldName} contains invalid JSON.");
        }
    }

    private static CategoryResponse ToCategoryResponse(Category category) =>
        new(category.Id, category.ParentId, category.Name, category.Description, category.IconUrl, category.SortOrder);

    private static ShopTemplateResponse ToTemplateResponse(ShopTemplate template) =>
        new(template.Id, template.Name, template.Description, template.PreviewImageUrl, template.CssVariables, template.LayoutConfig, template.IsPremium);

    private async Task<Shop> GetVerifiedShopAsync(Guid userId, CancellationToken cancellationToken)
    {
        await GetVerifiedSupplierAsync(userId, cancellationToken);
        return await repository.GetShopBySupplierAsync(userId, cancellationToken)
               ?? throw new HttpException(409, "Create a shop before managing products.");
    }

    private async Task<ProductResponse> ToResponseAsync(Product product, CancellationToken cancellationToken)
    {
        var variants = await repository.GetVariantsAsync(product.Id, cancellationToken);
        var images = await repository.GetImagesAsync(product.Id, cancellationToken);
        return CatalogResponseHelper.ToResponse(product, variants, images);
    }

    private static Product BuildProduct(Guid shopId, SaveProductRequest request, Guid userId)
    {
        if (request.MaxOrderQty is int max && max < request.MinOrderQty)
            throw new HttpException(400, "Maximum order quantity must be greater than or equal to the minimum quantity.");
        if (request.Variants.Select(variant => variant.Sku.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != request.Variants.Count)
            throw new HttpException(400, "Variant SKUs must be unique within a product.");
        if (request.Variants.Any(variant => request.BasePrice + variant.PriceAdjustment <= 0))
            throw new HttpException(400, "Each variant must have a positive selling price.");
        if (request.Images.Count(image => image.IsPrimary) > 1)
            throw new HttpException(400, "A product can have at most one primary image.");
        return new Product
        {
            ShopId = shopId, CategoryId = request.CategoryId, Name = request.Name.Trim(), Description = request.Description?.Trim(),
            BasePrice = request.BasePrice, MinOrderQty = request.MinOrderQty, MaxOrderQty = request.MaxOrderQty,
            Unit = request.Unit?.Trim(), SupportsCustomPackaging = request.SupportsCustomPackaging,
            Status = ProductStatus.Active, CreatedBy = userId
        };
    }

    private static List<ProductVariant> BuildVariants(Guid productId, IReadOnlyList<SaveProductVariantRequest> requests) =>
        requests.Select(request => new ProductVariant
        {
            Id = request.Id ?? Guid.Empty, ProductId = productId, Name = request.Name.Trim(), Sku = request.Sku.Trim(), Flavor = request.Flavor?.Trim(),
            Filling = request.Filling?.Trim(), SizeLabel = request.SizeLabel?.Trim(), WeightGram = request.WeightGram,
            PriceAdjustment = request.PriceAdjustment, StockQty = request.StockQty, ImageUrl = request.ImageUrl
        }).ToList();

    private static List<ProductImage> BuildImages(Guid productId, IReadOnlyList<SaveProductImageRequest> requests) =>
        requests.Select(request => new ProductImage
        {
            ProductId = productId, ImageUrl = request.ImageUrl.Trim(), IsPrimary = request.IsPrimary, SortOrder = request.SortOrder
        }).ToList();
}
