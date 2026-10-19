namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

public sealed class DeliveryRepository(ApplicationDbContext context, IDateTimeProvider clock) : IDeliveryRepository
{
    public async Task<IReadOnlyList<Delivery>> GetMineAsync(Guid userId, bool supplier,
        CancellationToken cancellationToken = default)
    {
        var query = context.Deliveries.AsNoTracking().Where(delivery => !delivery.IsDeleted);
        if (supplier)
        {
            query = query.Where(delivery =>
                (delivery.OrderId.HasValue && context.Orders.Any(order => order.Id == delivery.OrderId
                    && context.Shops.Any(shop => shop.Id == order.ShopId
                        && context.SupplierProfiles.Any(profile => profile.Id == shop.SupplierId && profile.UserId == userId))))
                || (delivery.ContractId.HasValue && context.Contracts.Any(contract => contract.Id == delivery.ContractId
                    && context.SupplierProfiles.Any(profile => profile.Id == contract.SupplierId && profile.UserId == userId))));
        }
        else
        {
            query = query.Where(delivery =>
                (delivery.OrderId.HasValue && context.Orders.Any(order => order.Id == delivery.OrderId && order.CustomerId == userId))
                || (delivery.ContractId.HasValue && context.Contracts.Any(contract => contract.Id == delivery.ContractId && contract.CustomerId == userId)));
        }
        return await query.OrderByDescending(delivery => delivery.CreatedAt).Take(100).ToListAsync(cancellationToken);
    }

    public Task<Delivery?> GetAsync(Guid deliveryId, CancellationToken cancellationToken = default) =>
        context.Deliveries.AsNoTracking().FirstOrDefaultAsync(delivery => delivery.Id == deliveryId && !delivery.IsDeleted, cancellationToken);

    public async Task<bool> CanAccessAsync(Delivery delivery, Guid userId, bool supplier,
        CancellationToken cancellationToken = default)
    {
        if (delivery.OrderId is Guid orderId)
            return supplier
                ? await (from order in context.Orders.AsNoTracking()
                         join shop in context.Shops.AsNoTracking() on order.ShopId equals shop.Id
                         join profile in context.SupplierProfiles.AsNoTracking() on shop.SupplierId equals profile.Id
                         where order.Id == orderId && profile.UserId == userId && !profile.IsDeleted
                         select order.Id).AnyAsync(cancellationToken)
                : await context.Orders.AnyAsync(order => order.Id == orderId && order.CustomerId == userId && !order.IsDeleted, cancellationToken);
        if (delivery.ContractId is not Guid contractId) return false;
        return supplier
            ? await (from contract in context.Contracts.AsNoTracking()
                     join profile in context.SupplierProfiles.AsNoTracking() on contract.SupplierId equals profile.Id
                     where contract.Id == contractId && profile.UserId == userId && !profile.IsDeleted
                     select contract.Id).AnyAsync(cancellationToken)
            : await context.Contracts.AnyAsync(contract => contract.Id == contractId && contract.CustomerId == userId && !contract.IsDeleted, cancellationToken);
    }

