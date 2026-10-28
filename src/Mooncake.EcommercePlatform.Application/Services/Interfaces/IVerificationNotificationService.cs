namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Suppliers.Responses;

/// <summary>Authorized notification reads and durable event delivery.</summary>
public interface IVerificationNotificationService
{
    Task<VerificationInboxResponse> InboxAsync(Guid actorId, int page, int pageSize, CancellationToken ct);
    Task ReadAsync(Guid actorId, Guid id, CancellationToken ct);
    Task ReadAllAsync(Guid actorId, string readThroughToken, string key, CancellationToken ct);
    Task<bool> DeliverNextAsync(CancellationToken ct);
}
