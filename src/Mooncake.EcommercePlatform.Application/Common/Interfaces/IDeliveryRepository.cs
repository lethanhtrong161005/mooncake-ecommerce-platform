namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

public interface IDeliveryRepository
{
    Task<IReadOnlyList<Delivery>> GetMineAsync(Guid userId, bool supplier, CancellationToken cancellationToken = default);
    Task<Delivery?> GetAsync(Guid deliveryId, CancellationToken cancellationToken = default);
    Task<bool> CanAccessAsync(Delivery delivery, Guid userId, bool supplier, CancellationToken cancellationToken = default);
    Task<bool> CreateAsync(Delivery delivery, Guid supplierUserId, CancellationToken cancellationToken = default);
    Task<bool> TransitionAsync(Guid deliveryId, Guid actorUserId, bool supplier, DeliveryStatus nextStatus,
        string? trackingCode, CancellationToken cancellationToken = default);
    Task<bool> AddProofAsync(Guid deliveryId, Guid actorUserId, bool supplier, DeliveryProof proof,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DeliveryProof>> GetProofsAsync(Guid deliveryId, CancellationToken cancellationToken = default);
}
