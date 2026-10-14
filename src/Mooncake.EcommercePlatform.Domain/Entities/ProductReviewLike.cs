namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>Records that a user liked a product review.</summary>
public class ProductReviewLike : BaseEntity
{
    public Guid ProductReviewId { get; set; }

    public Guid CustomerId { get; set; }
}
