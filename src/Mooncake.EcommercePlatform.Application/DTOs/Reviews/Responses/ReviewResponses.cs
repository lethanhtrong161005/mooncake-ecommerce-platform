namespace Mooncake.EcommercePlatform.Application.DTOs.Reviews.Responses;

public sealed record ReviewResponse(Guid Id, Guid SupplierId, Guid? OrderId, Guid? ContractId,
    short? OverallRating, short? QualityRating, short? DeliveryRating, short? ServiceRating, string? Comment,
    IReadOnlyList<string> MediaUrls, bool IsAnonymous, string? SupplierReply, DateTime? SupplierRepliedAt,
    bool IsPublic, DateTime CreatedAtUtc);

public sealed record ProductReviewResponse(Guid Id, Guid ProductId, Guid? ProductVariantId, Guid OrderItemId,
    short? Rating, string? Title, string? Content, IReadOnlyList<string> MediaUrls, bool IsVerifiedPurchase,
    int LikeCount, bool IsLikedByCurrentUser, string? SupplierReply, DateTime? SupplierRepliedAt, DateTime CreatedAtUtc);

public sealed record ProductReviewSummary(decimal AverageRating, int TotalReviews,
    IReadOnlyDictionary<int, int> RatingDistribution, IReadOnlyList<ProductReviewResponse> Reviews);
