namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Application.DTOs.Contracts.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Contract mapping helper interface.</summary>
public interface IContractHelper
{
    ContractResponse ToResponse(Contract contract);
    ContractMilestoneResponse ToMilestoneResponse(ContractMilestone milestone);
    PenaltyCalculationResponse CalculatePenalty(Contract contract);
}
