namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Contracts.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Contracts.Responses;

public interface IContractService
{
    Task<IReadOnlyList<ContractResponse>> GetMineAsync(Guid userId, bool supplier, CancellationToken cancellationToken = default);
    Task<ContractResponse> GetAsync(Guid userId, Guid contractId, CancellationToken cancellationToken = default);
    Task<ContractResponse> RequestSignatureOtpAsync(Guid userId, Guid contractId, CancellationToken cancellationToken = default);
    Task<ContractResponse> BeginSignatureAsync(Guid userId, Guid contractId, CancellationToken cancellationToken = default);
    Task<ContractResponse> SignAsync(Guid userId, Guid contractId, string code, bool confirmTerms, string? ipAddress,
        string? userAgent, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ContractMilestoneResponse>> GetMilestonesAsync(Guid userId, Guid contractId, CancellationToken cancellationToken = default);
    Task<ContractMilestoneResponse> SubmitMilestoneProgressAsync(Guid userId, Guid contractId, Guid milestoneId, SubmitMilestoneProgressRequest request, CancellationToken cancellationToken = default);
    Task<ContractMilestoneResponse> VerifyMilestoneAsync(Guid userId, Guid contractId, Guid milestoneId, VerifyMilestoneRequest request, CancellationToken cancellationToken = default);
    Task<ContractExtensionResponse> RequestExtensionAsync(Guid userId, Guid contractId, RequestContractExtensionRequest request, CancellationToken cancellationToken = default);
    Task<ContractExtensionResponse> RespondExtensionAsync(Guid userId, Guid contractId, RespondContractExtensionRequest request, CancellationToken cancellationToken = default);
    Task<ContractDisputeResponse> RaiseDisputeAsync(Guid userId, Guid contractId, RaiseContractDisputeRequest request, CancellationToken cancellationToken = default);
    Task RequestFinalAcceptanceAsync(Guid userId, Guid contractId, CancellationToken cancellationToken = default);
    Task<ContractResponse> ConfirmFinalAcceptanceAsync(Guid userId, Guid contractId, ConfirmFinalAcceptanceRequest request, CancellationToken cancellationToken = default);
    Task<ContractDisputeResponse> ResolveDisputeAsync(Guid adminUserId, Guid contractId, AdminResolveDisputeRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ContractDisputeListItemResponse>> ListDisputesAsync(string? status = null, CancellationToken cancellationToken = default);
    Task<ContractDisputeResponse> ResolveDisputeByIdAsync(Guid adminUserId, Guid disputeId, AdminResolveDisputeRequest request, CancellationToken cancellationToken = default);
}

