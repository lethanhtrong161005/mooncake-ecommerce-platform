namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IReputationLogRepository"/>.</summary>
public class ReputationLogRepository(ApplicationDbContext context) : IReputationLogRepository
{
    public async Task<ReputationLog> CreateAsync(ReputationLog log, CancellationToken cancellationToken = default)
    {
        context.ReputationLogs.Add(log);
        await context.SaveChangesAsync(cancellationToken);
        return log;
    }

    public async Task<IEnumerable<ReputationLog>> GetBySupplierIdAsync(long supplierId, CancellationToken cancellationToken = default) =>
        await context.ReputationLogs
            .Include(l => l.Review)
            .Include(l => l.Contract)
            .AsNoTracking()
            .Where(l => l.SupplierId == supplierId)
            .OrderByDescending(l => l.CreatedAtUtc)
            .ToListAsync(cancellationToken);
}
