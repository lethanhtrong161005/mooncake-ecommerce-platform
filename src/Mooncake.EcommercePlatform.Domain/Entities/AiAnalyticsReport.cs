namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Represents the AiAnalyticsReport domain entity.</summary>
public class AiAnalyticsReport : BaseEntity
{
    public Guid RequestedBy { get; set; }

    public string? Parameters { get; set; }

    public string? ResultData { get; set; }

    public DateTime? CompletedAt { get; set; }

    public AiReportType ReportType { get; set; } = AiReportType.SalesTrend;

    public AiReportStatus Status { get; set; } = AiReportStatus.Pending;
}
