namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="ICustomPackagingRepository"/>.</summary>
public class CustomPackagingRepository(ApplicationDbContext context) : ICustomPackagingRepository
{
    public async Task<IEnumerable<CustomPackaging>> GetByCustomerIdAsync(long customerId, CancellationToken cancellationToken = default) =>
        await context.CustomPackagings
            .AsNoTracking()
            .Where(p => p.CustomerId == customerId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<CustomPackaging?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        await context.CustomPackagings
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<CustomPackaging> CreateAsync(CustomPackaging packaging, CancellationToken cancellationToken = default)
    {
        context.CustomPackagings.Add(packaging);
        await context.SaveChangesAsync(cancellationToken);
        return packaging;
    }

    public async Task<CustomPackaging> UpdateAsync(CustomPackaging packaging, CancellationToken cancellationToken = default)
    {
        context.CustomPackagings.Update(packaging);
        await context.SaveChangesAsync(cancellationToken);
        return packaging;
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var packaging = await context.CustomPackagings.FindAsync([id], cancellationToken);
        if (packaging is not null)
        {
            context.CustomPackagings.Remove(packaging);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
