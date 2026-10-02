namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Notifications.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Notifications.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Implements notification dispatching and management.</summary>
public class NotificationService(
    INotificationRepository notificationRepository,
    IUserRepository userRepository,
    INotificationHelper notificationHelper) : INotificationService
{
    public async Task<IEnumerable<NotificationResponse>> GetUserNotificationsAsync(long userId, bool? unreadOnly = null, CancellationToken cancellationToken = default)
    {
        var notifications = await notificationRepository.GetByUserIdAsync(userId, unreadOnly, cancellationToken);
        return notifications.Select(notificationHelper.ToResponse);
    }

    public async Task<int> GetUnreadCountAsync(long userId, CancellationToken cancellationToken = default) =>
        await notificationRepository.GetUnreadCountAsync(userId, cancellationToken);

    public async Task<NotificationResponse> MarkAsReadAsync(long notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await notificationRepository.GetByIdAsync(notificationId, cancellationToken)
                           ?? throw new HttpException(404, $"Notification with id '{notificationId}' was not found.");

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAtUtc = DateTime.UtcNow;
            await notificationRepository.UpdateAsync(notification, cancellationToken);
        }

        return notificationHelper.ToResponse(notification);
    }

    public async Task MarkAllAsReadAsync(long userId, CancellationToken cancellationToken = default)
    {
        await notificationRepository.MarkAllAsReadAsync(userId, cancellationToken);
    }

    public async Task<NotificationResponse> CreateNotificationAsync(CreateNotificationRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
                   ?? throw new HttpException(404, $"User with id '{request.UserId}' was not found.");

        var notification = new Notification
        {
            UserId = request.UserId,
            Type = request.Type,
            Title = request.Title,
            Body = request.Body,
            EntityType = request.EntityType,
            EntityId = request.EntityId,
            IsRead = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        var created = await notificationRepository.CreateAsync(notification, cancellationToken);
        return notificationHelper.ToResponse(created);
    }
}
