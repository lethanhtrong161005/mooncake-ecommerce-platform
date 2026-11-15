namespace Mooncake.EcommercePlatform.Application.DTOs.Deliveries.Responses;

using Mooncake.EcommercePlatform.Domain.Enums;

public record DeliveryItemResponse
{
    public Guid Id { get; init; }

    public Guid DeliveryId { get; init; }

    public Guid? OrderItemId { get; init; }

    public Guid? RfqItemId { get; init; }

    public string ProductName { get; init; } = string.Empty;

    public string? Unit { get; init; }

    public int Quantity { get; init; }
}

public record ContractFulfillmentLineResponse
{
    public Guid LineId { get; init; }

    public Guid? RfqItemId { get; init; }

    public Guid? OrderItemId { get; init; }

    public string ProductName { get; init; } = string.Empty;

    public string? Unit { get; init; }

    public int TotalQuantity { get; init; }

    public int ScheduledQuantity { get; init; }

    public int DeliveredQuantity { get; init; }

    public int RemainingQuantity { get; init; }

    public int Total => TotalQuantity;

    public int Scheduled => ScheduledQuantity;

    public int Delivered => DeliveredQuantity;

    public int Remaining => RemainingQuantity;
}

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

    public int? BatchNumber { get; init; }

    public int? TotalBatches { get; init; }

    public IReadOnlyList<DeliveryItemResponse> Items { get; init; } = Array.Empty<DeliveryItemResponse>();

    public int ProofCount { get; init; }

    public string? ContractNumber { get; init; }

    public string? OrderNumber { get; init; }

    public string? CustomerName { get; init; }

    public string? SupplierName { get; init; }

    public string? DriverName { get; init; }

    public string? DriverPhone { get; init; }

    public string? VehiclePlate { get; init; }

    public string? ShareCode { get; init; }

    public DateTime? DriverClaimedAt { get; init; }

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

    public string? ShareCode { get; init; }

    public DateTime? CreatedAtUtc { get; init; }

    public DateTime? ExpiresAtUtc { get; init; }

    public DateTime? RevokedAtUtc { get; init; }

    public bool IsActive { get; init; }

    public bool IsExpired { get; init; }

    public bool IsRevoked { get; init; }
}

public record DriverInfoDto
{
    public string? DriverName { get; init; }

    public string? DriverPhone { get; init; }

    public string? VehiclePlate { get; init; }

    public DateTime? DriverClaimedAt { get; init; }
}

public record AssignDriverResponse
{
    public string Token { get; init; } = string.Empty;

    public string ShareCode { get; init; } = string.Empty;

    public DateTime ExpiresAt { get; init; }

    public DriverInfoDto Driver { get; init; } = new();
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

    public int? BatchNumber { get; init; }

    public int? TotalBatches { get; init; }

    public IReadOnlyList<DeliveryItemResponse> Items { get; init; } = Array.Empty<DeliveryItemResponse>();

    public bool RequiresSignature { get; init; }

    public int ProofCount { get; init; }

    public DateTime ExpiresAtUtc { get; init; }

    public bool CanSubmitProof { get; init; }

    public string? Token { get; init; }

    public string? ShareCode { get; init; }

    public string? DriverName { get; init; }

    public string? DriverPhone { get; init; }

    public string? VehiclePlate { get; init; }

    public DateTime? DriverClaimedAt { get; init; }

    public string? OrderType { get; init; }

    public string? OrderNumber { get; init; }

    public string? ContractNumber { get; init; }

    public string? RecipientCompanyName { get; init; }
}

