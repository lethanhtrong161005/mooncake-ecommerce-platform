namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Represents the DeliveryProof domain entity.</summary>
public class DeliveryProof : BaseEntity
{
    public Guid DeliveryId { get; set; }

    public string? PhotoUrl { get; set; }

    public string? TakenBy { get; set; }

    public DateTime? TakenAt { get; set; }

    public decimal? LocationLat { get; set; }

    public decimal? LocationLng { get; set; }

    public string? SignatureUrl { get; set; }

    public string? Notes { get; set; }

    public ProofType ProofType { get; set; } = ProofType.Delivery;
}
