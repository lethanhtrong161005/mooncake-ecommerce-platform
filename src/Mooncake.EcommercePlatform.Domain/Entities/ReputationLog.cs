namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Represents the ReputationLog domain entity.</summary>
public class ReputationLog : BaseEntity
{
    public Guid SupplierId { get; set; }

    public decimal ScoreDelta { get; set; }

    public string? Reason { get; set; }

    public Guid? ReferenceId { get; set; }

    public string? ReferenceType { get; set; }

    public ReputationEvent EventType { get; set; } = ReputationEvent.CompletedOnTime;
}
