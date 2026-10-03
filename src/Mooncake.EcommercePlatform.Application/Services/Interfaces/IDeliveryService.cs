namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Deliveries.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Deliveries.Responses;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Two-way shipment management, status transitions, GPS-tagged proofs, and on-time reputation awards.</summary>
public interface IDeliveryService
{
    Task<DeliveryResponse> CreateDeliveryAsync(CreateDeliveryRequest request, CancellationToken cancellationToken = default);
    Task<DeliveryResponse> UpdateStatusAsync(long id, UpdateDeliveryStatusRequest request, CancellationToken cancellationToken = default);
    Task<DeliveryProofResponse> AddProofAsync(long id, AddDeliveryProofRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<DeliveryResponse>> GetDeliveriesAsync(long? orderId = null, long? contractId = null, DeliveryStatus? status = null, CancellationToken cancellationToken = default);
    Task<DeliveryResponse?> GetDeliveryByIdAsync(long id, CancellationToken cancellationToken = default);
}
