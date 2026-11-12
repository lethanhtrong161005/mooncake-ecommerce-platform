namespace Mooncake.EcommercePlatform.WebApi.Extensions;

using System.Security.Claims;
using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;

/// <summary>Extracts authenticated user claims for wishlist endpoints.</summary>
public static class WishlistClaimsExtensions
{
    public static Guid GetRequiredUserId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) && userId != Guid.Empty
            ? userId
            : throw new HttpException(401, MessageConstant.SessionExpired);
}

