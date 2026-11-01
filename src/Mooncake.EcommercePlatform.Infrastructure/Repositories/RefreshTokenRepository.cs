namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Auth.Models;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

/// <summary>EF Core refresh token persistence with atomic rotation.</summary>
public class RefreshTokenRepository(ApplicationDbContext context) : GenericRepository<RefreshToken>(context), IRefreshTokenRepository
{
    public async Task CreateAsync(AuthRefreshTokenData data, CancellationToken cancellationToken = default)
    {
        Context.RefreshTokens.Add(ToEntity(data));
        await Context.SaveChangesAsync(cancellationToken);
    }

    public async Task<AuthRefreshTokenData?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        var token = await Context.RefreshTokens.AsNoTracking().FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);
        return token is null ? null : ToData(token);
    }

    public async Task<bool> RotateAsync(
        string oldHash,
        AuthRefreshTokenData replacement,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await Context.Database.BeginTransactionAsync(cancellationToken);
        var updated = await Context.RefreshTokens
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

        Context.RefreshTokens.Add(ToEntity(replacement));
        await Context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public Task RevokeAsync(string tokenHash, DateTime nowUtc, CancellationToken cancellationToken = default) =>
        Context.RefreshTokens
            .Where(token => token.TokenHash == tokenHash && token.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAtUtc, nowUtc),
                cancellationToken);

    public Task RevokeAllForUserAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken = default) =>
        Context.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAtUtc == null && !token.IsDeleted)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAtUtc, nowUtc),
                cancellationToken);

    private static AuthRefreshTokenData ToData(RefreshToken token) => new()
    {
        Id = token.Id, UserId = token.UserId, TokenHash = token.TokenHash, ExpiresAtUtc = token.ExpiresAtUtc,
        RevokedAtUtc = token.RevokedAtUtc, ReplacedByTokenHash = token.ReplacedByTokenHash,
        IsDeleted = token.IsDeleted, CreatedAtUtc = token.CreatedAtUtc, UpdatedAtUtc = token.UpdatedAtUtc,
        CreatedBy = token.CreatedBy, UpdatedBy = token.UpdatedBy
    };

    private static RefreshToken ToEntity(AuthRefreshTokenData token) => new()
    {
        Id = token.Id, UserId = token.UserId, TokenHash = token.TokenHash, ExpiresAtUtc = token.ExpiresAtUtc,
        RevokedAtUtc = token.RevokedAtUtc, ReplacedByTokenHash = token.ReplacedByTokenHash,
        IsDeleted = token.IsDeleted, CreatedAtUtc = token.CreatedAtUtc, UpdatedAtUtc = token.UpdatedAtUtc,
        CreatedBy = token.CreatedBy, UpdatedBy = token.UpdatedBy
    };
}
