namespace Mooncake.EcommercePlatform.Infrastructure.Services;

using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Persistence;
using C = Mooncake.EcommercePlatform.Application.Common.Constant.ShopConstants;

/// <summary>Purpose-separated cursors are bound to a shop and normalized query.</summary>
public sealed class ShopCursorProtector(IDataProtectionProvider provider, IDateTimeProvider clock) : IShopCursorProtector
{
    private readonly IDataProtector _protector = provider.CreateProtector("Mooncake.Shop.Cursor.v1");
    private sealed record Envelope(ShopCursorScopeDto Scope, ShopCursorDto Cursor, DateTime ExpiresAtUtc);
    public string Protect(ShopCursorScopeDto scope, ShopCursorDto cursor) => _protector.Protect(ShopPolicyHelper.Serialize(new Envelope(scope, cursor, clock.UtcNow.AddMinutes(15))));
    public ShopCursorDto Unprotect(ShopCursorScopeDto scope, string token)
    {
        try {
            if (token.Length > 2048) throw new CryptographicException();
            var data = ShopPolicyHelper.Parse<Envelope>(_protector.Unprotect(token));
            if (data.Scope != scope || data.ExpiresAtUtc <= clock.UtcNow) throw new CryptographicException();
            return data.Cursor;
        }
        catch (Exception failure) when (failure is CryptographicException or JsonException or FormatException or ArgumentException) {
            throw new HttpException(400, C.Error, C.CursorInvalid);
        }
    }
}
