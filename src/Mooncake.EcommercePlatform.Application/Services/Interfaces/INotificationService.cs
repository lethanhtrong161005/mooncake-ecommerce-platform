namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Notifications.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Notifications.Responses;

/// <summary>Contract for in-app notification services.</summary>
public interface INotificationService
{
    Task<IEnumerable<NotificationResponse>> GetUserNotificationsAsync(long userId, bool? unreadOnly = null, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(long userId, CancellationToken cancellationToken = default);
    Task<NotificationResponse> MarkAsReadAsync(long notificationId, CancellationToken cancellationToken = default);
    Task MarkAllAsReadAsync(long userId, CancellationToken cancellationToken = default);
    Task<NotificationResponse> CreateNotificationAsync(CreateNotificationRequest request, CancellationToken cancellationToken = default);
}
