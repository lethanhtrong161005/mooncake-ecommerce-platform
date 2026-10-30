namespace Mooncake.EcommercePlatform.Application.DTOs.Deliveries.Requests;

using Mooncake.EcommercePlatform.Domain.Enums;

public record CreateDeliveryRequest
{
    public Guid? ContractId { get; init; }

    public Guid? OrderId { get; init; }

    public DeliveryType DeliveryType { get; init; } = DeliveryType.SelfArranged;

    public DateOnly? ScheduledDate { get; init; }

    public string FromAddress { get; init; } = string.Empty;

    public string ToAddress { get; init; } = string.Empty;

    public string RecipientName { get; init; } = string.Empty;

    public string RecipientPhone { get; init; } = string.Empty;

    public string? TrackingCode { get; init; }

    public string? Notes { get; init; }
}

public record UpdateDeliveryStatusRequest
{
    public DeliveryStatus Status { get; init; }

    public string? Notes { get; init; }

    public DateOnly? ActualDeliveryDate { get; init; }
}

public record UpdateCarrierInfoRequest
{
    public string? TrackingCode { get; init; }

    public string? RecipientName { get; init; }

    public string? RecipientPhone { get; init; }

    public string? FromAddress { get; init; }

    public string? ToAddress { get; init; }

    public string? Notes { get; init; }
}

public record CreateDeliveryProofRequest
{
    public ProofType ProofType { get; init; } = ProofType.Delivery;

    public string? PhotoUrl { get; init; }

    public string? SignatureUrl { get; init; }

    public string? PhotoBase64 { get; init; }

    public string? SignatureBase64 { get; init; }

    public string? TakenBy { get; init; }

    public decimal? LocationLat { get; init; }

    public decimal? LocationLng { get; init; }

    public string? Notes { get; init; }
}

public record CarrierWebhookPayload
{
    public string TrackingCode { get; init; } = string.Empty;

    public string CarrierStatus { get; init; } = string.Empty;

    public string? LocationName { get; init; }

    public DateTime EventTimeUtc { get; init; }

    public string? Notes { get; init; }
}

