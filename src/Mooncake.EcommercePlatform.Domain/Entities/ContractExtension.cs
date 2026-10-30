namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>Represents a deadline extension request raised by the supplier on an active contract.</summary>
public class ContractExtension : BaseEntity
{
    public Guid ContractId { get; set; }

    /// <summary>The proposed new delivery deadline requested by the supplier.</summary>
    public DateOnly ProposedDueDate { get; set; }

    /// <summary>Business reason for the extension (e.g. customs delay, material shortage).</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>Workflow status: Pending | Approved | Rejected.</summary>
    public string Status { get; set; } = "Pending";

    public Guid RequestedByUserId { get; set; }

    public DateTime RequestedAt { get; set; }

    public DateTime? RespondedAt { get; set; }

    public Guid? RespondedByUserId { get; set; }

    public string? ResponseNote { get; set; }
}

