namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

public sealed class CatalogRepository(ApplicationDbContext context, IDateTimeProvider clock) : ICatalogRepository
{
    public async Task<Shop?> GetVerifiedShopForSupplierAsync(Guid supplierUserId,
        CancellationToken cancellationToken = default) =>
        await (from shop in context.Shops.AsNoTracking()
               join profile in context.SupplierProfiles.AsNoTracking() on shop.SupplierId equals profile.Id
               where profile.UserId == supplierUserId && profile.VerificationStatus == SupplierVerificationStatus.Verified
                   && !profile.IsDeleted && !shop.IsDeleted
               select shop).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<ShopTemplate>> GetActiveShopTemplatesAsync(
        CancellationToken cancellationToken = default) =>
        await context.ShopTemplates.AsNoTracking().Where(template => template.IsActive && !template.IsDeleted)
            .OrderBy(template => template.SortOrder).ToListAsync(cancellationToken);

    public async Task<SupplierShopRecord?> GetSupplierShopAsync(Guid supplierUserId,
        CancellationToken cancellationToken = default) =>
        await (from shop in context.Shops.AsNoTracking()
               join profile in context.SupplierProfiles.AsNoTracking() on shop.SupplierId equals profile.Id
               join template in context.ShopTemplates.AsNoTracking() on shop.TemplateId equals template.Id into templates
               from template in templates.DefaultIfEmpty()
               where profile.UserId == supplierUserId && !profile.IsDeleted && !shop.IsDeleted
               select new SupplierShopRecord(shop, template)).FirstOrDefaultAsync(cancellationToken);

    public async Task<bool> SaveSupplierShopAsync(Shop shop, Guid supplierUserId,
        CancellationToken cancellationToken = default)
    {
        var supplier = await context.SupplierProfiles.FirstOrDefaultAsync(profile => profile.UserId == supplierUserId
            && profile.VerificationStatus == SupplierVerificationStatus.Verified && !profile.IsDeleted, cancellationToken);
        if (supplier is null || !await context.ShopTemplates.AnyAsync(template => template.Id == shop.TemplateId
                && template.IsActive && !template.IsDeleted, cancellationToken)) return false;
        if (await context.Shops.AnyAsync(item => item.Slug == shop.Slug && item.Id != shop.Id && !item.IsDeleted, cancellationToken))
            return false;
        var current = await context.Shops.FirstOrDefaultAsync(item => item.SupplierId == supplier.Id && !item.IsDeleted, cancellationToken);
        if (current is null)
        {
            shop.SupplierId = supplier.Id;
            shop.Status = ShopStatus.Active;
            context.Shops.Add(shop);
        }
        else
        {
            if (current.Id != shop.Id && shop.Id != Guid.Empty) return false;
            current.Name = shop.Name;
            current.Slug = shop.Slug;
            current.Description = shop.Description;
            current.BannerUrl = shop.BannerUrl;
            current.LogoUrl = shop.LogoUrl;
            current.TemplateId = shop.TemplateId;
            current.CustomColors = shop.CustomColors;
            current.UpdatedAt = shop.UpdatedAt;
            current.UpdatedBy = supplierUserId;
        }
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (exception.InnerException is Npgsql.PostgresException
            { SqlState: Npgsql.PostgresErrorCodes.UniqueViolation })
        {
            context.ChangeTracker.Clear();
            return false;
        }
    }

