namespace Mooncake.EcommercePlatform.Infrastructure.Services;

using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Wishlist.Persistence;

/// <summary>Data-protected user-scoped keyset cursor for customer wishlist pagination.</summary>
public sealed class WishlistCursorProtector(
    IDataProtectionProvider provider,
    IDateTimeProvider clock) : IWishlistCursorProtector
{
    private readonly IDataProtector _protector = provider.CreateProtector("Mooncake.Wishlist.Cursor.v1");

    private sealed record Envelope(Guid UserId, Guid WishlistId, DateTime SavedAtUtc, DateTime ExpiresAtUtc);

    public string Protect(WishlistCursorDto cursor)
    {
        var envelope = new Envelope(
            cursor.UserId,
            cursor.WishlistId,
            cursor.SavedAtUtc,
            clock.UtcNow.AddMinutes(WishlistConstants.CursorLifetimeMinutes));

        return _protector.Protect(JsonSerializer.Serialize(envelope));
    }

    public WishlistCursorDto Unprotect(Guid expectedUserId, string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token) || token.Length > 2048)
                throw new CryptographicException();

            var envelope = JsonSerializer.Deserialize<Envelope>(_protector.Unprotect(token))
                ?? throw new CryptographicException();

            if (envelope.UserId != expectedUserId || envelope.ExpiresAtUtc <= clock.UtcNow)
                throw new CryptographicException();

            return new WishlistCursorDto(envelope.UserId, envelope.WishlistId, envelope.SavedAtUtc);
        }
        catch (Exception failure) when (failure is CryptographicException or JsonException or FormatException or ArgumentException)
        {
            throw new HttpException(400, WishlistConstants.InvalidCursor);
        }
    }
}

