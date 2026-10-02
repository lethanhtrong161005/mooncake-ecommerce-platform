namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Application.DTOs.Ai.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Maps AI entities to response DTOs.</summary>
public class AiHelper : IAiHelper
{
    public AiConversationResponse ToResponse(AiConversation conversation) =>
        new(
            conversation.Id,
            conversation.UserId,
            conversation.Title,
            conversation.CreatedAtUtc,
            conversation.UpdatedAtUtc,
            conversation.AiMessages?.Select(ToResponse).ToList()
        );

    public AiMessageResponse ToResponse(AiMessage message) =>
        new(
            message.Id,
            message.ConversationId,
            message.Role,
            message.Content,
            message.Metadata,
            message.CreatedAtUtc
        );

    public AiAnalyticsReportResponse ToResponse(AiAnalyticsReport report) =>
        new(
            report.Id,
            report.SupplierId,
            report.ReportType,
            report.PeriodStart,
            report.PeriodEnd,
            report.Parameters,
            report.Result,
            report.Status,
            report.CreatedAtUtc,
            report.CompletedAtUtc
        );
}
