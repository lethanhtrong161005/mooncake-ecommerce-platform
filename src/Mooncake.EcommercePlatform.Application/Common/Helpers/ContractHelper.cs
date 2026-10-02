namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Application.DTOs.Contracts.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Maps Contract domain entities to response DTOs and computes late delivery penalties.</summary>
public class ContractHelper : IContractHelper
{
    public ContractResponse ToResponse(Contract contract)
    {
        var milestones = contract.ContractMilestones?
            .OrderBy(m => m.MilestoneNo)
            .Select(ToMilestoneResponse)
            .ToList() ?? [];

        PenaltyCalculationResponse? penaltyInfo = null;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (contract.Status == ContractStatus.Active && today > contract.DeliveryDeadline)
        {
            penaltyInfo = CalculatePenalty(contract);
        }

        var customerName = contract.Customer?.CompanyName
            ?? contract.Customer?.User?.FullName
            ?? $"Customer #{contract.CustomerId}";

        var supplierName = contract.Supplier?.BusinessName
            ?? contract.Supplier?.User?.FullName
            ?? $"Supplier #{contract.SupplierId}";

        return new ContractResponse(
            contract.Id,
            contract.QuotationId,
            contract.CustomerId,
            customerName,
            contract.SupplierId,
            supplierName,
            contract.Status,
            contract.TotalAmount,
            contract.DepositPercent,
            contract.DeliveryDeadline,
            contract.LatePenaltyPercentPerDay,
            contract.MaxPenaltyPercent,
            contract.Terms,
            contract.CustomerSignedAtUtc,
            contract.SupplierSignedAtUtc,
            contract.CompletedAtUtc,
            contract.CreatedAtUtc,
            contract.UpdatedAtUtc,
            milestones,
            penaltyInfo
        );
    }

    public ContractMilestoneResponse ToMilestoneResponse(ContractMilestone milestone) =>
        new(
            milestone.Id,
            milestone.ContractId,
            milestone.MilestoneNo,
            milestone.Name,
            milestone.MilestoneType,
            milestone.Amount,
            milestone.DueDate,
            milestone.Status,
            milestone.PaidAtUtc,
            milestone.CreatedAtUtc,
            milestone.UpdatedAtUtc
        );

    public PenaltyCalculationResponse CalculatePenalty(Contract contract)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var daysOverdue = Math.Max(0, today.DayNumber - contract.DeliveryDeadline.DayNumber);

        var theoreticalPenalty = daysOverdue * contract.LatePenaltyPercentPerDay;
        var effectivePenaltyPercent = Math.Min(theoreticalPenalty, contract.MaxPenaltyPercent);
        var penaltyAmount = Math.Round(contract.TotalAmount * (effectivePenaltyPercent / 100m), 2);
        var netPayableAmount = Math.Max(0, contract.TotalAmount - penaltyAmount);

        return new PenaltyCalculationResponse(
            contract.Id,
            contract.DeliveryDeadline,
            daysOverdue,
            contract.LatePenaltyPercentPerDay,
            contract.MaxPenaltyPercent,
            effectivePenaltyPercent,
            contract.TotalAmount,
            penaltyAmount,
            netPayableAmount
        );
    }
}
