namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Application.DTOs.Deliveries.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Maps Delivery and DeliveryProof entities to response DTOs.</summary>
public class DeliveryHelper : IDeliveryHelper
{
    public DeliveryResponse ToResponse(Delivery delivery)
    {
        var proofs = delivery.DeliveryProofs?
            .OrderByDescending(p => p.TakenAtUtc)
            .Select(ToProofResponse)
            .ToList() ?? [];

        return new DeliveryResponse(
            delivery.Id,
            delivery.OrderId,
            delivery.ContractId,
            delivery.Direction,
            delivery.Status,
            delivery.CarrierName,
            delivery.TrackingCode,
            delivery.DeliveryAddress,
            delivery.RecipientName,
            delivery.RecipientPhone,
            delivery.ScheduledAtUtc,
            delivery.ShippedAtUtc,
            delivery.DeliveredAtUtc,
            delivery.Note,
            delivery.CreatedAtUtc,
            delivery.UpdatedAtUtc,
            proofs
        );
    }

    public DeliveryProofResponse ToProofResponse(DeliveryProof proof) =>
        new(
            proof.Id,
            proof.DeliveryId,
            proof.ProofType,
            proof.PhotoUrl,
            proof.TakenByUserId,
            proof.TakenByUser?.FullName,
            proof.TakenAtUtc,
            proof.Latitude,
            proof.Longitude,
            proof.Note
        );
}
