using Mooncake.EcommercePlatform.Domain.Common;
using Mooncake.EcommercePlatform.Domain.Enums;
using System;

namespace Mooncake.EcommercePlatform.Domain.Entities;

public class AiAnalyticsReport : BaseEntity
{
    public long SupplierId { get; set; }
    public string ReportType { get; set; } = null!;
    public DateOnly? PeriodStart { get; set; }
    public DateOnly? PeriodEnd { get; set; }
    public string Parameters { get; set; } = "{}";
    public string? Result { get; set; }
    public AiReportStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    public Supplier Supplier { get; set; } = null!;
}
