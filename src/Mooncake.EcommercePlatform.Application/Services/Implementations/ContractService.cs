namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using System.Text.Json;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Contracts.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Contracts.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

public sealed class ContractService(
    IContractRepository repository,
    IAuthService authService,
    ISupplierRepository suppliers,
    IDateTimeProvider clock) : IContractService
{
    public async Task<IReadOnlyList<ContractResponse>> GetMineAsync(Guid userId, bool supplier, CancellationToken cancellationToken = default)
    {
        var contracts = await repository.GetContractsForUserAsync(userId, supplier, cancellationToken);
        return contracts.Select(c => ToResponse(c)).ToArray();
    }

    public async Task<ContractResponse> GetAsync(Guid userId, Guid contractId, CancellationToken cancellationToken = default)
    {
        var contract = await GetOwnedAsync(userId, contractId, cancellationToken);
        var milestones = await repository.GetMilestonesAsync(contractId, cancellationToken);
        var pendingExt = await repository.GetPendingExtensionAsync(contractId, cancellationToken);
        var activeDispute = await repository.GetActiveDisputeAsync(contractId, cancellationToken);

        return ToResponse(
            contract,
            milestones.Select(ToMilestoneResponse).ToArray(),
            pendingExt != null ? ToExtensionResponse(pendingExt) : null,
            activeDispute != null ? ToDisputeResponse(activeDispute) : null);
    }

    public async Task<ContractResponse> RequestSignatureOtpAsync(Guid userId, Guid contractId, CancellationToken cancellationToken = default)
    {
        var contract = await GetOwnedAsync(userId, contractId, cancellationToken);
        if (contract.Status != ContractStatus.PendingSignature)
            throw new HttpException(409, "The contract must be pending signature before requesting an OTP.");
        if (await repository.HasSignedAsync(contract, userId, cancellationToken))
            throw new HttpException(409, "This party has already signed the contract.");
        await authService.RequestContractSignatureOtpAsync(userId, contractId, cancellationToken);
        return ToResponse(contract);
    }

    public async Task<ContractResponse> BeginSignatureAsync(Guid userId, Guid contractId, CancellationToken cancellationToken = default)
    {
        _ = await GetOwnedAsync(userId, contractId, cancellationToken);
        if (!await repository.BeginSignatureAsync(contractId, userId, cancellationToken))
        {
            var contract = await GetOwnedAsync(userId, contractId, cancellationToken);
            if (contract.Status != ContractStatus.PendingSignature)
                throw new HttpException(409, "Only a draft contract can be sent for signature.");
        }
        return await GetAsync(userId, contractId, cancellationToken);
    }

    public async Task<ContractResponse> SignAsync(Guid userId, Guid contractId, string code, bool confirmTerms, string? ipAddress,
        string? userAgent, CancellationToken cancellationToken = default)
    {
        if (!confirmTerms) throw new HttpException(400, "Confirming the contract terms is required.");
        var contract = await GetOwnedAsync(userId, contractId, cancellationToken);
        if (contract.Status != ContractStatus.PendingSignature)
            throw new HttpException(409, "The contract is not awaiting signatures.");
        if (await repository.HasSignedAsync(contract, userId, cancellationToken))
            return ToResponse(contract);
        await authService.VerifyContractSignatureOtpAsync(userId, contractId, code, cancellationToken);
        var signed = await repository.SignAsync(contractId, userId, ipAddress, userAgent, cancellationToken)
                     ?? throw new HttpException(409, "The contract state changed while signing. Refresh and try again.");
        return ToResponse(signed);
    }

    public async Task<IReadOnlyList<ContractMilestoneResponse>> GetMilestonesAsync(Guid userId, Guid contractId, CancellationToken cancellationToken = default)
    {
        _ = await GetOwnedAsync(userId, contractId, cancellationToken);
        var milestones = await repository.GetMilestonesAsync(contractId, cancellationToken);
        return milestones.Select(ToMilestoneResponse).ToArray();
    }

    public async Task<ContractMilestoneResponse> SubmitMilestoneProgressAsync(Guid userId, Guid contractId, Guid milestoneId, SubmitMilestoneProgressRequest request, CancellationToken cancellationToken = default)
    {
        var contract = await GetOwnedAsync(userId, contractId, cancellationToken);
        if (contract.CustomerId == userId)
            throw new HttpException(403, "Only the supplier can submit milestone progress.");
        if (contract.Status != ContractStatus.Active)
            throw new HttpException(409, "Milestone progress can only be submitted for active contracts.");

        var milestone = await repository.GetMilestoneAsync(contractId, milestoneId, cancellationToken)
            ?? throw new HttpException(404, "Milestone not found.");
        if (milestone.IsDeposit)
            throw new HttpException(400, "Deposit milestones cannot have progress submitted.");

        var updated = await repository.SubmitMilestoneProgressAsync(contractId, milestoneId, request.ProgressNotes, request.EvidenceUrls, userId, cancellationToken)
            ?? throw new HttpException(409, "Failed to submit milestone progress.");

        return ToMilestoneResponse(updated);
    }

    public async Task<ContractMilestoneResponse> VerifyMilestoneAsync(Guid userId, Guid contractId, Guid milestoneId, VerifyMilestoneRequest request, CancellationToken cancellationToken = default)
    {
        var contract = await GetOwnedAsync(userId, contractId, cancellationToken);
        if (contract.CustomerId != userId)
            throw new HttpException(403, "Only the customer can verify milestone progress.");
        if (contract.Status != ContractStatus.Active)
            throw new HttpException(409, "Milestones can only be verified for active contracts.");

        var milestone = await repository.GetMilestoneAsync(contractId, milestoneId, cancellationToken)
            ?? throw new HttpException(404, "Milestone not found.");
        if (milestone.Status != MilestoneStatus.PendingReview && milestone.Status != MilestoneStatus.InProgress)
            throw new HttpException(409, "Milestone is not awaiting review.");

        var updated = await repository.VerifyMilestoneAsync(contractId, milestoneId, request.IsApproved, request.RejectionReason, userId, cancellationToken)
            ?? throw new HttpException(409, "Failed to verify milestone.");

        return ToMilestoneResponse(updated);
    }

    public async Task<ContractExtensionResponse> RequestExtensionAsync(Guid userId, Guid contractId, RequestContractExtensionRequest request, CancellationToken cancellationToken = default)
    {
        var contract = await GetOwnedAsync(userId, contractId, cancellationToken);
        if (contract.CustomerId == userId)
            throw new HttpException(403, "Only the supplier can request a contract extension.");
        if (contract.Status != ContractStatus.Active)
            throw new HttpException(409, "Extensions can only be requested for active contracts.");

        var today = DateOnly.FromDateTime(clock.UtcNow);
        if (request.ProposedDueDate <= today)
            throw new HttpException(400, "Proposed due date must be in the future.");

        var pending = await repository.GetPendingExtensionAsync(contractId, cancellationToken);
        if (pending != null)
            throw new HttpException(409, "An extension request is already pending for this contract.");

        var extension = await repository.RequestExtensionAsync(contractId, request.ProposedDueDate, request.Reason, userId, cancellationToken);
        return ToExtensionResponse(extension);
    }

    public async Task<ContractExtensionResponse> RespondExtensionAsync(Guid userId, Guid contractId, RespondContractExtensionRequest request, CancellationToken cancellationToken = default)
    {
        var contract = await GetOwnedAsync(userId, contractId, cancellationToken);
        if (contract.CustomerId != userId)
            throw new HttpException(403, "Only the customer can respond to extension requests.");

        var pending = await repository.GetPendingExtensionAsync(contractId, cancellationToken)
            ?? throw new HttpException(404, "No pending extension request found for this contract.");

        var updated = await repository.RespondExtensionAsync(contractId, request.IsApproved, request.ResponseNote, userId, cancellationToken)
            ?? throw new HttpException(409, "Failed to respond to extension request.");

        return ToExtensionResponse(updated);
    }

    public async Task<ContractDisputeResponse> RaiseDisputeAsync(Guid userId, Guid contractId, RaiseContractDisputeRequest request, CancellationToken cancellationToken = default)
    {
        var contract = await GetOwnedAsync(userId, contractId, cancellationToken);
        if (contract.Status is ContractStatus.Draft or ContractStatus.PendingSignature or ContractStatus.CompletedOnTime or ContractStatus.CompletedLate or ContractStatus.Cancelled or ContractStatus.Breached)
            throw new HttpException(409, "A dispute cannot be raised for contracts in the current status.");

        var existing = await repository.GetActiveDisputeAsync(contractId, cancellationToken);
        if (existing != null || contract.Status == ContractStatus.Disputed)
            throw new HttpException(409, "A dispute is already active for this contract.");

        var dispute = await repository.RaiseDisputeAsync(contractId, request.DisputeReason, request.EvidenceUrls, userId, cancellationToken);
        return ToDisputeResponse(dispute);
    }

    public async Task RequestFinalAcceptanceAsync(Guid userId, Guid contractId, CancellationToken cancellationToken = default)
    {
        var contract = await GetOwnedAsync(userId, contractId, cancellationToken);
        if (contract.CustomerId == userId)
            throw new HttpException(403, "Only the supplier can request final acceptance.");
        if (contract.Status != ContractStatus.Active)
            throw new HttpException(409, "Final acceptance can only be requested for active contracts.");

        var milestones = await repository.GetMilestonesAsync(contractId, cancellationToken);
        var depositPaid = milestones.Where(m => m.IsDeposit).All(m => m.Status == MilestoneStatus.Paid);
        if (!depositPaid)
            throw new HttpException(409, "The deposit milestone must be paid before requesting final acceptance.");

        var productionMilestones = milestones.Where(m => !m.IsDeposit && m.SequenceNumber < 3).ToList();
        if (productionMilestones.Any(m => m.Status != MilestoneStatus.CompletedOnTime && m.Status != MilestoneStatus.CompletedLate))
            throw new HttpException(409, "All previous production milestones must be completed before requesting final acceptance.");

        var success = await repository.RequestFinalAcceptanceAsync(contractId, userId, cancellationToken);
        if (!success)
            throw new HttpException(409, "Failed to request final acceptance.");
    }

    public async Task<ContractResponse> ConfirmFinalAcceptanceAsync(Guid userId, Guid contractId, ConfirmFinalAcceptanceRequest request, CancellationToken cancellationToken = default)
    {
        if (!request.ConfirmedQualityAndQuantity)
            throw new HttpException(400, "Final inspection confirmation is required.");

        var contract = await GetOwnedAsync(userId, contractId, cancellationToken);
        if (contract.CustomerId != userId)
            throw new HttpException(403, "Only the customer can confirm final acceptance.");
        if (contract.Status != ContractStatus.Active)
            throw new HttpException(409, "Final acceptance can only be confirmed for active contracts.");

        var completed = await repository.ConfirmFinalAcceptanceAsync(contractId, request.ConfirmedQualityAndQuantity, request.AcceptanceNotes, userId, cancellationToken)
            ?? throw new HttpException(409, "Failed to confirm final acceptance.");

        return await GetAsync(userId, contractId, cancellationToken);
    }

    public async Task<ContractDisputeResponse> ResolveDisputeAsync(Guid adminUserId, Guid contractId, AdminResolveDisputeRequest request, CancellationToken cancellationToken = default)
    {
        var contract = await repository.GetContractAsync(contractId, cancellationToken)
            ?? throw new HttpException(404, "Contract was not found.");
        if (contract.Status != ContractStatus.Disputed)
            throw new HttpException(409, "The contract is not in disputed status.");

        var resolved = await repository.ResolveDisputeAsync(contractId, request.ResolutionType, request.RefundAmount, request.AdminVerdict, adminUserId, cancellationToken)
            ?? throw new HttpException(409, "Failed to resolve dispute.");

        return ToDisputeResponse(resolved);
    }

    public Task<IReadOnlyList<ContractDisputeListItemResponse>> ListDisputesAsync(string? status = null, CancellationToken cancellationToken = default) =>
        repository.ListDisputesAsync(status, cancellationToken);

    public async Task<ContractDisputeResponse> ResolveDisputeByIdAsync(Guid adminUserId, Guid disputeId, AdminResolveDisputeRequest request, CancellationToken cancellationToken = default)
    {
        var dispute = await repository.GetDisputeByIdAsync(disputeId, cancellationToken)
            ?? throw new HttpException(404, "Dispute was not found.");

        return await ResolveDisputeAsync(adminUserId, dispute.ContractId, request, cancellationToken);
    }

    private async Task<Contract> GetOwnedAsync(Guid userId, Guid contractId, CancellationToken cancellationToken)
    {
        var contract = await repository.GetContractAsync(contractId, cancellationToken);
        if (contract is null || contract.IsDeleted || !await repository.IsPartyAsync(contract, userId, cancellationToken))
            throw new HttpException(404, "Contract was not found.");
        if (contract.CustomerId != userId)
        {
            var profile = await suppliers.GetProfileByIdAsync(contract.SupplierId, cancellationToken);
            var owner = await suppliers.GetUserAsync(userId, cancellationToken);
            Mooncake.EcommercePlatform.Application.Common.Helpers.SupplierVerificationHelper.Require(
                Mooncake.EcommercePlatform.Application.Common.Helpers.SupplierVerificationHelper.CanSell(owner, profile) ||
                (contract.SignedBySupplierAt.HasValue &&
                 Mooncake.EcommercePlatform.Application.Common.Helpers.SupplierVerificationHelper.CanFulfill(owner, profile, contract.CreatedAt)),
                403, Mooncake.EcommercePlatform.Application.Common.Constant.SupplierVerificationConstants.Forbidden);
        }
        return contract;
    }

    private static ContractResponse ToResponse(
        Contract contract,
        IReadOnlyList<ContractMilestoneResponse>? milestones = null,
        ContractExtensionResponse? pendingExtension = null,
        ContractDisputeResponse? activeDispute = null) =>
        new(
            contract.Id,
            contract.ContractNumber,
            contract.RfqId,
            contract.Title,
            contract.TotalValue,
            contract.DeliveryDeadline,
            contract.TermsAndConditions,
            contract.Status,
            contract.SignedByCustomerAt,
            contract.SignedBySupplierAt,
            contract.SignedDocumentSha256,
            contract.DepositAmount,
            contract.DepositPaidAt,
            contract.CompletedAt,
            milestones,
            pendingExtension,
            activeDispute);

    private static ContractMilestoneResponse ToMilestoneResponse(ContractMilestone m)
    {
        IReadOnlyList<string>? evidenceUrls = null;
        if (!string.IsNullOrWhiteSpace(m.EvidenceUrls))
        {
            try { evidenceUrls = JsonSerializer.Deserialize<List<string>>(m.EvidenceUrls); } catch { /* ignore */ }
        }

        return new(
            m.Id,
            m.SequenceNumber,
            m.Name,
            m.Description,
            m.IsDeposit,
            m.DueDate,
            m.CompletionDate,
            m.Amount,
            m.Percentage,
            m.Status,
            m.ProgressNotes,
            evidenceUrls);
    }

    private static ContractExtensionResponse ToExtensionResponse(ContractExtension e) =>
        new(e.Id, e.ContractId, e.ProposedDueDate, e.Reason, e.Status, e.RequestedAt, e.ResponseNote);

    private static ContractDisputeResponse ToDisputeResponse(ContractDispute d)
    {
        IReadOnlyList<string>? evidenceUrls = null;
        if (!string.IsNullOrWhiteSpace(d.EvidenceUrls))
        {
            try { evidenceUrls = JsonSerializer.Deserialize<List<string>>(d.EvidenceUrls); } catch { /* ignore */ }
        }

        return new(
            d.Id,
            d.ContractId,
            d.RaisedByUserId,
            d.DisputeReason,
            evidenceUrls ?? [],
            d.Status,
            d.AdminVerdict,
            d.ResolutionType,
            d.RefundAmount,
            d.RaisedAt,
            d.ResolvedAt);
    }
}
