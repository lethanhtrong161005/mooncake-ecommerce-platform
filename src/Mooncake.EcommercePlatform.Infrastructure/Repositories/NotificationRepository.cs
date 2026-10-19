namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

public sealed class NotificationRepository(ApplicationDbContext context, IDateTimeProvider clock) : INotificationRepository
{
    public async Task<IReadOnlyList<Notification>> GetMineAsync(Guid userId, bool unreadOnly, int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.Notifications.AsNoTracking().Where(item => item.UserId == userId && !item.IsDeleted);
        if (unreadOnly) query = query.Where(item => !item.IsRead);
        return await query.OrderByDescending(item => item.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default) =>
        context.Notifications.CountAsync(item => item.UserId == userId && !item.IsRead && !item.IsDeleted, cancellationToken);

    public async Task<bool> MarkReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await context.Notifications.FirstOrDefaultAsync(item => item.Id == notificationId
            && item.UserId == userId && !item.IsDeleted, cancellationToken);
        if (notification is null) return false;
        notification.IsRead = true;
        notification.UpdatedAt = clock.UtcNow;
        notification.UpdatedBy = userId;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        return await context.Notifications.Where(item => item.UserId == userId && !item.IsRead && !item.IsDeleted)
            .ExecuteUpdateAsync(update => update.SetProperty(item => item.IsRead, true)
                .SetProperty(item => item.UpdatedAt, now).SetProperty(item => item.UpdatedBy, userId), cancellationToken);
    }
}
