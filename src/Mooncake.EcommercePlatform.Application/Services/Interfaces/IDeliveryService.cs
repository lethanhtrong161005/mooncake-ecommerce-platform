namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Deliveries.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Deliveries.Responses;

public interface IDeliveryService
{
    Task<IReadOnlyList<DeliveryResponse>> GetMineAsync(Guid userId, bool isSupplier, CancellationToken cancellationToken = default);

    Task<DeliveryDetailResponse> GetAsync(Guid userId, Guid deliveryId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DeliveryResponse>> GetByContractAsync(Guid userId, Guid contractId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DeliveryResponse>> GetByOrderAsync(Guid userId, Guid orderId, CancellationToken cancellationToken = default);

    Task<DeliveryResponse> CreateAsync(Guid userId, CreateDeliveryRequest request, CancellationToken cancellationToken = default);

    Task<DeliveryResponse> UpdateStatusAsync(Guid userId, Guid deliveryId, UpdateDeliveryStatusRequest request, CancellationToken cancellationToken = default);

    Task<DeliveryResponse> UpdateCarrierInfoAsync(Guid userId, Guid deliveryId, UpdateCarrierInfoRequest request, CancellationToken cancellationToken = default);

    Task<DeliveryProofResponse> SubmitProofAsync(Guid userId, Guid deliveryId, CreateDeliveryProofRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DeliveryProofResponse>> GetProofsAsync(Guid userId, Guid deliveryId, CancellationToken cancellationToken = default);

    Task<DeliveryProofResponse> GetProofDetailAsync(Guid userId, Guid proofId, CancellationToken cancellationToken = default);

    Task<bool> VerifyProofIntegrityAsync(Guid proofId, CancellationToken cancellationToken = default);
}

