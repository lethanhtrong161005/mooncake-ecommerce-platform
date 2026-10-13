namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Persists one-use Google attempts and external identities.</summary>
public interface IGoogleAuthRepository
{
    Task CreateAttemptAsync(GoogleLoginAttempt attempt, CancellationToken cancellationToken);
    Task CleanupExpiredAttemptsAsync(DateTime cutoffUtc, CancellationToken cancellationToken);
    Task<GoogleLoginAttempt?> ConsumeAttemptAsync(
        string stateHash, DateTime nowUtc, CancellationToken cancellationToken);
    Task<User> FindOrCreateUserAsync(
        string subject, string email, string? name, string? picture,
        DateTime nowUtc, CancellationToken cancellationToken);
}
