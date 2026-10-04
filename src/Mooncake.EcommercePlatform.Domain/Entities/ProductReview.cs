namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>Represents the ProductReview domain entity.</summary>
public class ProductReview : BaseEntity
{
    public Guid CustomerId { get; set; }

    public Guid ProductId { get; set; }

    public Guid? ProductVariantId { get; set; }

    public Guid OrderItemId { get; set; }

    public short? Rating { get; set; }

    public string? Title { get; set; }

    public string? Content { get; set; }

    public string? MediaUrls { get; set; }

    public bool IsVerifiedPurchase { get; set; }

    public int LikeCount { get; set; }

    public string? SupplierReply { get; set; }

    public DateTime? SupplierRepliedAt { get; set; }

    public bool IsPublic { get; set; }

    public bool IsHiddenByAdmin { get; set; }
}
