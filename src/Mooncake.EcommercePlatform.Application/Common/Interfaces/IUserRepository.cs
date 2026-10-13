namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Data-access contract for the User aggregate.</summary>
public interface IUserRepository
{
    Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User> CreateAsync(User user, CancellationToken cancellationToken = default);
    Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task CreateRefreshSessionAsync(RefreshSession session, CancellationToken cancellationToken = default);
    Task<RefreshSession?> GetRefreshSessionByHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task RotateRefreshSessionAsync(RefreshSession current, RefreshSession replacement, CancellationToken cancellationToken = default);
    Task RevokeRefreshSessionAsync(RefreshSession session, CancellationToken cancellationToken = default);
    Task RevokeAllRefreshSessionsAsync(Guid userId, DateTime revokedAt, CancellationToken cancellationToken = default);
}
