namespace Mooncake.EcommercePlatform.Application.DTOs.Deliveries.Responses;

using Mooncake.EcommercePlatform.Domain.Enums;

public record DeliveryProofResponse
{
    public Guid Id { get; init; }

    public Guid DeliveryId { get; init; }

    public ProofType ProofType { get; init; }

    public string? PhotoUrl { get; init; }

    public string? SignatureUrl { get; init; }

    public string? TakenBy { get; init; }

    public DateTime? TakenAt { get; init; }

    public decimal? LocationLat { get; init; }

    public decimal? LocationLng { get; init; }

    public string? Notes { get; init; }

    public string? TamperProofHash { get; init; }

    public bool IsVerified { get; init; }

    public DateTime CreatedAt { get; init; }
}

public record DeliveryResponse
{
    public Guid Id { get; init; }

    public Guid? ContractId { get; init; }

    public Guid? OrderId { get; init; }

    public string DeliveryNumber { get; init; } = string.Empty;

    public DeliveryType DeliveryType { get; init; }

    public DeliveryStatus Status { get; init; }

    public DateOnly? ScheduledDate { get; init; }

    public DateOnly? ActualDeliveryDate { get; init; }

    public string? FromAddress { get; init; }

    public string? ToAddress { get; init; }

    public string? RecipientName { get; init; }

    public string? RecipientPhone { get; init; }

    public string? TrackingCode { get; init; }

    public string? Notes { get; init; }

    public int ProofCount { get; init; }

    public string? ContractNumber { get; init; }

    public string? OrderNumber { get; init; }

    public string? CustomerName { get; init; }

    public string? SupplierName { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }
}

public record DeliveryDetailResponse : DeliveryResponse
{
    public IReadOnlyList<DeliveryProofResponse> Proofs { get; init; } = Array.Empty<DeliveryProofResponse>();
}

public record DeliveryShareLinkResponse
{
    public Guid DeliveryId { get; init; }

    public string DeliveryNumber { get; init; } = string.Empty;

    public string? Token { get; init; }

    public DateTime? CreatedAtUtc { get; init; }

    public DateTime? ExpiresAtUtc { get; init; }

    public DateTime? RevokedAtUtc { get; init; }

    public bool IsActive { get; init; }

    public bool IsExpired { get; init; }

    public bool IsRevoked { get; init; }
}

public record PublicDriverDeliveryResponse
{
    public Guid DeliveryId { get; init; }

    public string DeliveryNumber { get; init; } = string.Empty;

    public DeliveryType DeliveryType { get; init; }

    public DeliveryStatus Status { get; init; }

    public DateOnly? ScheduledDate { get; init; }

    public DateOnly? ActualDeliveryDate { get; init; }

    public string? FromAddress { get; init; }

    public string? ToAddress { get; init; }

    public string? RecipientName { get; init; }

    public string? RecipientPhone { get; init; }

    public string? TrackingCode { get; init; }

    public string? Notes { get; init; }

    public string? ReferenceCode { get; init; }

    public bool RequiresSignature { get; init; }

    public int ProofCount { get; init; }

    public DateTime ExpiresAtUtc { get; init; }

    public bool CanSubmitProof { get; init; }
}

