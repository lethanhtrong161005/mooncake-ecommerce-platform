namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Notifications;

using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

public sealed class NotificationTransactionInterceptor(NotificationDispatchQueue queue) : DbTransactionInterceptor
{
    public override async Task TransactionCommittedAsync(DbTransaction transaction,
        TransactionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        await queue.FlushAsync(cancellationToken);
        await base.TransactionCommittedAsync(transaction, eventData, cancellationToken);
    }

    public override async Task TransactionRolledBackAsync(DbTransaction transaction,
        TransactionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        queue.Clear();
        await base.TransactionRolledBackAsync(transaction, eventData, cancellationToken);
    }
}
