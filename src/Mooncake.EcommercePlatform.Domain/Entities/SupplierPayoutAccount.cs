namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>Stores a supplier's verified settlement bank account for order payout simulation.</summary>
public sealed class SupplierPayoutAccount : BaseEntity
{
    public Guid SupplierUserId { get; set; }

    public string BankCode { get; set; } = string.Empty;

    public string BankName { get; set; } = string.Empty;

    public string AccountNumber { get; set; } = string.Empty;

    public string AccountHolderName { get; set; } = string.Empty;

    public string? BranchName { get; set; }

    public bool IsVerifiedSimulated { get; set; } = true;

    public long Version { get; set; } = 1L;
}

