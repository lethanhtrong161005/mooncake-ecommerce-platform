namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IUserRepository"/>.</summary>
public class UserRepository(ApplicationDbContext context) : IUserRepository
{
    public async Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.Users.AsNoTracking().Where(u => !u.IsDeleted).ToListAsync(cancellationToken);

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && !u.IsDeleted, cancellationToken);

    public async Task<User> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        context.Users.Update(user);
        await context.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await context.Users.FindAsync([id], cancellationToken);
        if (user is not null && !user.IsDeleted)
        {
            user.IsDeleted = true;
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task CreateRefreshSessionAsync(RefreshSession session, CancellationToken cancellationToken = default)
    {
        context.RefreshSessions.Add(session);
        await context.SaveChangesAsync(cancellationToken);
    }

    public Task<RefreshSession?> GetRefreshSessionByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        context.RefreshSessions.FirstOrDefaultAsync(session => session.TokenHash == tokenHash && !session.IsDeleted, cancellationToken);

    public async Task RotateRefreshSessionAsync(RefreshSession current, RefreshSession replacement, CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var revokedAt = DateTime.UtcNow;
        var affected = await context.RefreshSessions.Where(session => session.Id == current.Id && session.RevokedAt == null && session.ExpiresAt > revokedAt)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(session => session.RevokedAt, revokedAt)
                .SetProperty(session => session.ReplacedBySessionId, replacement.Id), cancellationToken);
        if (affected != 1) throw new Mooncake.EcommercePlatform.Application.Common.Exceptions.HttpException(401, "The refresh token is invalid or expired.");
        context.RefreshSessions.Add(replacement);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task RevokeRefreshSessionAsync(RefreshSession session, CancellationToken cancellationToken = default)
    {
        if (session.RevokedAt is null)
        {
            session.RevokedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RevokeAllRefreshSessionsAsync(Guid userId, DateTime revokedAt, CancellationToken cancellationToken = default)
    {
        await context.RefreshSessions.Where(session => session.UserId == userId && session.RevokedAt == null && !session.IsDeleted)
            .ExecuteUpdateAsync(setters => setters.SetProperty(session => session.RevokedAt, revokedAt), cancellationToken);
    }
}
