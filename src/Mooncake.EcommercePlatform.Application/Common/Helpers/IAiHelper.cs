namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Application.DTOs.Ai.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Contract for mapping AI entities to DTOs.</summary>
public interface IAiHelper
{
    AiConversationResponse ToResponse(AiConversation conversation);
    AiMessageResponse ToResponse(AiMessage message);
    AiAnalyticsReportResponse ToResponse(AiAnalyticsReport report);
}
