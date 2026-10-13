namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;
using Npgsql;

/// <summary>Database-backed Google login attempts and account lookup.</summary>
public class GoogleAuthRepository(ApplicationDbContext context) : IGoogleAuthRepository
{
    private const string Provider = "Google";

    public async Task CreateAttemptAsync(GoogleLoginAttempt attempt, CancellationToken cancellationToken)
    {
        context.GoogleLoginAttempts.Add(attempt);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task CleanupExpiredAttemptsAsync(DateTime cutoffUtc, CancellationToken cancellationToken) =>
        await context.GoogleLoginAttempts
            .Where(x => x.ExpiresAtUtc < cutoffUtc)
            .ExecuteDeleteAsync(cancellationToken);

    public async Task<GoogleLoginAttempt?> ConsumeAttemptAsync(
        string stateHash, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var count = await context.GoogleLoginAttempts
            .Where(x => x.StateHash == stateHash && x.ConsumedAtUtc == null && x.ExpiresAtUtc > nowUtc)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.ConsumedAtUtc, nowUtc), cancellationToken);
        return count == 1
            ? await context.GoogleLoginAttempts.AsNoTracking()
                .SingleAsync(x => x.StateHash == stateHash, cancellationToken)
            : null;
    }

    public async Task<User> FindOrCreateUserAsync(
        string subject, string email, string? name, string? picture,
        DateTime nowUtc, CancellationToken cancellationToken)
    {
        var existing = await FindBySubjectAsync(subject, cancellationToken);
        if (existing is not null) return EnsureActive(existing);

        try
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            if (await context.Users.AnyAsync(
                x => x.Email.ToLower() == email.ToLower(), cancellationToken))
            {
                existing = await FindBySubjectAsync(subject, cancellationToken);
                if (existing is not null) return EnsureActive(existing);
                throw new HttpException(409, MessageConstant.GoogleEmailConflict);
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = null,
                Phone = null,
                FullName = name,
                AvatarUrl = picture,
                EmailVerifiedAt = nowUtc,
                IsActive = true,
                Role = UserRole.Customer,
                CreatedAtUtc = nowUtc,
                UpdatedAtUtc = nowUtc
            };
            context.Users.Add(user);
            context.UserExternalLogins.Add(new UserExternalLogin
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Provider = Provider,
                ProviderSubject = subject,
                CreatedAtUtc = nowUtc,
                UpdatedAtUtc = nowUtc
            });
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return user;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            context.ChangeTracker.Clear();
            existing = await FindBySubjectAsync(subject, cancellationToken);
            if (existing is not null) return EnsureActive(existing);
            throw new HttpException(409, MessageConstant.GoogleEmailConflict);
        }
    }

    private async Task<User?> FindBySubjectAsync(string subject, CancellationToken cancellationToken) =>
        await context.UserExternalLogins.AsNoTracking()
            .Where(x => x.Provider == Provider && x.ProviderSubject == subject)
            .Select(x => x.User)
            .FirstOrDefaultAsync(cancellationToken);

    private static User EnsureActive(User user) =>
        user.IsDeleted || !user.IsActive
            ? throw new HttpException(401, MessageConstant.GoogleUserInactive)
            : user;
}
