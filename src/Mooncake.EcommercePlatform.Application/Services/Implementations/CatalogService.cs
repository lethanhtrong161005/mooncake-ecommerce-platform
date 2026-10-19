namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using System.Text.Json;
using System.Text.RegularExpressions;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Catalog.Responses;
using Mooncake.EcommercePlatform.Application.DTOs.Catalog.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

public sealed class CatalogService(ICatalogRepository repository, IDateTimeProvider clock) : ICatalogService
{
    public async Task<CatalogSearchResponse> SearchAsync(string? search, Guid? categoryId, Guid? shopId,
        decimal? minPrice, decimal? maxPrice, int? minOrderQty, bool? supportsCustomPackaging, string? sortBy,
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (minPrice < 0 || maxPrice < 0 || (minPrice.HasValue && maxPrice.HasValue && minPrice > maxPrice))
            throw new HttpException(400, "Price range is invalid.");
        if (minOrderQty is < 1) throw new HttpException(400, "Minimum order quantity must be positive.");
        if (page < 1 || pageSize is < 1 or > 100) throw new HttpException(400, "Pagination values are invalid.");
        var filter = new CatalogSearchFilter(search?.Trim(), categoryId, shopId, minPrice, maxPrice,
            minOrderQty, supportsCustomPackaging, sortBy?.Trim(), page, pageSize);
        var result = await repository.SearchAsync(filter, cancellationToken);
        return new CatalogSearchResponse(result.Items.Select(ToResponse).ToArray(), page, pageSize, result.TotalCount);
    }

    public async Task<CatalogProductResponse> GetProductAsync(Guid productId, CancellationToken cancellationToken = default) =>
        ToResponse(await repository.GetProductAsync(productId, cancellationToken)
                    ?? throw new HttpException(404, "Product was not found."));

    public async Task<IReadOnlyList<CategoryResponse>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        (await repository.GetCategoriesAsync(cancellationToken)).Select(category => new CategoryResponse(category.Id,
            category.ParentId, category.Name, category.Description, category.IconUrl, category.SortOrder)).ToArray();

    public async Task<IReadOnlyList<CatalogProductResponse>> GetFeaturedAsync(int limit, CancellationToken cancellationToken = default)
    {
        if (limit is < 1 or > 50) throw new HttpException(400, "Featured product limit must be between 1 and 50.");
        return (await repository.GetFeaturedAsync(limit, cancellationToken)).Select(ToResponse).ToArray();
    }

    public async Task<PublicShopResponse> GetShopAsync(string slug, CancellationToken cancellationToken = default)
    {
        var result = await repository.GetShopBySlugAsync(slug.Trim(), cancellationToken)
                     ?? throw new HttpException(404, "Shop was not found.");
        var products = await repository.GetShopProductsAsync(result.Shop.Id, cancellationToken);
        return new PublicShopResponse(CatalogResponseHelper.ToResponse(result.Shop), result.Supplier.CompanyName,
            result.Supplier.ReputationScore, products.Select(item => CatalogResponseHelper.ToResponse(item.Product,
                item.Variants, item.Images)).ToArray());
    }

