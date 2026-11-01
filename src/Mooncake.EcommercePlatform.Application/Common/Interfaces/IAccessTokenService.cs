namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Auth.Models;

/// <summary>Creates signed access tokens for authenticated users.</summary>
public interface IAccessTokenService
{
    string CreateToken(AuthUserData user);
    DateTime GetExpirationUtc();
}
