namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Auth.Models;

/// <summary>Data-access contract for the User aggregate.</summary>
public interface IUserRepository
{
    Task<IEnumerable<AuthUserData>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<AuthUserData?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AuthUserData?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<AuthUserData?> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default);
    Task<AuthUserData> CreateAsync(AuthUserData user, CancellationToken cancellationToken = default);
    Task<AuthUserData> UpdateAsync(AuthUserData user, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task CreateRefreshSessionAsync(AuthRefreshSessionData session, CancellationToken cancellationToken = default);
    Task<AuthRefreshSessionData?> GetRefreshSessionByHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task RotateRefreshSessionAsync(AuthRefreshSessionData current, AuthRefreshSessionData replacement, CancellationToken cancellationToken = default);
    Task RevokeRefreshSessionAsync(AuthRefreshSessionData session, CancellationToken cancellationToken = default);
    Task RevokeAllRefreshSessionsAsync(Guid userId, DateTime revokedAt, CancellationToken cancellationToken = default);
}
