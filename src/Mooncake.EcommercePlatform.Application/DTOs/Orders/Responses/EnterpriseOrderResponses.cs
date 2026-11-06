namespace Mooncake.EcommercePlatform.Application.DTOs.Orders.Responses;

using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Cursor-paginated list of orders without offset degradation.</summary>
public sealed record CursorPagedOrdersResponse(
    IReadOnlyList<OrderResponse> Items,
    string? NextCursor,
    bool HasMore,
    int TotalReturned);

/// <summary>Regional shipping rate configured on a shop.</summary>
public sealed record ShopShippingRateResponse(
    Guid Id,
    string RegionCode,
    string RegionName,
    decimal FeeAmount,
    decimal? FreeShippingThreshold,
    int EstimatedDays,
    bool IsSupported);

/// <summary>Shop direct bulk order policy and shipping rate table.</summary>
public sealed record ShopOrderPolicyResponse(
    Guid ShopId,
    bool DepositEnabled,
    decimal MinimumOrderTotalForDeposit,
    decimal DepositPercent,
    bool TaxIncluded,
    decimal TaxRate,
    bool AllowSelfPickup,
    bool RequireShippingZoneSelection,
    long Version,
    IReadOnlyList<ShopShippingRateResponse> ShippingRates);

/// <summary>Individual VNPay-capped payment part under an order payment obligation.</summary>
public sealed record OrderPaymentPartResponse(
    Guid Id,
    Guid ObligationId,
    int PartIndex,
    int TotalParts,
    decimal Amount,
    string Status,
    DateTime? PaidAtUtc,
    string? ActivePaymentNumber,
    string? TransactionId);

/// <summary>Scheduled payment obligation (Full, Deposit, BalanceBeforeShipping, PackagingFeeAdjustment).</summary>
public sealed record OrderPaymentObligationResponse(
    Guid Id,
    int SequenceNo,
    string ObligationType,
    string Title,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal RemainingAmount,
    string Status,
    bool IsRequiredBeforeProduction,
    bool IsRequiredBeforeShipping,
    DateTime? DueAtUtc,
    DateTime? PaidAtUtc,
    IReadOnlyList<OrderPaymentPartResponse> Parts);

/// <summary>Recorded payment attempt associated with an order or payment part.</summary>
public sealed record OrderPaymentTransactionItemResponse(
    Guid Id,
    Guid? PartId,
    string PaymentNumber,
    decimal Amount,
    decimal RefundedAmount,
    PaymentMethod Method,
    PaymentStatus Status,
    string? TransactionId,
    DateTime CreatedAtUtc,
    DateTime? PaidAtUtc);

/// <summary>Comprehensive payment schedule, parts, and paid/remaining breakdown for an order.</summary>
public sealed record OrderPaymentSummaryResponse(
    Guid OrderId,
    decimal TotalOrderAmount,
    decimal TotalScheduledAmount,
    decimal TotalPaidAmount,
    decimal TotalRefundedAmount,
    decimal RemainingAmount,
    bool CanStartProduction,
    bool CanMarkShipped,
    decimal VnPayMaxAmountPerPart,
    IReadOnlyList<OrderPaymentObligationResponse> Obligations,
    IReadOnlyList<OrderPaymentTransactionItemResponse> Transactions);

/// <summary>Immutable ledger line for simulated held/refund/eligible/payout tracking.</summary>
public sealed record OrderFinancialLedgerItemResponse(
    Guid Id,
    string EntryType,
    string BalanceBucket,
    decimal Amount,
    string Description,
    DateTime CreatedAtUtc);

/// <summary>Sandbox-simulated financial summary showing Held, Disputed, Refunded, Eligible, and PaidOutSimulated balances.</summary>
public sealed record OrderFinancialSummaryResponse(
    Guid OrderId,
    bool SandboxSimulationOnly,
    bool ProductionPayoutEnabled,
    decimal CapturedAmount,
    decimal HeldAmount,
    decimal DisputedLockedAmount,
    decimal RefundedSimulatedAmount,
    decimal EligibleAmount,
    decimal PayoutRequestedAmount,
    decimal PaidOutSimulatedAmount,
    IReadOnlyList<OrderFinancialLedgerItemResponse> LedgerEntries,
    IReadOnlyList<OrderRefundCaseResponse> RefundCases,
    IReadOnlyList<SupplierPayoutRequestResponse> PayoutRequests);

/// <summary>Auditable packaging fee change request attached to a mockup revision.</summary>
public sealed record PackagingFeeChangeRequestResponse(
    Guid Id,
    Guid OrderId,
    Guid RevisionId,
    decimal OldPackagingFee,
    decimal DeltaAmount,
    decimal NewPackagingFee,
    decimal OldOrderTotal,
    decimal NewOrderTotal,
    long OrderVersionSnapshot,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? ApprovedAtUtc,
    Guid? GeneratedObligationId);

