namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Notifications.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;

public sealed class NotificationService(INotificationRepository repository) : INotificationService
{
    public async Task<NotificationListResponse> GetMineAsync(Guid userId, bool unreadOnly, int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 100) throw new HttpException(400, "Pagination values are invalid.");
        var notifications = await repository.GetMineAsync(userId, unreadOnly, page, pageSize, cancellationToken);
        var unreadCount = await repository.GetUnreadCountAsync(userId, cancellationToken);
        return new NotificationListResponse(notifications.Select(item => new NotificationResponse(item.Id,
            item.Type, item.Title, item.Content, item.IsRead, item.ReferenceId, item.ReferenceType, item.CreatedAt)).ToArray(),
            unreadCount, page, pageSize);
    }

    public async Task MarkReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default)
    {
        if (!await repository.MarkReadAsync(userId, notificationId, cancellationToken))
            throw new HttpException(404, "Notification was not found.");
    }

    public Task<int> MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default) =>
        repository.MarkAllReadAsync(userId, cancellationToken);
}
