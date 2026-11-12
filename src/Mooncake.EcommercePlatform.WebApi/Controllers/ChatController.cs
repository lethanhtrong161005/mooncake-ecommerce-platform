namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.DTOs.Chat.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Wishlist.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.WebApi.DTOs.Requests;
using Mooncake.EcommercePlatform.WebApi.Extensions;

/// <summary>Private real-time messaging endpoints between customers and suppliers.</summary>
[Authorize]
[Route("api/v1/chat")]
public sealed class ChatController(IChatService chatService) : BaseApiController
{
    /// <summary>Opens (or retrieves existing) chat thread between current user and target shop.</summary>
    [HttpPost("threads")]
    public async Task<IActionResult> OpenThreadAsync(
        [FromBody] OpenChatThreadRequest request,
        CancellationToken cancellationToken = default) =>
        Success(
            await chatService.OpenThreadAsync(User.GetRequiredUserId(), request, cancellationToken),
            ChatConstants.ThreadOpened);

    /// <summary>Returns a cursor-paginated list of the current user's chat conversations.</summary>
    [HttpGet("threads")]
    public async Task<IActionResult> GetThreadsAsync(
        [FromQuery] ChatPageQuery query,
        CancellationToken cancellationToken = default) =>
        Success(
            await chatService.GetThreadsAsync(User.GetRequiredUserId(), query, cancellationToken),
            ChatConstants.ThreadsListed);

    /// <summary>Retrieves details of a specific thread including partner presence.</summary>
    [HttpGet("threads/{threadId:guid}")]
    public async Task<IActionResult> GetThreadAsync(
        [FromRoute, NonEmptyGuid(ErrorMessage = ChatConstants.InvalidThreadId)] Guid threadId,
        CancellationToken cancellationToken = default) =>
        Success(
            await chatService.GetThreadAsync(User.GetRequiredUserId(), threadId, cancellationToken),
            ChatConstants.ThreadFetched);

    /// <summary>Returns a cursor-paginated page of messages in a thread, ordered oldest to newest.</summary>
    [HttpGet("threads/{threadId:guid}/messages")]
    public async Task<IActionResult> GetMessagesAsync(
        [FromRoute, NonEmptyGuid(ErrorMessage = ChatConstants.InvalidThreadId)] Guid threadId,
        [FromQuery] ChatPageQuery query,
        CancellationToken cancellationToken = default) =>
        Success(
            await chatService.GetMessagesAsync(User.GetRequiredUserId(), threadId, query, cancellationToken),
            ChatConstants.MessagesListed);

    /// <summary>Sends a message containing text, a product card, and/or attachments.</summary>
    [HttpPost("threads/{threadId:guid}/messages")]
    [EnableRateLimiting(ChatConstants.SendRatePolicy)]
    public async Task<IActionResult> SendMessageAsync(
        [FromRoute, NonEmptyGuid(ErrorMessage = ChatConstants.InvalidThreadId)] Guid threadId,
        [FromBody] SendChatMessageRequest request,
        CancellationToken cancellationToken = default) =>
        Created(
            await chatService.SendMessageAsync(User.GetRequiredUserId(), threadId, request, cancellationToken),
            ChatConstants.MessageSent);

    /// <summary>Edits text of a sent message within the allowed 5-minute window.</summary>
    [HttpPatch("messages/{messageId:guid}")]
    public async Task<IActionResult> EditMessageAsync(
        [FromRoute, NonEmptyGuid(ErrorMessage = ChatConstants.InvalidMessageId)] Guid messageId,
        [FromBody] EditChatMessageRequest request,
        CancellationToken cancellationToken = default) =>
        Success(
            await chatService.EditMessageAsync(User.GetRequiredUserId(), messageId, request, cancellationToken),
            ChatConstants.MessageEdited);

    /// <summary>Recalls a message sent by the caller (soft delete with content hidden).</summary>
    [HttpDelete("messages/{messageId:guid}")]
    public async Task<IActionResult> RecallMessageAsync(
        [FromRoute, NonEmptyGuid(ErrorMessage = ChatConstants.InvalidMessageId)] Guid messageId,
        CancellationToken cancellationToken = default) =>
        Success(
            await chatService.RecallMessageAsync(User.GetRequiredUserId(), messageId, cancellationToken),
            ChatConstants.MessageRecalled);

