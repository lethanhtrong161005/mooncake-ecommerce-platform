namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Domain.Entities;

public interface INotificationRepository
{
    Task<IReadOnlyList<Notification>> GetMineAsync(Guid userId, bool unreadOnly, int page, int pageSize,
        CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> MarkReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default);
    Task<int> MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default);
}

public interface IRealtimeNotificationSink
{
    Task PublishAsync(RealtimeNotificationEvent notification, CancellationToken cancellationToken = default);
}

public sealed record RealtimeNotificationEvent(Guid Id, Guid UserId, string? Type, string? Title,
    string? Content, Guid? ReferenceId, string? ReferenceType, DateTime CreatedAtUtc);
