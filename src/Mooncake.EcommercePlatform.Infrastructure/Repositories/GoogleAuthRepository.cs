namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Auth.Models;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;
using Npgsql;

/// <summary>Database-backed Google login attempts and account lookup.</summary>
public class GoogleAuthRepository(ApplicationDbContext context) : GenericRepository<GoogleLoginAttempt>(context), IGoogleAuthRepository
{
    private const string Provider = "Google";

    public async Task CreateAttemptAsync(GoogleLoginAttemptData data, CancellationToken cancellationToken)
    {
        var attempt = new GoogleLoginAttempt
        {
            Id = data.Id, StateHash = data.StateHash, CodeVerifier = data.CodeVerifier, Nonce = data.Nonce,
            ExpiresAtUtc = data.ExpiresAtUtc, CreatedAtUtc = data.CreatedAtUtc, UpdatedAtUtc = data.UpdatedAtUtc
        };
        Context.GoogleLoginAttempts.Add(attempt);
        await Context.SaveChangesAsync(cancellationToken);
    }

    public async Task CleanupExpiredAttemptsAsync(DateTime cutoffUtc, CancellationToken cancellationToken) =>
        await Context.GoogleLoginAttempts
            .Where(x => x.ExpiresAtUtc < cutoffUtc)
            .ExecuteDeleteAsync(cancellationToken);

    public async Task<GoogleLoginAttemptData?> ConsumeAttemptAsync(
        string stateHash, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var count = await Context.GoogleLoginAttempts
            .Where(x => x.StateHash == stateHash && x.ConsumedAtUtc == null && x.ExpiresAtUtc > nowUtc)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.ConsumedAtUtc, nowUtc), cancellationToken);
        if (count != 1) return null;
        var attempt = await Context.GoogleLoginAttempts.AsNoTracking().SingleAsync(x => x.StateHash == stateHash, cancellationToken);
        return new GoogleLoginAttemptData
        {
            Id = attempt.Id, StateHash = attempt.StateHash, CodeVerifier = attempt.CodeVerifier, Nonce = attempt.Nonce,
            ExpiresAtUtc = attempt.ExpiresAtUtc, ConsumedAtUtc = attempt.ConsumedAtUtc,
            CreatedAtUtc = attempt.CreatedAtUtc, UpdatedAtUtc = attempt.UpdatedAtUtc
        };
    }

    public async Task<AuthUserData> FindOrCreateUserAsync(
        string subject, string email, string? name, string? picture,
        DateTime nowUtc, CancellationToken cancellationToken)
    {
        var existing = await FindBySubjectAsync(subject, cancellationToken);
        if (existing is not null) return EnsureActive(existing);

        try
        {
            await using var transaction = await Context.Database.BeginTransactionAsync(cancellationToken);
            if (await Context.Users.AnyAsync(
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
            Context.Users.Add(user);
            Context.UserExternalLogins.Add(new UserExternalLogin
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Provider = Provider,
                ProviderSubject = subject,
                CreatedAtUtc = nowUtc,
                UpdatedAtUtc = nowUtc
            });
            await Context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return ToAuthData(user);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            Context.ChangeTracker.Clear();
            existing = await FindBySubjectAsync(subject, cancellationToken);
            if (existing is not null) return EnsureActive(existing);
            throw new HttpException(409, MessageConstant.GoogleEmailConflict);
        }
    }

    private async Task<User?> FindBySubjectAsync(string subject, CancellationToken cancellationToken) =>
        await Context.UserExternalLogins.AsNoTracking()
            .Where(x => x.Provider == Provider && x.ProviderSubject == subject)
            .Select(x => x.User)
            .FirstOrDefaultAsync(cancellationToken);

    private static AuthUserData EnsureActive(User user) => user.IsDeleted || !user.IsActive
        ? throw new HttpException(401, MessageConstant.GoogleUserInactive)
        : ToAuthData(user);

    private static AuthUserData ToAuthData(User user) => new()
    {
        Id = user.Id, IsDeleted = user.IsDeleted, CreatedAtUtc = user.CreatedAtUtc, UpdatedAtUtc = user.UpdatedAtUtc,
        CreatedBy = user.CreatedBy, UpdatedBy = user.UpdatedBy, Email = user.Email, PasswordHash = user.PasswordHash,
        FullName = user.FullName, Phone = user.Phone, AvatarUrl = user.AvatarUrl, IsActive = user.IsActive,
        FailedLoginAttempts = user.FailedLoginAttempts, LockoutUntil = user.LockoutUntil, EmailVerifiedAt = user.EmailVerifiedAt,
        Role = user.Role, RequestedRole = user.RequestedRole, EmailVerificationCodeHash = user.EmailVerificationCodeHash,
        EmailVerificationCodeExpiresAt = user.EmailVerificationCodeExpiresAt, EmailVerificationCodeAttempts = user.EmailVerificationCodeAttempts,
        PasswordResetCodeHash = user.PasswordResetCodeHash, PasswordResetCodeExpiresAt = user.PasswordResetCodeExpiresAt,
        PasswordResetCodeAttempts = user.PasswordResetCodeAttempts, ContractSigningCodeHash = user.ContractSigningCodeHash,
        ContractSigningCodeExpiresAt = user.ContractSigningCodeExpiresAt, ContractSigningCodeAttempts = user.ContractSigningCodeAttempts
    };
}
