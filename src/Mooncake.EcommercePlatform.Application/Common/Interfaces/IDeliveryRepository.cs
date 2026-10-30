namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Domain.Entities;

public interface IDeliveryRepository
{
    Task<Delivery?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Delivery>> GetDeliveriesForUserAsync(Guid userId, bool isSupplier, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Delivery>> GetByContractIdAsync(Guid contractId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Delivery>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<Delivery> CreateAsync(Delivery delivery, CancellationToken cancellationToken = default);

    Task<Delivery> UpdateAsync(Delivery delivery, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DeliveryProof>> GetProofsAsync(Guid deliveryId, CancellationToken cancellationToken = default);

    Task<DeliveryProof?> GetProofByIdAsync(Guid proofId, CancellationToken cancellationToken = default);

    Task<DeliveryProof> CreateProofAsync(DeliveryProof proof, CancellationToken cancellationToken = default);

    Task<bool> CanAccessDeliveryAsync(Guid deliveryId, Guid userId, CancellationToken cancellationToken = default);

    Task<bool> CanManageDeliveryAsync(Guid deliveryId, Guid userId, CancellationToken cancellationToken = default);
}

