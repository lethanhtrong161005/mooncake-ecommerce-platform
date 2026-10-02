namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="ICustomerRepository"/>.</summary>
public class CustomerRepository(ApplicationDbContext context) : ICustomerRepository
{
    public async Task<Customer?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        await context.Customers
            .Include(c => c.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<Customer?> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default) =>
        await context.Customers
            .Include(c => c.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

    public async Task<Customer> CreateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        context.Customers.Add(customer);
        await context.SaveChangesAsync(cancellationToken);
        return customer;
    }

    public async Task<Customer> UpdateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        context.Customers.Update(customer);
        await context.SaveChangesAsync(cancellationToken);
        return customer;
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var customer = await context.Customers.FindAsync([id], cancellationToken);
        if (customer is not null)
        {
            context.Customers.Remove(customer);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
