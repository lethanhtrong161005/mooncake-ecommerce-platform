namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IShopRepository"/>.</summary>
public class ShopRepository(ApplicationDbContext context) : IShopRepository
{
    public async Task<IEnumerable<Shop>> GetAllActiveAsync(string? search = null, CancellationToken cancellationToken = default)
    {
        var query = context.Shops
            .Include(s => s.Supplier)
            .Include(s => s.Template)
            .AsNoTracking()
            .Where(s => s.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(s => s.Name.ToLower().Contains(term) || (s.Description != null && s.Description.ToLower().Contains(term)));
        }

        return await query.OrderByDescending(s => s.CreatedAtUtc).ToListAsync(cancellationToken);
    }

    public async Task<Shop?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        await context.Shops
            .Include(s => s.Supplier)
            .Include(s => s.Template)
            .Include(s => s.Products.Where(p => p.IsActive))
                .ThenInclude(p => p.ProductVariants.Where(v => v.IsActive))
            .Include(s => s.Products.Where(p => p.IsActive))
                .ThenInclude(p => p.ProductImages)
            .Include(s => s.PromotionRules.Where(r => r.IsActive))
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<Shop?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        await context.Shops
            .Include(s => s.Supplier)
            .Include(s => s.Template)
            .Include(s => s.Products.Where(p => p.IsActive))
                .ThenInclude(p => p.ProductVariants.Where(v => v.IsActive))
            .Include(s => s.Products.Where(p => p.IsActive))
                .ThenInclude(p => p.ProductImages)
            .Include(s => s.PromotionRules.Where(r => r.IsActive))
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Slug == slug, cancellationToken);

    public async Task<Shop?> GetBySupplierIdAsync(long supplierId, CancellationToken cancellationToken = default) =>
        await context.Shops
            .Include(s => s.Supplier)
            .Include(s => s.Template)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SupplierId == supplierId, cancellationToken);

    public async Task<Shop> CreateAsync(Shop shop, CancellationToken cancellationToken = default)
    {
        context.Shops.Add(shop);
        await context.SaveChangesAsync(cancellationToken);
        return shop;
    }

    public async Task<Shop> UpdateAsync(Shop shop, CancellationToken cancellationToken = default)
    {
        context.Shops.Update(shop);
        await context.SaveChangesAsync(cancellationToken);
        return shop;
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var shop = await context.Shops.FindAsync([id], cancellationToken);
        if (shop is not null)
        {
            context.Shops.Remove(shop);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
