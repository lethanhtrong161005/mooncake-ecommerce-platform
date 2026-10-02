using Mooncake.EcommercePlatform.Domain.Common;
using Mooncake.EcommercePlatform.Domain.Enums;
using System;

namespace Mooncake.EcommercePlatform.Domain.Entities;

public class DeliveryProof : BaseEntity
{
    public long DeliveryId { get; set; }
    public ProofType ProofType { get; set; }
    public string PhotoUrl { get; set; } = null!;
    public long? TakenByUserId { get; set; }
    public DateTime TakenAtUtc { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Note { get; set; }

    public Delivery Delivery { get; set; } = null!;
    public User? TakenByUser { get; set; }
}
