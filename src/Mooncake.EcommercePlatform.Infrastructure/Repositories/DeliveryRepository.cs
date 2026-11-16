namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.DTOs;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Deliveries.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Deliveries.Responses;
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

    public async Task<bool> CanConfirmDeliveryReceiptAsync(Guid deliveryId, Guid userId, CancellationToken cancellationToken = default)
    {
        var delivery = await context.Deliveries.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == deliveryId && !d.IsDeleted, cancellationToken);
        if (delivery?.ContractId is Guid contractId)
        {
            return await context.Contracts.AsNoTracking().AnyAsync(contract =>
                contract.Id == contractId && contract.CustomerId == userId && !contract.IsDeleted,
                cancellationToken);
        }

        if (delivery?.OrderId is Guid orderId)
        {
            return await context.Orders.AsNoTracking().AnyAsync(order =>
                order.Id == orderId && order.CustomerId == userId && !order.IsDeleted,
                cancellationToken);
        }

        return false;
    }

    public async Task<Guid?> GetSupplierUserIdForDeliveryAsync(Guid deliveryId, CancellationToken cancellationToken = default)
    {
        var contractSupplierUserId = await (
            from delivery in context.Deliveries.AsNoTracking()
            join contract in context.Contracts.AsNoTracking() on delivery.ContractId equals contract.Id
            join profile in context.SupplierProfiles.AsNoTracking() on contract.SupplierId equals profile.Id
            where delivery.Id == deliveryId && !delivery.IsDeleted && !contract.IsDeleted && !profile.IsDeleted
            select (Guid?)profile.UserId
        ).FirstOrDefaultAsync(cancellationToken);
        if (contractSupplierUserId.HasValue) return contractSupplierUserId;

        return await (
            from delivery in context.Deliveries.AsNoTracking()
            join order in context.Orders.AsNoTracking() on delivery.OrderId equals order.Id
            join shop in context.Shops.AsNoTracking() on order.ShopId equals shop.Id
            where delivery.Id == deliveryId && !delivery.IsDeleted && !order.IsDeleted && !shop.IsDeleted
            select (Guid?)shop.OwnerUserId
        ).FirstOrDefaultAsync(cancellationToken);
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

    public async Task<Delivery?> GetByShareCodeAsync(string shareCode, CancellationToken cancellationToken = default)
    {
        var upperCode = shareCode.ToUpperInvariant();
        return await context.Deliveries.AsNoTracking()
            .FirstOrDefaultAsync(d => d.ShareCode != null && d.ShareCode.ToUpper() == upperCode && !d.IsDeleted, cancellationToken);
    }

    public async Task<bool> IsShareCodeInUseAsync(string shareCode, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var upperCode = shareCode.ToUpperInvariant();
        return await context.Deliveries.AsNoTracking().AnyAsync(
            d => d.ShareCode != null
                 && d.ShareCode.ToUpper() == upperCode
                 && !d.IsDeleted
                 && !d.ShareTokenRevokedAtUtc.HasValue
                 && d.ShareTokenExpiresAtUtc.HasValue
                 && d.ShareTokenExpiresAtUtc.Value > utcNow,
            cancellationToken);
    }

    public async Task<Guid?> GetCustomerUserIdForDeliveryAsync(Guid deliveryId, CancellationToken cancellationToken = default)
    {
        var contractCustomerId = await (
            from delivery in context.Deliveries.AsNoTracking()
            join contract in context.Contracts.AsNoTracking() on delivery.ContractId equals contract.Id
            where delivery.Id == deliveryId && !delivery.IsDeleted && !contract.IsDeleted
            select (Guid?)contract.CustomerId
        ).FirstOrDefaultAsync(cancellationToken);
        if (contractCustomerId.HasValue) return contractCustomerId;

        return await (
            from delivery in context.Deliveries.AsNoTracking()
            join order in context.Orders.AsNoTracking() on delivery.OrderId equals order.Id
            where delivery.Id == deliveryId && !delivery.IsDeleted && !order.IsDeleted
            select (Guid?)order.CustomerId
        ).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddWorkflowEventAsync(WorkflowEvent workflowEvent, CancellationToken cancellationToken = default)
    {
        context.WorkflowEvents.Add(workflowEvent);
        await context.SaveChangesAsync(cancellationToken);
    }

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
                join cp in context.CustomerProfiles.AsNoTracking() on o.CustomerId equals cp.UserId into cpJoin
                from cp in cpJoin.DefaultIfEmpty()
                join s in context.Shops.AsNoTracking() on o.ShopId equals s.Id into shopJoin
                from s in shopJoin.DefaultIfEmpty()
                where orderIds.Contains(o.Id) && !o.IsDeleted
                select new
                {
                    o.Id,
                    o.OrderNumber,
                    CustomerName = c != null ? (c.FullName ?? c.Email) : null,
                    CustomerCompanyName = cp != null ? cp.CompanyName : null,
                    SupplierName = s != null ? s.Name : null
                }
            ).ToDictionaryAsync(
                x => x.Id,
                x => new DeliveryPartyContext(x.OrderNumber, null, x.CustomerName, x.SupplierName, x.CustomerCompanyName),
                cancellationToken);

        var contractIds = deliveries.Where(d => d.ContractId.HasValue).Select(d => d.ContractId!.Value).Distinct().ToArray();
        var contractMap = contractIds.Length == 0
            ? new Dictionary<Guid, DeliveryPartyContext>()
            : await (
                from ct in context.Contracts.AsNoTracking()
                join c in context.Users.AsNoTracking() on ct.CustomerId equals c.Id into customerJoin
                from c in customerJoin.DefaultIfEmpty()
                join cp in context.CustomerProfiles.AsNoTracking() on ct.CustomerId equals cp.UserId into cpJoin
                from cp in cpJoin.DefaultIfEmpty()
                join sp in context.SupplierProfiles.AsNoTracking() on ct.SupplierId equals sp.Id into spJoin
                from sp in spJoin.DefaultIfEmpty()
                where contractIds.Contains(ct.Id) && !ct.IsDeleted
                select new
                {
                    ct.Id,
                    ct.ContractNumber,
                    CustomerName = c != null ? (c.FullName ?? c.Email) : null,
                    CustomerCompanyName = cp != null ? cp.CompanyName : null,
                    SupplierName = sp != null ? sp.CompanyName : null
                }
            ).ToDictionaryAsync(
                x => x.Id,
                x => new DeliveryPartyContext(null, x.ContractNumber, x.CustomerName, x.SupplierName, x.CustomerCompanyName),
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

    public async Task<IReadOnlyList<DeliveryItem>> GetDeliveryItemsAsync(Guid deliveryId, CancellationToken cancellationToken = default) =>
        await context.DeliveryItems.AsNoTracking()
            .Where(di => di.DeliveryId == deliveryId && !di.IsDeleted)
            .OrderBy(di => di.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<DeliveryItem>>?> GetDeliveryItemsMapAsync(
        IReadOnlyCollection<Guid> deliveryIds, CancellationToken cancellationToken = default)
    {
        if (deliveryIds.Count == 0)
            return new Dictionary<Guid, IReadOnlyList<DeliveryItem>>();

        var items = await context.DeliveryItems.AsNoTracking()
            .Where(di => deliveryIds.Contains(di.DeliveryId) && !di.IsDeleted)
            .OrderBy(di => di.CreatedAt)
            .ToListAsync(cancellationToken);

        return items
            .GroupBy(di => di.DeliveryId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<DeliveryItem>)g.ToList());
    }

    public async Task<IReadOnlyDictionary<Guid, int>?> GetContractTotalBatchesMapAsync(
        IReadOnlyCollection<Guid> contractIds, CancellationToken cancellationToken = default)
    {
        if (contractIds.Count == 0)
            return new Dictionary<Guid, int>();

        return await context.Deliveries.AsNoTracking()
            .Where(d => d.ContractId.HasValue && contractIds.Contains(d.ContractId.Value) && !d.IsDeleted)
            .GroupBy(d => d.ContractId!.Value)
            .Select(g => new { ContractId = g.Key, MaxBatch = g.Max(d => (int?)d.BatchNumber) ?? g.Count() })
            .ToDictionaryAsync(x => x.ContractId, x => x.MaxBatch, cancellationToken);
    }

    public async Task<IReadOnlyList<DeliveryItem>> CreateDeliveryItemsAsync(
        IReadOnlyCollection<DeliveryItem> items, CancellationToken cancellationToken = default)
    {
        if (items.Count == 0)
            return [];

        context.DeliveryItems.AddRange(items);
        await context.SaveChangesAsync(cancellationToken);
        return items.ToList();
    }

    public async Task<IReadOnlyList<DeliveryItem>> SnapshotOrderItemsAsync(
        Guid deliveryId, Guid orderId, Guid userId, CancellationToken cancellationToken = default)
    {
        var orderItems = await (
            from oi in context.OrderItems.AsNoTracking()
            join p in context.Products.AsNoTracking() on oi.ProductId equals p.Id into prodJoin
            from p in prodJoin.DefaultIfEmpty()
            where oi.OrderId == orderId && !oi.IsDeleted
            orderby oi.CreatedAt
            select new
            {
                OrderItemId = oi.Id,
                ProductName = oi.ProductNameSnapshot,
                oi.Quantity,
                Unit = p != null ? p.Unit : null
            }
        ).ToListAsync(cancellationToken);

        var items = orderItems.Select(oi => new DeliveryItem
        {
            Id = Guid.NewGuid(),
            DeliveryId = deliveryId,
            OrderItemId = oi.OrderItemId,
            ProductName = oi.ProductName,
            Unit = oi.Unit,
            Quantity = oi.Quantity,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId
        }).ToList();

        if (items.Count > 0)
        {
            context.DeliveryItems.AddRange(items);
            await context.SaveChangesAsync(cancellationToken);
        }

        return items;
    }

    public async Task<int> GetNextBatchNumberForContractAsync(Guid contractId, CancellationToken cancellationToken = default)
    {
        var max = await context.Deliveries.AsNoTracking()
            .Where(d => d.ContractId == contractId && !d.IsDeleted)
            .MaxAsync(d => (int?)d.BatchNumber, cancellationToken);
        return (max ?? 0) + 1;
    }

    public async Task<int> GetTotalBatchesForContractAsync(Guid contractId, CancellationToken cancellationToken = default)
    {
        var max = await context.Deliveries.AsNoTracking()
            .Where(d => d.ContractId == contractId && !d.IsDeleted)
            .MaxAsync(d => (int?)d.BatchNumber, cancellationToken);
        if (max.HasValue && max.Value > 0) return max.Value;

        return await context.Deliveries.AsNoTracking()
            .Where(d => d.ContractId == contractId && !d.IsDeleted)
            .CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ContractLineInfo>> GetContractLinesAsync(Guid contractId, CancellationToken cancellationToken = default)
    {
        var contract = await context.Contracts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == contractId && !c.IsDeleted, cancellationToken);
        if (contract == null) return [];

        if (contract.RfqId.HasValue)
        {
            return await context.RfqItems.AsNoTracking()
                .Where(r => r.RfqId == contract.RfqId.Value && !r.IsDeleted)
                .OrderBy(r => r.CreatedAt)
                .Select(r => new ContractLineInfo(r.Id, r.ProductName ?? string.Empty, r.Unit, r.Quantity))
                .ToListAsync(cancellationToken);
        }

        if (contract.OrderId.HasValue)
        {
            return await (
                from oi in context.OrderItems.AsNoTracking()
                join p in context.Products.AsNoTracking() on oi.ProductId equals p.Id into prodJoin
                from p in prodJoin.DefaultIfEmpty()
                where oi.OrderId == contract.OrderId.Value && !oi.IsDeleted
                orderby oi.CreatedAt
                select new ContractLineInfo(oi.Id, oi.ProductNameSnapshot, p != null ? p.Unit : null, oi.Quantity)
            ).ToListAsync(cancellationToken);
        }

        return [];
    }

    public async Task<IReadOnlyList<ContractFulfillmentLineResponse>> GetContractFulfillmentAsync(Guid contractId, CancellationToken cancellationToken = default)
    {
        var contract = await context.Contracts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == contractId && !c.IsDeleted, cancellationToken);
        if (contract == null) return [];

        var lines = await GetContractLinesAsync(contractId, cancellationToken);
        if (lines.Count == 0) return [];

        var deliveries = await context.Deliveries.AsNoTracking()
            .Where(d => d.ContractId == contractId && !d.IsDeleted)
            .ToListAsync(cancellationToken);

        var deliveryIds = deliveries.Select(d => d.Id).ToList();
        var deliveryItems = deliveryIds.Count == 0
            ? []
            : await context.DeliveryItems.AsNoTracking()
                .Where(di => deliveryIds.Contains(di.DeliveryId) && !di.IsDeleted)
                .ToListAsync(cancellationToken);

        var activeDeliveryIds = deliveries.Where(d => d.Status != DeliveryStatus.Returned).Select(d => d.Id).ToHashSet();
        var deliveredDeliveryIds = deliveries.Where(d => d.Status == DeliveryStatus.Delivered).Select(d => d.Id).ToHashSet();

        var result = new List<ContractFulfillmentLineResponse>(lines.Count);
        foreach (var line in lines)
        {
            var lineDeliveryItems = deliveryItems.Where(di => di.RfqItemId == line.LineId || di.OrderItemId == line.LineId).ToList();
            var scheduled = lineDeliveryItems.Where(di => activeDeliveryIds.Contains(di.DeliveryId)).Sum(di => di.Quantity);
            var delivered = lineDeliveryItems.Where(di => deliveredDeliveryIds.Contains(di.DeliveryId)).Sum(di => di.Quantity);
            var remaining = Math.Max(0, line.Quantity - scheduled);

            result.Add(new ContractFulfillmentLineResponse
            {
                LineId = line.LineId,
                RfqItemId = contract.RfqId.HasValue ? line.LineId : null,
                OrderItemId = contract.OrderId.HasValue ? line.LineId : null,
                ProductName = line.ProductName,
                Unit = line.Unit,
                TotalQuantity = line.Quantity,
                ScheduledQuantity = scheduled,
                DeliveredQuantity = delivered,
                RemainingQuantity = remaining
            });
        }

        return result;
    }

    private Task<bool> IsAdminAsync(Guid userId, CancellationToken cancellationToken) =>
        context.Users.AsNoTracking().AnyAsync(u => u.Id == userId && !u.IsDeleted && u.Role == UserRole.Admin, cancellationToken);
}
