namespace Mooncake.EcommercePlatform.Application.DTOs.Deliveries.Requests;

using System.ComponentModel.DataAnnotations;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Request to initiate a shipment/delivery for an Order or Contract.</summary>
public record CreateDeliveryRequest
{
    public long? OrderId { get; init; }

    public long? ContractId { get; init; }

    [Required]
    public DeliveryDirection Direction { get; init; } = DeliveryDirection.SupplierToCustomer;

    [MaxLength(100)]
    public string? CarrierName { get; init; }

    [MaxLength(100)]
    public string? TrackingCode { get; init; }

    [Required]
    [MaxLength(500)]
    public string DeliveryAddress { get; init; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string RecipientName { get; init; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string RecipientPhone { get; init; } = string.Empty;

    public DateTime? ScheduledAtUtc { get; init; }

    public string? Note { get; init; }
}

/// <summary>Request to update the delivery status along its lifecycle.</summary>
public record UpdateDeliveryStatusRequest
{
    [Required]
    public DeliveryStatus Status { get; init; }

    public string? Note { get; init; }
}

/// <summary>Request to attach photographic and GPS-tagged proof to a delivery.</summary>
public record AddDeliveryProofRequest
{
    [Required]
    public ProofType ProofType { get; init; } = ProofType.Delivery;

    [Required]
    [MaxLength(1000)]
    public string PhotoUrl { get; init; } = string.Empty;

    public long? TakenByUserId { get; init; }

    [Range(-90.0, 90.0)]
    public decimal? Latitude { get; init; }

    [Range(-180.0, 180.0)]
    public decimal? Longitude { get; init; }

    public string? Note { get; init; }
}
