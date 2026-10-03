namespace Mooncake.EcommercePlatform.Application.DTOs.Contracts.Responses;

using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Contract milestone projection.</summary>
public record ContractMilestoneResponse(
    long Id,
    long ContractId,
    short MilestoneNo,
    string Name,
    MilestoneType MilestoneType,
    decimal Amount,
    DateOnly? DueDate,
    MilestoneStatus Status,
    DateTime? PaidAtUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc
);

/// <summary>Penalty calculation projection for overdue contracts.</summary>
public record PenaltyCalculationResponse(
    long ContractId,
    DateOnly DeliveryDeadline,
    int DaysOverdue,
    decimal LatePenaltyPercentPerDay,
    decimal MaxPenaltyPercent,
    decimal EffectivePenaltyPercent,
    decimal TotalContractAmount,
    decimal PenaltyAmount,
    decimal NetPayableAmount
);

/// <summary>Contract projection returned to callers.</summary>
public record ContractResponse(
    long Id,
    long QuotationId,
    long CustomerId,
    string CustomerName,
    long SupplierId,
    string SupplierBusinessName,
    ContractStatus Status,
    decimal TotalAmount,
    decimal DepositPercent,
    DateOnly DeliveryDeadline,
    decimal LatePenaltyPercentPerDay,
    decimal MaxPenaltyPercent,
    string? Terms,
    DateTime? CustomerSignedAtUtc,
    DateTime? SupplierSignedAtUtc,
    DateTime? CompletedAtUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    List<ContractMilestoneResponse> Milestones,
    PenaltyCalculationResponse? PenaltyInfo = null
);
