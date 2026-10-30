namespace Mooncake.EcommercePlatform.Application.DTOs.Ai.Responses;

using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Response containing the AI conversation session state.</summary>
public class AiSessionResponse
{
    public Guid ConversationId { get; set; }

    public string SessionToken { get; set; } = string.Empty;

    public AiConversationType ConversationType { get; set; }

    public DateTime CreatedAt { get; set; }
}

/// <summary>Product card metadata returned alongside the AI text response.</summary>
public class AiProductRecommendationDto
{
    public Guid ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string ShopName { get; set; } = string.Empty;

    public string? PrimaryImageUrl { get; set; }

    public decimal BasePrice { get; set; }

    public int MinOrderQty { get; set; }

    public string? VariantName { get; set; }

    public decimal? VariantPrice { get; set; }

    public string? ReputationBadgeTitle { get; set; }

    public decimal ReputationScore { get; set; }

    public string RecommendationReason { get; set; } = string.Empty;
}

/// <summary>Response for an individual chat interaction turn.</summary>
public class AiMessageResponse
{
    public Guid MessageId { get; set; }

    public AiMessageRole Role { get; set; }

    public string Content { get; set; } = string.Empty;

    public int TokensUsed { get; set; }

    public string ModelUsed { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public List<AiProductRecommendationDto> RecommendedProducts { get; set; } = new();

    public List<string> SuggestedFollowUps { get; set; } = new();
}

/// <summary>Item in the conversation history list.</summary>
public class AiHistoryItemDto
{
    public Guid MessageId { get; set; }

    public AiMessageRole Role { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

/// <summary>Response containing the full message history of a chat session.</summary>
public class AiConversationHistoryResponse
{
    public Guid ConversationId { get; set; }

    public string SessionToken { get; set; } = string.Empty;

    public AiConversationType ConversationType { get; set; }

    public List<AiHistoryItemDto> Messages { get; set; } = new();
}

/// <summary>Operational metrics summary for executive reports.</summary>
public class AiReportMetricsSummaryDto
{
    public decimal TotalGmv { get; set; }

    public decimal GmvGrowthRate { get; set; }

    public int TotalOrders { get; set; }

    public int TotalContracts { get; set; }

    public decimal OnTimeDeliveryRate { get; set; }

    public decimal RfqWinRate { get; set; }
}

/// <summary>Risk alert identified by the AI analysis.</summary>
public class AiRiskAlertDto
{
    public string Severity { get; set; } = "Medium";

    public string Description { get; set; } = string.Empty;

    public string PotentialImpact { get; set; } = string.Empty;
}

/// <summary>Actionable recommendation proposed by the AI analysis.</summary>
public class AiStrategicActionDto
{
    public string Priority { get; set; } = "High";

    public string Action { get; set; } = string.Empty;

    public string ExpectedOutcome { get; set; } = string.Empty;
}

/// <summary>Structured natural language insights returned by Gemini.</summary>
public class AiReportResultDto
{
    public string ExecutiveSummary { get; set; } = string.Empty;

    public List<string> KeyHighlights { get; set; } = new();

    public List<AiRiskAlertDto> RiskAlerts { get; set; } = new();

    public List<AiStrategicActionDto> StrategicRecommendations { get; set; } = new();

    public AiReportMetricsSummaryDto MetricsSummary { get; set; } = new();
}

/// <summary>Response representing an AI sales analysis report.</summary>
public class AiAnalyticsReportResponse
{
    public Guid ReportId { get; set; }

    public AiReportType ReportType { get; set; }

    public AiReportStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public AiReportResultDto? Result { get; set; }
}

/// <summary>Realtime quantitative dashboard summary returned without waiting for LLM generation.</summary>
public class AiDashboardSummaryResponse
{
    public decimal TotalGmv { get; set; }

    public decimal GmvGrowthRate { get; set; }

    public int TotalOrders { get; set; }

    public int TotalContracts { get; set; }

    public decimal OnTimeDeliveryRate { get; set; }

    public decimal RfqWinRate { get; set; }

    public int ActiveSuppliersCount { get; set; }

    public int ActiveProductsCount { get; set; }

    public List<string> TopSellingCategories { get; set; } = new();
}

