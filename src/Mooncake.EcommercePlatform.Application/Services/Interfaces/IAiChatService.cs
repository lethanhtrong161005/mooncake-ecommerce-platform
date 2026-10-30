namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Ai.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Ai.Responses;

/// <summary>Service for AI Chatbot consultant and RAG recommendations.</summary>
public interface IAiChatService
{
    Task<AiSessionResponse> StartSessionAsync(Guid? userId, StartAiSessionRequest request, CancellationToken cancellationToken = default);

    Task<AiMessageResponse> ProcessMessageAsync(Guid? userId, SendAiMessageRequest request, CancellationToken cancellationToken = default);

    Task<AiConversationHistoryResponse> GetHistoryAsync(string sessionToken, CancellationToken cancellationToken = default);

    Task EndSessionAsync(string sessionToken, CancellationToken cancellationToken = default);
}

