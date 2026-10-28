namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Suppliers.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Durable inbox, leased outbox and per-user ordered fanout persistence.</summary>
public interface IVerificationNotificationRepository
{
    Task<VerificationInboxData> InboxAsync(Guid actorId, bool isAdmin, int page, int pageSize, CancellationToken ct);
    Task<int> MarkReadAsync(Guid actorId, bool isAdmin, Guid? id, long? cutoff, DateTime now, CancellationToken ct);
    Task<bool> CanReadAsync(Guid actorId, Guid id, bool isAdmin, CancellationToken ct);
    Task<VerificationOutboxMessage?> ClaimAsync(DateTime now, CancellationToken ct);
    Task CompleteAsync(Guid id, Guid leaseToken, CancellationToken ct);
    Task FailAsync(Guid id, Guid leaseToken, string errorCode, DateTime now, CancellationToken ct);
    Task<List<Guid>> AdminIdsAsync(CancellationToken ct);
    Task<WorkflowEvent?> EventAsync(Guid eventId, CancellationToken ct);
    Task<List<Notification>> MaterializeAsync(VerificationOutboxMessage message, IReadOnlyList<Guid> recipients, bool admins, DateTime now, CancellationToken ct);
    Task<bool> IsActiveAsync(Guid userId, CancellationToken ct);
}

/// <summary>Purpose-bound, short-lived read-all boundary; implementation shares its key ring across instances.</summary>
public interface IVerificationReadTokenProtector
{
    string Protect(VerificationReadBoundary boundary);
    VerificationReadBoundary? Unprotect(string token);
}
