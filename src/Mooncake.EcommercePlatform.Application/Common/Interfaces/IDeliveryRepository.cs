namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Data access contract for deliveries and GPS-tagged delivery proofs.</summary>
public interface IDeliveryRepository
{
    Task<Delivery> CreateAsync(Delivery delivery, CancellationToken cancellationToken = default);
    Task<Delivery> UpdateAsync(Delivery delivery, CancellationToken cancellationToken = default);
    Task<Delivery?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Delivery>> GetDeliveriesAsync(long? orderId = null, long? contractId = null, DeliveryStatus? status = null, CancellationToken cancellationToken = default);

    Task<DeliveryProof> AddProofAsync(DeliveryProof proof, CancellationToken cancellationToken = default);
    Task<IEnumerable<DeliveryProof>> GetProofsByDeliveryIdAsync(long deliveryId, CancellationToken cancellationToken = default);
}
