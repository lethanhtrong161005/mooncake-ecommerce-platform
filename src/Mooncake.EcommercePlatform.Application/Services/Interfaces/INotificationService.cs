namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Notifications;
using Mooncake.EcommercePlatform.Domain.Entities;

public interface INotificationService
{
    Task<NotificationInboxResponse> GetInboxAsync(Guid userId, int page, int pageSize, string? category, bool? isRead, CancellationToken ct = default);
    Task<UnreadCountResponse> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);
    Task<bool> MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken ct = default);
    Task<int> MarkAllAsReadAsync(Guid userId, string? category, DateTime? readThroughUtc, CancellationToken ct = default);
    Task<NotificationItemDto> DispatchNotificationAsync(Guid userId, string type, string title, string content, string? referenceType = null, Guid? referenceId = null, string? actionUrl = null, string priority = "medium", CancellationToken ct = default);
    Task<NotificationPreferencesDto> GetPreferencesAsync(Guid userId, CancellationToken ct = default);
    Task<NotificationPreferencesDto> UpdatePreferencesAsync(Guid userId, NotificationPreferencesDto preferences, CancellationToken ct = default);
}

