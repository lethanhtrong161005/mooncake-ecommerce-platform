namespace Mooncake.EcommercePlatform.Application.DTOs.Ai.Responses;

/// <summary>AI conversation summary returned to callers.</summary>
public record AiConversationResponse(
    long Id,
    long UserId,
    string? Title,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    List<AiMessageResponse>? Messages = null
);
