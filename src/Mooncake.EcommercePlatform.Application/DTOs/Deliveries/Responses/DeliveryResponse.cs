namespace Mooncake.EcommercePlatform.Application.DTOs.Deliveries.Responses;

using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>GPS-tagged delivery proof projection.</summary>
public record DeliveryProofResponse(
    long Id,
    long DeliveryId,
    ProofType ProofType,
    string PhotoUrl,
    long? TakenByUserId,
    string? TakenByUserName,
    DateTime TakenAtUtc,
    decimal? Latitude,
    decimal? Longitude,
    string? Note
);

/// <summary>Delivery shipment projection returned to callers.</summary>
public record DeliveryResponse(
    long Id,
    long? OrderId,
    long? ContractId,
    DeliveryDirection Direction,
    DeliveryStatus Status,
    string? CarrierName,
    string? TrackingCode,
    string DeliveryAddress,
    string RecipientName,
    string RecipientPhone,
    DateTime? ScheduledAtUtc,
    DateTime? ShippedAtUtc,
    DateTime? DeliveredAtUtc,
    string? Note,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    List<DeliveryProofResponse> Proofs
);
