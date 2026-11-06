namespace Mooncake.EcommercePlatform.Application.DTOs.Orders.Requests;

using System.ComponentModel.DataAnnotations;
using Mooncake.EcommercePlatform.Application.Common.DTOs;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Validated offset page and SQL filters for customer and supplier orders.</summary>
public sealed record OrderPageQueryRequest : PagedQuery
{
    public OrderStatus[]? Status { get; init; }
    public Guid? ShopId { get; init; }
    public bool? HasPackaging { get; init; }
    public PaymentStatus? PaymentStatus { get; init; }
    public bool? NeedsAction { get; init; }
}

/// <summary>Cursor-based pagination query for customer or supplier order lists.</summary>
public sealed record OrderCursorQueryRequest
{
    [MaxLength(128)]
    public string? Cursor { get; init; }

    [Range(1, 100)]
    public int Limit { get; init; } = 20;

    public OrderStatus? Status { get; init; }
}

/// <summary>Supplier proposal for an alternate delivery date when the requested date cannot be met.</summary>
public sealed record ProposeOrderDeliveryDateRequest
{
    [Required]
    public DateOnly ProposedDeliveryDate { get; init; }

    [Required, MinLength(5), MaxLength(1000)]
    public string Reason { get; init; } = string.Empty;

    public long? ExpectedVersion { get; init; }
}

/// <summary>Customer decision on a supplier's proposed delivery date.</summary>
public sealed record RespondOrderDeliveryProposalRequest
{
    public bool Approve { get; init; }

    [MaxLength(1000)]
    public string? Reason { get; init; }

    public long? ExpectedVersion { get; init; }
}

/// <summary>Mutual cancellation request submitted when unilateral cancellation is no longer permitted.</summary>
public sealed record CreateOrderCancellationRequest
{
    [Required, MinLength(5), MaxLength(1000)]
    public string Reason { get; init; } = string.Empty;

    public long? ExpectedVersion { get; init; }
}

/// <summary>Counterparty decision on a pending mutual cancellation request.</summary>
public sealed record RespondOrderCancellationRequest
{
    public bool Approve { get; init; }

    [MaxLength(1000)]
    public string? Notes { get; init; }
}

/// <summary>Customer receipt confirmation referencing the PoD shipment.</summary>
public sealed record ConfirmOrderDeliveryReceiptRequest
{
    public Guid? DeliveryId { get; init; }

    [MaxLength(1000)]
    public string? Notes { get; init; }
}

/// <summary>Customer report of missing, damaged, or wrong items upon delivery.</summary>
public sealed record ReportOrderDeliveryDisputeRequest
{
    public Guid? DeliveryId { get; init; }

    [Required, MaxLength(50)]
    public string IssueType { get; init; } = "DamagedGoods";

    [Required, MinLength(10), MaxLength(2000)]
    public string Description { get; init; } = string.Empty;

    [MaxLength(10)]
    public List<string>? EvidenceUrls { get; init; }
}

/// <summary>Administrator resolution for a reported delivery dispute.</summary>
public sealed record ResolveOrderDeliveryDisputeRequest
{
    [Required, MaxLength(40)]
    public string ResolutionAction { get; init; } = "ReleasePayout";

    [Range(0, 100_000_000_000)]
    public decimal? RefundAmount { get; init; }

    [Required, MinLength(5), MaxLength(1000)]
    public string Notes { get; init; } = string.Empty;
}

/// <summary>Generates a VNPay redirect URL for a specific order payment part.</summary>
public sealed record CreateOrderPartVnPayUrlRequest
{
    [MaxLength(1000)]
    public string? ReturnUrl { get; init; }
}

/// <summary>Customer refund claim for a cancelled or disputed order.</summary>
public sealed record CreateOrderRefundRequest
{
    [Range(1, 100_000_000_000)]
    public decimal? RequestedAmount { get; init; }

    [Required, MinLength(5), MaxLength(1000)]
    public string Reason { get; init; } = string.Empty;
}

/// <summary>Administrator processing decision for a sandbox order refund case.</summary>
public sealed record ProcessOrderRefundCaseRequest
{
    public bool Approve { get; init; }

    [Range(0, 100_000_000_000)]
    public decimal? ApprovedAmount { get; init; }

    [MaxLength(120)]
    public string? RefundReferenceCode { get; init; }

    [Required, MinLength(5), MaxLength(1000)]
    public string Notes { get; init; } = string.Empty;
}

/// <summary>Supplier configuration for shop deposit, VAT, and regional shipping policies.</summary>
public sealed record UpsertShopOrderPolicyRequest
{
    public Guid? ShopId { get; init; }

    public bool DepositEnabled { get; init; }

    [Range(0, 100_000_000_000)]
    public decimal MinimumOrderTotalForDeposit { get; init; }

    [Range(10, 50)]
    public decimal DepositPercent { get; init; } = 30m;

    public bool TaxIncluded { get; init; } = true;

    [Range(0, 100)]
    public decimal TaxRate { get; init; } = 8m;

    public bool AllowSelfPickup { get; init; } = true;

    public bool RequireShippingZoneSelection { get; init; }

    [MaxLength(30)]
    public List<UpsertShopShippingRateItemRequest>? ShippingRates { get; init; }
}

/// <summary>Single regional shipping rate item configured by a supplier.</summary>
public sealed record UpsertShopShippingRateItemRequest
{
    [Required, MaxLength(40)]
    public string RegionCode { get; init; } = string.Empty;

    [Required, MaxLength(120)]
    public string RegionName { get; init; } = string.Empty;

    [Range(0, 1_000_000_000)]
    public decimal FeeAmount { get; init; }

    [Range(0, 100_000_000_000)]
    public decimal? FreeShippingThreshold { get; init; }

    [Range(0, 90)]
    public int EstimatedDays { get; init; } = 2;

    public bool IsSupported { get; init; } = true;
}

/// <summary>Supplier bank account configuration for simulated order payouts.</summary>
public sealed record UpsertSupplierPayoutAccountRequest
{
    [Required, MaxLength(30)]
    public string BankCode { get; init; } = string.Empty;

    [Required, MaxLength(120)]
    public string BankName { get; init; } = string.Empty;

    [Required, MinLength(6), MaxLength(50)]
    public string AccountNumber { get; init; } = string.Empty;

    [Required, MinLength(2), MaxLength(150)]
    public string AccountHolderName { get; init; } = string.Empty;

    [MaxLength(150)]
    public string? BranchName { get; init; }
}

/// <summary>Supplier request to disburse eligible funds for a delivered order in sandbox mode.</summary>
public sealed record CreateOrderPayoutRequest
{
    [MaxLength(500)]
    public string? Notes { get; init; }
}

/// <summary>Administrator decision on a supplier simulated payout request.</summary>
public sealed record ProcessOrderPayoutRequest
{
    public bool Approve { get; init; }

    [MaxLength(120)]
    public string? PayoutReferenceCode { get; init; }

    [Required, MinLength(5), MaxLength(1000)]
    public string Notes { get; init; } = string.Empty;
}

