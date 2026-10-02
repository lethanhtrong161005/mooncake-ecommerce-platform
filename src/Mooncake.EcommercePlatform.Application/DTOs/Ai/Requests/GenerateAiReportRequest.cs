namespace Mooncake.EcommercePlatform.Application.DTOs.Ai.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>Request to generate an AI analytics sales report for a supplier.</summary>
public record GenerateAiReportRequest
{
    [Required]
    public long SupplierId { get; init; }

    [Required]
    [MaxLength(100)]
    public string ReportType { get; init; } = "sales_forecast";

    public DateOnly? PeriodStart { get; init; }
    public DateOnly? PeriodEnd { get; init; }

    public string Parameters { get; init; } = "{}";
}