    public async Task<bool> CreateAsync(Delivery delivery, Guid supplierUserId, CancellationToken cancellationToken = default)
    {
        var activeOrderStatuses = new[] { OrderStatus.Processing, OrderStatus.Shipped };
        var ownsActiveWork = delivery.OrderId is Guid orderId
            ? await (from order in context.Orders
                     join shop in context.Shops on order.ShopId equals shop.Id
                     join profile in context.SupplierProfiles on shop.SupplierId equals profile.Id
                     where order.Id == orderId && activeOrderStatuses.Contains(order.Status)
                         && profile.UserId == supplierUserId && profile.VerificationStatus == SupplierVerificationStatus.Verified
                         && !order.IsDeleted && !shop.IsDeleted && !profile.IsDeleted
                     select order.Id).AnyAsync(cancellationToken)
            : await (from contract in context.Contracts
                     join profile in context.SupplierProfiles on contract.SupplierId equals profile.Id
                     where contract.Id == delivery.ContractId && contract.Status == ContractStatus.Active
                         && profile.UserId == supplierUserId && !contract.IsDeleted && !profile.IsDeleted
                     select contract.Id).AnyAsync(cancellationToken);
        if (!ownsActiveWork) return false;
        context.Deliveries.Add(delivery);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> TransitionAsync(Guid deliveryId, Guid actorUserId, bool supplier, DeliveryStatus nextStatus,
        string? trackingCode, CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var delivery = await context.Deliveries.FromSqlInterpolated(
                $"SELECT * FROM deliveries WHERE id = {deliveryId} FOR UPDATE")
            .FirstOrDefaultAsync(cancellationToken);
        if (delivery is null || delivery.IsDeleted || !await CanAccessAsync(delivery, actorUserId, supplier, cancellationToken))
            return false;
        var allowed = supplier
            ? (delivery.Status, nextStatus) is (DeliveryStatus.Pending, DeliveryStatus.Confirmed)
                or (DeliveryStatus.Confirmed, DeliveryStatus.PickedUp)
                or (DeliveryStatus.Confirmed, DeliveryStatus.Failed)
                or (DeliveryStatus.PickedUp, DeliveryStatus.InTransit)
                or (DeliveryStatus.InTransit, DeliveryStatus.Failed)
            : (delivery.Status, nextStatus) is (DeliveryStatus.InTransit, DeliveryStatus.Delivered);
        if (!allowed) return false;

        var previousStatus = delivery.Status;
        var now = clock.UtcNow;
        delivery.Status = nextStatus;
        delivery.UpdatedAt = now;
        delivery.UpdatedBy = actorUserId;
        if (!string.IsNullOrWhiteSpace(trackingCode)) delivery.TrackingCode = trackingCode;
        if (nextStatus == DeliveryStatus.Delivered) delivery.ActualDeliveryDate = DateOnly.FromDateTime(now);
        context.WorkflowEvents.Add(new WorkflowEvent
        {
            AggregateType = "Delivery", AggregateId = delivery.Id, EventType = "DeliveryStatusChanged",
            FromStatus = previousStatus.ToString(), ToStatus = nextStatus.ToString(), ActorUserId = actorUserId
        });
        await AddPartyNotificationsAsync(delivery, nextStatus, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> AddProofAsync(Guid deliveryId, Guid actorUserId, bool supplier, DeliveryProof proof,
        CancellationToken cancellationToken = default)
    {
        var delivery = await context.Deliveries.FirstOrDefaultAsync(item => item.Id == deliveryId && !item.IsDeleted, cancellationToken);
        if (delivery is null || !await CanAccessAsync(delivery, actorUserId, supplier, cancellationToken)) return false;
        if (proof.ProofType == ProofType.Delivery && delivery.Status != DeliveryStatus.Delivered) return false;
        if (proof.ProofType == ProofType.Pickup && delivery.Status is not (DeliveryStatus.Confirmed or DeliveryStatus.PickedUp)) return false;
        context.DeliveryProofs.Add(proof);
        delivery.UpdatedAt = clock.UtcNow;
        delivery.UpdatedBy = actorUserId;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<DeliveryProof>> GetProofsAsync(Guid deliveryId,
        CancellationToken cancellationToken = default) =>
        await context.DeliveryProofs.AsNoTracking().Where(proof => proof.DeliveryId == deliveryId && !proof.IsDeleted)
            .OrderBy(proof => proof.TakenAt).ToListAsync(cancellationToken);

    private async Task AddPartyNotificationsAsync(Delivery delivery, DeliveryStatus status,
        CancellationToken cancellationToken)
    {
        var recipientIds = new HashSet<Guid>();
        if (delivery.OrderId is Guid orderId)
        {
            var customerId = await context.Orders.Where(order => order.Id == orderId)
                .Select(order => order.CustomerId).FirstOrDefaultAsync(cancellationToken);
            var supplierId = await (from order in context.Orders
                                    join shop in context.Shops on order.ShopId equals shop.Id
                                    join profile in context.SupplierProfiles on shop.SupplierId equals profile.Id
                                    where order.Id == orderId
                                    select profile.UserId).FirstOrDefaultAsync(cancellationToken);
            if (customerId != Guid.Empty) recipientIds.Add(customerId);
            if (supplierId != Guid.Empty) recipientIds.Add(supplierId);
        }
        if (delivery.ContractId is Guid contractId)
        {
            var customerId = await context.Contracts.Where(contract => contract.Id == contractId)
                .Select(contract => contract.CustomerId).FirstOrDefaultAsync(cancellationToken);
            var supplierId = await (from contract in context.Contracts
                                    join profile in context.SupplierProfiles on contract.SupplierId equals profile.Id
                                    where contract.Id == contractId
                                    select profile.UserId).FirstOrDefaultAsync(cancellationToken);
            if (customerId != Guid.Empty) recipientIds.Add(customerId);
            if (supplierId != Guid.Empty) recipientIds.Add(supplierId);
        }
        foreach (var userId in recipientIds)
            context.Notifications.Add(new Notification
            {
                UserId = userId, Type = "delivery.status", Title = "Delivery status updated",
                Content = $"Delivery {delivery.DeliveryNumber} is now {status}.",
                ReferenceType = "delivery", ReferenceId = delivery.Id
            });
    }
}
