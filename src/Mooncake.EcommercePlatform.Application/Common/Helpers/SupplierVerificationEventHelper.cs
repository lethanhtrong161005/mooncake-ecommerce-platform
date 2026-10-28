namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using System.Text.Json;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using C = Mooncake.EcommercePlatform.Application.Common.Constant.SupplierVerificationConstants;
using H = Mooncake.EcommercePlatform.Application.Common.Helpers.SupplierVerificationHelper;

/// <summary>Appends the journal, administrative audit and independent delivery intents atomically.</summary>
public static class SupplierVerificationEventHelper
{
    public static void Append(ISupplierRepository repository, DateTime now, string traceId,
        SupplierProfile profile, Guid actor, string previous, string type, string? reason, bool email)
    {
        var eventId = Guid.NewGuid();
        var metadata = JsonSerializer.Serialize(new { submissionId = profile.CurrentSubmissionId, version = profile.Version, traceId = traceId });
        repository.AddEvent(new() { Id = eventId, AggregateType = C.Aggregate, AggregateId = profile.Id, EventType = type,
            FromStatus = previous, ToStatus = profile.VerificationStatus.ToString(), ActorUserId = actor, Reason = reason,
            Metadata = metadata, CreatedAt = now });
        if (email) repository.AddAudit(new() { Id = Guid.NewGuid(), ActorUserId = actor, Action = type, EntityName = C.Aggregate,
            EntityId = profile.Id.ToString(), MetadataJson = JsonSerializer.Serialize(new { previous, status = profile.VerificationStatus.ToString(),
                version = profile.Version, reasonHash = reason == null ? null : H.Hash(reason), traceId = traceId }), CreatedAt = now });
        foreach (var channel in email ? new[] { C.Email, C.InApp } : new[] { C.InApp })
            repository.AddOutbox(new() { Id = Guid.NewGuid(), EventId = eventId, AggregateId = profile.Id,
                AggregateVersion = profile.Version, EventType = type, Channel = channel, OwnerUserId = profile.UserId,
                NextAttemptAtUtc = now, CreatedAt = now });
    }
}
