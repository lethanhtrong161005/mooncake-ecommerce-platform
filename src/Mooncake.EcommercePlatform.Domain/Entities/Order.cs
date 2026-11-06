namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Represents the Order domain entity.</summary>
public class Order : BaseEntity
{
    public Guid CustomerId { get; set; }

    public Guid ShopId { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public string? IdempotencyKey { get; set; }

    public string? IdempotencyRequestHash { get; set; }

    public decimal Subtotal { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal ShippingFee { get; set; }

    public decimal PackagingFee { get; set; }

    public decimal TotalAmount { get; set; }

    public string? Notes { get; set; }

    public string? DeliveryAddress { get; set; }

    public string? RecipientName { get; set; }

    public string? RecipientPhone { get; set; }

    public DateOnly? DeliveryDateExpected { get; set; }

    public DateTime? CancelledAt { get; set; }

    public Guid? CancelledByUserId { get; set; }

    public string? CancellationReason { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public long Version { get; set; } = 1L;

    public DateTime? ResponseDeadlineAt { get; set; }

    public DateTime? ReservationExpiresAt { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public DateOnly? ConfirmedDeliveryDate { get; set; }

    public DateOnly? ProposedDeliveryDate { get; set; }

    public string? DeliveryProposalReason { get; set; }

    public DateTime? DeliveryApprovalDeadlineAt { get; set; }

    public string? PriceSnapshotVersion { get; set; }

    public string? ShippingZoneCode { get; set; }

    public string? ShippingZoneNameSnapshot { get; set; }

    public bool TaxIncludedSnapshot { get; set; } = true;

    public decimal TaxRateSnapshot { get; set; }

    public bool DepositRequiredSnapshot { get; set; }

    public decimal DepositPercentSnapshot { get; set; }

    public string? TermsSnapshotJson { get; set; }
}
