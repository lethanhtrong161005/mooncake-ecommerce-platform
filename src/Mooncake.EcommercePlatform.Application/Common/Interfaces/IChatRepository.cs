namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Chat.Persistence;
using Mooncake.EcommercePlatform.Application.DTOs.Chat.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Chat.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Persistence port for customer-supplier chat.</summary>
public interface IChatRepository
{
    Task<ChatShopOwnerDto?> FindActiveShopOwnerAsync(Guid shopId, CancellationToken ct = default);

    /// <summary>Returns the unique thread of the pair, creating it (and participant states) when missing.</summary>
    Task<Guid> GetOrCreateThreadAsync(Guid customerId, ChatShopOwnerDto shop, DateTime now, CancellationToken ct = default);

    Task<ChatThreadAccessDto?> GetThreadAccessAsync(Guid threadId, CancellationToken ct = default);

    Task<ChatThreadResponse?> GetThreadAsync(Guid threadId, Guid viewerId, CancellationToken ct = default);

    Task<ChatThreadPageResultDto> GetThreadPageAsync(Guid userId, ChatCursorDto? after, int pageSize,
        CancellationToken ct = default);

    Task<ChatMessagePageResultDto> GetMessagePageAsync(Guid userId, Guid threadId, ChatCursorDto? before,
        int pageSize, CancellationToken ct = default);

    Task<ChatProductSnapshotDto?> GetProductSnapshotAsync(Guid productId, Guid shopId, CancellationToken ct = default);

    Task<ChatMessageResponse?> FindByClientMessageIdAsync(Guid threadId, Guid senderId, Guid clientMessageId,
        CancellationToken ct = default);

    /// <summary>Atomically inserts a message, links pending attachments, and updates thread and unread state.</summary>
    Task<ChatMessageResponse?> AddMessageAsync(ChatMessage message, IReadOnlyList<Guid> attachmentIds,
        string preview, Guid recipientId, CancellationToken ct = default);

    Task<IReadOnlyList<ChatAttachmentResponse>> AddAttachmentsAsync(IReadOnlyList<ChatAttachment> attachments,
        CancellationToken ct = default);

    /// <summary>Returns the attachment only when the user belongs to the thread of its message.</summary>
    Task<ChatAttachment?> GetLinkedAttachmentForMemberAsync(Guid attachmentId, Guid userId,
        CancellationToken ct = default);

    Task<ChatMessage?> GetMessageAsync(Guid messageId, CancellationToken ct = default);

    Task EditMessageAsync(ChatMessage message, string newBody, DateTime now, CancellationToken ct = default);

    Task RecallMessageAsync(ChatMessage message, DateTime now, CancellationToken ct = default);

    Task SetThreadBlockAsync(Guid threadId, Guid? blockedByUserId, DateTime now, CancellationToken ct = default);

    Task<ChatReadResultDto?> MarkReadAsync(Guid threadId, Guid userId, Guid messageId, DateTime now,
        CancellationToken ct = default);

    Task<int> GetTotalUnreadAsync(Guid userId, CancellationToken ct = default);

    Task<IReadOnlyList<Guid>> GetPartnerIdsAsync(Guid userId, CancellationToken ct = default);

    Task<IReadOnlyList<Guid>> FilterPartnersAsync(Guid userId, IReadOnlyList<Guid> candidateIds,
        CancellationToken ct = default);

    Task<IReadOnlyList<ChatPresenceRecordDto>> GetPresenceRecordsAsync(IReadOnlyList<Guid> userIds,
        CancellationToken ct = default);

    Task SetLastSeenAsync(Guid userId, DateTime lastSeenAt, CancellationToken ct = default);

    Task SetShowActiveStatusAsync(Guid userId, bool show, CancellationToken ct = default);
}

/// <summary>Protects keyset cursors so they are opaque, user-scoped, and short-lived.</summary>
public interface IChatCursorProtector
{
    string Protect(ChatCursorDto cursor);

    ChatCursorDto Unprotect(Guid expectedUserId, Guid expectedScopeId, string token);
}

/// <summary>Pushes chat events to connected users without exposing SignalR to the application layer.</summary>
public interface IChatRealtimePublisher
{
    Task PublishToUsersAsync(IReadOnlyList<Guid> userIds, string eventName, object payload,
        CancellationToken ct = default);
}

/// <summary>Tracks live hub connections per user (process-local; use a Redis backplane to scale out).</summary>
public interface IPresenceTracker
{
    /// <summary>Registers a connection and returns true when it is the user's first one.</summary>
    bool Connect(Guid userId, string connectionId);

    /// <summary>Removes a connection and returns true when the user has no connections left.</summary>
    bool Disconnect(Guid userId, string connectionId);

    bool IsOnline(Guid userId);
}

/// <summary>Sanitizes uploads, stores them in object storage, and signs private download links.</summary>
public interface IChatAttachmentStorage
{
    Task<ChatStoredFileDto> StoreAsync(ChatUploadItem item, CancellationToken ct = default);

    string CreateDownloadUrl(ChatAttachment attachment, DateTime expiresAt);
}

