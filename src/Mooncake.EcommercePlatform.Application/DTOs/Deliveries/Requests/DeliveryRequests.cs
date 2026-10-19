namespace Mooncake.EcommercePlatform.Application.DTOs.Deliveries.Requests;

using System.ComponentModel.DataAnnotations;
using Mooncake.EcommercePlatform.Domain.Enums;

public sealed record CreateDeliveryRequest
{
    public Guid? ContractId { get; init; }
    public Guid? OrderId { get; init; }
    public DeliveryType DeliveryType { get; init; } = DeliveryType.SelfArranged;
    public DateOnly? ScheduledDate { get; init; }
    [MaxLength(1000)] public string? FromAddress { get; init; }
    [MaxLength(1000)] public string? ToAddress { get; init; }
    [MaxLength(255)] public string? RecipientName { get; init; }
    [Phone, MaxLength(30)] public string? RecipientPhone { get; init; }
    [MaxLength(1000)] public string? Notes { get; init; }
}

public sealed record AddDeliveryProofRequest
{
    [Required] public ProofType ProofType { get; init; }
    [Required, Url, MaxLength(1000)] public string PhotoUrl { get; init; } = string.Empty;
    [Url, MaxLength(1000)] public string? SignatureUrl { get; init; }
    [Range(-90, 90)] public decimal? LocationLat { get; init; }
    [Range(-180, 180)] public decimal? LocationLng { get; init; }
    [MaxLength(1000)] public string? Notes { get; init; }
}
