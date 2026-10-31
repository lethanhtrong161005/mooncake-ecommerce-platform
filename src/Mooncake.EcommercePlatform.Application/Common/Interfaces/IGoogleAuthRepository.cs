namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Auth.Models;

/// <summary>Persists one-use Google attempts and external identities.</summary>
public interface IGoogleAuthRepository
{
    Task CreateAttemptAsync(GoogleLoginAttemptData attempt, CancellationToken cancellationToken);
    Task CleanupExpiredAttemptsAsync(DateTime cutoffUtc, CancellationToken cancellationToken);
    Task<GoogleLoginAttemptData?> ConsumeAttemptAsync(
        string stateHash, DateTime nowUtc, CancellationToken cancellationToken);
    Task<AuthUserData> FindOrCreateUserAsync(
        string subject, string email, string? name, string? picture,
        DateTime nowUtc, CancellationToken cancellationToken);
}
