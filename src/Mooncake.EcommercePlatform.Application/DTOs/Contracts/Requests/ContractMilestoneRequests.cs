namespace Mooncake.EcommercePlatform.Application.DTOs.Contracts.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>Supplier submits evidence proving milestone work is done.</summary>
public sealed record SubmitMilestoneProgressRequest
{
    [MaxLength(2000)] public string? ProgressNotes { get; init; }

    /// <summary>List of publicly accessible evidence image/document URLs.</summary>
    public IReadOnlyList<string>? EvidenceUrls { get; init; }
}

/// <summary>Customer approves or rejects the supplier's milestone progress.</summary>
public sealed record VerifyMilestoneRequest
{
    public bool IsApproved { get; init; }

    [MaxLength(1000)] public string? RejectionReason { get; init; }
}

/// <summary>Supplier requests a deadline extension for a contract milestone.</summary>
public sealed record RequestContractExtensionRequest
{
    [Required] public DateOnly ProposedDueDate { get; init; }

    [Required, MaxLength(1000)] public string Reason { get; init; } = string.Empty;
}

/// <summary>Customer responds (approve or reject) to a pending extension request.</summary>
public sealed record RespondContractExtensionRequest
{
    public bool IsApproved { get; init; }

    [MaxLength(500)] public string? ResponseNote { get; init; }
}

/// <summary>Either party raises a formal dispute on a contract.</summary>
public sealed record RaiseContractDisputeRequest
{
    [Required, MaxLength(2000)] public string DisputeReason { get; init; } = string.Empty;

    public IReadOnlyList<string>? EvidenceUrls { get; init; }
}

/// <summary>Customer confirms final acceptance of the delivered goods/services.</summary>
public sealed record ConfirmFinalAcceptanceRequest
{
    public bool ConfirmedQualityAndQuantity { get; init; }

    [MaxLength(1000)] public string? AcceptanceNotes { get; init; }
}

/// <summary>Admin resolves an active contract dispute (Admin-only).</summary>
public sealed record AdminResolveDisputeRequest
{
    /// <summary>SupplierAtFault | CustomerAtFault | MutualAgreement</summary>
    [Required, MaxLength(50)] public string ResolutionType { get; init; } = string.Empty;

    public decimal RefundAmount { get; init; }

    [Required, MaxLength(2000)] public string AdminVerdict { get; init; } = string.Empty;
}

