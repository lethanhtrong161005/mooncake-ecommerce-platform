namespace Mooncake.EcommercePlatform.Application.DTOs.Orders.Responses;

using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Order summary, immutable line-item snapshots, SLA deadlines, allowed actions, and payment/delivery snapshots.</summary>
public sealed record OrderResponse(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    Guid ShopId,
    OrderStatus Status,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal ShippingFee,
    decimal PackagingFee,
    decimal TotalAmount,
    string? DeliveryAddress,
    string? RecipientName,
    string? RecipientPhone,
    DateOnly? DeliveryDateExpected,
    string? Notes,
    DateTime CreatedAtUtc,
    IReadOnlyList<OrderItemResponse> Items,
    CustomPackagingSummaryResponse? CustomPackaging)
{
    public long Version { get; init; } = 1L;
    public string? ShopName { get; init; }
    public string? CustomerName { get; init; }
    public DateTime? ResponseDeadlineAtUtc { get; init; }
    public DateTime? ReservationExpiresAtUtc { get; init; }
    public DateTime? ConfirmedAtUtc { get; init; }
    public DateOnly? ConfirmedDeliveryDate { get; init; }
    public DateOnly? ProposedDeliveryDate { get; init; }
    public string? DeliveryProposalReason { get; init; }
    public DateTime? DeliveryApprovalDeadlineAtUtc { get; init; }
    public DateTime? CancelledAtUtc { get; init; }
    public string? CancellationReason { get; init; }
    public string? PriceSnapshotVersion { get; init; }
    public string? ShippingZoneCode { get; init; }
    public string? ShippingZoneName { get; init; }
    public bool TaxIncluded { get; init; } = true;
    public decimal TaxRate { get; init; }
    public bool DepositRequired { get; init; }
    public decimal DepositPercent { get; init; }
    public bool CanStartProduction { get; init; }
    public bool CanMarkShipped { get; init; }
    public IReadOnlyList<string> AllowedActions { get; init; } = [];
    public OrderPaymentSummaryResponse? PaymentSummary { get; init; }
    public OrderFinancialSummaryResponse? FinancialSummary { get; init; }
    public OrderDeliverySnapshotResponse? LatestDelivery { get; init; }
    public IReadOnlyList<PackagingFeeChangeRequestResponse> FeeChangeRequests { get; init; } = [];
    public OrderCancellationRequestResponse? PendingCancellationRequest { get; init; }
    public OrderDeliveryDisputeResponse? ActiveDispute { get; init; }
}

/// <summary>Order line using product and SKU snapshots captured at checkout.</summary>
public sealed record OrderItemResponse(
    Guid ProductId,
    Guid VariantId,
    string ProductName,
    string VariantName,
    string Sku,
    int Quantity,
    decimal UnitPrice,
    decimal DiscountAmount,
    decimal Subtotal);

/// <summary>Compact packaging state embedded in order lists and details.</summary>
public sealed record CustomPackagingSummaryResponse(
    Guid Id,
    string CompanyName,
    string? CompanyLogoUrl,
    string? BoxDesignUrl,
    string PackagingType,
    CustomPackagingStatus Status,
    int RevisionsCount);
