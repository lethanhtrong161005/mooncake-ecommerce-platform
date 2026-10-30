namespace Mooncake.EcommercePlatform.Application.DTOs.Contracts.Responses;

using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Response DTO for a single contract milestone.</summary>
public sealed record ContractMilestoneResponse(
    Guid Id,
    int SequenceNumber,
    string Name,
    string? Description,
    bool IsDeposit,
    DateOnly? DueDate,
    DateOnly? CompletionDate,
    decimal? Amount,
    decimal? Percentage,
    MilestoneStatus Status,
    string? ProgressNotes,
    IReadOnlyList<string>? EvidenceUrls);

/// <summary>Response DTO for a contract extension request.</summary>
public sealed record ContractExtensionResponse(
    Guid Id,
    Guid ContractId,
    DateOnly ProposedDueDate,
    string Reason,
    string Status,
    DateTime RequestedAt,
    string? ResponseNote);

/// <summary>Response DTO for a contract dispute.</summary>
public sealed record ContractDisputeResponse(
    Guid Id,
    Guid ContractId,
    Guid RaisedByUserId,
    string DisputeReason,
    IReadOnlyList<string>? EvidenceUrls,
    string Status,
    string? AdminVerdict,
    string? ResolutionType,
    decimal RefundAmount,
    DateTime RaisedAt,
    DateTime? ResolvedAt);

/// <summary>Response DTO for dispute list on Admin Dashboard.</summary>
public sealed record ContractDisputeListItemResponse(
    Guid Id,
    Guid ContractId,
    string ContractNumber,
    string CustomerName,
    string SupplierName,
    string DisputeReason,
    IReadOnlyList<string>? EvidenceUrls,
    string Status,
    string? ResolutionType,
    string? AdminVerdict,
    decimal RefundAmount,
    decimal HeldEscrowAmount,
    DateTime RaisedAt,
    DateTime? ResolvedAt);


