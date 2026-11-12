namespace Mooncake.EcommercePlatform.Application.DTOs.Chat.Responses;

using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Live or last-seen activity of a chat participant.</summary>
public sealed record ChatPresenceResponse(Guid UserId, bool IsOnline, DateTime? LastSeenAt);

/// <summary>The other participant from the viewer's perspective.</summary>
public sealed record ChatPartnerResponse(
    Guid UserId,
    string DisplayName,
    string? AvatarUrl,
    ChatSenderRole Role,
    Guid ShopId,
    string? ShopSlug,
    ChatPresenceResponse? Presence);

/// <summary>Conversation summary for the thread list and header.</summary>
public sealed record ChatThreadResponse(
    Guid Id,
    ChatSenderRole ViewerRole,
    ChatPartnerResponse Partner,
    string? LastMessagePreview,
    Guid? LastMessageSenderId,
    DateTime LastActivityAt,
    int UnreadCount,
    Guid? PartnerLastReadMessageId,
    bool IsBlocked = false,
    bool IsBlockedByMe = false);

public sealed record ChatThreadPageResponse(
    IReadOnlyList<ChatThreadResponse> Items,
    bool HasMore,
    string? NextCursor);

public sealed record ChatAttachmentResponse(
    Guid Id,
    ChatAttachmentKind Kind,
    string? Url,
    string FileName,
    string MimeType,
    long SizeBytes,
    int? Width,
    int? Height);

public sealed record ChatProductCardResponse(
    Guid ProductId,
    string Name,
    string? ImageUrl,
    decimal? Price);

public sealed record ChatMessageResponse(
    Guid Id,
    Guid ThreadId,
    Guid SenderId,
    ChatSenderRole SenderRole,
    ChatMessageType Type,
    string? Body,
    Guid ClientMessageId,
    bool IsRecalled,
    DateTime CreatedAt,
    ChatProductCardResponse? Product,
    IReadOnlyList<ChatAttachmentResponse> Attachments,
    bool IsEdited = false,
    DateTime? EditedAt = null);

/// <summary>Messages ordered oldest to newest; the cursor walks further into the past.</summary>
public sealed record ChatMessagePageResponse(
    IReadOnlyList<ChatMessageResponse> Items,
    bool HasMore,
    string? NextCursor);

public sealed record ChatMessageEditedResponse(Guid ThreadId, Guid MessageId, string Body, DateTime EditedAt);

public sealed record ChatMessageRecalledResponse(Guid ThreadId, Guid MessageId, DateTime RecalledAt);

public sealed record ChatUnreadCountResponse(int Total);

public sealed record ChatReadResponse(Guid ThreadId, Guid LastReadMessageId, int UnreadCount, int TotalUnread);

public sealed record ChatDownloadUrlResponse(string Url, string FileName, DateTime ExpiresAt);

public sealed record ChatPresenceSettingsResponse(bool ShowActiveStatus);
