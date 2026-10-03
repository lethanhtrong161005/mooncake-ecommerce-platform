namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="ISupplierRepository"/>.</summary>
public class SupplierRepository(ApplicationDbContext context) : ISupplierRepository
{
    public async Task<IEnumerable<Supplier>> GetAllAsync(bool? isVerified = null, CancellationToken cancellationToken = default)
    {
        var query = context.Suppliers.Include(s => s.User).AsNoTracking();
        if (isVerified.HasValue)
        {
            query = query.Where(s => s.IsVerified == isVerified.Value);
        }

        return await query.OrderByDescending(s => s.CreatedAtUtc).ToListAsync(cancellationToken);
    }

    public async Task<Supplier?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        await context.Suppliers
            .Include(s => s.User)
            .Include(s => s.Shops)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<Supplier?> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default) =>
        await context.Suppliers
            .Include(s => s.User)
            .Include(s => s.Shops)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

    public async Task<Supplier> CreateAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync(cancellationToken);
        return supplier;
    }

    public async Task<Supplier> UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        context.Suppliers.Update(supplier);
        await context.SaveChangesAsync(cancellationToken);
        return supplier;
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var supplier = await context.Suppliers.FindAsync([id], cancellationToken);
        if (supplier is not null)
        {
            context.Suppliers.Remove(supplier);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
