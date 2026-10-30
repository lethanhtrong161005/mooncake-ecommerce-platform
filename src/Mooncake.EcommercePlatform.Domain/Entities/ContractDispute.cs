namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>Represents a formal dispute raised by either party on a contract.</summary>
public class ContractDispute : BaseEntity
{
    public Guid ContractId { get; set; }

    public Guid RaisedByUserId { get; set; }

    public string DisputeReason { get; set; } = string.Empty;

    /// <summary>JSON array of evidence URLs supporting the dispute claim.</summary>
    public string? EvidenceUrls { get; set; }

    /// <summary>Workflow status: Pending | Resolved.</summary>
    public string Status { get; set; } = "Pending";

    /// <summary>Admin resolution notes / verdict text.</summary>
    public string? AdminVerdict { get; set; }

    /// <summary>Resolution type determined by admin: SupplierAtFault | CustomerAtFault | MutualAgreement.</summary>
    public string? ResolutionType { get; set; }

    /// <summary>Amount to refund to the customer (0 if none).</summary>
    public decimal RefundAmount { get; set; }

    public DateTime RaisedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public Guid? ResolvedByUserId { get; set; }
}

