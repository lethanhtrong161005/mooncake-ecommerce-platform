namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Chat.Persistence;
using Mooncake.EcommercePlatform.Application.DTOs.Chat.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Chat.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>
/// Orchestrates private customer-supplier chat. Input shape is validated by request DTOs;
/// this service only enforces rules that need persisted state (membership, ownership, idempotency).
/// </summary>
public sealed class ChatService(
    IChatRepository repository,
    IChatCursorProtector cursors,
    IChatRealtimePublisher realtime,
    IPresenceTracker presence,
    IChatAttachmentStorage storage,
    IDateTimeProvider clock) : IChatService
{
    public async Task<ChatThreadResponse> OpenThreadAsync(Guid userId, OpenChatThreadRequest request,
        CancellationToken ct = default)
    {
        var shop = await repository.FindActiveShopOwnerAsync(request.TargetId, ct)
            ?? throw new HttpException(404, ChatConstants.ShopNotFound);

        if (shop.OwnerUserId == userId)
            throw new HttpException(400, ChatConstants.CannotChatOwnShop);

        var threadId = await repository.GetOrCreateThreadAsync(userId, shop, clock.UtcNow, ct);
        return await GetThreadAsync(userId, threadId, ct);
    }

    public async Task<ChatThreadPageResponse> GetThreadsAsync(Guid userId, ChatPageQuery query,
        CancellationToken ct = default)
    {
        var after = query.Cursor is null ? null : cursors.Unprotect(userId, Guid.Empty, query.Cursor.Trim());
        var page = await repository.GetThreadPageAsync(userId, after, query.PageSize, ct);
        var items = await WithPresenceAsync(page.Items, ct);
        var next = page.HasMore && page.NextCursor is not null ? cursors.Protect(page.NextCursor) : null;
        return new ChatThreadPageResponse(items, page.HasMore, next);
    }

    public async Task<ChatThreadResponse> GetThreadAsync(Guid userId, Guid threadId, CancellationToken ct = default)
    {
        var thread = await repository.GetThreadAsync(threadId, userId, ct)
            ?? throw new HttpException(404, ChatConstants.ThreadNotFound);
        return (await WithPresenceAsync([thread], ct))[0];
    }

    public async Task<ChatMessagePageResponse> GetMessagesAsync(Guid userId, Guid threadId, ChatPageQuery query,
        CancellationToken ct = default)
    {
        await RequireMemberAsync(userId, threadId, ct);
        var before = query.Cursor is null ? null : cursors.Unprotect(userId, threadId, query.Cursor.Trim());
        var page = await repository.GetMessagePageAsync(userId, threadId, before, query.PageSize, ct);
        var next = page.HasMore && page.NextCursor is not null ? cursors.Protect(page.NextCursor) : null;
        return new ChatMessagePageResponse(page.Items, page.HasMore, next);
    }

    public async Task<ChatMessageResponse> SendMessageAsync(Guid userId, Guid threadId,
        SendChatMessageRequest request, CancellationToken ct = default)
    {
        var thread = await RequireMemberAsync(userId, threadId, ct);
        if (thread.BlockedByUserId != null)
            throw new HttpException(403, ChatConstants.ConversationBlocked);

        var existing = await repository.FindByClientMessageIdAsync(threadId, userId, request.ClientMessageId, ct);
        if (existing is not null)
            return existing;

        var product = request.ProductId is { } productId
            ? await repository.GetProductSnapshotAsync(productId, thread.ShopId, ct)
                ?? throw new HttpException(404, ChatConstants.ProductNotFound)
            : null;

        var body = request.NormalizedBody;
        var attachmentIds = request.NormalizedAttachmentIds;
        var message = new ChatMessage
        {
            ThreadId = threadId,
            SenderId = userId,
            SenderRole = thread.RoleOf(userId),
            Type = product is not null ? ChatMessageType.ProductCard
                : attachmentIds.Count > 0 ? ChatMessageType.Attachment
                : ChatMessageType.Text,
            Body = body,
            ClientMessageId = request.ClientMessageId,
            ProductId = product?.ProductId,
            ProductName = product?.Name,
            ProductImageUrl = product?.ImageUrl,
            ProductPrice = product?.Price,
            CreatedAt = clock.UtcNow,
            UpdatedAt = clock.UtcNow,
            CreatedBy = userId
        };

        var recipientId = thread.PartnerOf(userId);
        var created = await repository.AddMessageAsync(message, attachmentIds, BuildPreview(message), recipientId, ct)
            ?? await repository.FindByClientMessageIdAsync(threadId, userId, request.ClientMessageId, ct)
            ?? throw new HttpException(409, ChatConstants.AttachmentsUnavailable);

        await realtime.PublishToUsersAsync([userId, recipientId], ChatConstants.EventMessageCreated, created, ct);
        var recipientUnread = await repository.GetTotalUnreadAsync(recipientId, ct);
        await realtime.PublishToUsersAsync([recipientId], ChatConstants.EventUnreadChanged,
            new ChatUnreadCountResponse(recipientUnread), ct);

        return created;
    }

    public async Task<ChatMessageRecalledResponse> RecallMessageAsync(Guid userId, Guid messageId,
        CancellationToken ct = default)
    {
        var message = await repository.GetMessageAsync(messageId, ct)
            ?? throw new HttpException(404, ChatConstants.MessageNotFound);
        var thread = await RequireMemberAsync(userId, message.ThreadId, ct);

        if (message.SenderId != userId)
            throw new HttpException(403, ChatConstants.NotMessageOwner);

        var now = message.RecalledAt ?? clock.UtcNow;
        if (message.RecalledAt is null)
            await repository.RecallMessageAsync(message, now, ct);

        var result = new ChatMessageRecalledResponse(message.ThreadId, message.Id, now);
        await realtime.PublishToUsersAsync([thread.CustomerId, thread.SupplierId],
            ChatConstants.EventMessageRecalled, result, ct);
        return result;
    }

    public async Task<ChatMessageEditedResponse> EditMessageAsync(Guid userId, Guid messageId,
        EditChatMessageRequest request, CancellationToken ct = default)
    {
        var message = await repository.GetMessageAsync(messageId, ct)
            ?? throw new HttpException(404, ChatConstants.MessageNotFound);
        var thread = await RequireMemberAsync(userId, message.ThreadId, ct);

        if (message.SenderId != userId)
            throw new HttpException(403, ChatConstants.NotMessageOwner);

        if (message.RecalledAt is not null)
            throw new HttpException(400, ChatConstants.CannotEditRecalledMessage);

        var now = clock.UtcNow;
        if (now - message.CreatedAt > TimeSpan.FromMinutes(ChatConstants.EditWindowMinutes))
            throw new HttpException(400, ChatConstants.EditWindowExpired);

        var newBody = request.NormalizedBody;
        await repository.EditMessageAsync(message, newBody, now, ct);

        var result = new ChatMessageEditedResponse(message.ThreadId, message.Id, newBody, now);
        await realtime.PublishToUsersAsync([thread.CustomerId, thread.SupplierId],
            ChatConstants.EventMessageEdited, result, ct);

        return result;
    }

    public async Task<ChatThreadResponse> BlockThreadAsync(Guid userId, Guid threadId, CancellationToken ct = default)
    {
        var thread = await RequireMemberAsync(userId, threadId, ct);
        var now = clock.UtcNow;
        await repository.SetThreadBlockAsync(threadId, userId, now, ct);

        var updated = await GetThreadAsync(userId, threadId, ct);
        await realtime.PublishToUsersAsync([thread.CustomerId, thread.SupplierId],
            ChatConstants.EventThreadUpdated, updated, ct);

        return updated;
    }

    public async Task<ChatThreadResponse> UnblockThreadAsync(Guid userId, Guid threadId, CancellationToken ct = default)
    {
        var thread = await RequireMemberAsync(userId, threadId, ct);
        if (thread.BlockedByUserId != null && thread.BlockedByUserId != userId)
            throw new HttpException(403, ChatConstants.CannotUnblockPartnerBlock);

        var now = clock.UtcNow;
        await repository.SetThreadBlockAsync(threadId, null, now, ct);

        var updated = await GetThreadAsync(userId, threadId, ct);
        await realtime.PublishToUsersAsync([thread.CustomerId, thread.SupplierId],
            ChatConstants.EventThreadUpdated, updated, ct);

        return updated;
    }

    public async Task<IReadOnlyList<ChatAttachmentResponse>> UploadAttachmentsAsync(Guid userId,
        IReadOnlyList<ChatUploadItem> files, CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        var entities = new List<ChatAttachment>(files.Count);
        foreach (var file in files)
        {
            var stored = await storage.StoreAsync(file, ct);
            entities.Add(new ChatAttachment
            {
                UploaderId = userId,
                Kind = stored.Kind,
                Url = stored.Url,
                PublicId = stored.PublicId,
                FileName = stored.FileName,
                MimeType = stored.MimeType,
                SizeBytes = stored.SizeBytes,
                Width = stored.Width,
                Height = stored.Height,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = userId
            });
        }

        return await repository.AddAttachmentsAsync(entities, ct);
    }

    public async Task<ChatDownloadUrlResponse> GetDownloadUrlAsync(Guid userId, Guid attachmentId,
        CancellationToken ct = default)
    {
        var attachment = await repository.GetLinkedAttachmentForMemberAsync(attachmentId, userId, ct)
            ?? throw new HttpException(404, ChatConstants.AttachmentNotFound);

        var expiresAt = clock.UtcNow.AddMinutes(ChatConstants.DownloadUrlLifetimeMinutes);
        var url = attachment.Kind == ChatAttachmentKind.Image
            ? attachment.Url
            : storage.CreateDownloadUrl(attachment, expiresAt);
        return new ChatDownloadUrlResponse(url, attachment.FileName, expiresAt);
    }

    public async Task<ChatReadResponse> MarkReadAsync(Guid userId, Guid threadId, MarkChatReadRequest request,
        CancellationToken ct = default)
    {
        var thread = await RequireMemberAsync(userId, threadId, ct);
        var result = await repository.MarkReadAsync(threadId, userId, request.MessageId, clock.UtcNow, ct)
            ?? throw new HttpException(404, ChatConstants.MessageNotFound);

        await realtime.PublishToUsersAsync([thread.PartnerOf(userId), userId], ChatConstants.EventReadUpdated,
            new { threadId, userId, lastReadMessageId = result.LastReadMessageId }, ct);
        await realtime.PublishToUsersAsync([userId], ChatConstants.EventUnreadChanged,
            new ChatUnreadCountResponse(result.TotalUnread), ct);

        return new ChatReadResponse(threadId, result.LastReadMessageId, result.UnreadCount, result.TotalUnread);
    }

    public async Task<ChatUnreadCountResponse> GetUnreadCountAsync(Guid userId, CancellationToken ct = default) =>
        new(await repository.GetTotalUnreadAsync(userId, ct));

    public async Task<IReadOnlyList<ChatPresenceResponse>> GetPresenceAsync(Guid userId, ChatPresenceQuery query,
        CancellationToken ct = default)
    {
        // Presence is only disclosed to users who share a conversation.
        var partners = await repository.FilterPartnersAsync(userId, query.ToUserIds(), ct);
        return await BuildPresenceAsync(partners, ct);
    }

    public async Task<ChatPresenceSettingsResponse> UpdatePresenceSettingsAsync(Guid userId,
        UpdateChatPresenceSettingsRequest request, CancellationToken ct = default)
    {
        var show = request.ShowActiveStatus!.Value;
        await repository.SetShowActiveStatusAsync(userId, show, ct);
        await PublishPresenceAsync(userId, ct);
        return new ChatPresenceSettingsResponse(show);
    }

    public async Task<bool> IsThreadMemberAsync(Guid userId, Guid threadId, CancellationToken ct = default) =>
        (await repository.GetThreadAccessAsync(threadId, ct))?.IsMember(userId) == true;

    public Task NotifyUserOnlineAsync(Guid userId, CancellationToken ct = default) =>
        PublishPresenceAsync(userId, ct);

    public async Task NotifyUserOfflineAsync(Guid userId, CancellationToken ct = default)
    {
        await repository.SetLastSeenAsync(userId, clock.UtcNow, ct);
        await PublishPresenceAsync(userId, ct);
    }

    private async Task<ChatThreadAccessDto> RequireMemberAsync(Guid userId, Guid threadId, CancellationToken ct)
    {
        var thread = await repository.GetThreadAccessAsync(threadId, ct);

        // Non-members get 404 so thread existence is not disclosed.
        return thread is not null && thread.IsMember(userId)
            ? thread
            : throw new HttpException(404, ChatConstants.ThreadNotFound);
    }

    private async Task PublishPresenceAsync(Guid userId, CancellationToken ct)
    {
        var partners = await repository.GetPartnerIdsAsync(userId, ct);
        if (partners.Count == 0)
            return;

        var state = (await BuildPresenceAsync([userId], ct))[0];
        await realtime.PublishToUsersAsync(partners, ChatConstants.EventPresenceChanged, state, ct);
    }

    private async Task<IReadOnlyList<ChatPresenceResponse>> BuildPresenceAsync(IReadOnlyList<Guid> userIds,
        CancellationToken ct)
    {
        if (userIds.Count == 0)
            return [];

        var records = (await repository.GetPresenceRecordsAsync(userIds, ct)).ToDictionary(r => r.UserId);
        return userIds
            .Select(id =>
            {
                var record = records.GetValueOrDefault(id);
                var visible = record?.ShowActiveStatus ?? true;
                return new ChatPresenceResponse(
                    id,
                    visible && presence.IsOnline(id),
                    visible ? record?.LastSeenAt : null);
            })
            .ToArray();
    }

    private async Task<IReadOnlyList<ChatThreadResponse>> WithPresenceAsync(
        IReadOnlyList<ChatThreadResponse> threads, CancellationToken ct)
    {
        var states = (await BuildPresenceAsync(threads.Select(t => t.Partner.UserId).Distinct().ToArray(), ct))
            .ToDictionary(p => p.UserId);
        return threads
            .Select(t => t with { Partner = t.Partner with { Presence = states.GetValueOrDefault(t.Partner.UserId) } })
            .ToArray();
    }

    private static string BuildPreview(ChatMessage message)
    {
        if (message.Body is { } body)
            return body.Length <= ChatConstants.PreviewLength ? body : $"{body[..ChatConstants.PreviewLength]}…";

        return message.Type == ChatMessageType.ProductCard
            ? ChatConstants.ProductPreview
            : ChatConstants.AttachmentPreview;
    }
}

