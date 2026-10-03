using Mooncake.EcommercePlatform.Domain.Common;
using Mooncake.EcommercePlatform.Domain.Enums;
using System;
using System.Collections.Generic;

namespace Mooncake.EcommercePlatform.Domain.Entities;

public class Delivery : BaseEntity
{
    public long? OrderId { get; set; }
    public long? ContractId { get; set; }
    public DeliveryDirection Direction { get; set; }
    public DeliveryStatus Status { get; set; }
    public string? CarrierName { get; set; }
    public string? TrackingCode { get; set; }
    public string DeliveryAddress { get; set; } = null!;
    public string RecipientName { get; set; } = null!;
    public string RecipientPhone { get; set; } = null!;
    public DateTime? ScheduledAtUtc { get; set; }
    public DateTime? ShippedAtUtc { get; set; }
    public DateTime? DeliveredAtUtc { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Order? Order { get; set; }
    public Contract? Contract { get; set; }
    public ICollection<DeliveryProof> DeliveryProofs { get; set; } = [];
}
