namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Persistence contract for refresh token lifecycle operations.</summary>
public interface IRefreshTokenRepository
{
    Task CreateAsync(RefreshToken token, CancellationToken cancellationToken = default);
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<bool> RotateAsync(
        string oldHash,
        RefreshToken replacement,
        DateTime nowUtc,
        CancellationToken cancellationToken = default);
    Task RevokeAsync(string tokenHash, DateTime nowUtc, CancellationToken cancellationToken = default);
    Task RevokeAllForUserAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken = default);
}
