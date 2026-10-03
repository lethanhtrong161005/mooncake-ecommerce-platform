namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Contracts.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Contracts.Responses;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Contract management, dual digital signing, penalty calculation, and milestone operations.</summary>
public interface IContractService
{
    Task<IEnumerable<ContractResponse>> GetContractsAsync(long? customerId = null, long? supplierId = null, ContractStatus? status = null, CancellationToken cancellationToken = default);
    Task<ContractResponse?> GetContractByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<ContractResponse> SignContractAsync(long id, SignContractRequest request, CancellationToken cancellationToken = default);
    Task<ContractResponse> UpdateTermsAsync(long id, UpdateContractTermsRequest request, CancellationToken cancellationToken = default);
    Task<PenaltyCalculationResponse> GetPenaltyInfoAsync(long id, CancellationToken cancellationToken = default);
    Task<ContractResponse> CompleteContractAsync(long id, CancellationToken cancellationToken = default);
}
