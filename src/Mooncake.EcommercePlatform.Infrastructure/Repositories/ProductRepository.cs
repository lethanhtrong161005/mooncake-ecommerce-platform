namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Products.Requests;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IProductRepository"/>.</summary>
public class ProductRepository(ApplicationDbContext context) : IProductRepository
{
    public async Task<(IEnumerable<Product> Products, int TotalCount)> SearchProductsAsync(ProductQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        var query = context.Products
            .Include(p => p.Shop)
            .Include(p => p.Category)
            .Include(p => p.ProductVariants.Where(v => v.IsActive))
            .Include(p => p.ProductImages)
            .AsNoTracking()
            .Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(parameters.Keyword))
        {
            var term = parameters.Keyword.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term) || (p.Description != null && p.Description.ToLower().Contains(term)));
        }

        if (parameters.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == parameters.CategoryId.Value);
        }

        if (parameters.ShopId.HasValue)
        {
            query = query.Where(p => p.ShopId == parameters.ShopId.Value);
        }

        if (parameters.MinPrice.HasValue)
        {
            query = query.Where(p => p.ProductVariants.Any(v => v.IsActive && v.Price >= parameters.MinPrice.Value));
        }

        if (parameters.MaxPrice.HasValue)
        {
            query = query.Where(p => p.ProductVariants.Any(v => v.IsActive && v.Price <= parameters.MaxPrice.Value));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Sorting
        query = parameters.SortBy switch
        {
            "price_asc" => query.OrderBy(p => p.ProductVariants.Where(v => v.IsActive).Min(v => (decimal?)v.Price) ?? 0),
            "price_desc" => query.OrderByDescending(p => p.ProductVariants.Where(v => v.IsActive).Max(v => (decimal?)v.Price) ?? 0),
            "name_asc" => query.OrderBy(p => p.Name),
            _ => query.OrderByDescending(p => p.CreatedAtUtc)
        };

        var page = parameters.Page <= 0 ? 1 : parameters.Page;
        var pageSize = parameters.PageSize <= 0 ? 20 : parameters.PageSize;

        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    public async Task<Product?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        await context.Products
            .Include(p => p.Shop)
            .Include(p => p.Category)
            .Include(p => p.ProductVariants)
            .Include(p => p.ProductImages)
            .Include(p => p.PromotionRules.Where(r => r.IsActive))
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<Product> CreateAsync(Product product, CancellationToken cancellationToken = default)
    {
        context.Products.Add(product);
        await context.SaveChangesAsync(cancellationToken);
        return product;
    }

    public async Task<Product> UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        context.Products.Update(product);
        await context.SaveChangesAsync(cancellationToken);
        return product;
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var product = await context.Products.FindAsync([id], cancellationToken);
        if (product is not null)
        {
            context.Products.Remove(product);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IEnumerable<ProductVariant>> GetVariantsByProductIdAsync(long productId, CancellationToken cancellationToken = default) =>
        await context.ProductVariants
            .AsNoTracking()
            .Where(v => v.ProductId == productId)
            .OrderBy(v => v.Price)
            .ToListAsync(cancellationToken);

    public async Task<ProductVariant?> GetVariantByIdAsync(long variantId, CancellationToken cancellationToken = default) =>
        await context.ProductVariants
            .Include(v => v.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == variantId, cancellationToken);

    public async Task<ProductVariant> CreateVariantAsync(ProductVariant variant, CancellationToken cancellationToken = default)
    {
        context.ProductVariants.Add(variant);
        await context.SaveChangesAsync(cancellationToken);
        return variant;
    }

    public async Task<ProductVariant> UpdateVariantAsync(ProductVariant variant, CancellationToken cancellationToken = default)
    {
        context.ProductVariants.Update(variant);
        await context.SaveChangesAsync(cancellationToken);
        return variant;
    }

    public async Task DeleteVariantAsync(long variantId, CancellationToken cancellationToken = default)
    {
        var variant = await context.ProductVariants.FindAsync([variantId], cancellationToken);
        if (variant is not null)
        {
            context.ProductVariants.Remove(variant);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<ProductImage> AddImageAsync(ProductImage image, CancellationToken cancellationToken = default)
    {
        if (image.IsPrimary)
        {
            // Reset existing primary images for this product
            var existingPrimaries = await context.ProductImages
                .Where(img => img.ProductId == image.ProductId && img.IsPrimary)
                .ToListAsync(cancellationToken);

            foreach (var img in existingPrimaries)
            {
                img.IsPrimary = false;
            }
        }

        context.ProductImages.Add(image);
        await context.SaveChangesAsync(cancellationToken);
        return image;
    }

    public async Task DeleteImageAsync(long imageId, CancellationToken cancellationToken = default)
    {
        var img = await context.ProductImages.FindAsync([imageId], cancellationToken);
        if (img is not null)
        {
            context.ProductImages.Remove(img);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task SetPrimaryImageAsync(long productId, long imageId, CancellationToken cancellationToken = default)
    {
        var images = await context.ProductImages
            .Where(img => img.ProductId == productId)
            .ToListAsync(cancellationToken);

        foreach (var img in images)
        {
            img.IsPrimary = img.Id == imageId;
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
