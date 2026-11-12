namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Chat.Persistence;
using Mooncake.EcommercePlatform.Application.DTOs.Chat.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;
using Npgsql;

/// <summary>EF Core persistence for customer-supplier chat with keyset pagination.</summary>
public sealed class ChatRepository(ApplicationDbContext db) : IChatRepository
{
    private const string DefaultShopName = "Gian hàng";
    private const string DefaultCustomerName = "Khách hàng";

    public Task<ChatShopOwnerDto?> FindActiveShopOwnerAsync(Guid targetId, CancellationToken ct = default) =>
        db.Shops.AsNoTracking()
            .Where(s => (s.Id == targetId || s.OwnerUserId == targetId || s.SupplierId == targetId)
                && !s.IsDeleted && s.Status == ShopStatus.Active)
            .Select(s => new ChatShopOwnerDto(s.Id, s.OwnerUserId))
            .FirstOrDefaultAsync(ct);

    public async Task<Guid> GetOrCreateThreadAsync(Guid customerId, ChatShopOwnerDto shop, DateTime now,
        CancellationToken ct = default)
    {
        var existing = await FindThreadIdAsync(customerId, shop.ShopId, ct);
        if (existing is { } id)
            return id;

        var thread = new ChatThread
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            SupplierId = shop.OwnerUserId,
            ShopId = shop.ShopId,
            LastActivityAt = now,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = customerId
        };
        db.ChatThreads.Add(thread);
        db.ChatParticipantStates.AddRange(
            NewState(thread.Id, customerId, now),
            NewState(thread.Id, shop.OwnerUserId, now));

