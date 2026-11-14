namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Deliveries.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Maps delivery and proof-of-delivery entities to immutable response DTOs.</summary>
public static class DeliveryResponseHelper
{
    /// <summary>Maps a delivery entity and its proof count to a summary response.</summary>
    public static DeliveryResponse ToResponse(Delivery delivery, int proofCount, DeliveryPartyContext? context = null) => new()
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
        ContractNumber = context?.ContractNumber,
        OrderNumber = context?.OrderNumber,
        CustomerName = context?.CustomerName,
        SupplierName = context?.SupplierName,
        CreatedAt = delivery.CreatedAt,
        UpdatedAt = delivery.UpdatedAt
    };

    /// <summary>Maps a delivery entity and its proof list to a detail response.</summary>
    public static DeliveryDetailResponse ToDetailResponse(
        Delivery delivery, IReadOnlyList<DeliveryProofResponse> proofs, DeliveryPartyContext? context = null) => new()
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
        ContractNumber = context?.ContractNumber,
        OrderNumber = context?.OrderNumber,
        CustomerName = context?.CustomerName,
        SupplierName = context?.SupplierName,
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

    /// <summary>Maps delivery share token state to a supplier share-link status response.</summary>
    public static DeliveryShareLinkResponse ToShareLinkResponse(Delivery delivery, DateTime utcNow)
    {
        var hasToken = !string.IsNullOrWhiteSpace(delivery.ShareToken);
        var isRevoked = delivery.ShareTokenRevokedAtUtc.HasValue;
        var isExpired = hasToken && !isRevoked && delivery.ShareTokenExpiresAtUtc.HasValue && delivery.ShareTokenExpiresAtUtc.Value <= utcNow;
        var isActive = hasToken && !isRevoked && delivery.ShareTokenExpiresAtUtc.HasValue && delivery.ShareTokenExpiresAtUtc.Value > utcNow;

        return new()
        {
            DeliveryId = delivery.Id,
            DeliveryNumber = delivery.DeliveryNumber,
            Token = isActive ? delivery.ShareToken : null,
            CreatedAtUtc = delivery.ShareTokenCreatedAtUtc,
            ExpiresAtUtc = delivery.ShareTokenExpiresAtUtc,
            RevokedAtUtc = delivery.ShareTokenRevokedAtUtc,
            IsActive = isActive,
            IsExpired = isExpired,
            IsRevoked = isRevoked
        };
    }

    /// <summary>Maps a delivery entity to an anonymous field driver PoD payload.</summary>
    public static PublicDriverDeliveryResponse ToPublicDriverResponse(
        Delivery delivery, int proofCount, string? referenceCode = null) => new()
    {
        DeliveryId = delivery.Id,
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
        ReferenceCode = referenceCode,
        RequiresSignature = delivery.ContractId.HasValue,
        ProofCount = proofCount,
        ExpiresAtUtc = delivery.ShareTokenExpiresAtUtc ?? delivery.CreatedAt,
        CanSubmitProof = delivery.Status != Domain.Enums.DeliveryStatus.Delivered
                         && delivery.Status != Domain.Enums.DeliveryStatus.Returned
    };
}

