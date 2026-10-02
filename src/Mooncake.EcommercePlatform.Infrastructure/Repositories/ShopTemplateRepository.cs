namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IShopTemplateRepository"/>.</summary>
public class ShopTemplateRepository(ApplicationDbContext context) : IShopTemplateRepository
{
    public async Task<IEnumerable<ShopTemplate>> GetAllAsync(bool? activeOnly = null, CancellationToken cancellationToken = default)
    {
        var query = context.ShopTemplates.AsNoTracking();
        if (activeOnly.HasValue && activeOnly.Value)
        {
            query = query.Where(t => t.IsActive);
        }
        return await query.OrderBy(t => t.Name).ToListAsync(cancellationToken);
    }

    public async Task<ShopTemplate?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        await context.ShopTemplates.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<ShopTemplate> CreateAsync(ShopTemplate template, CancellationToken cancellationToken = default)
    {
        context.ShopTemplates.Add(template);
        await context.SaveChangesAsync(cancellationToken);
        return template;
    }

    public async Task<ShopTemplate> UpdateAsync(ShopTemplate template, CancellationToken cancellationToken = default)
    {
        context.ShopTemplates.Update(template);
        await context.SaveChangesAsync(cancellationToken);
        return template;
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var template = await context.ShopTemplates.FindAsync([id], cancellationToken);
        if (template is not null)
        {
            context.ShopTemplates.Remove(template);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
