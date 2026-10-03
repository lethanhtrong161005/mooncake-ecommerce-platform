namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="INotificationRepository"/>.</summary>
public class NotificationRepository(ApplicationDbContext context) : INotificationRepository
{
    public async Task<IEnumerable<Notification>> GetByUserIdAsync(long userId, bool? unreadOnly = null, CancellationToken cancellationToken = default)
    {
        var query = context.Notifications.AsNoTracking().Where(n => n.UserId == userId);
        if (unreadOnly.HasValue && unreadOnly.Value)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query.OrderByDescending(n => n.CreatedAtUtc).ToListAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(long userId, CancellationToken cancellationToken = default) =>
        await context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);

    public async Task<Notification?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        await context.Notifications.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    public async Task<Notification> CreateAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        context.Notifications.Add(notification);
        await context.SaveChangesAsync(cancellationToken);
        return notification;
    }

    public async Task<Notification> UpdateAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        context.Notifications.Update(notification);
        await context.SaveChangesAsync(cancellationToken);
        return notification;
    }

    public async Task MarkAllAsReadAsync(long userId, CancellationToken cancellationToken = default)
    {
        var unreadNotifications = await context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
            notification.ReadAtUtc = now;
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
