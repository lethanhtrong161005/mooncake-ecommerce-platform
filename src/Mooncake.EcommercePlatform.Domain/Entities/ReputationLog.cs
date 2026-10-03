using Mooncake.EcommercePlatform.Domain.Common;
using Mooncake.EcommercePlatform.Domain.Enums;
using System;

namespace Mooncake.EcommercePlatform.Domain.Entities;

public class ReputationLog : BaseEntity
{
    public long SupplierId { get; set; }
    public ReputationEventType EventType { get; set; }
    public int ScoreDelta { get; set; }
    public long? ReviewId { get; set; }
    public long? ContractId { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public Supplier Supplier { get; set; } = null!;
    public Review? Review { get; set; }
    public Contract? Contract { get; set; }
}
