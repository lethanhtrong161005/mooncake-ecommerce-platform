namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>Splits an order payment obligation into VNPay-compliant payment parts capped at the platform per-transaction limit.</summary>
public sealed class OrderPaymentPart : BaseEntity
{
    public Guid OrderId { get; set; }

    public Guid ObligationId { get; set; }

    public int PartIndex { get; set; }

    public int TotalParts { get; set; }

    public decimal Amount { get; set; }

    public string Status { get; set; } = "Pending";

    public DateTime? PaidAt { get; set; }
}

