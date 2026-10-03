namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Data access contract for AI conversations, messages, and analytics reports.</summary>
public interface IAiRepository
{
    Task<IEnumerable<AiConversation>> GetConversationsByUserIdAsync(long userId, CancellationToken cancellationToken = default);
    Task<AiConversation?> GetConversationByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<AiConversation> CreateConversationAsync(AiConversation conversation, CancellationToken cancellationToken = default);
    Task DeleteConversationAsync(long id, CancellationToken cancellationToken = default);

    Task<IEnumerable<AiMessage>> GetMessagesByConversationIdAsync(long conversationId, CancellationToken cancellationToken = default);
    Task<AiMessage> AddMessageAsync(AiMessage message, CancellationToken cancellationToken = default);

    Task<IEnumerable<AiAnalyticsReport>> GetReportsBySupplierIdAsync(long supplierId, CancellationToken cancellationToken = default);
    Task<AiAnalyticsReport?> GetReportByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<AiAnalyticsReport> CreateReportAsync(AiAnalyticsReport report, CancellationToken cancellationToken = default);
    Task<AiAnalyticsReport> UpdateReportAsync(AiAnalyticsReport report, CancellationToken cancellationToken = default);
}
