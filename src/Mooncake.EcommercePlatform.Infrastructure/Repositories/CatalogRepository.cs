namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

/// <summary>EF Core persistence for public catalog and supplier-owned catalog data.</summary>
public sealed class CatalogRepository(ApplicationDbContext context) : ICatalogRepository
{
    public async Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        await context.Categories.AsNoTracking().Where(category => !category.IsDeleted)
            .OrderBy(category => category.SortOrder).ThenBy(category => category.Name).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ShopTemplate>> GetActiveTemplatesAsync(CancellationToken cancellationToken = default) =>
        await context.ShopTemplates.AsNoTracking().Where(template => !template.IsDeleted && template.IsActive)
            .OrderBy(template => template.SortOrder).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ShopTemplate>> GetAllShopTemplatesAsync(CancellationToken cancellationToken = default) =>
        await context.ShopTemplates.AsNoTracking().Where(template => !template.IsDeleted)
            .OrderBy(template => template.SortOrder).ToListAsync(cancellationToken);

    public Task<SupplierProfile?> GetSupplierProfileAsync(Guid userId, CancellationToken cancellationToken = default) =>
        context.SupplierProfiles.AsNoTracking().FirstOrDefaultAsync(profile => profile.UserId == userId && !profile.IsDeleted, cancellationToken);

    public Task<Shop?> GetShopBySupplierAsync(Guid userId, CancellationToken cancellationToken = default) =>
        (from shop in context.Shops
         join profile in context.SupplierProfiles on shop.SupplierId equals profile.Id
         where profile.UserId == userId && !profile.IsDeleted && !shop.IsDeleted
         select shop).FirstOrDefaultAsync(cancellationToken);

    public Task<Shop?> GetShopBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        (from shop in context.Shops.AsNoTracking()
         join profile in context.SupplierProfiles.AsNoTracking() on shop.SupplierId equals profile.Id
         where shop.Slug == slug && shop.Status == ShopStatus.Active && !shop.IsDeleted && !profile.IsDeleted
               && profile.VerificationStatus == SupplierVerificationStatus.Verified
         select shop).FirstOrDefaultAsync(cancellationToken);

    public Task<Shop?> GetShopAsync(Guid shopId, CancellationToken cancellationToken = default) =>
        context.Shops.AsNoTracking().FirstOrDefaultAsync(shop => shop.Id == shopId && !shop.IsDeleted, cancellationToken);

    public Task<bool> IsSlugTakenAsync(string slug, Guid? exceptShopId = null, CancellationToken cancellationToken = default) =>
        context.Shops.AnyAsync(shop => shop.Slug == slug && !shop.IsDeleted && (exceptShopId == null || shop.Id != exceptShopId), cancellationToken);

    public Task<bool> IsTemplateActiveAsync(Guid templateId, CancellationToken cancellationToken = default) =>
        context.ShopTemplates.AnyAsync(template => template.Id == templateId && template.IsActive && !template.IsDeleted, cancellationToken);

    public Task<bool> CategoryExistsAsync(Guid categoryId, CancellationToken cancellationToken = default) =>
        context.Categories.AnyAsync(category => category.Id == categoryId && !category.IsDeleted, cancellationToken);

