namespace Mooncake.EcommercePlatform.Application.DTOs.Notifications.Responses;

/// <summary>Notification projection returned to callers.</summary>
public record NotificationResponse(
    long Id,
    long UserId,
    string Type,
    string Title,
    string? Body,
    string? EntityType,
    long? EntityId,
    bool IsRead,
    DateTime? ReadAtUtc,
    DateTime CreatedAtUtc
);
