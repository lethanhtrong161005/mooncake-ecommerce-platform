namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Contracts.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Contracts.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Implements contract signing, terms modification, penalty computation, and completion workflows.</summary>
public class ContractService(
    IContractRepository contractRepository,
    IContractHelper contractHelper) : IContractService
{
    public async Task<IEnumerable<ContractResponse>> GetContractsAsync(
        long? customerId = null,
        long? supplierId = null,
        ContractStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var contracts = await contractRepository.GetContractsAsync(customerId, supplierId, status, cancellationToken);
        return contracts.Select(contractHelper.ToResponse);
    }

    public async Task<ContractResponse?> GetContractByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var contract = await contractRepository.GetByIdAsync(id, cancellationToken);
        return contract == null ? null : contractHelper.ToResponse(contract);
    }

    public async Task<ContractResponse> SignContractAsync(long id, SignContractRequest request, CancellationToken cancellationToken = default)
    {
        var contract = await contractRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new HttpException(404, $"Contract with ID {id} was not found.");

        if (contract.Status != ContractStatus.Draft && contract.Status != ContractStatus.Active)
        {
            throw new HttpException(400, "Cannot sign a completed, cancelled, or breached contract.");
        }

        var now = DateTime.UtcNow;

        if (request.IsCustomerSigner)
        {
            contract.CustomerSignedAtUtc = now;
        }
        else
        {
            contract.SupplierSignedAtUtc = now;
        }

        // Both parties signed -> activate contract
        if (contract.CustomerSignedAtUtc.HasValue && contract.SupplierSignedAtUtc.HasValue)
        {
            contract.Status = ContractStatus.Active;
        }

        contract.UpdatedAtUtc = now;
        await contractRepository.UpdateAsync(contract, cancellationToken);

        var reloaded = await contractRepository.GetByIdAsync(id, cancellationToken);
        return contractHelper.ToResponse(reloaded ?? contract);
    }

    public async Task<ContractResponse> UpdateTermsAsync(long id, UpdateContractTermsRequest request, CancellationToken cancellationToken = default)
    {
        var contract = await contractRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new HttpException(404, $"Contract with ID {id} was not found.");

        if (contract.Status != ContractStatus.Draft)
        {
            throw new HttpException(400, "Terms can only be updated while the contract is in Draft status.");
        }

        if (!string.IsNullOrWhiteSpace(request.Terms))
        {
            contract.Terms = request.Terms.Trim();
        }

        if (request.LatePenaltyPercentPerDay.HasValue)
        {
            contract.LatePenaltyPercentPerDay = request.LatePenaltyPercentPerDay.Value;
        }

        if (request.MaxPenaltyPercent.HasValue)
        {
            contract.MaxPenaltyPercent = request.MaxPenaltyPercent.Value;
        }

        contract.UpdatedAtUtc = DateTime.UtcNow;
        await contractRepository.UpdateAsync(contract, cancellationToken);

        var reloaded = await contractRepository.GetByIdAsync(id, cancellationToken);
        return contractHelper.ToResponse(reloaded ?? contract);
    }

    public async Task<PenaltyCalculationResponse> GetPenaltyInfoAsync(long id, CancellationToken cancellationToken = default)
    {
        var contract = await contractRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new HttpException(404, $"Contract with ID {id} was not found.");

        return contractHelper.CalculatePenalty(contract);
    }

    public async Task<ContractResponse> CompleteContractAsync(long id, CancellationToken cancellationToken = default)
    {
        var contract = await contractRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new HttpException(404, $"Contract with ID {id} was not found.");

        if (contract.Status != ContractStatus.Active)
        {
            throw new HttpException(400, "Only active contracts can be marked completed.");
        }

        // Verify all milestones are paid
        if (contract.ContractMilestones.Any(m => m.Status != MilestoneStatus.Paid))
        {
            throw new HttpException(400, "Cannot complete contract while there are unpaid milestones.");
        }

        var now = DateTime.UtcNow;
        contract.Status = ContractStatus.Completed;
        contract.CompletedAtUtc = now;
        contract.UpdatedAtUtc = now;

        await contractRepository.UpdateAsync(contract, cancellationToken);

        var reloaded = await contractRepository.GetByIdAsync(id, cancellationToken);
        return contractHelper.ToResponse(reloaded ?? contract);
    }
}
