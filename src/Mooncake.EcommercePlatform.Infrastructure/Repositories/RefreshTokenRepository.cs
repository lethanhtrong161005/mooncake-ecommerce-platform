namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

/// <summary>EF Core refresh token persistence with atomic rotation.</summary>
public class RefreshTokenRepository(ApplicationDbContext context) : IRefreshTokenRepository
{
    public async Task CreateAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        context.RefreshTokens.Add(token);
        await context.SaveChangesAsync(cancellationToken);
    }

    public Task<RefreshToken?> GetByHashAsync(
        string tokenHash, CancellationToken cancellationToken = default) =>
        context.RefreshTokens.AsNoTracking()
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public async Task<bool> RotateAsync(
        string oldHash,
        RefreshToken replacement,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var updated = await context.RefreshTokens
            .Where(token => token.TokenHash == oldHash && token.RevokedAtUtc == null
                && token.ExpiresAtUtc > nowUtc && !token.IsDeleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(token => token.RevokedAtUtc, nowUtc)
                .SetProperty(token => token.ReplacedByTokenHash, replacement.TokenHash),
                cancellationToken);

        if (updated != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        context.RefreshTokens.Add(replacement);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public Task RevokeAsync(string tokenHash, DateTime nowUtc, CancellationToken cancellationToken = default) =>
        context.RefreshTokens
            .Where(token => token.TokenHash == tokenHash && token.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAtUtc, nowUtc),
                cancellationToken);

    public Task RevokeAllForUserAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken = default) =>
        context.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAtUtc == null && !token.IsDeleted)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAtUtc, nowUtc),
                cancellationToken);
}
