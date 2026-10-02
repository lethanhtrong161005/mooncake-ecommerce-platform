namespace Mooncake.EcommercePlatform.Application.DTOs.Ai.Responses;

using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>AI analytics report projection returned to callers.</summary>
public record AiAnalyticsReportResponse(
    long Id,
    long SupplierId,
    string ReportType,
    DateOnly? PeriodStart,
    DateOnly? PeriodEnd,
    string Parameters,
    string? Result,
    AiReportStatus Status,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc
);
