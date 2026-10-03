namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Data access contract for customer reviews.</summary>
public interface IReviewRepository
{
    Task<Review> CreateAsync(Review review, CancellationToken cancellationToken = default);
    Task<Review?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Review?> GetByOrderIdAsync(long orderId, CancellationToken cancellationToken = default);
    Task<Review?> GetByContractIdAsync(long contractId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Review>> GetBySupplierIdAsync(long supplierId, CancellationToken cancellationToken = default);
}
