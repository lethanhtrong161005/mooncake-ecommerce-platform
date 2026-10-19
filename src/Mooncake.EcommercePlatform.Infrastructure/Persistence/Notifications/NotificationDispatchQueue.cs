namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Notifications;

using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;

public sealed class NotificationDispatchQueue(IRealtimeNotificationSink sink)
{
    private readonly List<Notification> _pending = [];

    public void Capture(IEnumerable<Notification> notifications)
    {
        foreach (var notification in notifications)
            if (!_pending.Contains(notification)) _pending.Add(notification);
    }

    public async Task FlushAsync(CancellationToken cancellationToken)
    {
        var committed = _pending.ToArray();
        _pending.Clear();
        foreach (var item in committed)
        {
            try
            {
                await sink.PublishAsync(new RealtimeNotificationEvent(item.Id, item.UserId, item.Type, item.Title,
                    item.Content, item.ReferenceId, item.ReferenceType, item.CreatedAt), cancellationToken);
            }
            catch
            {
                // The durable notification remains available through the notification API.
            }
        }
    }

    public void Clear() => _pending.Clear();
}
