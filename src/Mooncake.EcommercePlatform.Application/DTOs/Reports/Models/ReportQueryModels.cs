namespace Mooncake.EcommercePlatform.Application.DTOs.Reports.Models;

using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Lightweight order projection used by report aggregations.</summary>
public sealed record ReportOrderRow(Guid Id, Guid ShopId, OrderStatus Status, decimal TotalAmount, DateTime CreatedAtUtc);

/// <summary>Lightweight order ledger projection used by report aggregations.</summary>
public sealed record ReportOrderLedgerRow(Guid OrderId, string EntryType, decimal Amount);

/// <summary>Lightweight contract projection used by report aggregations.</summary>
public sealed record ReportContractRow(Guid Id, Guid SupplierId, ContractStatus Status, decimal TotalValue, DateTime CreatedAtUtc);

/// <summary>Resolved contract dispute refund projection used by report aggregations.</summary>
public sealed record ReportContractRefundRow(Guid ContractId, decimal RefundAmount);

/// <summary>Snapshot of platform orders, contracts, disputes, and escrow data for admin metrics computation.</summary>
public sealed record AdminReportSnapshot(
    IReadOnlyList<ReportOrderRow> Orders,
    IReadOnlyList<ReportOrderLedgerRow> OrderLedgerEntries,
    IReadOnlyList<ReportContractRow> Contracts,
    IReadOnlyList<ReportContractRefundRow> ResolvedContractRefunds,
    int PendingSuppliersCount,
    int PendingContractDisputesCount,
    int OpenDeliveryDisputesCount,
    decimal TotalPaidMilestoneEscrowBalance);

/// <summary>Raw pipeline counts for RFQ conversion funnel calculations.</summary>
public sealed record ConversionFunnelRawCounts(
    int RfqsCount,
    int BidsCount,
    int BidsAwardedCount,
    int ContractsSignedCount,
    int ContractsCompletedCount);

/// <summary>Verified supplier profile projection used in top-supplier report ranking.</summary>
public sealed record ReportSupplierProfileRow(
    Guid Id,
    string? CompanyName,
    string? TaxCode,
    decimal ReputationScore,
    int TotalContracts,
    int ContractsOnTime);

/// <summary>Shop ownership projection used to link orders to verified suppliers.</summary>
public sealed record ReportShopRow(Guid Id, Guid SupplierId, string? Name);

/// <summary>Snapshot of verified suppliers, shops, recognized orders, and contracts for supplier ranking.</summary>
public sealed record TopSuppliersReportSnapshot(
    IReadOnlyList<ReportSupplierProfileRow> VerifiedSuppliers,
    IReadOnlyList<ReportShopRow> Shops,
    IReadOnlyList<ReportOrderRow> RecognizedOrders,
    IReadOnlyList<ReportOrderLedgerRow> OrderRefundEntries,
    IReadOnlyList<ReportContractRow> RecognizedContracts,
    IReadOnlyList<ReportContractRefundRow> ResolvedContractRefunds);

/// <summary>Contract row projection used when generating CSV report exports.</summary>
public sealed record ContractExportRow(
    string ContractNumber,
    string? Title,
    decimal? TotalValue,
    ContractStatus Status,
    DateTime? SignedByCustomerAt,
    DateTime? SignedBySupplierAt);
