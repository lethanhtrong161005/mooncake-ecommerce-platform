namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Reviews.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Reviews.Responses;

public interface IReviewService
{
    Task<ReviewResponse> CreateTransactionReviewAsync(Guid customerId, CreateReviewRequest request,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReviewResponse>> GetSupplierReviewsAsync(Guid supplierId, CancellationToken cancellationToken = default);
    Task ReplyToReviewAsync(Guid supplierUserId, Guid reviewId, string reply, CancellationToken cancellationToken = default);
    Task ModerateReviewAsync(Guid adminUserId, Guid reviewId, ModerateReviewRequest request, CancellationToken cancellationToken = default);
    Task<ProductReviewSummary> GetProductReviewsAsync(Guid productId, Guid? userId, CancellationToken cancellationToken = default);
    Task AddProductReviewAsync(Guid customerId, Guid productId, CreateProductReviewRequest request,
        CancellationToken cancellationToken = default);
    Task SetProductReviewLikeAsync(Guid customerId, Guid reviewId, bool liked, CancellationToken cancellationToken = default);
    Task ReplyToProductReviewAsync(Guid supplierUserId, Guid reviewId, string reply, CancellationToken cancellationToken = default);
    Task ModerateProductReviewAsync(Guid adminUserId, Guid reviewId, ModerateReviewRequest request,
        CancellationToken cancellationToken = default);
}
