namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Application.DTOs.Deliveries.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Maps delivery and proof-of-delivery entities to immutable response DTOs.</summary>
public static class DeliveryResponseHelper
{
    /// <summary>Maps a delivery entity and its proof count to a summary response.</summary>
    public static DeliveryResponse ToResponse(Delivery delivery, int proofCount) => new()
    {
        Id = delivery.Id,
        ContractId = delivery.ContractId,
        OrderId = delivery.OrderId,
        DeliveryNumber = delivery.DeliveryNumber,
        DeliveryType = delivery.DeliveryType,
        Status = delivery.Status,
        ScheduledDate = delivery.ScheduledDate,
        ActualDeliveryDate = delivery.ActualDeliveryDate,
        FromAddress = delivery.FromAddress,
        ToAddress = delivery.ToAddress,
        RecipientName = delivery.RecipientName,
        RecipientPhone = delivery.RecipientPhone,
        TrackingCode = delivery.TrackingCode,
        Notes = delivery.Notes,
        ProofCount = proofCount,
        CreatedAt = delivery.CreatedAt,
        UpdatedAt = delivery.UpdatedAt
    };

    /// <summary>Maps a delivery entity and its proof list to a detail response.</summary>
    public static DeliveryDetailResponse ToDetailResponse(Delivery delivery, IReadOnlyList<DeliveryProofResponse> proofs) => new()
    {
        Id = delivery.Id,
        ContractId = delivery.ContractId,
        OrderId = delivery.OrderId,
        DeliveryNumber = delivery.DeliveryNumber,
        DeliveryType = delivery.DeliveryType,
        Status = delivery.Status,
        ScheduledDate = delivery.ScheduledDate,
        ActualDeliveryDate = delivery.ActualDeliveryDate,
        FromAddress = delivery.FromAddress,
        ToAddress = delivery.ToAddress,
        RecipientName = delivery.RecipientName,
        RecipientPhone = delivery.RecipientPhone,
        TrackingCode = delivery.TrackingCode,
        Notes = delivery.Notes,
        ProofCount = proofs.Count,
        Proofs = proofs,
        CreatedAt = delivery.CreatedAt,
        UpdatedAt = delivery.UpdatedAt
    };

    /// <summary>Maps a delivery proof entity to a proof response.</summary>
    public static DeliveryProofResponse ToProofResponse(DeliveryProof proof) => new()
    {
        Id = proof.Id,
        DeliveryId = proof.DeliveryId,
        ProofType = proof.ProofType,
        PhotoUrl = proof.PhotoUrl,
        SignatureUrl = proof.SignatureUrl,
        TakenBy = proof.TakenBy,
        TakenAt = proof.TakenAt,
        LocationLat = proof.LocationLat,
        LocationLng = proof.LocationLng,
        Notes = proof.Notes,
        TamperProofHash = proof.TamperProofHash,
        IsVerified = !string.IsNullOrWhiteSpace(proof.TamperProofHash),
        CreatedAt = proof.CreatedAt
    };
}

