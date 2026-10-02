namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Ai.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Ai.Responses;

/// <summary>Contract for AI conversational assistant and analytics reporting.</summary>
public interface IAiService
{
    Task<AiConversationResponse> CreateConversationAsync(long userId, CreateAiConversationRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<AiConversationResponse>> GetUserConversationsAsync(long userId, CancellationToken cancellationToken = default);
    Task<AiConversationResponse> GetConversationByIdAsync(long conversationId, CancellationToken cancellationToken = default);
    Task<AiMessageResponse> SendMessageAsync(long conversationId, SendAiMessageRequest request, CancellationToken cancellationToken = default);

    Task<AiAnalyticsReportResponse> GenerateReportAsync(GenerateAiReportRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<AiAnalyticsReportResponse>> GetSupplierReportsAsync(long supplierId, CancellationToken cancellationToken = default);
    Task<AiAnalyticsReportResponse> GetReportByIdAsync(long reportId, CancellationToken cancellationToken = default);
}
