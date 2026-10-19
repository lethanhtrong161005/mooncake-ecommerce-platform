namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Notifications.Responses;

public interface INotificationService
{
    Task<NotificationListResponse> GetMineAsync(Guid userId, bool unreadOnly, int page, int pageSize,
        CancellationToken cancellationToken = default);
    Task MarkReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default);
    Task<int> MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default);
}
