namespace Mooncake.EcommercePlatform.Application.DTOs.Reviews.Requests;

using System.ComponentModel.DataAnnotations;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Request to submit a 4-criteria transaction review for an Order or Contract.</summary>
public sealed record CreateTransactionReviewRequest
{
    public Guid? OrderId { get; init; }

    public Guid? ContractId { get; init; }

    [Range(1, 5, ErrorMessage = "Quality rating must be between 1 and 5.")]
    public short QualityRating { get; init; } = 5;

    [Range(1, 5, ErrorMessage = "Delivery rating must be between 1 and 5.")]
    public short DeliveryRating { get; init; } = 5;

    [Range(1, 5, ErrorMessage = "Service rating must be between 1 and 5.")]
    public short ServiceRating { get; init; } = 5;

    [Range(1, 5, ErrorMessage = "Overall rating must be between 1 and 5.")]
    public short OverallRating { get; init; } = 5;

    [Required(ErrorMessage = "Review comment is required.")]
    [StringLength(2000, MinimumLength = 5, ErrorMessage = "Comment must be between 5 and 2000 characters.")]
    public string Comment { get; init; } = string.Empty;

    public bool IsAnonymous { get; init; }

    public List<string>? MediaUrls { get; init; }
}

/// <summary>Request for supplier to write an official single reply.</summary>
public sealed record SupplierReplyRequest
{
    [Required(ErrorMessage = "Reply content is required.")]
    [StringLength(1000, MinimumLength = 2, ErrorMessage = "Reply must be between 2 and 1000 characters.")]
    public string ReplyContent { get; init; } = string.Empty;
}

/// <summary>Request to create a verified purchase product review on a specific variant.</summary>
public sealed record CreateProductReviewRequest
{
    [Required(ErrorMessage = "OrderItemId is required to verify purchase.")]
    public Guid OrderItemId { get; init; }

    public Guid? ProductVariantId { get; init; }

    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
    public short Rating { get; init; } = 5;

    [StringLength(255, ErrorMessage = "Title must not exceed 255 characters.")]
    public string? Title { get; init; }

    [Required(ErrorMessage = "Review content is required.")]
    [StringLength(3000, MinimumLength = 5, ErrorMessage = "Content must be between 5 and 3000 characters.")]
    public string Content { get; init; } = string.Empty;

    public List<string>? MediaUrls { get; init; }
}

/// <summary>Request for admin to moderate/hide or restore a review.</summary>
public sealed record ModerateReviewRequest
{
    public bool IsHidden { get; init; } = true;

    [StringLength(500, ErrorMessage = "Reason must not exceed 500 characters.")]
    public string? Reason { get; init; }
}

/// <summary>Request for admin to manually adjust a supplier's reputation score.</summary>
public sealed record AdminAdjustReputationRequest
{
    public ReputationEvent EventType { get; init; } = ReputationEvent.AdminPenalty;

    [Range(-100, 100, ErrorMessage = "Score delta must be between -100 and 100.")]
    public decimal ScoreDelta { get; init; }

    [Required(ErrorMessage = "Adjustment reason is required.")]
    [StringLength(500, ErrorMessage = "Reason must not exceed 500 characters.")]
    public string Reason { get; init; } = string.Empty;

    public Guid? ReferenceId { get; init; }

    [StringLength(50, ErrorMessage = "Reference type must not exceed 50 characters.")]
    public string? ReferenceType { get; init; }
}

