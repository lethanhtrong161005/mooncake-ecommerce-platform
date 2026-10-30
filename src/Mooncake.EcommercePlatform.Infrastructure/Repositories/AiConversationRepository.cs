namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

/// <summary>Repository for AI chat conversations and message history.</summary>
public class AiConversationRepository : IAiConversationRepository
{
    private readonly ApplicationDbContext _dbContext;

    public AiConversationRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AiConversation?> GetBySessionTokenAsync(string sessionToken, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AiConversations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.SessionToken == sessionToken && !c.IsDeleted, cancellationToken);
    }

    public async Task<AiConversation> CreateConversationAsync(AiConversation conversation, CancellationToken cancellationToken = default)
    {
        if (conversation.Id == Guid.Empty)
        {
            conversation.Id = Guid.NewGuid();
        }

        conversation.CreatedAt = DateTime.UtcNow;
        conversation.UpdatedAt = DateTime.UtcNow;

        _dbContext.AiConversations.Add(conversation);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return conversation;
    }

    public async Task<AiMessage> AddMessageAsync(AiMessage message, CancellationToken cancellationToken = default)
    {
        if (message.Id == Guid.Empty)
        {
            message.Id = Guid.NewGuid();
        }

        message.CreatedAt = DateTime.UtcNow;
        message.UpdatedAt = DateTime.UtcNow;

        _dbContext.AiMessages.Add(message);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return message;
    }

    public async Task<List<AiMessage>> GetRecentMessagesAsync(Guid conversationId, int count = 6, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AiMessages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId && !m.IsDeleted)
            .OrderByDescending(m => m.CreatedAt)
            .Take(count)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AiMessage>> GetAllMessagesAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AiMessages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId && !m.IsDeleted)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task EndConversationAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        var conversation = await _dbContext.AiConversations.FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);
        if (conversation != null)
        {
            conversation.EndedAt = DateTime.UtcNow;
            conversation.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

