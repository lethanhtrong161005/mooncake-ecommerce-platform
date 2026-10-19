namespace Mooncake.EcommercePlatform.Application.DTOs.Deliveries.Responses;

using Mooncake.EcommercePlatform.Domain.Enums;

public sealed record DeliveryResponse(Guid Id, Guid? OrderId, Guid? ContractId, string DeliveryNumber,
    DeliveryType DeliveryType, DateOnly? ScheduledDate, DateOnly? ActualDeliveryDate, string? FromAddress,
    string? ToAddress, string? RecipientName, string? RecipientPhone, string? TrackingCode,
    DeliveryStatus Status, string? Notes, IReadOnlyList<DeliveryProofResponse> Proofs);

public sealed record DeliveryProofResponse(Guid Id, ProofType ProofType, string? PhotoUrl, string? SignatureUrl,
    string? TakenBy, DateTime? TakenAt, decimal? LocationLat, decimal? LocationLng, string? Notes);
