namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Domain.Entities;

public interface IReviewRepository
{
    Task<Review?> CreateTransactionReviewAsync(Guid customerId, Review review, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Review>> GetSupplierReviewsAsync(Guid supplierId, CancellationToken cancellationToken = default);
    Task<bool> ReplyToReviewAsync(Guid reviewId, Guid supplierUserId, string reply, CancellationToken cancellationToken = default);
    Task<bool> ModerateReviewAsync(Guid reviewId, Guid adminUserId, bool isPublic, string? reason, CancellationToken cancellationToken = default);
    Task<bool> CreateProductReviewAsync(Guid customerId, ProductReview review, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductReview>> GetProductReviewsAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<IReadOnlySet<Guid>> GetLikedProductReviewIdsAsync(Guid customerId, IReadOnlyCollection<Guid> reviewIds,
        CancellationToken cancellationToken = default);
    Task<bool> SetProductReviewLikeAsync(Guid reviewId, Guid customerId, bool liked, CancellationToken cancellationToken = default);
    Task<bool> ReplyToProductReviewAsync(Guid reviewId, Guid supplierUserId, string reply, CancellationToken cancellationToken = default);
    Task<bool> ModerateProductReviewAsync(Guid reviewId, Guid adminUserId, bool isPublic, string? reason,
        CancellationToken cancellationToken = default);
}
