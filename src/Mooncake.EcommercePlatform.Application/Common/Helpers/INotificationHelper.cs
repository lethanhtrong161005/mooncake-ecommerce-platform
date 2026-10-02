namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Application.DTOs.Notifications.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Contract for Notification mapping helpers.</summary>
public interface INotificationHelper
{
    NotificationResponse ToResponse(Notification notification);
}
