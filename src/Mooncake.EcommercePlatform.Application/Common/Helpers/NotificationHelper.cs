namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Application.DTOs.Notifications.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Maps Notification entities to DTOs.</summary>
public class NotificationHelper : INotificationHelper
{
    public NotificationResponse ToResponse(Notification notification) =>
        new(
            notification.Id,
            notification.UserId,
            notification.Type,
            notification.Title,
            notification.Body,
            notification.EntityType,
            notification.EntityId,
            notification.IsRead,
            notification.ReadAtUtc,
            notification.CreatedAtUtc
        );
}
