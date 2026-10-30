namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

public sealed class DeliveryRepository(ApplicationDbContext context) : IDeliveryRepository
{
    public async Task<Delivery?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.Deliveries.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<Delivery>> GetDeliveriesForUserAsync(Guid userId, bool isSupplier, CancellationToken cancellationToken = default)
    {
        if (isSupplier)
        {
            var supplierDeliveries = await (
                from d in context.Deliveries.AsNoTracking()
                where !d.IsDeleted
                join c in context.Contracts.AsNoTracking() on d.ContractId equals c.Id into contractJoin
                from c in contractJoin.DefaultIfEmpty()
                join sp in context.SupplierProfiles.AsNoTracking() on c.SupplierId equals sp.Id into spJoin
                from sp in spJoin.DefaultIfEmpty()
                join o in context.Orders.AsNoTracking() on d.OrderId equals o.Id into orderJoin
                from o in orderJoin.DefaultIfEmpty()
                join s in context.Shops.AsNoTracking() on o.ShopId equals s.Id into shopJoin
                from s in shopJoin.DefaultIfEmpty()
                join osp in context.SupplierProfiles.AsNoTracking() on s.SupplierId equals osp.Id into ospJoin
                from osp in ospJoin.DefaultIfEmpty()
                where (sp != null && sp.UserId == userId) || (osp != null && osp.UserId == userId)
                orderby d.CreatedAt descending
                select d
            ).Distinct().Take(100).ToListAsync(cancellationToken);

            return supplierDeliveries;
        }

        var customerDeliveries = await (
            from d in context.Deliveries.AsNoTracking()
            where !d.IsDeleted
            join c in context.Contracts.AsNoTracking() on d.ContractId equals c.Id into contractJoin
            from c in contractJoin.DefaultIfEmpty()
            join o in context.Orders.AsNoTracking() on d.OrderId equals o.Id into orderJoin
            from o in orderJoin.DefaultIfEmpty()
            where (c != null && c.CustomerId == userId) || (o != null && o.CustomerId == userId)
            orderby d.CreatedAt descending
            select d
        ).Distinct().Take(100).ToListAsync(cancellationToken);

        return customerDeliveries;
    }

    public async Task<IReadOnlyList<Delivery>> GetByContractIdAsync(Guid contractId, CancellationToken cancellationToken = default) =>
        await context.Deliveries.AsNoTracking()
            .Where(d => d.ContractId == contractId && !d.IsDeleted)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Delivery>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        await context.Deliveries.AsNoTracking()
            .Where(d => d.OrderId == orderId && !d.IsDeleted)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<Delivery> CreateAsync(Delivery delivery, CancellationToken cancellationToken = default)
    {
        context.Deliveries.Add(delivery);
        await context.SaveChangesAsync(cancellationToken);
        return delivery;
    }

    public async Task<Delivery> UpdateAsync(Delivery delivery, CancellationToken cancellationToken = default)
    {
        context.Deliveries.Update(delivery);
        await context.SaveChangesAsync(cancellationToken);
        return delivery;
    }

    public async Task<IReadOnlyList<DeliveryProof>> GetProofsAsync(Guid deliveryId, CancellationToken cancellationToken = default) =>
        await context.DeliveryProofs.AsNoTracking()
            .Where(p => p.DeliveryId == deliveryId && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<DeliveryProof?> GetProofByIdAsync(Guid proofId, CancellationToken cancellationToken = default) =>
        await context.DeliveryProofs.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == proofId && !p.IsDeleted, cancellationToken);

    public async Task<DeliveryProof> CreateProofAsync(DeliveryProof proof, CancellationToken cancellationToken = default)
    {
        context.DeliveryProofs.Add(proof);
        await context.SaveChangesAsync(cancellationToken);
        return proof;
    }

    public async Task<bool> CanAccessDeliveryAsync(Guid deliveryId, Guid userId, CancellationToken cancellationToken = default)
    {
        var delivery = await context.Deliveries.AsNoTracking().FirstOrDefaultAsync(d => d.Id == deliveryId && !d.IsDeleted, cancellationToken);
        if (delivery == null) return false;

        // Check Contract party
        if (delivery.ContractId.HasValue)
        {
            var isParty = await context.Contracts.AsNoTracking().AnyAsync(c =>
                c.Id == delivery.ContractId.Value &&
                !c.IsDeleted &&
                (c.CustomerId == userId || context.SupplierProfiles.Any(sp => sp.Id == c.SupplierId && sp.UserId == userId && !sp.IsDeleted)),
                cancellationToken);
            if (isParty) return true;
        }

        // Check Order party
        if (delivery.OrderId.HasValue)
        {
            var isParty = await context.Orders.AsNoTracking().AnyAsync(o =>
                o.Id == delivery.OrderId.Value &&
                !o.IsDeleted &&
                (o.CustomerId == userId || (from s in context.Shops
                                            join sp in context.SupplierProfiles on s.SupplierId equals sp.Id
                                            where s.Id == o.ShopId && sp.UserId == userId && !sp.IsDeleted
                                            select sp.Id).Any()),
                cancellationToken);
            if (isParty) return true;
        }

        // Check if user is Admin or Creator
        return delivery.CreatedBy == userId;
    }

    public async Task<bool> CanManageDeliveryAsync(Guid deliveryId, Guid userId, CancellationToken cancellationToken = default)
    {
        var delivery = await context.Deliveries.AsNoTracking().FirstOrDefaultAsync(d => d.Id == deliveryId && !d.IsDeleted, cancellationToken);
        if (delivery == null) return false;

        // Supplier of Contract can manage
        if (delivery.ContractId.HasValue)
        {
            var isSupplier = await context.Contracts.AsNoTracking().AnyAsync(c =>
                c.Id == delivery.ContractId.Value &&
                !c.IsDeleted &&
                context.SupplierProfiles.Any(sp => sp.Id == c.SupplierId && sp.UserId == userId && !sp.IsDeleted),
                cancellationToken);
            if (isSupplier) return true;
        }

        // Shop/Supplier of Order can manage
        if (delivery.OrderId.HasValue)
        {
            var isSupplier = await (from o in context.Orders.AsNoTracking()
                                    join s in context.Shops.AsNoTracking() on o.ShopId equals s.Id
                                    join sp in context.SupplierProfiles.AsNoTracking() on s.SupplierId equals sp.Id
                                    where o.Id == delivery.OrderId.Value && !o.IsDeleted && sp.UserId == userId && !sp.IsDeleted
                                    select sp.Id).AnyAsync(cancellationToken);
            if (isSupplier) return true;
        }

        return delivery.CreatedBy == userId;
    }
}
