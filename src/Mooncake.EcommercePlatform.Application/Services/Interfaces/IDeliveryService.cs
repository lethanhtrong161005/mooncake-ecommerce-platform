namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Deliveries.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Deliveries.Responses;
using Mooncake.EcommercePlatform.Domain.Enums;

public interface IDeliveryService
{
    Task<IReadOnlyList<DeliveryResponse>> GetMineAsync(Guid userId, bool supplier, CancellationToken cancellationToken = default);
    Task<DeliveryResponse> GetAsync(Guid userId, bool supplier, Guid deliveryId, CancellationToken cancellationToken = default);
    Task<DeliveryResponse> CreateAsync(Guid supplierUserId, CreateDeliveryRequest request, CancellationToken cancellationToken = default);
    Task<DeliveryResponse> TransitionAsync(Guid userId, bool supplier, Guid deliveryId, DeliveryStatus status,
        string? trackingCode, CancellationToken cancellationToken = default);
    Task<DeliveryResponse> AddProofAsync(Guid userId, bool supplier, Guid deliveryId, AddDeliveryProofRequest request,
        CancellationToken cancellationToken = default);
}
