namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Auth.Models;

/// <summary>Persistence contract for refresh token lifecycle operations.</summary>
public interface IRefreshTokenRepository
{
    Task CreateAsync(AuthRefreshTokenData token, CancellationToken cancellationToken = default);
    Task<AuthRefreshTokenData?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<bool> RotateAsync(
        string oldHash,
        AuthRefreshTokenData replacement,
        DateTime nowUtc,
        CancellationToken cancellationToken = default);
    Task RevokeAsync(string tokenHash, DateTime nowUtc, CancellationToken cancellationToken = default);
    Task RevokeAllForUserAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken = default);
}
