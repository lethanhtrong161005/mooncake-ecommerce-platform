namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Data access contract for contracts and milestones.</summary>
public interface IContractRepository
{
    Task<IEnumerable<Contract>> GetContractsAsync(long? customerId = null, long? supplierId = null, ContractStatus? status = null, CancellationToken cancellationToken = default);
    Task<Contract?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Contract?> GetByQuotationIdAsync(long quotationId, CancellationToken cancellationToken = default);
    Task<Contract> CreateAsync(Contract contract, CancellationToken cancellationToken = default);
    Task<Contract> UpdateAsync(Contract contract, CancellationToken cancellationToken = default);

    Task<ContractMilestone?> GetMilestoneByIdAsync(long milestoneId, CancellationToken cancellationToken = default);
    Task<ContractMilestone> UpdateMilestoneAsync(ContractMilestone milestone, CancellationToken cancellationToken = default);
}
