namespace Mooncake.EcommercePlatform.Application.DTOs.Chat.Persistence;

using Mooncake.EcommercePlatform.Application.DTOs.Chat.Responses;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Keyset position; ScopeId is the thread for message cursors and empty for thread-list cursors.</summary>
public sealed record ChatCursorDto(Guid UserId, Guid ScopeId, DateTime AtUtc, Guid Id);

/// <summary>Minimal membership view of a thread used for authorization decisions.</summary>
public sealed record ChatThreadAccessDto(
    Guid ThreadId,
    Guid CustomerId,
    Guid SupplierId,
    Guid ShopId,
    Guid? BlockedByUserId = null)
{
    public bool IsMember(Guid userId) => userId == CustomerId || userId == SupplierId;

    public Guid PartnerOf(Guid userId) => userId == CustomerId ? SupplierId : CustomerId;

    public ChatSenderRole RoleOf(Guid userId) =>
        userId == CustomerId ? ChatSenderRole.Customer : ChatSenderRole.Supplier;
}

public sealed record ChatShopOwnerDto(Guid ShopId, Guid OwnerUserId);

public sealed record ChatProductSnapshotDto(Guid ProductId, string Name, string? ImageUrl, decimal Price);

/// <summary>Thread row before presence enrichment.</summary>
public sealed record ChatThreadPageResultDto(
    IReadOnlyList<ChatThreadResponse> Items,
    bool HasMore,
    ChatCursorDto? NextCursor);

public sealed record ChatMessagePageResultDto(
    IReadOnlyList<ChatMessageResponse> Items,
    bool HasMore,
    ChatCursorDto? NextCursor);

/// <summary>Result of storing a sanitized file in object storage.</summary>
public sealed record ChatStoredFileDto(
    ChatAttachmentKind Kind,
    string Url,
    string PublicId,
    string FileName,
    string MimeType,
    long SizeBytes,
    int? Width,
    int? Height);

public sealed record ChatPresenceRecordDto(Guid UserId, DateTime? LastSeenAt, bool ShowActiveStatus);

public sealed record ChatReadResultDto(Guid LastReadMessageId, int UnreadCount, int TotalUnread);

