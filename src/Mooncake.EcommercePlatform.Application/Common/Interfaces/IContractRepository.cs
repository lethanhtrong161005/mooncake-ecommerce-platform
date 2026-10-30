namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Domain.Entities;

public interface IContractRepository
{
    Task<Contract?> GetContractAsync(Guid contractId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Contract>> GetContractsForUserAsync(Guid userId, bool supplier, CancellationToken cancellationToken = default);
    Task<bool> IsPartyAsync(Contract contract, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> HasSignedAsync(Contract contract, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> BeginSignatureAsync(Guid contractId, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<Contract?> SignAsync(Guid contractId, Guid actorUserId, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ContractMilestone>> GetMilestonesAsync(Guid contractId, CancellationToken cancellationToken = default);
    Task<ContractMilestone?> GetMilestoneAsync(Guid contractId, Guid milestoneId, CancellationToken cancellationToken = default);
    Task<ContractExtension?> GetPendingExtensionAsync(Guid contractId, CancellationToken cancellationToken = default);
    Task<ContractDispute?> GetActiveDisputeAsync(Guid contractId, CancellationToken cancellationToken = default);
    Task<ContractMilestone?> SubmitMilestoneProgressAsync(Guid contractId, Guid milestoneId, string? notes, IReadOnlyList<string>? evidenceUrls, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<ContractMilestone?> VerifyMilestoneAsync(Guid contractId, Guid milestoneId, bool isApproved, string? rejectionReason, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<ContractExtension> RequestExtensionAsync(Guid contractId, DateOnly proposedDueDate, string reason, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<ContractExtension?> RespondExtensionAsync(Guid contractId, bool isApproved, string? responseNote, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<ContractDispute> RaiseDisputeAsync(Guid contractId, string reason, IReadOnlyList<string>? evidenceUrls, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<bool> RequestFinalAcceptanceAsync(Guid contractId, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<Contract?> ConfirmFinalAcceptanceAsync(Guid contractId, bool confirmedQualityAndQuantity, string? acceptanceNotes, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<ContractDispute?> ResolveDisputeAsync(Guid contractId, string resolutionType, decimal refundAmount, string adminVerdict, Guid adminUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Mooncake.EcommercePlatform.Application.DTOs.Contracts.Responses.ContractDisputeListItemResponse>> ListDisputesAsync(string? status = null, CancellationToken cancellationToken = default);
    Task<ContractDispute?> GetDisputeByIdAsync(Guid disputeId, CancellationToken cancellationToken = default);
}

