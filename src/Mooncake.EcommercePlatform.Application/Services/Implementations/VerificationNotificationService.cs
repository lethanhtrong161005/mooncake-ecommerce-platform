namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Suppliers.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using H = Mooncake.EcommercePlatform.Application.Common.Helpers.SupplierVerificationHelper;
using C = Mooncake.EcommercePlatform.Application.Common.Constant.SupplierVerificationConstants;

/// <summary>Notification policy and at-least-once delivery independently of the original HTTP command.</summary>
public sealed class VerificationNotificationService(ISupplierRepository suppliers,
    IVerificationNotificationRepository repository, IVerificationReadTokenProtector tokens,
    IVerificationRealtimePublisher publisher, IEmailSender email, IDateTimeProvider clock) : IVerificationNotificationService
{
    public async Task<VerificationInboxResponse> InboxAsync(Guid actorId, int page, int pageSize, CancellationToken ct)
    {
        H.Require(page is >= 1 and <= 100000 && pageSize is >= 1 and <= 100);
        var user = H.RequireActive(await suppliers.GetUserAsync(actorId, ct));
        var snapshot = tokens.Protect(new(actorId, C.Category, user.NotificationSequence, clock.UtcNow.AddMinutes(5)));
        var data = await repository.InboxAsync(actorId, user.Role == UserRole.Admin, page, pageSize, ct);
        return new(data.Items, page, pageSize, data.TotalCount, data.UnreadCount, snapshot);
    }
    public async Task ReadAsync(Guid actorId, Guid id, CancellationToken ct)
    {
        await suppliers.InTransactionAsync([actorId], async token =>
        {
            var actor = H.RequireActive(await suppliers.GetUserAsync(actorId, token));
            H.Require(await repository.CanReadAsync(actorId, id, actor.Role == UserRole.Admin, token), 404, C.NotFound);
            var changed = await repository.MarkReadAsync(actorId, actor.Role == UserRole.Admin, id, null, clock.UtcNow, token);
            if (changed > 0) QueueRead(actorId);
            return true;
        }, ct);
    }
    public async Task ReadAllAsync(Guid actorId, string readThroughToken, string key, CancellationToken ct)
    {
        var operation = H.ParseKey(key);
        var boundary = tokens.Unprotect(readThroughToken);
        H.Require(boundary?.UserId == actorId && boundary.Category == C.Category && boundary.ExpiresAtUtc > clock.UtcNow);
        await suppliers.InTransactionAsync([actorId], async token =>
        {
            var actor = H.RequireActive(await suppliers.GetUserAsync(actorId, token));
            var previous = await suppliers.GetIdempotencyAsync(actorId, C.Read, actorId, operation, token);
            var hash = H.Hash(readThroughToken);
            if (previous != null) { H.Require(previous.RequestHash == hash, 409, C.KeyMismatch); return true; }
            var changed = await repository.MarkReadAsync(actorId, actor.Role == UserRole.Admin, null, boundary!.Sequence, clock.UtcNow, token);
            if (changed > 0) QueueRead(actorId);
            suppliers.AddIdempotency(new() { Id = Guid.NewGuid(), ActorUserId = actorId, Operation = C.Read,
                ResourceId = actorId, Key = operation, RequestHash = hash, ResultJson = "true",
                CreatedAt = clock.UtcNow, ExpiresAtUtc = clock.UtcNow.AddHours(24) });
            return true;
        }, ct);
    }
    public async Task<bool> DeliverNextAsync(CancellationToken ct)
    {
        var item = await repository.ClaimAsync(clock.UtcNow, ct);
        if (item == null) return false;
        try
        {
            if (item.EventType == C.Read)
            {
                if (await repository.IsActiveAsync(item.OwnerUserId, ct))
                    await publisher.PublishAsync([item.OwnerUserId], C.Read, new { eventId = item.EventId }, ct);
            }
            else if (item.Channel == C.Email)
            {
                var owner = await suppliers.GetUserAsync(item.OwnerUserId, ct);
                var history = await repository.EventAsync(item.EventId, ct);
                if (owner is { IsActive: true, IsDeleted: false } && history != null)
                    await email.SendAsync(owner.Email, C.EmailSubject,
                        string.Format(C.EmailBody, item.EventType switch {
                            C.Approved => C.EmailApproved, C.Rejected => C.EmailRejected, _ => C.EmailRevoked
                        }, history.Reason ?? ""), ct);
            }
            else
            {
                var submitted = item.EventType is C.Submitted or C.Resubmitted;
                var admins = await repository.AdminIdsAsync(ct);
                var notices = await repository.MaterializeAsync(item, submitted ? admins : [item.OwnerUserId], submitted, clock.UtcNow, ct);
                foreach (var notice in notices)
                    await publisher.PublishAsync([notice.UserId], C.NotificationCreated, new { schemaVersion = 1,
                        eventId = item.EventId, notificationId = notice.Id, type = item.EventType, occurredAtUtc = item.CreatedAt }, ct);
                // Only minimal invalidation is sent; authority and legal details are fetched through REST.
                if (await repository.IsActiveAsync(item.OwnerUserId, ct))
                    await publisher.PublishAsync([item.OwnerUserId], C.VerificationChanged, new { schemaVersion = 1,
                        eventId = item.EventId, eventType = item.EventType, profileId = item.AggregateId,
                        aggregateVersion = item.AggregateVersion, occurredAtUtc = item.CreatedAt }, ct);
                if (admins.Count > 0)
                    await publisher.PublishAsync(admins, C.QueueChanged, new { schemaVersion = 1, eventId = item.EventId, occurredAtUtc = item.CreatedAt }, ct);
            }
            await repository.CompleteAsync(item.Id, item.LeaseToken!.Value, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            // Persist a bounded error category, not SMTP bodies, provider URLs, document data or credentials.
            await repository.FailAsync(item.Id, item.LeaseToken!.Value, exception.GetType().Name, clock.UtcNow, ct);
        }
        return true;
    }
    private void QueueRead(Guid actorId) => suppliers.AddOutbox(new VerificationOutboxMessage {
        Id = Guid.NewGuid(), EventId = Guid.NewGuid(), OwnerUserId = actorId, Channel = C.InApp,
        EventType = C.Read, NextAttemptAtUtc = clock.UtcNow, CreatedAt = clock.UtcNow });
}
