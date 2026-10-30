namespace Mooncake.EcommercePlatform.Application.DTOs.Contracts.Responses;

using Mooncake.EcommercePlatform.Domain.Enums;

public sealed record ContractResponse(
    Guid Id,
    string ContractNumber,
    Guid? RfqId,
    string? Title,
    decimal? TotalValue,
    DateOnly? DeliveryDeadline,
    string? TermsAndConditions,
    ContractStatus Status,
    DateTime? SignedByCustomerAt,
    DateTime? SignedBySupplierAt,
    string? SignedDocumentSha256,
    decimal? DepositAmount = null,
    DateTime? DepositPaidAt = null,
    DateTime? CompletedAt = null,
    IReadOnlyList<ContractMilestoneResponse>? Milestones = null,
    ContractExtensionResponse? PendingExtension = null,
    ContractDisputeResponse? ActiveDispute = null);
