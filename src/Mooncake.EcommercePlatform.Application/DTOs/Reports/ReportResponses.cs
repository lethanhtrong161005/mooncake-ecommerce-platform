namespace Mooncake.EcommercePlatform.Application.DTOs.Reports;

/// <summary>Time-series GMV and platform commission data point for admin analytics charts.</summary>
public sealed record AdminGmvTrendPointResponse(
    string Key,
    string Label,
    decimal DirectOrderGmv,
    decimal ContractGmv,
    decimal TotalGmv,
    decimal PlatformCommission,
    int OrderCount,
    int ContractCount);

/// <summary>Aggregated direct B2B order status counts.</summary>
public sealed record OrderStatusBreakdownResponse(
    int Pending,
    int Processing,
    int Delivered,
    int Cancelled);

/// <summary>Aggregated RFQ contract status counts.</summary>
public sealed record ContractStatusBreakdownResponse(
    int Pending,
    int Active,
    int Completed,
    int Disputed);

/// <summary>Platform-wide executive KPIs and time-series metrics for the admin dashboard.</summary>
public sealed record AdminDashboardMetricsResponse(
    string Timeframe,
    decimal TotalGmv,
    decimal TotalOrderGmv,
    decimal TotalContractsGmv,
    decimal DirectPaidRevenue,
    decimal PlatformRevenue,
    int TotalOrdersCount,
    int TotalContractsCount,
    int ActiveContractsCount,
    int PendingSuppliersCount,
    int PendingDisputesCount,
    int PendingContractDisputesCount,
    int OpenDeliveryDisputesCount,
    decimal TotalEscrowBalance,
    IReadOnlyList<AdminGmvTrendPointResponse> DailyTrend,
    IReadOnlyList<AdminGmvTrendPointResponse> MonthlyTrend,
    OrderStatusBreakdownResponse OrderStatusBreakdown,
    ContractStatusBreakdownResponse ContractStatusBreakdown,
    double GrowthRatePercentage);

/// <summary>Single conversion stage in the RFQ-to-Contract pipeline funnel.</summary>
public sealed record ConversionFunnelStageResponse(
    string StageName,
    int Count,
    double ConversionRatePercentage);

/// <summary>Conversion funnel metrics for the selected timeframe.</summary>
public sealed record ConversionFunnelResponse(
    string Timeframe,
    IReadOnlyList<ConversionFunnelStageResponse> Stages,
    double OverallSuccessRate);

/// <summary>Top verified supplier ranking entry combined from direct orders and RFQ contracts.</summary>
public sealed record TopSupplierReportResponse(
    Guid SupplierId,
    string ShopName,
    string TaxCode,
    int OrdersCount,
    int DirectOrdersCount,
    int ContractsCount,
    decimal Gmv,
    decimal DirectOrderGmv,
    decimal ContractGmv,
    double OnTimeRate,
    decimal Score,
    string Status);

/// <summary>Exported report binary file result.</summary>
public sealed record ReportExportFileResponse(
    byte[] Content,
    string ContentType,
    string FileName);
