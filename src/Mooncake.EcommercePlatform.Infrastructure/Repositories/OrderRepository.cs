namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IOrderRepository"/>.</summary>
public class OrderRepository(ApplicationDbContext context) : IOrderRepository
{
    public async Task<IEnumerable<Order>> GetOrdersAsync(long? customerId = null, long? shopId = null, OrderStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = context.Orders
            .Include(o => o.Customer).ThenInclude(c => c!.User)
            .Include(o => o.Shop)
            .Include(o => o.OrderItems).ThenInclude(i => i.Variant)
            .Include(o => o.OrderItems).ThenInclude(i => i.PromotionRule)
            .Include(o => o.OrderItems).ThenInclude(i => i.CustomPackaging)
            .AsNoTracking();

        if (customerId.HasValue)
        {
            query = query.Where(o => o.CustomerId == customerId.Value);
        }

        if (shopId.HasValue)
        {
            query = query.Where(o => o.ShopId == shopId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        return await query.OrderByDescending(o => o.CreatedAtUtc).ToListAsync(cancellationToken);
    }

    public async Task<Order?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        await context.Orders
            .Include(o => o.Customer).ThenInclude(c => c!.User)
            .Include(o => o.Shop)
            .Include(o => o.OrderItems).ThenInclude(i => i.Variant)
            .Include(o => o.OrderItems).ThenInclude(i => i.PromotionRule)
            .Include(o => o.OrderItems).ThenInclude(i => i.CustomPackaging)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<Order> CreateOrderAsync(Order order, CancellationToken cancellationToken = default)
    {
        context.Orders.Add(order);
        await context.SaveChangesAsync(cancellationToken);
        return order;
    }

    public async Task<Order> UpdateOrderAsync(Order order, CancellationToken cancellationToken = default)
    {
        context.Orders.Update(order);
        await context.SaveChangesAsync(cancellationToken);
        return order;
    }

    public async Task DeleteOrderAsync(long id, CancellationToken cancellationToken = default)
    {
        var order = await context.Orders.FindAsync([id], cancellationToken);
        if (order is not null)
        {
            context.Orders.Remove(order);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
