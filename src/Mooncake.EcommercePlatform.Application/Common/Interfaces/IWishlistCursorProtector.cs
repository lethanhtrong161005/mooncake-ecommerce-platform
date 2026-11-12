namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Wishlist.Persistence;

/// <summary>Signs and validates user-scoped opaque pagination cursors for wishlist queries.</summary>
public interface IWishlistCursorProtector
{
    string Protect(WishlistCursorDto cursor);

    WishlistCursorDto Unprotect(Guid expectedUserId, string token);
}