    public async Task<IReadOnlyList<CatalogSuggestionResponse>> GetSuggestionsAsync(string query, int limit,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2) return [];
        if (limit is < 1 or > 20) throw new HttpException(400, "Suggestion limit must be between 1 and 20.");
        return (await repository.GetSuggestionsAsync(query.Trim(), limit, cancellationToken))
            .Select(item => new CatalogSuggestionResponse(item.ProductId, item.ProductName, item.ShopId, item.ShopName)).ToArray();
    }

    public async Task<ProductResponse> SaveSupplierProductAsync(Guid supplierUserId, Guid? productId,
        SaveProductRequest request, CancellationToken cancellationToken = default)
    {
        if (request.MaxOrderQty.HasValue && request.MaxOrderQty < request.MinOrderQty)
            throw new HttpException(400, "Maximum order quantity cannot be lower than the minimum.");
        if (!Enum.IsDefined(request.Status)) throw new HttpException(400, "Product status is invalid.");
        if (request.Images.Count > 0 && request.Images.Count(image => image.IsPrimary) != 1)
            throw new HttpException(400, "Exactly one product image must be primary.");
        var shop = await repository.GetVerifiedShopForSupplierAsync(supplierUserId, cancellationToken)
                   ?? throw new HttpException(403, "A verified supplier shop is required to manage products.");
        if (!await repository.CategoryExistsAsync(request.CategoryId, cancellationToken))
            throw new HttpException(400, "Product category was not found.");
        Product? existing = null;
        if (productId.HasValue)
        {
            existing = await repository.GetSupplierProductAsync(productId.Value, supplierUserId, cancellationToken)
                       ?? throw new HttpException(404, "Product was not found.");
            if (existing.ShopId != shop.Id) throw new HttpException(404, "Product was not found.");
        }
        foreach (var variant in request.Variants)
            if (await repository.SkuExistsAsync(variant.Sku.Trim(), variant.Id, cancellationToken))
                throw new HttpException(409, $"SKU '{variant.Sku}' is already in use.");

        var now = clock.UtcNow;
        var product = existing ?? new Product { Id = Guid.NewGuid(), ShopId = shop.Id, CreatedBy = supplierUserId, CreatedAt = now };
        product.CategoryId = request.CategoryId;
        product.Name = request.Name.Trim();
        product.Description = request.Description?.Trim();
        product.BasePrice = request.BasePrice;
        product.MinOrderQty = request.MinOrderQty;
        product.MaxOrderQty = request.MaxOrderQty;
        product.Unit = request.Unit?.Trim();
        product.SupportsCustomPackaging = request.SupportsCustomPackaging;
        product.Status = request.Status;
        product.UpdatedBy = supplierUserId;
        product.UpdatedAt = now;
        var variants = request.Variants.Select(item => new ProductVariant
        {
            Id = item.Id ?? Guid.NewGuid(), ProductId = product.Id, Name = item.Name.Trim(), Sku = item.Sku.Trim(),
            Flavor = item.Flavor?.Trim(), Filling = item.Filling?.Trim(), SizeLabel = item.SizeLabel?.Trim(),
            WeightGram = item.WeightGram, PriceAdjustment = item.PriceAdjustment, StockQty = item.InitialStockQty,
            ImageUrl = item.ImageUrl?.Trim(), CreatedBy = supplierUserId, UpdatedBy = supplierUserId,
            CreatedAt = now, UpdatedAt = now
        }).ToArray();
        var images = request.Images.Select((item, index) => new ProductImage
        {
            Id = Guid.NewGuid(), ProductId = product.Id, ImageUrl = item.ImageUrl.Trim(),
            IsPrimary = item.IsPrimary, SortOrder = item.SortOrder == 0 ? index : item.SortOrder,
            CreatedBy = supplierUserId, CreatedAt = now, UpdatedAt = now
        }).ToArray();
        if (!await repository.SaveProductAsync(product, variants, images, supplierUserId, cancellationToken))
            throw new HttpException(409, "A product variant in use by an active order cannot be removed.");
        var saved = await repository.GetSupplierProductRecordAsync(product.Id, supplierUserId, cancellationToken)
                    ?? throw new HttpException(500, "Saved product could not be retrieved.");
        return CatalogResponseHelper.ToResponse(saved.Product, saved.Variants, saved.Images);
    }

    public async Task<IReadOnlyList<ProductResponse>> GetSupplierProductsAsync(Guid supplierUserId,
        CancellationToken cancellationToken = default)
    {
        var products = await repository.GetSupplierProductsAsync(supplierUserId, cancellationToken);
        var responses = new List<ProductResponse>(products.Count);
        foreach (var product in products)
        {
            var full = await repository.GetSupplierProductRecordAsync(product.Id, supplierUserId, cancellationToken);
            if (full is not null) responses.Add(CatalogResponseHelper.ToResponse(full.Product, full.Variants, full.Images));
        }
        return responses;
    }

    public async Task<ProductResponse> AdjustStockAsync(Guid supplierUserId, Guid productId,
        AdjustProductStockRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Delta == 0) throw new HttpException(400, "Stock adjustment must be non-zero.");
        if (!await repository.AdjustStockAsync(productId, request.VariantId, request.Delta, supplierUserId,
                request.Reason.Trim(), cancellationToken))
            throw new HttpException(409, "The product variant was not found, is not owned by this supplier, or has insufficient stock.");
        var product = await repository.GetProductAsync(productId, cancellationToken)
                      ?? throw new HttpException(404, "Product was not found.");
        return CatalogResponseHelper.ToResponse(product.Product, product.Variants, product.Images);
    }

    public async Task<IReadOnlyList<ShopTemplateResponse>> GetActiveShopTemplatesAsync(
        CancellationToken cancellationToken = default) =>
        (await repository.GetActiveShopTemplatesAsync(cancellationToken)).Select(template =>
            new ShopTemplateResponse(template.Id, template.Name, template.Description, template.PreviewImageUrl,
                template.CssVariables, template.LayoutConfig, template.SortOrder)).ToArray();

    public async Task<SupplierShopResponse> GetSupplierShopAsync(Guid supplierUserId,
        CancellationToken cancellationToken = default)
    {
        var result = await repository.GetSupplierShopAsync(supplierUserId, cancellationToken)
                     ?? throw new HttpException(404, "Supplier shop was not found.");
        return ToSupplierShopResponse(result);
    }

    public async Task<SupplierShopResponse> SaveSupplierShopAsync(Guid supplierUserId, SaveShopRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateCustomColors(request.CustomColors);
        var current = await repository.GetSupplierShopAsync(supplierUserId, cancellationToken);
        var now = clock.UtcNow;
        var shop = current?.Shop ?? new Shop
        {
            Id = Guid.NewGuid(), SupplierId = Guid.Empty, CreatedAt = now, CreatedBy = supplierUserId,
            Status = ShopStatus.Active
        };
        shop.Name = request.Name.Trim();
        shop.Slug = request.Slug.Trim();
        shop.Description = request.Description?.Trim();
        shop.BannerUrl = request.BannerUrl?.Trim();
        shop.LogoUrl = request.LogoUrl?.Trim();
        shop.TemplateId = request.TemplateId;
        shop.CustomColors = request.CustomColors;
        shop.UpdatedAt = now;
        shop.UpdatedBy = supplierUserId;
        if (!await repository.SaveSupplierShopAsync(shop, supplierUserId, cancellationToken))
            throw new HttpException(409, "The shop slug is already in use or the selected template is unavailable.");
        return await GetSupplierShopAsync(supplierUserId, cancellationToken);
    }

    public async Task<SupplierShopResponse> SetSupplierShopStatusAsync(Guid supplierUserId, ShopStatus status,
        CancellationToken cancellationToken = default)
    {
        if (status is not (ShopStatus.Active or ShopStatus.Inactive))
            throw new HttpException(400, "Suppliers can only set their shop to active or inactive.");
        if (!await repository.SetSupplierShopStatusAsync(supplierUserId, status, cancellationToken))
            throw new HttpException(409, "A suspended shop cannot be reactivated by its supplier.");
        return await GetSupplierShopAsync(supplierUserId, cancellationToken);
    }

    private static SupplierShopResponse ToSupplierShopResponse(SupplierShopRecord result) =>
        new(CatalogResponseHelper.ToResponse(result.Shop), result.Shop.CustomColors, result.Template?.Name);

    private static void ValidateCustomColors(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        try
        {
            using var document = JsonDocument.Parse(value);
            if (document.RootElement.ValueKind != JsonValueKind.Object || document.RootElement.EnumerateObject().Count() > 20)
                throw new HttpException(400, "Custom colors must be a JSON object with no more than 20 entries.");
            foreach (var color in document.RootElement.EnumerateObject())
                if (!Regex.IsMatch(color.Name, "^--[a-z0-9-]{1,40}$", RegexOptions.CultureInvariant)
                    || color.Value.ValueKind != JsonValueKind.String
                    || !Regex.IsMatch(color.Value.GetString() ?? string.Empty,
                        "^(#[0-9a-fA-F]{3,8}|rgba?\\([0-9.,% ]+\\))$", RegexOptions.CultureInvariant))
                    throw new HttpException(400, "Custom color keys and values are invalid.");
        }
        catch (JsonException)
        {
            throw new HttpException(400, "Custom colors must be valid JSON.");
        }
    }

    private static CatalogProductResponse ToResponse(CatalogProductRecord item) => new(
        CatalogResponseHelper.ToResponse(item.Product, item.Variants, item.Images), item.Shop.Slug, item.Shop.Name,
        item.Supplier.CompanyName, item.Supplier.ReputationScore, item.AverageRating, item.ReviewCount);
}
