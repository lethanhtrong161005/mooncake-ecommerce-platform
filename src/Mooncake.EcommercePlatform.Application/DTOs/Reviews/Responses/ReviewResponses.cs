namespace Mooncake.EcommercePlatform.Application.DTOs.Reviews.Responses;

/// <summary>Response for a transaction review.</summary>
public sealed record TransactionReviewResponse
{
    public Guid Id { get; init; }
    public Guid CustomerId { get; init; }
    public string CustomerDisplayName { get; init; } = string.Empty;
    public string? CustomerAvatarUrl { get; init; }
    public bool IsAnonymous { get; init; }
    public Guid SupplierId { get; init; }
    public string SupplierName { get; init; } = string.Empty;
    public Guid? OrderId { get; init; }
    public string? OrderNumber { get; init; }
    public Guid? ContractId { get; init; }
    public string? ContractNumber { get; init; }
    public short OverallRating { get; init; }
    public short QualityRating { get; init; }
    public short DeliveryRating { get; init; }
    public short ServiceRating { get; init; }
    public string Comment { get; init; } = string.Empty;
    public List<string> MediaUrls { get; init; } = new();
    public string? SupplierReply { get; init; }
    public DateTime? SupplierRepliedAt { get; init; }
    public decimal ReputationScoreDelta { get; init; }
    public decimal NewSupplierReputationScore { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>Summary of reviews for a supplier's shop.</summary>
public sealed record ShopReviewSummaryResponse
{
    public Guid SupplierId { get; init; }
    public string ShopSlug { get; init; } = string.Empty;
    public decimal AverageRating { get; init; }
    public int TotalReviews { get; init; }
    public decimal QualityAverage { get; init; }
    public decimal DeliveryAverage { get; init; }
    public decimal ServiceAverage { get; init; }
    public List<TransactionReviewResponse> Items { get; init; } = new();
}

/// <summary>Full product review feed response including summary and items.</summary>
public sealed record ProductReviewFeedResponse
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public ProductReviewSummaryDto Summary { get; init; } = new();
    public List<ProductReviewItemDto> Items { get; init; } = new();
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
    public int TotalCount { get; init; }
    public bool HasMore { get; init; }
}

/// <summary>Summary statistics of product reviews.</summary>
public sealed record ProductReviewSummaryDto
{
    public decimal AverageRating { get; init; }
    public int TotalReviews { get; init; }
    public int VerifiedPurchaseCount { get; init; }
    public int WithMediaCount { get; init; }
    public StarDistributionDto StarDistribution { get; init; } = new();
}

/// <summary>Histogram of review counts per star rating (1 to 5).</summary>
public sealed record StarDistributionDto
{
    public int Star5 { get; init; }
    public int Star4 { get; init; }
    public int Star3 { get; init; }
    public int Star2 { get; init; }
    public int Star1 { get; init; }
}

/// <summary>Single product review item.</summary>
public sealed record ProductReviewItemDto
{
    public Guid Id { get; init; }
    public string AuthorName { get; init; } = string.Empty;
    public string? AuthorAvatarUrl { get; init; }
    public bool IsVerifiedPurchase { get; init; }
    public Guid? VariantId { get; init; }
    public string? VariantName { get; init; }
    public short Rating { get; init; }
    public string? Title { get; init; }
    public string Content { get; init; } = string.Empty;
    public List<string> MediaUrls { get; init; } = new();
    public int LikeCount { get; init; }
    public bool IsLikedByMe { get; init; }
    public string? SupplierReply { get; init; }
    public DateTime? SupplierRepliedAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>Response when toggling like on a product review.</summary>
public sealed record ToggleReviewLikeResponse
{
    public Guid ReviewId { get; init; }
    public bool IsLiked { get; init; }
    public int LikeCount { get; init; }
}

/// <summary>Response for a supplier's reputation profile and badges.</summary>
public sealed record SupplierReputationResponse
{
    public Guid SupplierId { get; init; }
    public string CompanyName { get; init; } = string.Empty;
    public decimal ReputationScore { get; init; }
    public string BadgeTier { get; init; } = string.Empty;
    public string BadgeTitle { get; init; } = string.Empty;
    public string BadgeColor { get; init; } = string.Empty;
    public SupplierStatsDto Stats { get; init; } = new();
    public List<ReputationLogDto> RecentLogs { get; init; } = new();
}

/// <summary>Operational statistics for a supplier.</summary>
public sealed record SupplierStatsDto
{
    public int TotalContracts { get; init; }
    public int ContractsOnTime { get; init; }
    public int ContractsLate { get; init; }
    public int ContractsBreached { get; init; }
    public decimal? AverageRating { get; init; }
    public int PositiveReviewsCount { get; init; }
    public int NegativeReviewsCount { get; init; }
}

/// <summary>Reputation audit log item.</summary>
public sealed record ReputationLogDto
{
    public Guid Id { get; init; }
    public string EventType { get; init; } = string.Empty;
    public decimal ScoreDelta { get; init; }
    public string? Reason { get; init; }
    public Guid? ReferenceId { get; init; }
    public string? ReferenceType { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>Order item eligible for verified purchase product review.</summary>
public sealed record EligibleOrderItemDto
{
    public Guid OrderItemId { get; init; }
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public Guid? ProductVariantId { get; init; }
    public string? VariantName { get; init; }
    public DateTime DeliveredAt { get; init; }
}

/// <summary>Order eligible for transaction review.</summary>
public sealed record EligibleOrderDto
{
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public Guid SupplierId { get; init; }
    public string SupplierName { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public DateTime DeliveredAt { get; init; }
}

