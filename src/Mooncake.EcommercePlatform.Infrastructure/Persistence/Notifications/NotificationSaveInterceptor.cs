namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Notifications;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Mooncake.EcommercePlatform.Domain.Entities;

public sealed class NotificationSaveInterceptor(NotificationDispatchQueue queue) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            queue.Capture(eventData.Context.ChangeTracker.Entries<Notification>()
                .Where(entry => entry.State == EntityState.Added).Select(entry => entry.Entity));
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
