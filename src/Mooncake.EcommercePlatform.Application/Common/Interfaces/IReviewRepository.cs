namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Reviews.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Repository interface for Reviews, Product Reviews, Likes, and Reputation tracking.</summary>
public interface IReviewRepository
{
    // Transaction Reviews
    Task<Review> CreateReviewAsync(Review review, CancellationToken ct = default);
    Task<Review?> GetReviewByIdAsync(Guid id, CancellationToken ct = default);
    Task<Review?> GetReviewByOrderIdAsync(Guid orderId, CancellationToken ct = default);
    Task<Review?> GetReviewByContractIdAsync(Guid contractId, CancellationToken ct = default);
    Task<List<Review>> GetShopReviewsAsync(Guid supplierId, int? rating, int pageNumber, int pageSize, CancellationToken ct = default);
    Task<int> CountShopReviewsAsync(Guid supplierId, int? rating, CancellationToken ct = default);
    Task UpdateReviewAsync(Review review, CancellationToken ct = default);

    // Product Reviews
    Task<ProductReview> CreateProductReviewAsync(ProductReview review, CancellationToken ct = default);
    Task<ProductReview?> GetProductReviewByIdAsync(Guid id, CancellationToken ct = default);
    Task<ProductReview?> GetProductReviewByOrderItemAsync(Guid customerId, Guid orderItemId, CancellationToken ct = default);
    Task<List<ProductReview>> GetProductReviewsFeedAsync(Guid productId, Guid? variantId, int? rating, bool? hasMedia, int pageNumber, int pageSize, string? sortBy, CancellationToken ct = default);
    Task<int> CountProductReviewsAsync(Guid productId, Guid? variantId, int? rating, bool? hasMedia, CancellationToken ct = default);
    Task<ProductReviewSummaryDto> GetProductReviewStatsAsync(Guid productId, CancellationToken ct = default);
    Task UpdateProductReviewAsync(ProductReview review, CancellationToken ct = default);

    // Likes
    Task<ProductReviewLike?> GetReviewLikeAsync(Guid reviewId, Guid customerId, CancellationToken ct = default);
    Task AddReviewLikeAsync(ProductReviewLike like, CancellationToken ct = default);
    Task RemoveReviewLikeAsync(Guid reviewId, Guid customerId, CancellationToken ct = default);
    Task<HashSet<Guid>> GetUserLikedReviewIdsAsync(Guid customerId, List<Guid> reviewIds, CancellationToken ct = default);

    // Reputation & Profiles
    Task<ReputationLog> AddReputationLogAsync(ReputationLog log, CancellationToken ct = default);
    Task<List<ReputationLog>> GetReputationLogsAsync(Guid supplierId, int limit, CancellationToken ct = default);
    Task<decimal> UpdateSupplierReputationScoreAsync(Guid supplierId, decimal scoreDelta, CancellationToken ct = default);
    Task<SupplierProfile?> GetSupplierProfileAsync(Guid supplierId, CancellationToken ct = default);
    Task<SupplierProfile?> GetSupplierProfileByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<SupplierProfile?> GetSupplierByShopSlugAsync(string slug, CancellationToken ct = default);
    Task<CustomerProfile?> GetCustomerProfileByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Product?> GetProductByIdAsync(Guid productId, CancellationToken ct = default);
    Task<(Product? Product, Guid? SupplierId)> GetProductWithSupplierAsync(Guid productId, CancellationToken ct = default);

    // Eligibility & Context
    Task<(Order? Order, Guid? SupplierId)> GetOrderWithSupplierForCustomerAsync(Guid customerUserId, Guid orderId, CancellationToken ct = default);
    Task<Contract?> GetContractForCustomerAsync(Guid customerUserId, Guid contractId, CancellationToken ct = default);
    Task<List<EligibleOrderDto>> GetEligibleOrdersForCustomerAsync(Guid customerUserId, CancellationToken ct = default);
    Task<List<EligibleOrderItemDto>> GetEligibleOrderItemsForProductAsync(Guid customerUserId, Guid productId, CancellationToken ct = default);
}