    public async Task<bool> SetSupplierShopStatusAsync(Guid supplierUserId, ShopStatus status,
        CancellationToken cancellationToken = default)
    {
        var shop = await (from item in context.Shops
                          join profile in context.SupplierProfiles on item.SupplierId equals profile.Id
                          where profile.UserId == supplierUserId && !profile.IsDeleted && !item.IsDeleted
                          select item).FirstOrDefaultAsync(cancellationToken);
        if (shop is null || shop.Status == ShopStatus.Suspended) return false;
        shop.Status = status;
        shop.UpdatedAt = clock.UtcNow;
        shop.UpdatedBy = supplierUserId;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<bool> CategoryExistsAsync(Guid categoryId, CancellationToken cancellationToken = default) =>
        context.Categories.AnyAsync(category => category.Id == categoryId && !category.IsDeleted, cancellationToken);

    public async Task<Product?> GetSupplierProductAsync(Guid productId, Guid supplierUserId,
        CancellationToken cancellationToken = default) =>
        await (from product in context.Products.AsNoTracking()
               join shop in context.Shops.AsNoTracking() on product.ShopId equals shop.Id
               join profile in context.SupplierProfiles.AsNoTracking() on shop.SupplierId equals profile.Id
               where product.Id == productId && profile.UserId == supplierUserId && !product.IsDeleted
                   && !shop.IsDeleted && !profile.IsDeleted
               select product).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<Product>> GetSupplierProductsAsync(Guid supplierUserId,
        CancellationToken cancellationToken = default) =>
        await (from product in context.Products.AsNoTracking()
               join shop in context.Shops.AsNoTracking() on product.ShopId equals shop.Id
               join profile in context.SupplierProfiles.AsNoTracking() on shop.SupplierId equals profile.Id
               where profile.UserId == supplierUserId && !product.IsDeleted && !shop.IsDeleted && !profile.IsDeleted
               orderby product.UpdatedAt descending
               select product).Take(200).ToListAsync(cancellationToken);

    public async Task<CatalogProductRecord?> GetSupplierProductRecordAsync(Guid productId, Guid supplierUserId,
        CancellationToken cancellationToken = default)
    {
        var product = await GetSupplierProductAsync(productId, supplierUserId, cancellationToken);
        if (product is null) return null;
        var records = await BuildRecordsAsync([product], cancellationToken);
        return records.FirstOrDefault();
    }

    public Task<bool> SkuExistsAsync(string sku, Guid? exceptVariantId = null,
        CancellationToken cancellationToken = default) =>
        context.ProductVariants.AnyAsync(variant => variant.Sku == sku && !variant.IsDeleted
            && (!exceptVariantId.HasValue || variant.Id != exceptVariantId.Value), cancellationToken);

    public async Task<bool> SaveProductAsync(Product product, IReadOnlyList<ProductVariant> variants,
        IReadOnlyList<ProductImage> images, Guid supplierUserId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var shopOwned = await (from shop in context.Shops
                               join profile in context.SupplierProfiles on shop.SupplierId equals profile.Id
                               where shop.Id == product.ShopId && profile.UserId == supplierUserId
                                   && profile.VerificationStatus == SupplierVerificationStatus.Verified
                                   && !shop.IsDeleted && !profile.IsDeleted
                               select shop.Id).AnyAsync(cancellationToken);
        if (!shopOwned || !await CategoryExistsAsync(product.CategoryId, cancellationToken)) return false;

        var existing = await context.Products.FirstOrDefaultAsync(item => item.Id == product.Id && !item.IsDeleted, cancellationToken);
        List<ProductVariant> previousVariants = [];
        List<ProductImage> previousImages = [];
        if (existing is not null)
        {
            if (existing.ShopId != product.ShopId) return false;
            previousVariants = await context.ProductVariants.Where(variant => variant.ProductId == product.Id && !variant.IsDeleted)
                .ToListAsync(cancellationToken);
            if (variants.Any(incoming => previousVariants.All(existingVariant => existingVariant.Id != incoming.Id)))
                return false;
            previousImages = await context.ProductImages.Where(image => image.ProductId == product.Id && !image.IsDeleted)
                .ToListAsync(cancellationToken);
            var incomingIds = variants.Select(variant => variant.Id).ToHashSet();
            var removedVariantIds = previousVariants.Where(variant => !incomingIds.Contains(variant.Id))
                .Select(variant => variant.Id).ToArray();
            var activeOrderStatuses = new[] { OrderStatus.Pending, OrderStatus.Confirmed, OrderStatus.Processing, OrderStatus.Shipped };
            if (removedVariantIds.Length > 0 && await (from item in context.OrderItems
                    join order in context.Orders on item.OrderId equals order.Id
                    where removedVariantIds.Contains(item.VariantId) && activeOrderStatuses.Contains(order.Status)
                    select item.Id).AnyAsync(cancellationToken)) return false;
            CopyProduct(product, existing);
            product = existing;
            var now = clock.UtcNow;
            foreach (var variant in previousVariants)
            {
                var incoming = variants.FirstOrDefault(item => item.Id == variant.Id);
                if (incoming is null)
                {
                    variant.IsDeleted = true;
                    variant.UpdatedAt = now;
                    variant.UpdatedBy = supplierUserId;
                    continue;
                }
                CopyVariant(incoming, variant);
                variant.UpdatedAt = now;
                variant.UpdatedBy = supplierUserId;
            }
            foreach (var variant in variants.Where(item => previousVariants.All(old => old.Id != item.Id)))
                context.ProductVariants.Add(variant);
            foreach (var image in previousImages)
            {
                image.IsDeleted = true;
                image.UpdatedAt = now;
                image.UpdatedBy = supplierUserId;
            }
        }
        else
        {
            context.Products.Add(product);
            context.ProductVariants.AddRange(variants);
        }
        context.ProductImages.AddRange(images);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> AdjustStockAsync(Guid productId, Guid variantId, int delta, Guid supplierUserId,
        string reason, CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var product = await context.Products.FromSqlInterpolated($"SELECT * FROM products WHERE id = {productId} FOR UPDATE")
            .FirstOrDefaultAsync(cancellationToken);
        if (product is null || product.IsDeleted) return false;
        var supplierOwnsProduct = await (from shop in context.Shops
                                         join profile in context.SupplierProfiles on shop.SupplierId equals profile.Id
                                         where shop.Id == product.ShopId && profile.UserId == supplierUserId
                                             && profile.VerificationStatus == SupplierVerificationStatus.Verified
                                             && !shop.IsDeleted && !profile.IsDeleted
                                         select shop.Id).AnyAsync(cancellationToken);
        if (!supplierOwnsProduct) return false;
        var variant = await context.ProductVariants.FromSqlInterpolated(
                $"SELECT * FROM product_variants WHERE id = {variantId} AND product_id = {productId} FOR UPDATE")
            .FirstOrDefaultAsync(cancellationToken);
        if (variant is null || variant.IsDeleted || (long)variant.StockQty + delta < 0) return false;
        var previousStock = variant.StockQty;
        variant.StockQty = checked(variant.StockQty + delta);
        variant.UpdatedAt = clock.UtcNow;
        variant.UpdatedBy = supplierUserId;
        if (product.Status == ProductStatus.OutOfStock && variant.StockQty > 0) product.Status = ProductStatus.Active;
        else if (!await context.ProductVariants.AnyAsync(item => item.ProductId == productId && !item.IsDeleted
                     && item.Id != variantId && item.StockQty > 0, cancellationToken) && variant.StockQty == 0)
            product.Status = ProductStatus.OutOfStock;
        product.UpdatedAt = clock.UtcNow;
        product.UpdatedBy = supplierUserId;
        context.WorkflowEvents.Add(new WorkflowEvent
        {
            AggregateType = "ProductVariant", AggregateId = variantId, EventType = "StockAdjusted",
            ActorUserId = supplierUserId, Reason = reason,
            Metadata = System.Text.Json.JsonSerializer.Serialize(new { previousStock, variant.StockQty, delta })
        });
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private static void CopyProduct(Product source, Product target)
    {
        target.CategoryId = source.CategoryId;
        target.Name = source.Name;
        target.Description = source.Description;
        target.BasePrice = source.BasePrice;
        target.MinOrderQty = source.MinOrderQty;
        target.MaxOrderQty = source.MaxOrderQty;
        target.Unit = source.Unit;
        target.SupportsCustomPackaging = source.SupportsCustomPackaging;
        target.Status = source.Status;
        target.UpdatedAt = source.UpdatedAt;
        target.UpdatedBy = source.UpdatedBy;
    }

    private static void CopyVariant(ProductVariant source, ProductVariant target)
    {
        target.Name = source.Name;
        target.Sku = source.Sku;
        target.Flavor = source.Flavor;
        target.Filling = source.Filling;
        target.SizeLabel = source.SizeLabel;
        target.WeightGram = source.WeightGram;
        target.PriceAdjustment = source.PriceAdjustment;
        target.ImageUrl = source.ImageUrl;
    }

    public async Task<(IReadOnlyList<CatalogProductRecord> Items, int TotalCount)> SearchAsync(
        CatalogSearchFilter filter, CancellationToken cancellationToken = default)
    {
        var query = PublicProducts();
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var pattern = $"%{filter.Search}%";
            query = query.Where(product => EF.Functions.ILike(product.Name, pattern)
                || (product.Description != null && EF.Functions.ILike(product.Description, pattern))
                || context.Shops.Any(shop => shop.Id == product.ShopId && EF.Functions.ILike(shop.Name!, pattern))
                || context.SupplierProfiles.Any(profile => context.Shops.Any(shop => shop.Id == product.ShopId
                    && shop.SupplierId == profile.Id && profile.CompanyName != null
                    && EF.Functions.ILike(profile.CompanyName, pattern))));
        }
        if (filter.CategoryId.HasValue) query = query.Where(product => product.CategoryId == filter.CategoryId.Value);
        if (filter.ShopId.HasValue) query = query.Where(product => product.ShopId == filter.ShopId.Value);
        if (filter.MinPrice.HasValue) query = query.Where(product => product.BasePrice >= filter.MinPrice.Value);
        if (filter.MaxPrice.HasValue) query = query.Where(product => product.BasePrice <= filter.MaxPrice.Value);
        if (filter.MinOrderQty.HasValue) query = query.Where(product => product.MinOrderQty >= filter.MinOrderQty.Value);
        if (filter.SupportsCustomPackaging == true) query = query.Where(product => product.SupportsCustomPackaging);

        var totalCount = await query.CountAsync(cancellationToken);
        query = filter.SortBy?.ToLowerInvariant() switch
        {
            "price_asc" => query.OrderBy(product => product.BasePrice),
            "price_desc" => query.OrderByDescending(product => product.BasePrice),
            "reputation" => query.OrderByDescending(product => context.SupplierProfiles
                .Where(profile => context.Shops.Any(shop => shop.Id == product.ShopId && shop.SupplierId == profile.Id))
                .Select(profile => profile.ReputationScore).FirstOrDefault()),
            "rating" => query.OrderByDescending(product => context.Reviews
                .Where(review => context.Shops.Any(shop => shop.Id == product.ShopId && shop.SupplierId == review.SupplierId)
                    && review.IsPublic && !review.IsDeleted).Average(review => (decimal?)review.OverallRating)),
            _ => query.OrderByDescending(product => product.CreatedAt)
        };
        var products = await query.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToListAsync(cancellationToken);
        return (await BuildRecordsAsync(products, cancellationToken), totalCount);
    }

    public async Task<CatalogProductRecord?> GetProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var product = await PublicProducts().FirstOrDefaultAsync(item => item.Id == productId, cancellationToken);
        if (product is null) return null;
        return (await BuildRecordsAsync([product], cancellationToken)).FirstOrDefault();
    }

    public async Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        await context.Categories.AsNoTracking().Where(category => !category.IsDeleted)
            .OrderBy(category => category.SortOrder).ThenBy(category => category.Name).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CatalogProductRecord>> GetFeaturedAsync(int limit, CancellationToken cancellationToken = default)
    {
        var products = await PublicProducts().OrderByDescending(product => product.CreatedAt).Take(limit)
            .ToListAsync(cancellationToken);
        return await BuildRecordsAsync(products, cancellationToken);
    }

    public async Task<IReadOnlyList<CatalogProductRecord>> GetShopProductsAsync(Guid shopId,
        CancellationToken cancellationToken = default)
    {
        var products = await PublicProducts().Where(product => product.ShopId == shopId)
            .OrderByDescending(product => product.CreatedAt).Take(100).ToListAsync(cancellationToken);
        return await BuildRecordsAsync(products, cancellationToken);
    }

    public async Task<ShopCatalogRecord?> GetShopBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var result = await (from shop in context.Shops.AsNoTracking()
                            join profile in context.SupplierProfiles.AsNoTracking() on shop.SupplierId equals profile.Id
                            where shop.Slug == slug && shop.Status == ShopStatus.Active && !shop.IsDeleted
                                && profile.VerificationStatus == SupplierVerificationStatus.Verified && !profile.IsDeleted
                            select new ShopCatalogRecord(shop, profile)).FirstOrDefaultAsync(cancellationToken);
        return result;
    }

