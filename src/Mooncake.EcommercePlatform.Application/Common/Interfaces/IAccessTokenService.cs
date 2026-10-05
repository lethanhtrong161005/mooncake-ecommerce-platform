namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Creates signed access tokens for authenticated users.</summary>
public interface IAccessTokenService
{
    string CreateToken(User user);
    DateTime GetExpirationUtc();
}
