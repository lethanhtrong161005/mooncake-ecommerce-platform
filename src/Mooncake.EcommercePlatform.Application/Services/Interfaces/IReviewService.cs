namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Reviews.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Reviews.Responses;

/// <summary>Service for transaction reviews, product reviews, and supplier reputation tracking.</summary>
public interface IReviewService
{
    // Transaction reviews (Feature #21)
    Task<TransactionReviewResponse> CreateTransactionReviewAsync(Guid customerUserId, CreateTransactionReviewRequest request, CancellationToken ct = default);
    Task<TransactionReviewResponse> ReplyTransactionReviewAsync(Guid supplierUserId, Guid reviewId, SupplierReplyRequest request, CancellationToken ct = default);
    Task<ShopReviewSummaryResponse> GetShopReviewsAsync(string slug, int? rating, int pageNumber, int pageSize, CancellationToken ct = default);
    Task<List<EligibleOrderDto>> GetEligibleOrdersForReviewAsync(Guid customerUserId, CancellationToken ct = default);

    // Product reviews stream (Feature #22)
    Task<ProductReviewFeedResponse> GetProductReviewsFeedAsync(Guid productId, Guid? currentUserId, Guid? variantId, int? rating, bool? hasMedia, int pageNumber, int pageSize, string? sortBy, CancellationToken ct = default);
    Task<List<EligibleOrderItemDto>> GetEligibleOrderItemsAsync(Guid customerUserId, Guid productId, CancellationToken ct = default);
    Task<ProductReviewItemDto> CreateProductReviewAsync(Guid customerUserId, Guid productId, CreateProductReviewRequest request, CancellationToken ct = default);
    Task<ToggleReviewLikeResponse> ToggleProductReviewLikeAsync(Guid currentUserId, Guid reviewId, CancellationToken ct = default);
    Task<ProductReviewItemDto> ReplyProductReviewAsync(Guid supplierUserId, Guid reviewId, SupplierReplyRequest request, CancellationToken ct = default);

    // Reputation & Moderation (Feature #22b)
    Task<SupplierReputationResponse> GetSupplierReputationAsync(Guid supplierId, CancellationToken ct = default);
    Task<SupplierReputationResponse> AdminAdjustReputationAsync(Guid adminUserId, Guid supplierId, AdminAdjustReputationRequest request, CancellationToken ct = default);
    Task<bool> ModerateReviewAsync(Guid adminUserId, Guid reviewId, bool isProductReview, ModerateReviewRequest request, CancellationToken ct = default);
}

