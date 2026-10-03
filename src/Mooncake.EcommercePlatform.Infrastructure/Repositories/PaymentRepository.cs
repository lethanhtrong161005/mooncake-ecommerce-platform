namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IPaymentRepository"/>.</summary>
public class PaymentRepository(ApplicationDbContext context) : IPaymentRepository
{
    public async Task<Payment> CreateAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        context.Payments.Add(payment);
        await context.SaveChangesAsync(cancellationToken);
        return payment;
    }

    public async Task<Payment> UpdateAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        context.Payments.Update(payment);
        await context.SaveChangesAsync(cancellationToken);
        return payment;
    }

    public async Task<Payment?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        await context.Payments
            .Include(p => p.Order)
            .Include(p => p.Milestone)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<Payment?> GetByTransactionRefAsync(string transactionRef, CancellationToken cancellationToken = default) =>
        await context.Payments
            .Include(p => p.Order)
            .Include(p => p.Milestone)
            .FirstOrDefaultAsync(p => p.TransactionRef == transactionRef, cancellationToken);

    public async Task<IEnumerable<Payment>> GetPaymentsAsync(
        long? orderId = null,
        long? milestoneId = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.Payments
            .Include(p => p.Order)
            .Include(p => p.Milestone)
            .AsNoTracking();

        if (orderId.HasValue)
        {
            query = query.Where(p => p.OrderId == orderId.Value);
        }

        if (milestoneId.HasValue)
        {
            query = query.Where(p => p.MilestoneId == milestoneId.Value);
        }

        return await query.OrderByDescending(p => p.CreatedAtUtc).ToListAsync(cancellationToken);
    }
}