    /// <summary>Advances the caller's read marker for the conversation.</summary>
    [HttpPost("threads/{threadId:guid}/read")]
    public async Task<IActionResult> MarkReadAsync(
        [FromRoute, NonEmptyGuid(ErrorMessage = ChatConstants.InvalidThreadId)] Guid threadId,
        [FromBody] MarkChatReadRequest request,
        CancellationToken cancellationToken = default) =>
        Success(
            await chatService.MarkReadAsync(User.GetRequiredUserId(), threadId, request, cancellationToken),
            ChatConstants.ReadMarked);

    /// <summary>Blocks conversation so no new messages can be exchanged.</summary>
    [HttpPost("threads/{threadId:guid}/block")]
    public async Task<IActionResult> BlockThreadAsync(
        [FromRoute, NonEmptyGuid(ErrorMessage = ChatConstants.InvalidThreadId)] Guid threadId,
        CancellationToken cancellationToken = default) =>
        Success(
            await chatService.BlockThreadAsync(User.GetRequiredUserId(), threadId, cancellationToken),
            ChatConstants.ThreadBlocked);

    /// <summary>Unblocks a conversation previously blocked by the caller.</summary>
    [HttpPost("threads/{threadId:guid}/unblock")]
    public async Task<IActionResult> UnblockThreadAsync(
        [FromRoute, NonEmptyGuid(ErrorMessage = ChatConstants.InvalidThreadId)] Guid threadId,
        CancellationToken cancellationToken = default) =>
        Success(
            await chatService.UnblockThreadAsync(User.GetRequiredUserId(), threadId, cancellationToken),
            ChatConstants.ThreadUnblocked);

    /// <summary>Uploads up to 10 images or documents to be linked to a message.</summary>
    [HttpPost("attachments")]
    [EnableRateLimiting(ChatConstants.UploadRatePolicy)]
    [RequestSizeLimit(ChatConstants.MaxUploadRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = ChatConstants.MaxUploadRequestBytes)]
    public async Task<IActionResult> UploadAttachmentsAsync(
        [FromForm] ChatFileUploadRequest request,
        CancellationToken cancellationToken = default) =>
        Created(
            await chatService.UploadAttachmentsAsync(User.GetRequiredUserId(), request.ToUploadItems(), cancellationToken),
            ChatConstants.AttachmentsUploaded);

    /// <summary>Generates a temporary signed download URL for private documents.</summary>
    [HttpGet("attachments/{attachmentId:guid}/download")]
    public async Task<IActionResult> GetDownloadUrlAsync(
        [FromRoute, NonEmptyGuid(ErrorMessage = ChatConstants.InvalidAttachmentId)] Guid attachmentId,
        CancellationToken cancellationToken = default) =>
        Success(
            await chatService.GetDownloadUrlAsync(User.GetRequiredUserId(), attachmentId, cancellationToken),
            ChatConstants.DownloadUrlCreated);

    /// <summary>Returns total unread count across all conversations for badge display.</summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCountAsync(
        CancellationToken cancellationToken = default) =>
        Success(
            await chatService.GetUnreadCountAsync(User.GetRequiredUserId(), cancellationToken),
            ChatConstants.UnreadFetched);

    /// <summary>Returns live presence and last-seen timestamp for given partner user IDs.</summary>
    [HttpGet("presence")]
    public async Task<IActionResult> GetPresenceAsync(
        [FromQuery] ChatPresenceQuery query,
        CancellationToken cancellationToken = default) =>
        Success(
            await chatService.GetPresenceAsync(User.GetRequiredUserId(), query, cancellationToken),
            ChatConstants.PresenceFetched);

    /// <summary>Updates caller preference for sharing active status with chat partners.</summary>
    [HttpPut("presence/settings")]
    public async Task<IActionResult> UpdatePresenceSettingsAsync(
        [FromBody] UpdateChatPresenceSettingsRequest request,
        CancellationToken cancellationToken = default) =>
        Success(
            await chatService.UpdatePresenceSettingsAsync(User.GetRequiredUserId(), request, cancellationToken),
            ChatConstants.PresenceSettingsUpdated);
}

