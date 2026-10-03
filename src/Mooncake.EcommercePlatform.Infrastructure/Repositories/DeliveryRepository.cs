namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IDeliveryRepository"/>.</summary>
public class DeliveryRepository(ApplicationDbContext context) : IDeliveryRepository
{
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

    public async Task<Delivery?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        await context.Deliveries
            .Include(d => d.Order)
            .Include(d => d.Contract)
            .Include(d => d.DeliveryProofs.OrderByDescending(p => p.TakenAtUtc))
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task<IEnumerable<Delivery>> GetDeliveriesAsync(
        long? orderId = null,
        long? contractId = null,
        DeliveryStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.Deliveries
            .Include(d => d.Order)
            .Include(d => d.Contract)
            .Include(d => d.DeliveryProofs)
            .AsNoTracking();

        if (orderId.HasValue)
        {
            query = query.Where(d => d.OrderId == orderId.Value);
        }

        if (contractId.HasValue)
        {
            query = query.Where(d => d.ContractId == contractId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(d => d.Status == status.Value);
        }

        return await query.OrderByDescending(d => d.CreatedAtUtc).ToListAsync(cancellationToken);
    }

    public async Task<DeliveryProof> AddProofAsync(DeliveryProof proof, CancellationToken cancellationToken = default)
    {
        context.DeliveryProofs.Add(proof);
        await context.SaveChangesAsync(cancellationToken);
        return proof;
    }

    public async Task<IEnumerable<DeliveryProof>> GetProofsByDeliveryIdAsync(long deliveryId, CancellationToken cancellationToken = default) =>
        await context.DeliveryProofs
            .Include(p => p.TakenByUser)
            .AsNoTracking()
            .Where(p => p.DeliveryId == deliveryId)
            .OrderByDescending(p => p.TakenAtUtc)
            .ToListAsync(cancellationToken);
}
