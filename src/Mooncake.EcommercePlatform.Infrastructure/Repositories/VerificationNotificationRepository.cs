namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Suppliers.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;
using C = Mooncake.EcommercePlatform.Application.Common.Constant.SupplierVerificationConstants;

/// <summary>Durable notifications and short outbox leases; network delivery occurs outside transactions.</summary>
public sealed class VerificationNotificationRepository(ApplicationDbContext db) : IVerificationNotificationRepository
{
    public async Task<VerificationInboxData> InboxAsync(Guid actorId, bool isAdmin, int page, int pageSize, CancellationToken ct)
    {
        var rows = db.Notifications.AsNoTracking().Where(x => x.UserId == actorId && !x.IsDeleted &&
            x.SourceEventId != null && (x.AudienceKind != C.Admin || isAdmin));
        var stats = await rows.GroupBy(x => 1).Select(g => new { Total = g.Count(), Unread = g.Count(x => !x.IsRead) }).SingleOrDefaultAsync(ct);
        var items = await rows.OrderByDescending(x => x.Sequence).ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).Select(x => new VerificationNotificationResponse(
                x.Id, x.Type, x.Title, x.Content, x.ReferenceId, x.SourceEventId, x.AggregateVersion,
                x.Sequence, x.IsRead, x.CreatedAt)).ToListAsync(ct);
        return new(items, stats?.Total ?? 0, stats?.Unread ?? 0);
    }
    public Task<int> MarkReadAsync(Guid actorId, bool isAdmin, Guid? id, long? cutoff, DateTime now, CancellationToken ct) =>
        db.Notifications.Where(x => x.UserId == actorId && !x.IsDeleted && x.SourceEventId != null &&
            (x.AudienceKind != C.Admin || isAdmin) && (!id.HasValue || x.Id == id) &&
            (!cutoff.HasValue || x.Sequence <= cutoff) && !x.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsRead, true).SetProperty(x => x.ReadAtUtc, now), ct);
    public Task<bool> CanReadAsync(Guid actorId, Guid id, bool isAdmin, CancellationToken ct) =>
        db.Notifications.AsNoTracking().AnyAsync(x => x.Id == id && x.UserId == actorId && !x.IsDeleted &&
            x.SourceEventId != null && (x.AudienceKind != C.Admin || isAdmin), ct);
    public async Task<VerificationOutboxMessage?> ClaimAsync(DateTime now, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var row = await db.VerificationOutboxMessages.FromSqlInterpolated(
            $"SELECT * FROM verification_outbox_messages WHERE (status = 'Pending' AND next_attempt_at_utc <= {now}) OR (status = 'Processing' AND lease_until_utc < {now}) ORDER BY created_at LIMIT 1 FOR UPDATE SKIP LOCKED")
            .SingleOrDefaultAsync(ct);
        if (row != null)
        {
            row.Status = C.Processing; row.LeaseToken = Guid.NewGuid();
            row.LeaseUntilUtc = now.AddMinutes(2); row.AttemptCount++;
            await db.SaveChangesAsync(ct);
        }
        await transaction.CommitAsync(ct);
        return row;
    }
    public Task CompleteAsync(Guid id, Guid leaseToken, CancellationToken ct) =>
        db.VerificationOutboxMessages.Where(x => x.Id == id && x.LeaseToken == leaseToken)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, C.Completed).SetProperty(x => x.LeaseUntilUtc, (DateTime?)null), ct);
    public async Task FailAsync(Guid id, Guid leaseToken, string errorCode, DateTime now, CancellationToken ct)
    {
        var item = await db.VerificationOutboxMessages.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.LeaseToken == leaseToken, ct);
        if (item == null) return;
        var status = item.AttemptCount >= 8 ? C.DeadLetter : C.Pending;
        var next = now.AddSeconds(Math.Min(3600, 30 * Math.Pow(2, item.AttemptCount)) + Random.Shared.Next(1, 10));
        await db.VerificationOutboxMessages.Where(x => x.Id == id && x.LeaseToken == leaseToken)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, status).SetProperty(x => x.LastErrorCode, errorCode)
                .SetProperty(x => x.NextAttemptAtUtc, next).SetProperty(x => x.LeaseUntilUtc, (DateTime?)null), ct);
    }
    public Task<List<Guid>> AdminIdsAsync(CancellationToken ct) => db.Users.AsNoTracking()
        .Where(x => x.Role == UserRole.Admin && x.IsActive && !x.IsDeleted).Select(x => x.Id).ToListAsync(ct);
    public Task<WorkflowEvent?> EventAsync(Guid eventId, CancellationToken ct) =>
        db.WorkflowEvents.AsNoTracking().SingleOrDefaultAsync(x => x.Id == eventId, ct);
    public Task<bool> IsActiveAsync(Guid userId, CancellationToken ct) =>
        db.Users.AsNoTracking().AnyAsync(x => x.Id == userId && x.IsActive && !x.IsDeleted, ct);
    public async Task<List<Notification>> MaterializeAsync(VerificationOutboxMessage message,
        IReadOnlyList<Guid> recipients, bool admins, DateTime now, CancellationToken ct)
    {
        var result = new List<Notification>();
        foreach (var batch in recipients.Distinct().Order().Chunk(100))
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            foreach (var entry in db.ChangeTracker.Entries<User>().ToArray()) entry.State = EntityState.Detached;
            var users = await db.Users.FromSqlInterpolated(
                $"SELECT * FROM users WHERE id = ANY ({batch}) ORDER BY id FOR UPDATE").ToListAsync(ct);
            var ids = users.Where(x => x.IsActive && !x.IsDeleted && (!admins || x.Role == UserRole.Admin)).Select(x => x.Id).ToArray();
            var existing = await db.Notifications.Where(x => ids.Contains(x.UserId) &&
                x.SourceEventId == message.EventId && x.Type == message.EventType).ToListAsync(ct);
            var newIds = ids.Except(existing.Select(x => x.UserId)).ToArray();
            if (newIds.Length > 0)
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE users SET notification_sequence = notification_sequence + 1 WHERE id = ANY ({newIds})", ct);
            var counters = await db.Users.AsNoTracking().Where(x => ids.Contains(x.Id))
                .Select(x => new { x.Id, x.NotificationSequence }).ToDictionaryAsync(x => x.Id, x => x.NotificationSequence, ct);
            foreach (var user in users.Where(x => ids.Contains(x.Id)))
            {
                var row = existing.FirstOrDefault(x => x.UserId == user.Id);
                if (row == null)
                {
                    row = new Notification { Id = Guid.NewGuid(), UserId = user.Id, SourceEventId = message.EventId,
                        AggregateVersion = message.AggregateVersion, Sequence = counters[user.Id],
                        AudienceKind = admins ? C.Admin : C.Owner, Type = message.EventType,
                        Title = C.NotificationTitle, Content = C.NotificationContent, ReferenceId = message.AggregateId,
                        ReferenceType = C.Category, CreatedAt = now };
                    db.Notifications.Add(row);
                }
                result.Add(row);
            }
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        return result;
    }
}
