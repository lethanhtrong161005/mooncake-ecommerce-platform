namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Represents the ContractMilestone domain entity.</summary>
public class ContractMilestone : BaseEntity
{
    public Guid ContractId { get; set; }

    public int SequenceNumber { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsDeposit { get; set; }

    public DateOnly? DueDate { get; set; }

    public DateOnly? CompletionDate { get; set; }

    public decimal? Amount { get; set; }

    public decimal? Percentage { get; set; }

    public MilestoneStatus Status { get; set; } = MilestoneStatus.Pending;

    /// <summary>Notes submitted by the supplier when reporting milestone progress.</summary>
    public string? ProgressNotes { get; set; }

    /// <summary>JSON array of evidence URLs uploaded by the supplier (e.g. photos of completed goods).</summary>
    public string? EvidenceUrls { get; set; }
}
