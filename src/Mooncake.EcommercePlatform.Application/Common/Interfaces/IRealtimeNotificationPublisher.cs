namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

/// <summary>Abstaction for dispatching real-time notifications to users via SignalR.</summary>
public interface IRealtimeNotificationPublisher
{
    Task PublishToUserAsync(Guid userId, string eventName, object payload, CancellationToken cancellationToken = default);
    Task PublishToUsersAsync(IReadOnlyList<Guid> userIds, string eventName, object payload, CancellationToken cancellationToken = default);
    Task PublishToGroupAsync(string groupName, string eventName, object payload, CancellationToken cancellationToken = default);
}