/// <summary>Mutual cancellation request state.</summary>
public sealed record OrderCancellationRequestResponse(
    Guid Id,
    Guid OrderId,
    Guid RequestedByUserId,
    string RequestedByRole,
    string OrderStatusSnapshot,
    string Reason,
    string Status,
    decimal ProposedRefundAmount,
    DateTime CreatedAtUtc,
    DateTime? ReviewedAtUtc,
    string? ReviewNotes);

/// <summary>Refund case state in sandbox simulation mode.</summary>
public sealed record OrderRefundCaseResponse(
    Guid Id,
    Guid OrderId,
    string? OrderNumber,
    string CaseNumber,
    decimal RequestedAmount,
    decimal ApprovedAmount,
    string Reason,
    string Status,
    bool SandboxSimulated,
    DateTime CreatedAtUtc,
    DateTime? ProcessedAtUtc,
    string? AdminNotes,
    string? RefundReferenceCode);

/// <summary>Supplier settlement bank account details.</summary>
public sealed record SupplierPayoutAccountResponse(
    Guid Id,
    Guid SupplierUserId,
    string BankCode,
    string BankName,
    string AccountNumber,
    string MaskedAccountNumber,
    string AccountHolderName,
    string? BranchName,
    bool IsVerifiedSimulated,
    long Version,
    DateTime UpdatedAtUtc);

/// <summary>Simulated supplier payout request for an eligible delivered order.</summary>
public sealed record SupplierPayoutRequestResponse(
    Guid Id,
    Guid OrderId,
    string? OrderNumber,
    string RequestNumber,
    decimal Amount,
    string Status,
    bool SandboxSimulated,
    string BankSnapshotJson,
    DateTime CreatedAtUtc,
    DateTime? ProcessedAtUtc,
    string? AdminNotes,
    string? PayoutReferenceCode);

/// <summary>Delivery discrepancy or damage dispute reported by a customer.</summary>
public sealed record OrderDeliveryDisputeResponse(
    Guid Id,
    Guid OrderId,
    string? OrderNumber,
    Guid? DeliveryId,
    string IssueType,
    string Description,
    IReadOnlyList<string> EvidenceUrls,
    string Status,
    string? ResolutionNotes,
    DateTime CreatedAtUtc,
    DateTime? ResolvedAtUtc);

/// <summary>Compact Proof-of-Delivery photo and shipment snapshot embedded on an order.</summary>
public sealed record OrderDeliverySnapshotResponse(
    Guid DeliveryId,
    string DeliveryNumber,
    DeliveryStatus Status,
    string? TrackingCode,
    DateOnly? ScheduledDate,
    DateOnly? ActualDeliveryDate,
    int ProofCount,
    IReadOnlyList<OrderDeliveryProofPhotoResponse> Proofs);

/// <summary>PoD photo entry embedded inside an order delivery snapshot.</summary>
public sealed record OrderDeliveryProofPhotoResponse(
    Guid Id,
    ProofType ProofType,
    string? PhotoUrl,
    string? TakenBy,
    DateTime TakenAtUtc,
    string? Notes);

/// <summary>Supplier-wide aggregate financial balances across all shops owned by the supplier.</summary>
public sealed record SupplierFinancialOverviewResponse(
    bool SandboxSimulationOnly,
    decimal TotalCapturedAmount,
    decimal TotalHeldAmount,
    decimal TotalDisputedAmount,
    decimal TotalRefundedSimulatedAmount,
    decimal TotalEligibleAmount,
    decimal TotalPayoutRequestedAmount,
    decimal TotalPaidOutSimulatedAmount,
    SupplierPayoutAccountResponse? PayoutAccount,
    IReadOnlyList<SupplierPayoutRequestResponse> PayoutRequests);

/// <summary>Administrator reconciliation overview for direct bulk order refunds, disputes, and simulated payouts.</summary>
public sealed record AdminOrderOperationsOverviewResponse(
    bool SandboxSimulationOnly,
    decimal PlatformHeldAmount,
    decimal PlatformDisputedAmount,
    decimal PlatformEligibleAmount,
    decimal PlatformRefundedSimulatedAmount,
    decimal PlatformPaidOutSimulatedAmount,
    IReadOnlyList<OrderRefundCaseResponse> RefundCases,
    IReadOnlyList<SupplierPayoutRequestResponse> PayoutRequests,
    IReadOnlyList<OrderDeliveryDisputeResponse> DeliveryDisputes);

public sealed record SupplierOrderDashboardSummaryResponse(int TotalOrders, int PendingOrders,
    int ProcessingOrders, int PackagingOrders, decimal TotalRevenue);

