namespace Mooncake.EcommercePlatform.Application.DTOs.Payments.Responses;

/// <summary>Response summarizing the escrow ledger and status.</summary>
public sealed record EscrowSummaryResponse(
    decimal TotalEscrowBalance,
    int TotalActiveContracts,
    decimal PendingDisbursementAmount,
    decimal DisputedEscrowAmount,
    IReadOnlyList<EscrowLedgerItemDto> EscrowLedgerItems);

public sealed record EscrowLedgerItemDto(
    Guid ContractId,
    string ContractNumber,
    string CustomerName,
    string SupplierName,
    decimal ContractTotalValue,
    decimal HeldEscrowAmount,
    int PaidMilestonesCount,
    int TotalMilestonesCount,
    string EscrowStatus,
    DateTime? LastDepositDate);