    public Task<Category?> GetCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default) =>
        context.Categories.FirstOrDefaultAsync(category => category.Id == categoryId && !category.IsDeleted, cancellationToken);

    public Task<bool> IsCategoryNameTakenAsync(string name, Guid? exceptCategoryId = null, CancellationToken cancellationToken = default) =>
        context.Categories.AnyAsync(category => category.Name.ToLower() == name.ToLower() && !category.IsDeleted
            && (exceptCategoryId == null || category.Id != exceptCategoryId), cancellationToken);

    public Task<bool> HasCategoryChildrenAsync(Guid categoryId, CancellationToken cancellationToken = default) =>
        context.Categories.AnyAsync(category => category.ParentId == categoryId && !category.IsDeleted, cancellationToken);

    public Task<bool> HasProductsInCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default) =>
        context.Products.AnyAsync(product => product.CategoryId == categoryId && !product.IsDeleted, cancellationToken);

    public async Task<bool> WouldCreateCategoryCycleAsync(Guid categoryId, Guid parentCategoryId, CancellationToken cancellationToken = default)
    {
        var visited = new HashSet<Guid>();
        Guid? currentId = parentCategoryId;
        while (currentId.HasValue)
        {
            if (currentId.Value == categoryId || !visited.Add(currentId.Value))
                return true;
            currentId = await context.Categories.AsNoTracking()
                .Where(category => category.Id == currentId.Value && !category.IsDeleted)
                .Select(category => category.ParentId)
                .FirstOrDefaultAsync(cancellationToken);
        }
        return false;
    }

    public async Task SaveCategoryAsync(Category category, CancellationToken cancellationToken = default)
    {
        if (category.Id == Guid.Empty)
            context.Categories.Add(category);
        else
            context.Categories.Update(category);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var category = await context.Categories.FirstOrDefaultAsync(item => item.Id == categoryId && !item.IsDeleted, cancellationToken);
        if (category is null)
            return false;
        category.IsDeleted = true;
        category.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<ShopTemplate?> GetShopTemplateAsync(Guid templateId, CancellationToken cancellationToken = default) =>
        context.ShopTemplates.FirstOrDefaultAsync(template => template.Id == templateId && !template.IsDeleted, cancellationToken);

    public Task<bool> IsTemplateNameTakenAsync(string name, Guid? exceptTemplateId = null, CancellationToken cancellationToken = default) =>
        context.ShopTemplates.AnyAsync(template => template.Name != null && template.Name.ToLower() == name.ToLower()
            && !template.IsDeleted && (exceptTemplateId == null || template.Id != exceptTemplateId), cancellationToken);

    public Task<bool> IsTemplateInUseAsync(Guid templateId, CancellationToken cancellationToken = default) =>
        context.Shops.AnyAsync(shop => shop.TemplateId == templateId && !shop.IsDeleted, cancellationToken);

    public async Task SaveShopTemplateAsync(ShopTemplate template, CancellationToken cancellationToken = default)
    {
        if (template.Id == Guid.Empty)
            context.ShopTemplates.Add(template);
        else
            context.ShopTemplates.Update(template);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteShopTemplateAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        var template = await context.ShopTemplates.FirstOrDefaultAsync(item => item.Id == templateId && !item.IsDeleted, cancellationToken);
        if (template is null)
            return false;
        template.IsDeleted = true;
        template.IsActive = false;
        template.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task SaveShopAsync(Shop shop, CancellationToken cancellationToken = default)
    {
        if (shop.Id == Guid.Empty)
            context.Shops.Add(shop);
        else
            context.Shops.Update(shop);
        shop.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<CatalogSearchPage> SearchProductsAsync(string? search, Guid? categoryId, Guid? shopId, decimal? minPrice, decimal? maxPrice, string sortBy, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = from product in context.Products.AsNoTracking()
                    join shop in context.Shops.AsNoTracking() on product.ShopId equals shop.Id
                    join profile in context.SupplierProfiles.AsNoTracking() on shop.SupplierId equals profile.Id
                    where !product.IsDeleted && product.Status == ProductStatus.Active && !shop.IsDeleted && shop.Status == ShopStatus.Active
                          && !profile.IsDeleted && profile.VerificationStatus == SupplierVerificationStatus.Verified
                    select product;
        if (categoryId.HasValue)
            query = query.Where(product => product.CategoryId == categoryId.Value);
        if (shopId.HasValue)
            query = query.Where(product => product.ShopId == shopId.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(product => product.Name.ToLower().Contains(search.ToLower()) || (product.Description != null && product.Description.ToLower().Contains(search.ToLower())));
        if (minPrice.HasValue || maxPrice.HasValue)
            query = query.Where(product => context.ProductVariants.Any(variant => variant.ProductId == product.Id && !variant.IsDeleted
                && (!minPrice.HasValue || product.BasePrice + variant.PriceAdjustment >= minPrice.Value)
                && (!maxPrice.HasValue || product.BasePrice + variant.PriceAdjustment <= maxPrice.Value)));

        var totalCount = await query.CountAsync(cancellationToken);
        query = sortBy switch
        {
            "price_asc" => query.OrderBy(product => context.ProductVariants.Where(variant => variant.ProductId == product.Id && !variant.IsDeleted)
                .Select(variant => product.BasePrice + variant.PriceAdjustment).Min()),
            "price_desc" => query.OrderByDescending(product => context.ProductVariants.Where(variant => variant.ProductId == product.Id && !variant.IsDeleted)
                .Select(variant => product.BasePrice + variant.PriceAdjustment).Max()),
            "name" => query.OrderBy(product => product.Name),
            "oldest" => query.OrderBy(product => product.CreatedAt),
            _ => query.OrderByDescending(product => product.CreatedAt)
        };
        var products = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new CatalogSearchPage(products, totalCount);
    }

    public async Task<IReadOnlyList<string>> GetProductSuggestionsAsync(string query, int limit, CancellationToken cancellationToken = default) =>
        await (from product in context.Products.AsNoTracking()
               join shop in context.Shops.AsNoTracking() on product.ShopId equals shop.Id
               join profile in context.SupplierProfiles.AsNoTracking() on shop.SupplierId equals profile.Id
               where !product.IsDeleted && product.Status == ProductStatus.Active && !shop.IsDeleted && shop.Status == ShopStatus.Active
                     && !profile.IsDeleted && profile.VerificationStatus == SupplierVerificationStatus.Verified
                     && product.Name.ToLower().Contains(query.ToLower())
               select product.Name).Distinct().OrderBy(name => name).Take(limit).ToListAsync(cancellationToken);

    public Task<Product?> GetProductAsync(Guid productId, CancellationToken cancellationToken = default) =>
        (from product in context.Products
         join shop in context.Shops on product.ShopId equals shop.Id
         join profile in context.SupplierProfiles on shop.SupplierId equals profile.Id
         where product.Id == productId && !product.IsDeleted && !shop.IsDeleted && shop.Status == ShopStatus.Active
               && !profile.IsDeleted && profile.VerificationStatus == SupplierVerificationStatus.Verified
         select product).FirstOrDefaultAsync(cancellationToken);

    public Task<Product?> GetSupplierProductAsync(Guid productId, Guid shopId, CancellationToken cancellationToken = default) =>
        context.Products.FirstOrDefaultAsync(product => product.Id == productId && product.ShopId == shopId && !product.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<Product>> GetSupplierProductsAsync(Guid shopId, CancellationToken cancellationToken = default) =>
        await context.Products.AsNoTracking().Where(product => product.ShopId == shopId && !product.IsDeleted)
            .OrderByDescending(product => product.UpdatedAt).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ProductVariant>> GetVariantsAsync(Guid productId, CancellationToken cancellationToken = default) =>
        await context.ProductVariants.AsNoTracking().Where(variant => variant.ProductId == productId && !variant.IsDeleted)
            .OrderBy(variant => variant.Name).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ProductImage>> GetImagesAsync(Guid productId, CancellationToken cancellationToken = default) =>
        await context.ProductImages.AsNoTracking().Where(image => image.ProductId == productId && !image.IsDeleted)
            .OrderByDescending(image => image.IsPrimary).ThenBy(image => image.SortOrder).ToListAsync(cancellationToken);

    public async Task<bool> SaveProductAsync(Product product, IReadOnlyList<ProductVariant> variants, IReadOnlyList<ProductImage> images, CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        if (product.Id == Guid.Empty)
            context.Products.Add(product);
        else
            context.Products.Update(product);
        product.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        var currentVariants = await context.ProductVariants
            .Where(variant => variant.ProductId == product.Id && !variant.IsDeleted).ToListAsync(cancellationToken);
        var currentImages = await context.ProductImages
            .Where(image => image.ProductId == product.Id && !image.IsDeleted).ToListAsync(cancellationToken);
        var retainedVariantIds = new HashSet<Guid>();
        var newVariants = new List<ProductVariant>();

        foreach (var requestedVariant in variants)
        {
            var existingVariant = requestedVariant.Id != Guid.Empty
                ? currentVariants.FirstOrDefault(variant => variant.Id == requestedVariant.Id)
                : currentVariants.FirstOrDefault(variant => string.Equals(variant.Sku, requestedVariant.Sku, StringComparison.OrdinalIgnoreCase));
            if (requestedVariant.Id != Guid.Empty && existingVariant is null)
                return false;

            if (existingVariant is null)
            {
                requestedVariant.ProductId = product.Id;
                newVariants.Add(requestedVariant);
                continue;
            }

            existingVariant.Name = requestedVariant.Name;
            existingVariant.Sku = requestedVariant.Sku;
            existingVariant.Flavor = requestedVariant.Flavor;
            existingVariant.Filling = requestedVariant.Filling;
            existingVariant.SizeLabel = requestedVariant.SizeLabel;
            existingVariant.WeightGram = requestedVariant.WeightGram;
            existingVariant.PriceAdjustment = requestedVariant.PriceAdjustment;
            existingVariant.ImageUrl = requestedVariant.ImageUrl;
            existingVariant.UpdatedBy = product.UpdatedBy ?? product.CreatedBy;
            existingVariant.UpdatedAt = DateTime.UtcNow;
            requestedVariant.Id = existingVariant.Id;
            requestedVariant.StockQty = existingVariant.StockQty;
            retainedVariantIds.Add(existingVariant.Id);
        }

        var removedVariantIds = currentVariants.Where(variant => !retainedVariantIds.Contains(variant.Id)
                && variants.All(requested => requested.Id != Guid.Empty || !string.Equals(requested.Sku, variant.Sku, StringComparison.OrdinalIgnoreCase)))
            .Select(variant => variant.Id).ToArray();
        if (removedVariantIds.Length > 0 && await context.OrderItems.AsNoTracking()
                .Join(context.Orders.AsNoTracking().Where(order => !order.IsDeleted && order.Status == OrderStatus.Pending),
                    item => item.OrderId, order => order.Id, (item, _) => item)
                .AnyAsync(item => removedVariantIds.Contains(item.VariantId) && !item.IsDeleted, cancellationToken))
            return false;

        foreach (var removedVariant in currentVariants.Where(variant => removedVariantIds.Contains(variant.Id)))
        {
            removedVariant.IsDeleted = true;
            removedVariant.UpdatedAt = DateTime.UtcNow;
        }
        foreach (var oldImage in currentImages)
            oldImage.IsDeleted = true;
        foreach (var image in images)
            image.ProductId = product.Id;
        context.ProductVariants.AddRange(newVariants);
        context.ProductImages.AddRange(images);
        await context.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var product = await context.Products.FirstOrDefaultAsync(item => item.Id == productId && !item.IsDeleted, cancellationToken);
        if (product is null)
            return false;
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        product.IsDeleted = true;
        product.Status = ProductStatus.Inactive;
        product.UpdatedAt = DateTime.UtcNow;
        var variants = await context.ProductVariants.Where(variant => variant.ProductId == productId && !variant.IsDeleted).ToListAsync(cancellationToken);
        var images = await context.ProductImages.Where(image => image.ProductId == productId && !image.IsDeleted).ToListAsync(cancellationToken);
        foreach (var variant in variants)
            variant.IsDeleted = true;
        foreach (var image in images)
            image.IsDeleted = true;
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> AdjustVariantStockAsync(Guid productId, Guid variantId, int quantityDelta, Guid actorUserId, string reason, CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var changed = await context.ProductVariants
            .Where(variant => variant.Id == variantId && variant.ProductId == productId && !variant.IsDeleted
                && variant.StockQty + quantityDelta >= 0)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(variant => variant.StockQty, variant => variant.StockQty + quantityDelta)
                .SetProperty(variant => variant.UpdatedAt, now)
                .SetProperty(variant => variant.UpdatedBy, actorUserId), cancellationToken);
        if (changed != 1)
            return false;
        context.WorkflowEvents.Add(new WorkflowEvent
        {
            AggregateType = "ProductVariant", AggregateId = variantId, EventType = "StockAdjusted",
            ActorUserId = actorUserId, Reason = reason, Metadata = System.Text.Json.JsonSerializer.Serialize(new { quantityDelta }),
            CreatedAt = now
        });
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
