namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Repository for AI chat conversations and messages.</summary>
public interface IAiConversationRepository
{
    Task<AiConversation?> GetBySessionTokenAsync(string sessionToken, CancellationToken cancellationToken = default);

    Task<AiConversation> CreateConversationAsync(AiConversation conversation, CancellationToken cancellationToken = default);

    Task<AiMessage> AddMessageAsync(AiMessage message, CancellationToken cancellationToken = default);

    Task<List<AiMessage>> GetRecentMessagesAsync(Guid conversationId, int count = 6, CancellationToken cancellationToken = default);

    Task<List<AiMessage>> GetAllMessagesAsync(Guid conversationId, CancellationToken cancellationToken = default);

    Task EndConversationAsync(Guid conversationId, CancellationToken cancellationToken = default);
}

