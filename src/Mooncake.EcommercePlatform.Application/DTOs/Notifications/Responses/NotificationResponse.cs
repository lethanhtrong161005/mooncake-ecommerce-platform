namespace Mooncake.EcommercePlatform.Application.DTOs.Notifications.Responses;

public sealed record NotificationResponse(Guid Id, string? Type, string? Title, string? Content, bool IsRead,
    Guid? ReferenceId, string? ReferenceType, DateTime CreatedAtUtc);

public sealed record NotificationListResponse(IReadOnlyList<NotificationResponse> Items, int UnreadCount,
    int Page, int PageSize);
