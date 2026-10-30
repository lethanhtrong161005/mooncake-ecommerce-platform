namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Notifications;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;

public sealed class NotificationService(
    INotificationRepository notificationRepository,
    IRealtimeNotificationPublisher? realtimePublisher = null) : INotificationService
{
    public Task<NotificationInboxResponse> GetInboxAsync(
        Guid userId, int page, int pageSize, string? category, bool? isRead, CancellationToken ct = default) =>
        notificationRepository.GetInboxAsync(userId, page, pageSize, category, isRead, ct);

    public async Task<UnreadCountResponse> GetUnreadCountAsync(Guid userId, CancellationToken ct = default)
    {
        var count = await notificationRepository.GetUnreadCountAsync(userId, ct);
        return new UnreadCountResponse(count, DateTime.UtcNow);
    }

    public async Task<bool> MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken ct = default)
    {
        var result = await notificationRepository.MarkAsReadAsync(userId, notificationId, ct);
        if (result && realtimePublisher != null)
        {
            var newCount = await notificationRepository.GetUnreadCountAsync(userId, ct);
            await realtimePublisher.PublishToUserAsync(
                userId,
                "NotificationsMarkedAsRead",
                new { ids = new[] { notificationId.ToString() }, unreadCount = newCount },
                ct);
        }
        return result;
    }

    public async Task<int> MarkAllAsReadAsync(Guid userId, string? category, DateTime? readThroughUtc, CancellationToken ct = default)
    {
        var updated = await notificationRepository.MarkAllAsReadAsync(userId, category, readThroughUtc, ct);
        if (updated > 0 && realtimePublisher != null)
        {
            await realtimePublisher.PublishToUserAsync(
                userId,
                "AllNotificationsRead",
                new { unreadCount = 0 },
                ct);
        }
        return updated;
    }

    public async Task<NotificationItemDto> DispatchNotificationAsync(
        Guid userId,
        string type,
        string title,
        string content,
        string? referenceType = null,
        Guid? referenceId = null,
        string? actionUrl = null,
        string priority = "medium",
        CancellationToken ct = default)
    {
        var entity = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Title = title,
            Content = content,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await notificationRepository.CreateNotificationAsync(entity, ct);

        var dto = new NotificationItemDto(
            entity.Id,
            type,
            ResolveCategory(type, referenceType),
            title,
            content,
            referenceId,
            referenceType,
            actionUrl ?? ResolveActionUrl(referenceType, referenceId),
            priority,
            false,
            null,
            entity.CreatedAt
        );

        if (realtimePublisher != null)
        {
            var unreadCount = await notificationRepository.GetUnreadCountAsync(userId, ct);
            await realtimePublisher.PublishToUserAsync(userId, "ReceiveNotification", dto, ct);
            await realtimePublisher.PublishToUserAsync(userId, "UnreadCountUpdated", new { unreadCount }, ct);
        }

        return dto;
    }

    public Task<NotificationPreferencesDto> GetPreferencesAsync(Guid userId, CancellationToken ct = default) =>
        notificationRepository.GetPreferencesAsync(userId, ct);

    public Task<NotificationPreferencesDto> UpdatePreferencesAsync(Guid userId, NotificationPreferencesDto preferences, CancellationToken ct = default) =>
        notificationRepository.UpdatePreferencesAsync(userId, preferences, ct);

    private static string ResolveCategory(string? type, string? refType)
    {
        var t = (type ?? string.Empty).ToLowerInvariant();
        var r = (refType ?? string.Empty).ToLowerInvariant();
        if (t.Contains("pay") || r.Contains("pay")) return "payment";
        if (t.Contains("contract") || r.Contains("contract")) return "contract";
        if (t.Contains("order") || r.Contains("order")) return "order";
        if (t.Contains("rfq") || r.Contains("rfq")) return "rfq";
        return "system";
    }

    private static string ResolveActionUrl(string? refType, Guid? refId)
    {
        var r = (refType ?? string.Empty).ToLowerInvariant();
        if (refId == null) return "/#/";
        if (r.Contains("contract")) return $"/#/hop-dong-tien-do?id={refId}";
        if (r.Contains("order")) return $"/#/dat-hang-thiet-ke?id={refId}";
        if (r.Contains("delivery")) return $"/#/giao-nhan-pod?id={refId}";
        if (r.Contains("rfq")) return $"/#/rfq-dau-thau?id={refId}";
        return "/#/";
    }
}

