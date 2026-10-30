namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Persistence operations for custom packaging and revision review.</summary>
public interface ICustomPackagingRepository
{
    Task<Order?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<CustomPackaging?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<CustomPackagingRevision?> GetRevisionAsync(Guid revisionId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomPackagingRevision>> GetRevisionsAsync(Guid customPackagingId,
        CancellationToken cancellationToken = default);
    Task<bool> IsShopOwnedByUserAsync(Guid shopId, Guid userId,
        CancellationToken cancellationToken = default);
    Task<bool> CreateAsync(CustomPackaging packaging, Guid customerId,
        CancellationToken cancellationToken = default);
    Task<CustomPackagingRevision?> SubmitRevisionAsync(CustomPackaging packaging,
        CustomPackagingRevision revision, Guid customerId, CancellationToken cancellationToken = default);
    Task<bool> ReviewRevisionAsync(CustomPackaging packaging, CustomPackagingRevision revision,
        bool approve, string? customerNotes, Guid actorUserId, DateTime reviewedAtUtc,
        CancellationToken cancellationToken = default);
}
