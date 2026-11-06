namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>Immutable double-entry-style ledger tracking held, disputed, refunded, eligible, and simulated payout balances for an order.</summary>
public sealed class OrderFinancialLedgerEntry : BaseEntity
{
    public Guid OrderId { get; set; }

    public Guid ShopId { get; set; }

    public Guid CustomerId { get; set; }

    public Guid? PaymentId { get; set; }

    public Guid? RefundCaseId { get; set; }

    public Guid? PayoutRequestId { get; set; }

    public string EntryType { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string BalanceBucket { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string IdempotencyKey { get; set; } = string.Empty;
}

