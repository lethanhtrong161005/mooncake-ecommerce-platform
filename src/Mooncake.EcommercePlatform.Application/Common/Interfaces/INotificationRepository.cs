namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Notifications;
using Mooncake.EcommercePlatform.Domain.Entities;

public interface INotificationRepository
{
    Task<NotificationInboxResponse> GetInboxAsync(Guid userId, int page, int pageSize, string? category, bool? isRead, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);
    Task<bool> MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken ct = default);
    Task<int> MarkAllAsReadAsync(Guid userId, string? category, DateTime? readThroughUtc, CancellationToken ct = default);
    Task<Notification> CreateNotificationAsync(Notification notification, CancellationToken ct = default);
    Task<NotificationPreferencesDto> GetPreferencesAsync(Guid userId, CancellationToken ct = default);
    Task<NotificationPreferencesDto> UpdatePreferencesAsync(Guid userId, NotificationPreferencesDto preferences, CancellationToken ct = default);
}