    public async Task<IReadOnlyList<CatalogSuggestionRecord>> GetSuggestionsAsync(string query, int limit,
        CancellationToken cancellationToken = default)
    {
        var pattern = $"%{query}%";
        return await (from product in PublicProducts()
                      join shop in context.Shops.AsNoTracking() on product.ShopId equals shop.Id
                      where EF.Functions.ILike(product.Name, pattern)
                      orderby product.Name
                      select new CatalogSuggestionRecord(product.Id, product.Name, shop.Id, shop.Name))
            .Take(limit).ToListAsync(cancellationToken);
    }

    private IQueryable<Product> PublicProducts() =>
        from product in context.Products.AsNoTracking()
        join shop in context.Shops.AsNoTracking() on product.ShopId equals shop.Id
        join profile in context.SupplierProfiles.AsNoTracking() on shop.SupplierId equals profile.Id
        where product.Status == ProductStatus.Active && !product.IsDeleted && shop.Status == ShopStatus.Active
            && !shop.IsDeleted && profile.VerificationStatus == SupplierVerificationStatus.Verified && !profile.IsDeleted
        select product;

    private async Task<IReadOnlyList<CatalogProductRecord>> BuildRecordsAsync(IReadOnlyList<Product> products,
        CancellationToken cancellationToken)
    {
        if (products.Count == 0) return [];
        var productIds = products.Select(product => product.Id).ToArray();
        var details = await (from product in context.Products.AsNoTracking()
                             join shop in context.Shops.AsNoTracking() on product.ShopId equals shop.Id
                             join profile in context.SupplierProfiles.AsNoTracking() on shop.SupplierId equals profile.Id
                             where productIds.Contains(product.Id) && !product.IsDeleted && !shop.IsDeleted && !profile.IsDeleted
                             select new { Product = product, Shop = shop, Supplier = profile }).ToListAsync(cancellationToken);
        var variants = await context.ProductVariants.AsNoTracking().Where(variant => productIds.Contains(variant.ProductId) && !variant.IsDeleted)
            .OrderBy(variant => variant.CreatedAt).ToListAsync(cancellationToken);
        var images = await context.ProductImages.AsNoTracking().Where(image => productIds.Contains(image.ProductId) && !image.IsDeleted)
            .OrderBy(image => image.SortOrder).ToListAsync(cancellationToken);
        var supplierIds = details.Select(item => item.Supplier.Id).Distinct().ToArray();
        var ratings = await context.Reviews.AsNoTracking().Where(review => supplierIds.Contains(review.SupplierId)
                && review.IsPublic && !review.IsDeleted && review.OverallRating.HasValue)
            .GroupBy(review => review.SupplierId)
            .Select(group => new
            {
                SupplierId = group.Key,
                Average = group.Average(review => (decimal)review.OverallRating!.Value),
                Count = group.Count()
            })
            .ToDictionaryAsync(item => item.SupplierId, cancellationToken);
        return details.Select(item =>
        {
            ratings.TryGetValue(item.Supplier.Id, out var rating);
            return new CatalogProductRecord(item.Product, item.Shop, item.Supplier,
                variants.Where(variant => variant.ProductId == item.Product.Id).ToArray(),
                images.Where(image => image.ProductId == item.Product.Id).ToArray(), rating?.Average, rating?.Count ?? 0);
        }).ToArray();
    }
}