        try
        {
            await db.SaveChangesAsync(ct);
            return thread.Id;
        }
        catch (DbUpdateException failure) when (IsUniqueViolation(failure))
        {
            db.ChangeTracker.Clear();
            return await FindThreadIdAsync(customerId, shop.ShopId, ct)
                ?? throw new HttpException(409, ChatConstants.ThreadNotFound);
        }
    }

    public Task<ChatThreadAccessDto?> GetThreadAccessAsync(Guid threadId, CancellationToken ct = default) =>
        db.ChatThreads.AsNoTracking()
            .Where(t => t.Id == threadId && !t.IsDeleted)
            .Select(t => new ChatThreadAccessDto(t.Id, t.CustomerId, t.SupplierId, t.ShopId, t.BlockedByUserId))
            .FirstOrDefaultAsync(ct);

    public async Task<ChatThreadResponse?> GetThreadAsync(Guid threadId, Guid viewerId, CancellationToken ct = default)
    {
        var rows = await ThreadRows(viewerId).Where(r => r.Id == threadId).Take(1).ToListAsync(ct);
        return rows.Select(r => ToThreadResponse(r, viewerId)).FirstOrDefault();
    }

    public async Task<ChatThreadPageResultDto> GetThreadPageAsync(Guid userId, ChatCursorDto? after, int pageSize,
        CancellationToken ct = default)
    {
        var query = ThreadRows(userId).Where(r => r.LastMessageId != null);
        if (after is not null)
        {
            query = query.Where(r => r.LastActivityAt < after.AtUtc
                || (r.LastActivityAt == after.AtUtc && r.Id.CompareTo(after.Id) < 0));
        }

        var rows = await query
            .OrderByDescending(r => r.LastActivityAt)
            .ThenByDescending(r => r.Id)
            .Take(pageSize + 1)
            .ToListAsync(ct);

        var hasMore = rows.Count > pageSize;
        var page = rows.Take(pageSize).ToList();
        var last = page.LastOrDefault();
        var next = hasMore && last is not null
            ? new ChatCursorDto(userId, Guid.Empty, last.LastActivityAt, last.Id)
            : null;

        return new ChatThreadPageResultDto(page.Select(r => ToThreadResponse(r, userId)).ToArray(), hasMore, next);
    }

    public async Task<ChatMessagePageResultDto> GetMessagePageAsync(Guid userId, Guid threadId,
        ChatCursorDto? before, int pageSize, CancellationToken ct = default)
    {
        var query = db.ChatMessages.AsNoTracking().Where(m => m.ThreadId == threadId && !m.IsDeleted);
        if (before is not null)
        {
            query = query.Where(m => m.CreatedAt < before.AtUtc
                || (m.CreatedAt == before.AtUtc && m.Id.CompareTo(before.Id) < 0));
        }

        var rows = await query
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.Id)
            .Take(pageSize + 1)
            .ToListAsync(ct);

        var hasMore = rows.Count > pageSize;
        var page = rows.Take(pageSize).ToList();
        var oldest = page.LastOrDefault();
        var next = hasMore && oldest is not null
            ? new ChatCursorDto(userId, threadId, oldest.CreatedAt, oldest.Id)
            : null;

        var attachments = await LoadAttachmentsAsync(page.Select(m => m.Id).ToArray(), ct);
        var items = page
            .AsEnumerable()
            .Reverse()
            .Select(m => ToMessageResponse(m, attachments))
            .ToArray();

        return new ChatMessagePageResultDto(items, hasMore, next);
    }

    public Task<ChatProductSnapshotDto?> GetProductSnapshotAsync(Guid productId, Guid shopId,
        CancellationToken ct = default) =>
        db.Products.AsNoTracking()
            .Where(p => p.Id == productId && p.ShopId == shopId && !p.IsDeleted)
            .Select(p => new ChatProductSnapshotDto(
                p.Id,
                p.Name,
                db.ProductImages
                    .Where(i => i.ProductId == p.Id && !i.IsDeleted)
                    .OrderByDescending(i => i.IsPrimary)
                    .ThenBy(i => i.SortOrder)
                    .Select(i => i.ImageUrl)
                    .FirstOrDefault(),
                p.BasePrice))
            .FirstOrDefaultAsync(ct);

    public async Task<ChatMessageResponse?> FindByClientMessageIdAsync(Guid threadId, Guid senderId,
        Guid clientMessageId, CancellationToken ct = default)
    {
        var message = await db.ChatMessages.AsNoTracking()
            .FirstOrDefaultAsync(m => m.ThreadId == threadId
                && m.SenderId == senderId
                && m.ClientMessageId == clientMessageId, ct);
        if (message is null)
            return null;

        var attachments = await LoadAttachmentsAsync([message.Id], ct);
        return ToMessageResponse(message, attachments);
    }

    public async Task<ChatMessageResponse?> AddMessageAsync(ChatMessage message, IReadOnlyList<Guid> attachmentIds,
        string preview, Guid recipientId, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var attachments = attachmentIds.Count == 0
            ? []
            : await db.ChatAttachments
                .Where(a => attachmentIds.Contains(a.Id)
                    && a.UploaderId == message.SenderId
                    && a.MessageId == null
                    && !a.IsDeleted)
                .ToListAsync(ct);

        if (attachments.Count != attachmentIds.Count)
            throw new HttpException(409, ChatConstants.AttachmentsUnavailable);

        message.Id = Guid.NewGuid();
        db.ChatMessages.Add(message);
        foreach (var attachment in attachments)
        {
            attachment.MessageId = message.Id;
            attachment.SortOrder = IndexOf(attachmentIds, attachment.Id);
            attachment.UpdatedAt = message.CreatedAt;
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException failure) when (IsUniqueViolation(failure))
        {
            await transaction.RollbackAsync(ct);
            db.ChangeTracker.Clear();
            return null;
        }

        await db.ChatThreads
            .Where(t => t.Id == message.ThreadId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.LastMessageId, message.Id)
                .SetProperty(t => t.LastMessageSenderId, message.SenderId)
                .SetProperty(t => t.LastMessagePreview, preview)
                .SetProperty(t => t.LastActivityAt, message.CreatedAt)
                .SetProperty(t => t.UpdatedAt, message.CreatedAt), ct);

        await db.ChatParticipantStates
            .Where(s => s.ThreadId == message.ThreadId && s.UserId == recipientId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.UnreadCount, x => x.UnreadCount + 1), ct);

        await db.ChatParticipantStates
            .Where(s => s.ThreadId == message.ThreadId && s.UserId == message.SenderId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.UnreadCount, 0)
                .SetProperty(x => x.LastReadMessageId, message.Id)
                .SetProperty(x => x.LastReadAt, message.CreatedAt), ct);

        await transaction.CommitAsync(ct);

        var lookup = attachments
            .OrderBy(a => a.SortOrder)
            .Select(ToAttachmentResponse)
            .ToArray();
        return ToMessageResponse(message, new Dictionary<Guid, IReadOnlyList<ChatAttachmentResponse>>
        {
            [message.Id] = lookup
        });
    }

    public async Task<IReadOnlyList<ChatAttachmentResponse>> AddAttachmentsAsync(
        IReadOnlyList<ChatAttachment> attachments, CancellationToken ct = default)
    {
        foreach (var attachment in attachments)
            attachment.Id = Guid.NewGuid();

        db.ChatAttachments.AddRange(attachments);
        await db.SaveChangesAsync(ct);
        return attachments.Select(ToAttachmentResponse).ToArray();
    }

    public Task<ChatAttachment?> GetLinkedAttachmentForMemberAsync(Guid attachmentId, Guid userId,
        CancellationToken ct = default) =>
        (from attachment in db.ChatAttachments.AsNoTracking()
         join message in db.ChatMessages.AsNoTracking() on attachment.MessageId equals message.Id
         join thread in db.ChatThreads.AsNoTracking() on message.ThreadId equals thread.Id
         where attachment.Id == attachmentId
             && !attachment.IsDeleted
             && message.RecalledAt == null
             && !thread.IsDeleted
             && (thread.CustomerId == userId || thread.SupplierId == userId)
         select attachment).FirstOrDefaultAsync(ct);

    public Task<ChatMessage?> GetMessageAsync(Guid messageId, CancellationToken ct = default) =>
        db.ChatMessages.AsNoTracking().FirstOrDefaultAsync(m => m.Id == messageId && !m.IsDeleted, ct);

    public async Task EditMessageAsync(ChatMessage message, string newBody, DateTime now, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        await db.ChatMessages
            .Where(m => m.Id == message.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.Body, newBody)
                .SetProperty(m => m.EditedAt, now)
                .SetProperty(m => m.UpdatedAt, now), ct);

        var preview = newBody.Length <= ChatConstants.PreviewLength
            ? newBody
            : newBody.Substring(0, ChatConstants.PreviewLength) + "…";

        await db.ChatThreads
            .Where(t => t.Id == message.ThreadId && t.LastMessageId == message.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.LastMessagePreview, preview)
                .SetProperty(t => t.UpdatedAt, now), ct);

        await transaction.CommitAsync(ct);
    }

    public async Task RecallMessageAsync(ChatMessage message, DateTime now, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        await db.ChatMessages
            .Where(m => m.Id == message.Id && m.RecalledAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.RecalledAt, now)
                .SetProperty(m => m.UpdatedAt, now), ct);

        await db.ChatThreads
            .Where(t => t.Id == message.ThreadId && t.LastMessageId == message.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.LastMessagePreview, ChatConstants.RecalledPreview), ct);

        await transaction.CommitAsync(ct);
    }

    public Task SetThreadBlockAsync(Guid threadId, Guid? blockedByUserId, DateTime now, CancellationToken ct = default) =>
        db.ChatThreads
            .Where(t => t.Id == threadId && !t.IsDeleted)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.BlockedByUserId, blockedByUserId)
                .SetProperty(t => t.UpdatedAt, now), ct);

    public async Task<ChatReadResultDto?> MarkReadAsync(Guid threadId, Guid userId, Guid messageId, DateTime now,
        CancellationToken ct = default)
    {
        var target = await db.ChatMessages.AsNoTracking()
            .Where(m => m.Id == messageId && m.ThreadId == threadId && !m.IsDeleted)
            .Select(m => new { m.Id, m.CreatedAt })
            .FirstOrDefaultAsync(ct);
        if (target is null)
            return null;

        var state = await db.ChatParticipantStates
            .FirstOrDefaultAsync(s => s.ThreadId == threadId && s.UserId == userId, ct);
        if (state is null)
        {
            state = NewState(threadId, userId, now);
            db.ChatParticipantStates.Add(state);
        }

        var current = state.LastReadMessageId is { } currentId
            ? await db.ChatMessages.AsNoTracking()
                .Where(m => m.Id == currentId)
                .Select(m => new { m.Id, m.CreatedAt })
                .FirstOrDefaultAsync(ct)
            : null;

        var marker = current is not null
            && (current.CreatedAt > target.CreatedAt
                || (current.CreatedAt == target.CreatedAt && current.Id.CompareTo(target.Id) > 0))
            ? current
            : target;

        state.LastReadMessageId = marker.Id;
        state.LastReadAt = now;
        state.UpdatedAt = now;
        state.UnreadCount = await db.ChatMessages.AsNoTracking()
            .CountAsync(m => m.ThreadId == threadId
                && !m.IsDeleted
                && m.SenderId != userId
                && (m.CreatedAt > marker.CreatedAt
                    || (m.CreatedAt == marker.CreatedAt && m.Id.CompareTo(marker.Id) > 0)), ct);

        await db.SaveChangesAsync(ct);
        return new ChatReadResultDto(marker.Id, state.UnreadCount, await GetTotalUnreadAsync(userId, ct));
    }

    public Task<int> GetTotalUnreadAsync(Guid userId, CancellationToken ct = default) =>
        (from state in db.ChatParticipantStates.AsNoTracking()
         join thread in db.ChatThreads.AsNoTracking() on state.ThreadId equals thread.Id
         where state.UserId == userId && !thread.IsDeleted
         select state.UnreadCount).SumAsync(ct);

    public async Task<IReadOnlyList<Guid>> GetPartnerIdsAsync(Guid userId, CancellationToken ct = default) =>
        await db.ChatThreads.AsNoTracking()
            .Where(t => !t.IsDeleted && (t.CustomerId == userId || t.SupplierId == userId))
            .Select(t => t.CustomerId == userId ? t.SupplierId : t.CustomerId)
            .Distinct()
            .ToArrayAsync(ct);

    public async Task<IReadOnlyList<Guid>> FilterPartnersAsync(Guid userId, IReadOnlyList<Guid> candidateIds,
        CancellationToken ct = default) =>
        await db.ChatThreads.AsNoTracking()
            .Where(t => !t.IsDeleted
                && ((t.CustomerId == userId && candidateIds.Contains(t.SupplierId))
                    || (t.SupplierId == userId && candidateIds.Contains(t.CustomerId))))
            .Select(t => t.CustomerId == userId ? t.SupplierId : t.CustomerId)
            .Distinct()
            .ToArrayAsync(ct);

    public async Task<IReadOnlyList<ChatPresenceRecordDto>> GetPresenceRecordsAsync(IReadOnlyList<Guid> userIds,
        CancellationToken ct = default) =>
        await db.UserPresences.AsNoTracking()
            .Where(p => userIds.Contains(p.UserId))
            .Select(p => new ChatPresenceRecordDto(p.UserId, p.LastSeenAt, p.ShowActiveStatus))
            .ToArrayAsync(ct);

    public Task SetLastSeenAsync(Guid userId, DateTime lastSeenAt, CancellationToken ct = default) =>
        UpsertPresenceAsync(userId, p => p.LastSeenAt = lastSeenAt, ct);

    public Task SetShowActiveStatusAsync(Guid userId, bool show, CancellationToken ct = default) =>
        UpsertPresenceAsync(userId, p => p.ShowActiveStatus = show, ct);

    private async Task UpsertPresenceAsync(Guid userId, Action<UserPresence> apply, CancellationToken ct)
    {
        var presence = await db.UserPresences.FirstOrDefaultAsync(p => p.UserId == userId, ct);
        if (presence is null)
        {
            presence = new UserPresence { UserId = userId };
            db.UserPresences.Add(presence);
        }

        apply(presence);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException failure) when (IsUniqueViolation(failure))
        {
            // A concurrent connection created the row first; apply the change on top of it.
            db.ChangeTracker.Clear();
            var stored = await db.UserPresences.FirstAsync(p => p.UserId == userId, ct);
            apply(stored);
            await db.SaveChangesAsync(ct);
        }
    }

    private Task<Guid?> FindThreadIdAsync(Guid customerId, Guid shopId, CancellationToken ct) =>
        db.ChatThreads.AsNoTracking()
            .Where(t => t.CustomerId == customerId && t.ShopId == shopId && !t.IsDeleted)
            .Select(t => (Guid?)t.Id)
            .FirstOrDefaultAsync(ct);

    private IQueryable<ThreadRow> ThreadRows(Guid viewerId) =>
        from thread in db.ChatThreads.AsNoTracking()
        join shop in db.Shops.AsNoTracking() on thread.ShopId equals shop.Id
        join customer in db.Users.AsNoTracking() on thread.CustomerId equals customer.Id
        join supplier in db.Users.AsNoTracking() on thread.SupplierId equals supplier.Id
        where !thread.IsDeleted && (thread.CustomerId == viewerId || thread.SupplierId == viewerId)
        select new ThreadRow
        {
            Id = thread.Id,
            CustomerId = thread.CustomerId,
            SupplierId = thread.SupplierId,
            ShopId = shop.Id,
            ShopSlug = shop.Slug,
            ShopName = shop.Name,
            ShopLogoUrl = shop.LogoUrl,
            SupplierAvatarUrl = supplier.AvatarUrl,
            CustomerName = customer.FullName,
            CustomerEmail = customer.Email,
            CustomerAvatarUrl = customer.AvatarUrl,
            LastMessageId = thread.LastMessageId,
            LastMessagePreview = thread.LastMessagePreview,
            LastMessageSenderId = thread.LastMessageSenderId,
            LastActivityAt = thread.LastActivityAt,
            BlockedByUserId = thread.BlockedByUserId,
            UnreadCount = db.ChatParticipantStates
                .Where(s => s.ThreadId == thread.Id && s.UserId == viewerId)
                .Select(s => s.UnreadCount)
                .FirstOrDefault(),
            PartnerLastReadMessageId = db.ChatParticipantStates
                .Where(s => s.ThreadId == thread.Id && s.UserId != viewerId)
                .Select(s => s.LastReadMessageId)
                .FirstOrDefault()
        };

    private static ChatThreadResponse ToThreadResponse(ThreadRow row, Guid viewerId)
    {
        var viewerIsCustomer = row.CustomerId == viewerId;
        var partner = viewerIsCustomer
            ? new ChatPartnerResponse(
                row.SupplierId,
                string.IsNullOrWhiteSpace(row.ShopName) ? DefaultShopName : row.ShopName,
                row.ShopLogoUrl ?? row.SupplierAvatarUrl,
                ChatSenderRole.Supplier,
                row.ShopId,
                row.ShopSlug,
                null)
            : new ChatPartnerResponse(
                row.CustomerId,
                !string.IsNullOrWhiteSpace(row.CustomerName) ? row.CustomerName
                    : row.CustomerEmail?.Split('@')[0] ?? DefaultCustomerName,
                row.CustomerAvatarUrl,
                ChatSenderRole.Customer,
                row.ShopId,
                row.ShopSlug,
                null);

        return new ChatThreadResponse(
            row.Id,
            viewerIsCustomer ? ChatSenderRole.Customer : ChatSenderRole.Supplier,
            partner,
            row.LastMessagePreview,
            row.LastMessageSenderId,
            row.LastActivityAt,
            row.UnreadCount,
            row.PartnerLastReadMessageId,
            row.BlockedByUserId != null,
            row.BlockedByUserId == viewerId);
    }

    private async Task<IReadOnlyDictionary<Guid, IReadOnlyList<ChatAttachmentResponse>>> LoadAttachmentsAsync(
        IReadOnlyList<Guid> messageIds, CancellationToken ct)
    {
        if (messageIds.Count == 0)
            return new Dictionary<Guid, IReadOnlyList<ChatAttachmentResponse>>();

        var rows = await db.ChatAttachments.AsNoTracking()
            .Where(a => a.MessageId != null && messageIds.Contains(a.MessageId.Value) && !a.IsDeleted)
            .OrderBy(a => a.SortOrder)
            .ToListAsync(ct);

        return rows
            .GroupBy(a => a.MessageId!.Value)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<ChatAttachmentResponse>)g.Select(ToAttachmentResponse).ToArray());
    }

    private static ChatMessageResponse ToMessageResponse(ChatMessage message,
        IReadOnlyDictionary<Guid, IReadOnlyList<ChatAttachmentResponse>> attachments)
    {
        var recalled = message.RecalledAt is not null;
        var product = !recalled && message.ProductId is { } productId
            ? new ChatProductCardResponse(productId, message.ProductName ?? string.Empty,
                message.ProductImageUrl, message.ProductPrice)
            : null;

        return new ChatMessageResponse(
            message.Id,
            message.ThreadId,
            message.SenderId,
            message.SenderRole,
            message.Type,
            recalled ? null : message.Body,
            message.ClientMessageId,
            recalled,
            message.CreatedAt,
            product,
            recalled ? [] : attachments.GetValueOrDefault(message.Id) ?? [],
            message.EditedAt != null,
            message.EditedAt);
    }

    private static ChatAttachmentResponse ToAttachmentResponse(ChatAttachment attachment) =>
        new(
            attachment.Id,
            attachment.Kind,
            attachment.Kind == ChatAttachmentKind.Image ? attachment.Url : null,
            attachment.FileName,
            attachment.MimeType,
            attachment.SizeBytes,
            attachment.Width,
            attachment.Height);

    private static ChatParticipantState NewState(Guid threadId, Guid userId, DateTime now) =>
        new()
        {
            Id = Guid.NewGuid(),
            ThreadId = threadId,
            UserId = userId,
            CreatedAt = now,
            UpdatedAt = now
        };

    private static int IndexOf(IReadOnlyList<Guid> ids, Guid id)
    {
        for (var index = 0; index < ids.Count; index++)
        {
            if (ids[index] == id)
                return index;
        }

        return ids.Count;
    }

    private static bool IsUniqueViolation(DbUpdateException failure)
    {
        for (Exception? current = failure; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgres && postgres.SqlState == PostgresErrorCodes.UniqueViolation)
                return true;
        }

        return false;
    }

    private sealed class ThreadRow
    {
        public Guid Id { get; init; }
        public Guid CustomerId { get; init; }
        public Guid SupplierId { get; init; }
        public Guid ShopId { get; init; }
        public string? ShopSlug { get; init; }
        public string? ShopName { get; init; }
        public string? ShopLogoUrl { get; init; }
        public string? SupplierAvatarUrl { get; init; }
        public string? CustomerName { get; init; }
        public string? CustomerEmail { get; init; }
        public string? CustomerAvatarUrl { get; init; }
        public Guid? LastMessageId { get; init; }
        public string? LastMessagePreview { get; init; }
        public Guid? LastMessageSenderId { get; init; }
        public DateTime LastActivityAt { get; init; }
        public Guid? BlockedByUserId { get; init; }
        public int UnreadCount { get; init; }
        public Guid? PartnerLastReadMessageId { get; init; }
    }
}

