namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IAiRepository"/>.</summary>
public class AiRepository(ApplicationDbContext context) : IAiRepository
{
    public async Task<IEnumerable<AiConversation>> GetConversationsByUserIdAsync(long userId, CancellationToken cancellationToken = default) =>
        await context.AiConversations
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.UpdatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<AiConversation?> GetConversationByIdAsync(long id, CancellationToken cancellationToken = default) =>
        await context.AiConversations
            .Include(c => c.AiMessages.OrderBy(m => m.CreatedAtUtc))
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<AiConversation> CreateConversationAsync(AiConversation conversation, CancellationToken cancellationToken = default)
    {
        context.AiConversations.Add(conversation);
        await context.SaveChangesAsync(cancellationToken);
        return conversation;
    }

    public async Task DeleteConversationAsync(long id, CancellationToken cancellationToken = default)
    {
        var conversation = await context.AiConversations.FindAsync([id], cancellationToken);
        if (conversation is not null)
        {
            context.AiConversations.Remove(conversation);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IEnumerable<AiMessage>> GetMessagesByConversationIdAsync(long conversationId, CancellationToken cancellationToken = default) =>
        await context.AiMessages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<AiMessage> AddMessageAsync(AiMessage message, CancellationToken cancellationToken = default)
    {
        context.AiMessages.Add(message);

        // Update conversation timestamp
        var conv = await context.AiConversations.FindAsync([message.ConversationId], cancellationToken);
        if (conv is not null)
        {
            conv.UpdatedAtUtc = DateTime.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken);
        return message;
    }

    public async Task<IEnumerable<AiAnalyticsReport>> GetReportsBySupplierIdAsync(long supplierId, CancellationToken cancellationToken = default) =>
        await context.AiAnalyticsReports
            .AsNoTracking()
            .Where(r => r.SupplierId == supplierId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<AiAnalyticsReport?> GetReportByIdAsync(long id, CancellationToken cancellationToken = default) =>
        await context.AiAnalyticsReports
            .Include(r => r.Supplier)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<AiAnalyticsReport> CreateReportAsync(AiAnalyticsReport report, CancellationToken cancellationToken = default)
    {
        context.AiAnalyticsReports.Add(report);
        await context.SaveChangesAsync(cancellationToken);
        return report;
    }

    public async Task<AiAnalyticsReport> UpdateReportAsync(AiAnalyticsReport report, CancellationToken cancellationToken = default)
    {
        context.AiAnalyticsReports.Update(report);
        await context.SaveChangesAsync(cancellationToken);
        return report;
    }
}
