namespace Mooncake.EcommercePlatform.Application.DTOs.Ai.Requests;

using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Request to initiate or resume an AI chat session.</summary>
public class StartAiSessionRequest
{
    public string? ExistingSessionToken { get; set; }

    public AiConversationType ConversationType { get; set; } = AiConversationType.General;
}

/// <summary>Request to send a message to the AI Consultant.</summary>
public class SendAiMessageRequest
{
    public string SessionToken { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public AiConversationType ConversationType { get; set; } = AiConversationType.ProductInfo;

    public decimal? TargetBudget { get; set; }

    public int? TargetQuantity { get; set; }
}

/// <summary>Request to generate or refresh an intelligent analytics report.</summary>
public class GenerateAiAnalyticsReportRequest
{
    public AiReportType ReportType { get; set; } = AiReportType.SalesTrend;

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public Guid? TargetShopId { get; set; }

    public string? CategoryFilter { get; set; }
}

