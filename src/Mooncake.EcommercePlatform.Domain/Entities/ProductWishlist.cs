namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>Represents a product saved in an authenticated customer's wishlist.</summary>
public class ProductWishlist : BaseEntity
{
    public Guid UserId { get; set; }

    public Guid ProductId { get; set; }
}

