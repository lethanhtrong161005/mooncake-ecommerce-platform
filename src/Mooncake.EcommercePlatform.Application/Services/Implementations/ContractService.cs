namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Contracts.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

public sealed class ContractService(IContractRepository repository, IAuthService authService,
    ISupplierRepository suppliers) : IContractService
{
    public async Task<IReadOnlyList<ContractResponse>> GetMineAsync(Guid userId, bool supplier, CancellationToken cancellationToken = default)
    {
        var contracts = await repository.GetContractsForUserAsync(userId, supplier, cancellationToken);
        return contracts.Select(ToResponse).ToArray();
    }

    public async Task<ContractResponse> GetAsync(Guid userId, Guid contractId, CancellationToken cancellationToken = default)
    {
        var contract = await GetOwnedAsync(userId, contractId, cancellationToken);
        return ToResponse(contract);
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

    private static ContractResponse ToResponse(Contract contract) => new(contract.Id, contract.ContractNumber, contract.RfqId,
        contract.Title, contract.TotalValue, contract.DeliveryDeadline, contract.TermsAndConditions, contract.Status,
        contract.SignedByCustomerAt, contract.SignedBySupplierAt, contract.SignedDocumentSha256);
}
