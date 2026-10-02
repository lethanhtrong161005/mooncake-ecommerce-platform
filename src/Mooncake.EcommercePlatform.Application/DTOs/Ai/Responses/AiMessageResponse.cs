namespace Mooncake.EcommercePlatform.Application.DTOs.Ai.Responses;

using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>AI message item in a conversation.</summary>
public record AiMessageResponse(
    long Id,
    long ConversationId,
    AiMessageRole Role,
    string Content,
    string Metadata,
    DateTime CreatedAtUtc
);
