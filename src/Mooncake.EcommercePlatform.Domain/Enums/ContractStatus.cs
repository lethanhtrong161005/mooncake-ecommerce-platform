namespace Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Enum representing contract_status.</summary>
public enum ContractStatus
{
    Draft,
    PendingSignature,
    Signed,
    PendingDeposit,
    Active,
    CompletedOnTime,
    CompletedLate,
    Breached,
    Disputed,
    Cancelled
}
