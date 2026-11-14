namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.DTOs;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Deliveries.Requests;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

/// <summary>EF Core persistence and tenant authorization for shipments and Proof-of-Delivery records.</summary>
public sealed class DeliveryRepository(ApplicationDbContext context) : IDeliveryRepository
{
    public async Task<PagedResult<Delivery>> GetDeliveriesPageAsync(Guid userId, bool isSupplier,
        DeliveryPageQueryRequest query, CancellationToken cancellationToken = default)
    {
        IQueryable<Delivery> rows = context.Deliveries.AsNoTracking().Where(delivery => !delivery.IsDeleted);
        var isAdmin = await IsAdminAsync(userId, cancellationToken);
        if (!isAdmin)
        {
            rows = isSupplier
                ? rows.Where(delivery => delivery.ContractId.HasValue && context.Contracts.Any(contract =>
                    contract.Id == delivery.ContractId.Value && !contract.IsDeleted
                    && context.SupplierProfiles.Any(profile => profile.Id == contract.SupplierId
                        && profile.UserId == userId && !profile.IsDeleted))
                    || delivery.OrderId.HasValue && context.Orders.Any(order => order.Id == delivery.OrderId.Value
                        && !order.IsDeleted && context.Shops.Any(shop => shop.Id == order.ShopId && !shop.IsDeleted
                            && (shop.OwnerUserId == userId || context.SupplierProfiles.Any(profile =>
                                profile.Id == shop.SupplierId && profile.UserId == userId && !profile.IsDeleted)))))
                : rows.Where(delivery => delivery.ContractId.HasValue && context.Contracts.Any(contract =>
                    contract.Id == delivery.ContractId.Value && contract.CustomerId == userId && !contract.IsDeleted)
                    || delivery.OrderId.HasValue && context.Orders.Any(order => order.Id == delivery.OrderId.Value
                        && order.CustomerId == userId && !order.IsDeleted));
        }
        if (query.Status.HasValue) rows = rows.Where(delivery => delivery.Status == query.Status.Value);
        if (query.OrderId.HasValue) rows = rows.Where(delivery => delivery.OrderId == query.OrderId.Value);
        if (query.ContractId.HasValue) rows = rows.Where(delivery => delivery.ContractId == query.ContractId.Value);
        if (query.ScheduledFrom.HasValue) rows = rows.Where(delivery => delivery.ScheduledDate >= query.ScheduledFrom.Value);
        if (query.ScheduledTo.HasValue) rows = rows.Where(delivery => delivery.ScheduledDate <= query.ScheduledTo.Value);
        if (query.From.HasValue) rows = rows.Where(delivery => delivery.CreatedAt >= query.From.Value);
        if (query.To.HasValue) rows = rows.Where(delivery => delivery.CreatedAt <= query.To.Value);
        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var term = $"%{query.Q.Trim()}%";
            rows = rows.Where(delivery => EF.Functions.ILike(delivery.DeliveryNumber, term)
                || delivery.TrackingCode != null && EF.Functions.ILike(delivery.TrackingCode, term));
        }
        var total = await rows.CountAsync(cancellationToken);
        rows = query.Sort switch
        {
            "createdAtAsc" => rows.OrderBy(delivery => delivery.CreatedAt).ThenBy(delivery => delivery.Id),
            "scheduledDateAsc" => rows.OrderBy(delivery => delivery.ScheduledDate).ThenBy(delivery => delivery.Id),
            "scheduledDateDesc" => rows.OrderByDescending(delivery => delivery.ScheduledDate).ThenByDescending(delivery => delivery.Id),
            _ => rows.OrderByDescending(delivery => delivery.CreatedAt).ThenByDescending(delivery => delivery.Id)
        };
        var items = await PagedQueryHelper.ApplyPaging(rows, query).ToListAsync(cancellationToken);
        return new PagedResult<Delivery>(items, query.Page, query.PageSize, total);
    }

    public async Task<Delivery?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.Deliveries.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<Delivery>> GetDeliveriesForUserAsync(Guid userId, bool isSupplier, CancellationToken cancellationToken = default)
    {
        if (await IsAdminAsync(userId, cancellationToken))
        {
            return await context.Deliveries.AsNoTracking()
                .Where(d => !d.IsDeleted)
                .OrderByDescending(d => d.CreatedAt)
                .Take(100)
                .ToListAsync(cancellationToken);
        }

        if (isSupplier)
        {
            return await (
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
                where (c != null && !c.IsDeleted && sp != null && !sp.IsDeleted && sp.UserId == userId)
                      || (o != null && !o.IsDeleted && s != null && !s.IsDeleted
                          && (s.OwnerUserId == userId || (osp != null && !osp.IsDeleted && osp.UserId == userId)))
                orderby d.CreatedAt descending
                select d
            ).Distinct().Take(100).ToListAsync(cancellationToken);
        }

        return await (
            from d in context.Deliveries.AsNoTracking()
            where !d.IsDeleted
            join c in context.Contracts.AsNoTracking() on d.ContractId equals c.Id into contractJoin
            from c in contractJoin.DefaultIfEmpty()
            join o in context.Orders.AsNoTracking() on d.OrderId equals o.Id into orderJoin
            from o in orderJoin.DefaultIfEmpty()
            where (c != null && !c.IsDeleted && c.CustomerId == userId)
                  || (o != null && !o.IsDeleted && o.CustomerId == userId)
            orderby d.CreatedAt descending
            select d
        ).Distinct().Take(100).ToListAsync(cancellationToken);
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
        var local = context.Deliveries.Local.FirstOrDefault(d => d.Id == delivery.Id);
        if (local != null)
        {
            context.Entry(local).CurrentValues.SetValues(delivery);
        }
        else
        {
            context.Deliveries.Update(delivery);
        }
        await context.SaveChangesAsync(cancellationToken);
        return delivery;
    }

    public async Task<IReadOnlyList<DeliveryProof>> GetProofsAsync(Guid deliveryId, CancellationToken cancellationToken = default) =>
        await context.DeliveryProofs.AsNoTracking()
            .Where(p => p.DeliveryId == deliveryId && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, int>?> GetProofCountsAsync(
        IReadOnlyCollection<Guid> deliveryIds, CancellationToken cancellationToken = default)
    {
        if (deliveryIds.Count == 0)
            return new Dictionary<Guid, int>();

        return await context.DeliveryProofs.AsNoTracking()
            .Where(p => deliveryIds.Contains(p.DeliveryId) && !p.IsDeleted)
            .GroupBy(p => p.DeliveryId)
            .Select(group => new { DeliveryId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.DeliveryId, item => item.Count, cancellationToken);
    }

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
        var delivery = await context.Deliveries.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == deliveryId && !d.IsDeleted, cancellationToken);
        if (delivery == null)
            return false;

        if (delivery.ContractId.HasValue)
            return await CanAccessContractAsync(delivery.ContractId.Value, userId, cancellationToken);

        if (delivery.OrderId.HasValue)
            return await CanAccessOrderAsync(delivery.OrderId.Value, userId, cancellationToken);

        return await IsAdminAsync(userId, cancellationToken);
    }

    public async Task<bool> CanManageDeliveryAsync(Guid deliveryId, Guid userId, CancellationToken cancellationToken = default)
    {
        var delivery = await context.Deliveries.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == deliveryId && !d.IsDeleted, cancellationToken);
        if (delivery == null)
            return false;

        if (delivery.ContractId.HasValue)
            return await CanManageContractAsync(delivery.ContractId.Value, userId, cancellationToken);

        if (delivery.OrderId.HasValue)
            return await CanManageOrderAsync(delivery.OrderId.Value, userId, cancellationToken);

        return await IsAdminAsync(userId, cancellationToken);
    }

    public async Task<bool> CanAccessContractAsync(Guid contractId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (await IsAdminAsync(userId, cancellationToken))
            return true;

        return await context.Contracts.AsNoTracking().AnyAsync(c =>
            c.Id == contractId && !c.IsDeleted &&
            (c.CustomerId == userId || context.SupplierProfiles.Any(sp => sp.Id == c.SupplierId && sp.UserId == userId && !sp.IsDeleted)),
            cancellationToken);
    }

    public async Task<bool> CanManageContractAsync(Guid contractId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (await IsAdminAsync(userId, cancellationToken))
            return true;

        return await context.Contracts.AsNoTracking().AnyAsync(c =>
            c.Id == contractId && !c.IsDeleted &&
            context.SupplierProfiles.Any(sp => sp.Id == c.SupplierId && sp.UserId == userId && !sp.IsDeleted),
            cancellationToken);
    }

    public async Task<bool> CanAccessOrderAsync(Guid orderId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (await IsAdminAsync(userId, cancellationToken))
            return true;

        return await (
            from o in context.Orders.AsNoTracking()
            join s in context.Shops.AsNoTracking() on o.ShopId equals s.Id
            join sp in context.SupplierProfiles.AsNoTracking() on s.SupplierId equals sp.Id into spJoin
            from sp in spJoin.DefaultIfEmpty()
            where o.Id == orderId && !o.IsDeleted && !s.IsDeleted
                  && (o.CustomerId == userId
                      || s.OwnerUserId == userId
                      || (sp != null && !sp.IsDeleted && sp.UserId == userId))
            select o.Id
        ).AnyAsync(cancellationToken);
    }

    public async Task<bool> CanManageOrderAsync(Guid orderId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (await IsAdminAsync(userId, cancellationToken))
            return true;

        return await (
            from o in context.Orders.AsNoTracking()
            join s in context.Shops.AsNoTracking() on o.ShopId equals s.Id
            join sp in context.SupplierProfiles.AsNoTracking() on s.SupplierId equals sp.Id into spJoin
            from sp in spJoin.DefaultIfEmpty()
            where o.Id == orderId && !o.IsDeleted && !s.IsDeleted
                  && (s.OwnerUserId == userId || (sp != null && !sp.IsDeleted && sp.UserId == userId))
            select o.Id
        ).AnyAsync(cancellationToken);
    }

    public async Task<Delivery?> GetByShareTokenAsync(string token, CancellationToken cancellationToken = default) =>
        await context.Deliveries.AsNoTracking()
            .FirstOrDefaultAsync(d => d.ShareToken == token && !d.IsDeleted, cancellationToken);

    public async Task<string?> GetReferenceCodeAsync(Delivery delivery, CancellationToken cancellationToken = default)
    {
        if (delivery.OrderId.HasValue)
        {
            return await context.Orders.AsNoTracking()
                .Where(o => o.Id == delivery.OrderId.Value && !o.IsDeleted)
                .Select(o => o.OrderNumber)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (delivery.ContractId.HasValue)
        {
            return await context.Contracts.AsNoTracking()
                .Where(c => c.Id == delivery.ContractId.Value && !c.IsDeleted)
                .Select(c => c.ContractNumber)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return null;
    }

    public async Task<IReadOnlyDictionary<Guid, DeliveryPartyContext>?> GetPartyContextsAsync(
        IReadOnlyCollection<Delivery> deliveries, CancellationToken cancellationToken = default)
    {
        if (deliveries.Count == 0)
            return new Dictionary<Guid, DeliveryPartyContext>();

        var result = new Dictionary<Guid, DeliveryPartyContext>(deliveries.Count);

        var orderIds = deliveries.Where(d => d.OrderId.HasValue).Select(d => d.OrderId!.Value).Distinct().ToArray();
        var orderMap = orderIds.Length == 0
            ? new Dictionary<Guid, DeliveryPartyContext>()
            : await (
                from o in context.Orders.AsNoTracking()
                join c in context.Users.AsNoTracking() on o.CustomerId equals c.Id into customerJoin
                from c in customerJoin.DefaultIfEmpty()
                join s in context.Shops.AsNoTracking() on o.ShopId equals s.Id into shopJoin
                from s in shopJoin.DefaultIfEmpty()
                where orderIds.Contains(o.Id) && !o.IsDeleted
                select new
                {
                    o.Id,
                    o.OrderNumber,
                    CustomerName = c != null ? (c.FullName ?? c.Email) : null,
                    SupplierName = s != null ? s.Name : null
                }
            ).ToDictionaryAsync(
                x => x.Id,
                x => new DeliveryPartyContext(x.OrderNumber, null, x.CustomerName, x.SupplierName),
                cancellationToken);

        var contractIds = deliveries.Where(d => d.ContractId.HasValue).Select(d => d.ContractId!.Value).Distinct().ToArray();
        var contractMap = contractIds.Length == 0
            ? new Dictionary<Guid, DeliveryPartyContext>()
            : await (
                from ct in context.Contracts.AsNoTracking()
                join c in context.Users.AsNoTracking() on ct.CustomerId equals c.Id into customerJoin
                from c in customerJoin.DefaultIfEmpty()
                join sp in context.SupplierProfiles.AsNoTracking() on ct.SupplierId equals sp.Id into spJoin
                from sp in spJoin.DefaultIfEmpty()
                where contractIds.Contains(ct.Id) && !ct.IsDeleted
                select new
                {
                    ct.Id,
                    ct.ContractNumber,
                    CustomerName = c != null ? (c.FullName ?? c.Email) : null,
                    SupplierName = sp != null ? sp.CompanyName : null
                }
            ).ToDictionaryAsync(
                x => x.Id,
                x => new DeliveryPartyContext(null, x.ContractNumber, x.CustomerName, x.SupplierName),
                cancellationToken);

        foreach (var delivery in deliveries)
        {
            if (delivery.OrderId.HasValue && orderMap.TryGetValue(delivery.OrderId.Value, out var ord))
            {
                result[delivery.Id] = ord;
            }
            else if (delivery.ContractId.HasValue && contractMap.TryGetValue(delivery.ContractId.Value, out var ctr))
            {
                result[delivery.Id] = ctr;
            }
        }

        return result;
    }

    private Task<bool> IsAdminAsync(Guid userId, CancellationToken cancellationToken) =>
        context.Users.AsNoTracking().AnyAsync(u => u.Id == userId && !u.IsDeleted && u.Role == UserRole.Admin, cancellationToken);
}
