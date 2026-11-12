namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Chat.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Chat.Responses;

/// <summary>Use cases for private customer-supplier messaging.</summary>
public interface IChatService
{
    Task<ChatThreadResponse> OpenThreadAsync(Guid userId, OpenChatThreadRequest request, CancellationToken ct = default);

    Task<ChatThreadPageResponse> GetThreadsAsync(Guid userId, ChatPageQuery query, CancellationToken ct = default);

    Task<ChatThreadResponse> GetThreadAsync(Guid userId, Guid threadId, CancellationToken ct = default);

    Task<ChatMessagePageResponse> GetMessagesAsync(Guid userId, Guid threadId, ChatPageQuery query,
        CancellationToken ct = default);

    Task<ChatMessageResponse> SendMessageAsync(Guid userId, Guid threadId, SendChatMessageRequest request,
        CancellationToken ct = default);

    Task<ChatMessageEditedResponse> EditMessageAsync(Guid userId, Guid messageId, EditChatMessageRequest request,
        CancellationToken ct = default);

    Task<ChatMessageRecalledResponse> RecallMessageAsync(Guid userId, Guid messageId, CancellationToken ct = default);

    Task<ChatThreadResponse> BlockThreadAsync(Guid userId, Guid threadId, CancellationToken ct = default);

    Task<ChatThreadResponse> UnblockThreadAsync(Guid userId, Guid threadId, CancellationToken ct = default);

    Task<IReadOnlyList<ChatAttachmentResponse>> UploadAttachmentsAsync(Guid userId,
        IReadOnlyList<ChatUploadItem> files, CancellationToken ct = default);

    Task<ChatDownloadUrlResponse> GetDownloadUrlAsync(Guid userId, Guid attachmentId, CancellationToken ct = default);

    Task<ChatReadResponse> MarkReadAsync(Guid userId, Guid threadId, MarkChatReadRequest request,
        CancellationToken ct = default);

    Task<ChatUnreadCountResponse> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);

    Task<IReadOnlyList<ChatPresenceResponse>> GetPresenceAsync(Guid userId, ChatPresenceQuery query,
        CancellationToken ct = default);

    Task<ChatPresenceSettingsResponse> UpdatePresenceSettingsAsync(Guid userId,
        UpdateChatPresenceSettingsRequest request, CancellationToken ct = default);

    Task<bool> IsThreadMemberAsync(Guid userId, Guid threadId, CancellationToken ct = default);

    Task NotifyUserOnlineAsync(Guid userId, CancellationToken ct = default);

    Task NotifyUserOfflineAsync(Guid userId, CancellationToken ct = default);
}

