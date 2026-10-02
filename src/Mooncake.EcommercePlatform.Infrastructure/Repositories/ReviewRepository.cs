namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IReviewRepository"/>.</summary>
public class ReviewRepository(ApplicationDbContext context) : IReviewRepository
{
    public async Task<Review> CreateAsync(Review review, CancellationToken cancellationToken = default)
    {
        context.Reviews.Add(review);
        await context.SaveChangesAsync(cancellationToken);
        return review;
    }

    public async Task<Review?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        await context.Reviews
            .Include(r => r.Customer).ThenInclude(c => c.User)
            .Include(r => r.Supplier).ThenInclude(s => s.User)
            .Include(r => r.Order)
            .Include(r => r.Contract)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<Review?> GetByOrderIdAsync(long orderId, CancellationToken cancellationToken = default) =>
        await context.Reviews
            .Include(r => r.Customer).ThenInclude(c => c.User)
            .Include(r => r.Supplier)
            .FirstOrDefaultAsync(r => r.OrderId == orderId, cancellationToken);

    public async Task<Review?> GetByContractIdAsync(long contractId, CancellationToken cancellationToken = default) =>
        await context.Reviews
            .Include(r => r.Customer).ThenInclude(c => c.User)
            .Include(r => r.Supplier)
            .FirstOrDefaultAsync(r => r.ContractId == contractId, cancellationToken);

    public async Task<IEnumerable<Review>> GetBySupplierIdAsync(long supplierId, CancellationToken cancellationToken = default) =>
        await context.Reviews
            .Include(r => r.Customer).ThenInclude(c => c.User)
            .Include(r => r.Order)
            .Include(r => r.Contract)
            .AsNoTracking()
            .Where(r => r.SupplierId == supplierId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);
}
