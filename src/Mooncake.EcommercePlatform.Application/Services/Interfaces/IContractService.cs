namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Contracts.Responses;

public interface IContractService
{
    Task<IReadOnlyList<ContractResponse>> GetMineAsync(Guid userId, bool supplier, CancellationToken cancellationToken = default);
    Task<ContractResponse> GetAsync(Guid userId, Guid contractId, CancellationToken cancellationToken = default);
    Task<ContractResponse> RequestSignatureOtpAsync(Guid userId, Guid contractId, CancellationToken cancellationToken = default);
    Task<ContractResponse> BeginSignatureAsync(Guid userId, Guid contractId, CancellationToken cancellationToken = default);
    Task<ContractResponse> SignAsync(Guid userId, Guid contractId, string code, bool confirmTerms, string? ipAddress,
        string? userAgent, CancellationToken cancellationToken = default);
}
