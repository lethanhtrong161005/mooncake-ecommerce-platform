namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>Represents a scheduled payment obligation (full, deposit, balance, or packaging fee adjustment) on an order.</summary>
public sealed class OrderPaymentObligation : BaseEntity
{
    public Guid OrderId { get; set; }

    public int SequenceNo { get; set; }

    public string ObligationType { get; set; } = "Full";

    public string Title { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public string Status { get; set; } = "Pending";

    public bool IsRequiredBeforeProduction { get; set; } = true;

    public bool IsRequiredBeforeShipping { get; set; } = true;

    public DateTime? DueAt { get; set; }

    public DateTime? PaidAt { get; set; }

    public Guid? SourceReferenceId { get; set; }
}

