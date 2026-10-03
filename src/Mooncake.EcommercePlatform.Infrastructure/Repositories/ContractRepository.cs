namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IContractRepository"/>.</summary>
public class ContractRepository(ApplicationDbContext context) : IContractRepository
{
    public async Task<IEnumerable<Contract>> GetContractsAsync(
        long? customerId = null,
        long? supplierId = null,
        ContractStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.Contracts
            .Include(c => c.Customer).ThenInclude(cust => cust.User)
            .Include(c => c.Supplier).ThenInclude(supp => supp.User)
            .Include(c => c.ContractMilestones)
            .AsNoTracking();

        if (customerId.HasValue)
        {
            query = query.Where(c => c.CustomerId == customerId.Value);
        }

        if (supplierId.HasValue)
        {
            query = query.Where(c => c.SupplierId == supplierId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        return await query.OrderByDescending(c => c.CreatedAtUtc).ToListAsync(cancellationToken);
    }

    public async Task<Contract?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        await context.Contracts
            .Include(c => c.Customer).ThenInclude(cust => cust.User)
            .Include(c => c.Supplier).ThenInclude(supp => supp.User)
            .Include(c => c.Quotation).ThenInclude(q => q.QuotationItems)
            .Include(c => c.ContractMilestones.OrderBy(m => m.MilestoneNo))
            .Include(c => c.Deliveries)
            .Include(c => c.Reviews)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<Contract?> GetByQuotationIdAsync(long quotationId, CancellationToken cancellationToken = default) =>
        await context.Contracts
            .Include(c => c.ContractMilestones)
            .FirstOrDefaultAsync(c => c.QuotationId == quotationId, cancellationToken);

    public async Task<Contract> CreateAsync(Contract contract, CancellationToken cancellationToken = default)
    {
        context.Contracts.Add(contract);
        await context.SaveChangesAsync(cancellationToken);
        return contract;
    }

    public async Task<Contract> UpdateAsync(Contract contract, CancellationToken cancellationToken = default)
    {
        context.Contracts.Update(contract);
        await context.SaveChangesAsync(cancellationToken);
        return contract;
    }

    public async Task<ContractMilestone?> GetMilestoneByIdAsync(long milestoneId, CancellationToken cancellationToken = default) =>
        await context.ContractMilestones
            .Include(m => m.Contract).ThenInclude(c => c.Customer)
            .Include(m => m.Contract).ThenInclude(c => c.Supplier)
            .Include(m => m.Payments)
            .FirstOrDefaultAsync(m => m.Id == milestoneId, cancellationToken);

    public async Task<ContractMilestone> UpdateMilestoneAsync(ContractMilestone milestone, CancellationToken cancellationToken = default)
    {
        context.ContractMilestones.Update(milestone);
        await context.SaveChangesAsync(cancellationToken);
        return milestone;
    }
}
