namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>Records an auditable event or status transition for a business workflow.</summary>
public class WorkflowEvent : BaseEntity
{
    public string AggregateType { get; set; } = string.Empty;

    public Guid AggregateId { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string? FromStatus { get; set; }

    public string? ToStatus { get; set; }

    public Guid? ActorUserId { get; set; }

    public string? Reason { get; set; }

    public string? Metadata { get; set; }
}
