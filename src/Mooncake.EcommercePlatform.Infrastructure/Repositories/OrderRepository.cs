namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Npgsql;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

/// <summary>EF Core persistence for bulk orders, status history, and stock reservations.</summary>
public sealed class OrderRepository(ApplicationDbContext context) : IOrderRepository
{
    public Task<Shop?> GetShopAsync(Guid shopId, CancellationToken cancellationToken = default) =>
        (from shop in context.Shops.AsNoTracking()
         join profile in context.SupplierProfiles.AsNoTracking() on shop.SupplierId equals profile.Id
         where shop.Id == shopId && !shop.IsDeleted && shop.Status == ShopStatus.Active && !profile.IsDeleted
               && profile.VerificationStatus == SupplierVerificationStatus.Verified
         select shop).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<Product>> GetProductsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken = default) =>
        await context.Products.AsNoTracking().Where(product => productIds.Contains(product.Id) && !product.IsDeleted).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ProductVariant>> GetVariantsAsync(IReadOnlyCollection<Guid> variantIds, CancellationToken cancellationToken = default) =>
        await context.ProductVariants.AsNoTracking().Where(variant => variantIds.Contains(variant.Id) && !variant.IsDeleted).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PromotionRule>> GetActivePromotionsAsync(Guid shopId, CancellationToken cancellationToken = default) =>
        await context.PromotionRules.AsNoTracking().Where(rule => rule.ShopId == shopId && rule.IsActive && !rule.IsDeleted).ToListAsync(cancellationToken);

    public Task<Order?> GetByIdempotencyKeyAsync(Guid customerId, string idempotencyKey, CancellationToken cancellationToken = default) =>
        context.Orders.AsNoTracking().FirstOrDefaultAsync(order => order.CustomerId == customerId
            && order.IdempotencyKey == idempotencyKey && !order.IsDeleted, cancellationToken);

    public async Task<bool> CreateOrderAsync(Order order, IReadOnlyList<OrderItem> items, CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            context.Orders.Add(order);
            await context.SaveChangesAsync(cancellationToken);
            foreach (var group in items.GroupBy(item => item.VariantId))
            {
                var quantity = group.Sum(item => item.Quantity);
                var updated = await context.ProductVariants
                    .Where(variant => variant.Id == group.Key && !variant.IsDeleted && variant.StockQty >= quantity)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(variant => variant.StockQty, variant => variant.StockQty - quantity), cancellationToken);
                if (updated != 1)
                    return false;
            }

            foreach (var item in items)
                item.OrderId = order.Id;
            context.OrderItems.AddRange(items);
            context.WorkflowEvents.Add(new WorkflowEvent
            {
                AggregateType = "Order", AggregateId = order.Id, EventType = "OrderPlaced",
                ToStatus = OrderStatus.Pending.ToString(), ActorUserId = order.CustomerId
            });
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (IsIdempotencyConflict(exception))
        {
            await transaction.RollbackAsync(CancellationToken.None);
            context.ChangeTracker.Clear();
            return false;
        }
    }

    private static bool IsIdempotencyConflict(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "orders_customer_idempotency_key_key"
        };

    public Task<Order?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        context.Orders.FirstOrDefaultAsync(order => order.Id == orderId && !order.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<OrderItem>> GetOrderItemsAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        await context.OrderItems.AsNoTracking().Where(item => item.OrderId == orderId && !item.IsDeleted).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Order>> GetCustomerOrdersAsync(Guid customerId, CancellationToken cancellationToken = default) =>
        await context.Orders.AsNoTracking().Where(order => order.CustomerId == customerId && !order.IsDeleted)
            .OrderByDescending(order => order.CreatedAt).Take(100).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Order>> GetSupplierOrdersAsync(Guid supplierUserId, CancellationToken cancellationToken = default) =>
        await (from order in context.Orders.AsNoTracking()
               join shop in context.Shops on order.ShopId equals shop.Id
               join profile in context.SupplierProfiles on shop.SupplierId equals profile.Id
               where profile.UserId == supplierUserId && !profile.IsDeleted && !shop.IsDeleted && !order.IsDeleted
               orderby order.CreatedAt descending
               select order).Take(100).ToListAsync(cancellationToken);

    public async Task<bool> CancelPendingOrderAsync(Order order, Guid actorUserId, string reason, CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var changed = await context.Orders.Where(item => item.Id == order.Id && item.Status == OrderStatus.Pending && !item.IsDeleted)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.Status, OrderStatus.Cancelled)
                .SetProperty(item => item.CancelledAt, now).SetProperty(item => item.CancelledByUserId, actorUserId)
                .SetProperty(item => item.CancellationReason, reason).SetProperty(item => item.UpdatedAt, now), cancellationToken);
        if (changed != 1)
            return false;

        var items = await context.OrderItems.AsNoTracking().Where(item => item.OrderId == order.Id && !item.IsDeleted).ToListAsync(cancellationToken);
        foreach (var group in items.GroupBy(item => item.VariantId))
        {
            var quantity = group.Sum(item => item.Quantity);
            await context.ProductVariants.Where(variant => variant.Id == group.Key)
                .ExecuteUpdateAsync(setters => setters.SetProperty(variant => variant.StockQty, variant => variant.StockQty + quantity), cancellationToken);
        }
        context.WorkflowEvents.Add(new WorkflowEvent
        {
            AggregateType = "Order", AggregateId = order.Id, EventType = "OrderCancelled",
            FromStatus = OrderStatus.Pending.ToString(), ToStatus = OrderStatus.Cancelled.ToString(), ActorUserId = actorUserId, Reason = reason
        });
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        order.Status = OrderStatus.Cancelled;
        order.CancelledAt = now;
        order.CancelledByUserId = actorUserId;
        order.CancellationReason = reason;
        return true;
    }

    public async Task<bool> ConfirmPendingOrderAsync(Order order, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var changed = await context.Orders.Where(item => item.Id == order.Id && item.Status == OrderStatus.Pending && !item.IsDeleted)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.Status, OrderStatus.Confirmed)
                .SetProperty(item => item.UpdatedAt, now).SetProperty(item => item.UpdatedBy, actorUserId), cancellationToken);
        if (changed != 1)
            return false;
        context.WorkflowEvents.Add(new WorkflowEvent
        {
            AggregateType = "Order", AggregateId = order.Id, EventType = "OrderConfirmed",
            FromStatus = OrderStatus.Pending.ToString(), ToStatus = OrderStatus.Confirmed.ToString(), ActorUserId = actorUserId
        });
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        order.Status = OrderStatus.Confirmed;
        order.UpdatedAt = now;
        order.UpdatedBy = actorUserId;
        return true;
    }

    public Task<bool> IsShopOwnedByUserAsync(Guid shopId, Guid userId, CancellationToken cancellationToken = default) =>
        (from shop in context.Shops.AsNoTracking()
         join profile in context.SupplierProfiles.AsNoTracking() on shop.SupplierId equals profile.Id
         where shop.Id == shopId && profile.UserId == userId && profile.VerificationStatus == SupplierVerificationStatus.Verified
               && !shop.IsDeleted && !profile.IsDeleted
         select shop.Id).AnyAsync(cancellationToken);
}
