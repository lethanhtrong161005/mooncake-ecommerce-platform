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
}
