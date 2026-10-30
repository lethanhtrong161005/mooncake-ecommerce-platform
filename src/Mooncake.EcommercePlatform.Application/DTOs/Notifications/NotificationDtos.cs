namespace Mooncake.EcommercePlatform.Application.DTOs.Notifications;

using System.ComponentModel.DataAnnotations;

public sealed record NotificationItemDto(
    Guid Id,
    string Type,
    string Category,
    string Title,
    string Content,
    Guid? ReferenceId,
    string? ReferenceType,
    string? ActionUrl,
    string Priority,
    bool IsRead,
    DateTime? ReadAt,
    DateTime CreatedAt);

public sealed record NotificationInboxResponse(
    IReadOnlyList<NotificationItemDto> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages,
    int UnreadCount);

public sealed record UnreadCountResponse(
    int UnreadCount,
    DateTime? LatestNotificationUtc);

public sealed record MarkAllNotificationsReadRequest(
    string Category = "all",
    DateTime? ReadThroughTimestamp = null);

public sealed record NotificationPreferencesDto(
    bool EnableInApp = true,
    bool EnableWebPush = true,
    bool EnableEmailDigest = false);

