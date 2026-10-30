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
            var shopInfo = await context.Shops.AsNoTracking().SingleOrDefaultAsync(x => x.Id == order.ShopId, cancellationToken);
            if (shopInfo == null) return false;
            var eligibility = await Mooncake.EcommercePlatform.Infrastructure.Helpers.SupplierEligibilityLockHelper.LockAsync(
                context, shopInfo.SupplierId, [order.CustomerId], cancellationToken);
            if (!Mooncake.EcommercePlatform.Application.Common.Helpers.SupplierVerificationHelper.CanSell(eligibility.Owner, eligibility.Profile))
                return false;
            var currentShop = await context.Shops.FromSqlInterpolated(
                $"SELECT * FROM shops WHERE id = {order.ShopId} FOR SHARE").AsNoTracking().SingleOrDefaultAsync(cancellationToken);
            if (currentShop is not { IsDeleted: false, Status: ShopStatus.Active }) return false;
            context.Orders.Add(order);
            await context.SaveChangesAsync(cancellationToken);
            foreach (var group in items.GroupBy(item => item.VariantId))
            {
                var quantity = group.Sum(item => item.Quantity);
                var updated = await context.ProductVariants
                    .Where(variant => variant.Id == group.Key && !variant.IsDeleted && variant.StockQty >= quantity)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(variant => variant.StockQty, variant => variant.StockQty - quantity)
                        .SetProperty(variant => variant.InventoryVersion, variant => variant.InventoryVersion + 1),
                        cancellationToken);
                if (updated != 1)
                    return false;
                var inventory = await context.ProductVariants.AsNoTracking()
                    .Where(variant => variant.Id == group.Key)
                    .Select(variant => new { variant.ProductId, variant.ShopId, variant.StockQty })
                    .SingleAsync(cancellationToken);
                context.ProductInventoryMovements.Add(new ProductInventoryMovement
                {
                    ProductId = inventory.ProductId,
                    VariantId = group.Key,
                    ShopId = inventory.ShopId,
                    ActorUserId = order.CustomerId,
                    SourceType = "Order",
                    SourceId = order.Id.ToString(),
                    Delta = -quantity,
                    PreviousQty = inventory.StockQty + quantity,
                    CurrentQty = inventory.StockQty,
                    Reason = "Inventory reserved for a bulk order"
                });
            }

            foreach (var item in items)
                item.OrderId = order.Id;
            context.OrderItems.AddRange(items);
            context.WorkflowEvents.Add(new WorkflowEvent
            {
                AggregateType = "Order", AggregateId = order.Id, EventType = "OrderPlaced",
                ToStatus = OrderStatus.Pending.ToString(), ActorUserId = order.CustomerId
            });
            var supplierUserId = await (from shop in context.Shops
                                        join profile in context.SupplierProfiles on shop.SupplierId equals profile.Id
                                        where shop.Id == order.ShopId
                                        select profile.UserId).FirstAsync(cancellationToken);
            context.Notifications.Add(new Notification
            {
                UserId = supplierUserId, Type = "order.created", Title = "New order received",
                Content = $"Order {order.OrderNumber} is waiting for your response.", ReferenceType = "order", ReferenceId = order.Id
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

    public Task<CustomPackagingStatus?> GetCustomPackagingStatusAsync(Guid orderId,
        CancellationToken cancellationToken = default) =>
        context.CustomPackagings.AsNoTracking().Where(packaging =>
                packaging.OrderId == orderId && !packaging.IsDeleted)
            .Select(packaging => (CustomPackagingStatus?)packaging.Status)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, (CustomPackaging Packaging, int RevisionCount)>>
        GetPackagingSummariesAsync(IReadOnlyCollection<Guid> orderIds,
            CancellationToken cancellationToken = default)
    {
        var packagings = await context.CustomPackagings.AsNoTracking()
            .Where(packaging => orderIds.Contains(packaging.OrderId) && !packaging.IsDeleted)
            .ToListAsync(cancellationToken);
        var packagingIds = packagings.Select(packaging => packaging.Id).ToArray();
        var counts = await context.CustomPackagingRevisions.AsNoTracking()
            .Where(revision => packagingIds.Contains(revision.CustomPackagingId) && !revision.IsDeleted)
            .GroupBy(revision => revision.CustomPackagingId)
            .Select(group => new { PackagingId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.PackagingId, item => item.Count, cancellationToken);
        return packagings.ToDictionary(packaging => packaging.OrderId,
            packaging => (packaging, counts.GetValueOrDefault(packaging.Id)));
    }

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
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(variant => variant.StockQty, variant => variant.StockQty + quantity)
                    .SetProperty(variant => variant.InventoryVersion, variant => variant.InventoryVersion + 1),
                    cancellationToken);
            var inventory = await context.ProductVariants.AsNoTracking()
                .Where(variant => variant.Id == group.Key)
                .Select(variant => new { variant.ProductId, variant.ShopId, variant.StockQty })
                .SingleAsync(cancellationToken);
            context.ProductInventoryMovements.Add(new ProductInventoryMovement
            {
                ProductId = inventory.ProductId,
                VariantId = group.Key,
                ShopId = inventory.ShopId,
                ActorUserId = actorUserId,
                SourceType = "OrderCancellation",
                SourceId = order.Id.ToString(),
                Delta = quantity,
                PreviousQty = inventory.StockQty - quantity,
                CurrentQty = inventory.StockQty,
                Reason = "Inventory released after order cancellation"
            });
        }
        context.WorkflowEvents.Add(new WorkflowEvent
        {
            AggregateType = "Order", AggregateId = order.Id, EventType = "OrderCancelled",
            FromStatus = OrderStatus.Pending.ToString(), ToStatus = OrderStatus.Cancelled.ToString(), ActorUserId = actorUserId, Reason = reason
        });
        var supplierUserId = await (from shop in context.Shops
                                    join profile in context.SupplierProfiles on shop.SupplierId equals profile.Id
                                    where shop.Id == order.ShopId
                                    select profile.UserId).FirstAsync(cancellationToken);
        context.Notifications.Add(new Notification
        {
            UserId = order.CustomerId, Type = "order.cancelled", Title = "Order cancelled",
            Content = $"Order {order.OrderNumber} was cancelled. Reason: {reason}", ReferenceType = "order", ReferenceId = order.Id
        });
        if (supplierUserId != actorUserId)
            context.Notifications.Add(new Notification
            {
                UserId = supplierUserId, Type = "order.cancelled", Title = "Order cancelled",
                Content = $"Order {order.OrderNumber} was cancelled.", ReferenceType = "order", ReferenceId = order.Id
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
        var targetShop = await context.Shops.AsNoTracking().SingleAsync(x => x.Id == order.ShopId, cancellationToken);
        var eligibility = await Mooncake.EcommercePlatform.Infrastructure.Helpers.SupplierEligibilityLockHelper.LockAsync(
            context, targetShop.SupplierId, [actorUserId], cancellationToken);
        if (eligibility.Profile?.UserId != actorUserId ||
            !Mooncake.EcommercePlatform.Application.Common.Helpers.SupplierVerificationHelper.CanSell(eligibility.Owner, eligibility.Profile))
            return false;
        if (!await Mooncake.EcommercePlatform.Infrastructure.Helpers.ShopTradingLockHelper.CanTradeAsync(context, targetShop.SupplierId, cancellationToken))
            return false;
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
        context.Notifications.Add(new Notification
        {
            UserId = order.CustomerId, Type = "order.confirmed", Title = "Order confirmed",
            Content = $"Order {order.OrderNumber} was confirmed by the supplier.", ReferenceType = "order", ReferenceId = order.Id
        });
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        order.Status = OrderStatus.Confirmed;
        order.UpdatedAt = now;
        order.UpdatedBy = actorUserId;
        return true;
    }

    public async Task<bool> TransitionOrderAsync(Guid orderId, OrderStatus expectedStatus, OrderStatus nextStatus, Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var targetOrder = await context.Orders.AsNoTracking().SingleOrDefaultAsync(x => x.Id == orderId, cancellationToken);
        if (targetOrder == null) return false;
        var targetShop = await context.Shops.AsNoTracking().SingleAsync(x => x.Id == targetOrder.ShopId, cancellationToken);
        var eligibility = await Mooncake.EcommercePlatform.Infrastructure.Helpers.SupplierEligibilityLockHelper.LockAsync(
            context, targetShop.SupplierId, [actorUserId], cancellationToken);
        if (actorUserId != targetOrder.CustomerId && (eligibility.Profile?.UserId != actorUserId ||
            !Mooncake.EcommercePlatform.Application.Common.Helpers.SupplierVerificationHelper.CanFulfill(
                eligibility.Owner, eligibility.Profile, targetOrder.CreatedAt))) return false;
        var now = DateTime.UtcNow;
        CustomPackaging? packaging = null;
        if (nextStatus == OrderStatus.Processing)
        {
            packaging = await context.CustomPackagings.FromSqlInterpolated(
                    $"SELECT * FROM custom_packagings WHERE order_id = {orderId} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (packaging is not null && packaging.Status != CustomPackagingStatus.Approved)
                return false;
        }
        var changed = await context.Orders.Where(order => order.Id == orderId && order.Status == expectedStatus && !order.IsDeleted)
            .ExecuteUpdateAsync(setters => setters.SetProperty(order => order.Status, nextStatus)
                .SetProperty(order => order.UpdatedAt, now).SetProperty(order => order.UpdatedBy, actorUserId), cancellationToken);
        if (changed != 1) return false;
        if (packaging is not null)
        {
            packaging.Status = CustomPackagingStatus.InProduction;
            packaging.UpdatedAt = now;
            packaging.UpdatedBy = actorUserId;
        }
        if (nextStatus == OrderStatus.Delivered)
        {
            await context.CustomPackagings.Where(item => item.OrderId == orderId &&
                    item.Status == CustomPackagingStatus.InProduction && !item.IsDeleted)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.Status, CustomPackagingStatus.Completed)
                    .SetProperty(item => item.UpdatedAt, now)
                    .SetProperty(item => item.UpdatedBy, actorUserId), cancellationToken);
        }
        var orderInfo = await context.Orders.AsNoTracking().Where(order => order.Id == orderId)
            .Select(order => new { order.CustomerId, order.ShopId, order.OrderNumber }).FirstAsync(cancellationToken);
        var supplierUserId = await (from shop in context.Shops
                                    join profile in context.SupplierProfiles on shop.SupplierId equals profile.Id
                                    where shop.Id == orderInfo.ShopId
                                    select profile.UserId).FirstAsync(cancellationToken);
        context.WorkflowEvents.Add(new WorkflowEvent
        {
            AggregateType = "Order", AggregateId = orderId, EventType = "OrderStatusChanged",
            FromStatus = expectedStatus.ToString(), ToStatus = nextStatus.ToString(), ActorUserId = actorUserId
        });
        var notificationUserId = actorUserId == orderInfo.CustomerId ? supplierUserId : orderInfo.CustomerId;
        context.Notifications.Add(new Notification
        {
            UserId = notificationUserId, Type = "order.status_changed", Title = "Order status updated",
            Content = $"Order {orderInfo.OrderNumber} is now {nextStatus}.", ReferenceType = "order", ReferenceId = orderId
        });
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public Task<bool> IsShopOwnedByUserAsync(Guid shopId, Guid userId, CancellationToken cancellationToken = default) =>
        (from shop in context.Shops.AsNoTracking()
         join profile in context.SupplierProfiles.AsNoTracking() on shop.SupplierId equals profile.Id
         where shop.Id == shopId && profile.UserId == userId &&
               (profile.VerificationStatus == SupplierVerificationStatus.Verified || profile.RevokedAtUtc != null)
               && !shop.IsDeleted && !profile.IsDeleted
         select shop.Id).AnyAsync(cancellationToken);
}
