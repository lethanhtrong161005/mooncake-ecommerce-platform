namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.DTOs.Auth.Models;
using Mooncake.EcommercePlatform.Application.DTOs.Users.Models;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IUserRepository"/>.</summary>
public class UserRepository(ApplicationDbContext context) : IUserRepository, IUserManagementRepository
{
    public async Task<IReadOnlyList<ManagedUserData>> GetAllManagedUsersAsync(CancellationToken cancellationToken = default) =>
        (await context.Users.AsNoTracking().Where(x => !x.IsDeleted).OrderBy(x => x.Email).ToListAsync(cancellationToken))
        .Select(ToManagedData).ToList();

    public async Task<ManagedUserData?> GetManagedUserAsync(Guid id, CancellationToken cancellationToken = default) =>
        ToManagedDataOrNull(await context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken));

    public Task<bool> ManagedEmailExistsAsync(string email, Guid? exceptUserId = null, CancellationToken cancellationToken = default) =>
        context.Users.AnyAsync(x => !x.IsDeleted && x.Email.ToLower() == email.ToLower() && (exceptUserId == null || x.Id != exceptUserId), cancellationToken);

    public Task<bool> ManagedPhoneExistsAsync(string phone, Guid? exceptUserId = null, CancellationToken cancellationToken = default) =>
        context.Users.AnyAsync(x => !x.IsDeleted && x.Phone == phone && (exceptUserId == null || x.Id != exceptUserId), cancellationToken);

    public async Task<ManagedUserData> CreateManagedUserAsync(CreateManagedUserData data, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var entity = new User
        {
            Id = Guid.NewGuid(), Email = data.Email, FullName = data.FullName, Phone = data.Phone,
            PasswordHash = data.PasswordHash, Role = data.Role == Mooncake.EcommercePlatform.Domain.Enums.UserRole.Supplier
                ? Mooncake.EcommercePlatform.Domain.Enums.UserRole.Customer : data.Role,
            RequestedRole = data.Role, IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now
        };
        context.Users.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return ToManagedData(entity);
    }

    public async Task<ManagedUserData> UpdateManagedUserAsync(UpdateManagedUserData data, CancellationToken cancellationToken = default)
    {
        var entity = await context.Users.FirstOrDefaultAsync(x => x.Id == data.Id && !x.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("User was not found.");
        entity.Email = data.Email; entity.FullName = data.FullName; entity.Phone = data.Phone;
        entity.AvatarUrl = data.AvatarUrl; entity.UpdatedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return ToManagedData(entity);
    }

    public Task DeleteManagedUserAsync(Guid id, CancellationToken cancellationToken = default) => DeleteAsync(id, cancellationToken);

    public async Task<IEnumerable<AuthUserData>> GetAllAsync(CancellationToken cancellationToken = default) =>
        (await context.Users.AsNoTracking().Where(u => !u.IsDeleted).ToListAsync(cancellationToken)).Select(ToData);

    public async Task<AuthUserData?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Map(await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken));

    public async Task<AuthUserData?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        Map(await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && !u.IsDeleted, cancellationToken));

    public async Task<AuthUserData?> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default) =>
        Map(await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Phone == phone && !u.IsDeleted, cancellationToken));

    public async Task<AuthUserData> CreateAsync(AuthUserData user, CancellationToken cancellationToken = default)
    {
        var entity = ToEntity(user);
        context.Users.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return ToData(entity);
    }

    public async Task<AuthUserData> UpdateAsync(AuthUserData user, CancellationToken cancellationToken = default)
    {
        var entity = await context.Users.FirstOrDefaultAsync(x => x.Id == user.Id && !x.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("User was not found.");
        entity.Email = user.Email; entity.PasswordHash = user.PasswordHash; entity.FullName = user.FullName;
        entity.Phone = user.Phone; entity.AvatarUrl = user.AvatarUrl; entity.FailedLoginAttempts = user.FailedLoginAttempts;
        entity.LockoutUntil = user.LockoutUntil; entity.EmailVerifiedAt = user.EmailVerifiedAt;
        entity.EmailVerificationCodeHash = user.EmailVerificationCodeHash;
        entity.EmailVerificationCodeExpiresAt = user.EmailVerificationCodeExpiresAt;
        entity.EmailVerificationCodeAttempts = user.EmailVerificationCodeAttempts;
        entity.PasswordResetCodeHash = user.PasswordResetCodeHash;
        entity.PasswordResetCodeExpiresAt = user.PasswordResetCodeExpiresAt;
        entity.PasswordResetCodeAttempts = user.PasswordResetCodeAttempts;
        entity.ContractSigningCodeHash = user.ContractSigningCodeHash;
        entity.ContractSigningCodeExpiresAt = user.ContractSigningCodeExpiresAt;
        entity.ContractSigningCodeAttempts = user.ContractSigningCodeAttempts;
        entity.UpdatedAtUtc = user.UpdatedAtUtc == default ? DateTime.UtcNow : user.UpdatedAtUtc;
        await context.SaveChangesAsync(cancellationToken);
        return ToData(entity);
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

    public async Task CreateRefreshSessionAsync(AuthRefreshSessionData session, CancellationToken cancellationToken = default)
    {
        context.RefreshSessions.Add(ToEntity(session));
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<AuthRefreshSessionData?> GetRefreshSessionByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        Map(await context.RefreshSessions.AsNoTracking().FirstOrDefaultAsync(session => session.TokenHash == tokenHash && !session.IsDeleted, cancellationToken));

    public async Task RotateRefreshSessionAsync(AuthRefreshSessionData current, AuthRefreshSessionData replacement, CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var revokedAt = DateTime.UtcNow;
        var affected = await context.RefreshSessions.Where(session => session.Id == current.Id && session.RevokedAt == null && session.ExpiresAt > revokedAt)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(session => session.RevokedAt, revokedAt)
                .SetProperty(session => session.ReplacedBySessionId, replacement.Id), cancellationToken);
        if (affected != 1) throw new Mooncake.EcommercePlatform.Application.Common.Exceptions.HttpException(401, "The refresh token is invalid or expired.");
        context.RefreshSessions.Add(ToEntity(replacement));
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task RevokeRefreshSessionAsync(AuthRefreshSessionData session, CancellationToken cancellationToken = default)
    {
        if (session.RevokedAtUtc is null)
        {
            await context.RefreshSessions.Where(x => x.Id == session.Id && x.RevokedAt == null)
                .ExecuteUpdateAsync(x => x.SetProperty(s => s.RevokedAt, DateTime.UtcNow), cancellationToken);
        }
    }

    public async Task RevokeAllRefreshSessionsAsync(Guid userId, DateTime revokedAt, CancellationToken cancellationToken = default)
    {
        await context.RefreshSessions.Where(session => session.UserId == userId && session.RevokedAt == null && !session.IsDeleted)
            .ExecuteUpdateAsync(setters => setters.SetProperty(session => session.RevokedAt, revokedAt), cancellationToken);
    }

    private static AuthUserData? Map(User? user) => user is null ? null : ToData(user);
    private static AuthUserData ToData(User user) => new()
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
    private static User ToEntity(AuthUserData user) => new()
    {
        Id = user.Id == Guid.Empty ? Guid.NewGuid() : user.Id, IsDeleted = user.IsDeleted,
        CreatedAtUtc = user.CreatedAtUtc == default ? DateTime.UtcNow : user.CreatedAtUtc,
        UpdatedAtUtc = user.UpdatedAtUtc == default ? DateTime.UtcNow : user.UpdatedAtUtc,
        CreatedBy = user.CreatedBy, UpdatedBy = user.UpdatedBy, Email = user.Email, PasswordHash = user.PasswordHash,
        FullName = user.FullName, Phone = user.Phone, AvatarUrl = user.AvatarUrl, IsActive = user.IsActive,
        FailedLoginAttempts = user.FailedLoginAttempts, LockoutUntil = user.LockoutUntil, EmailVerifiedAt = user.EmailVerifiedAt,
        Role = user.Role, RequestedRole = user.RequestedRole, EmailVerificationCodeHash = user.EmailVerificationCodeHash,
        EmailVerificationCodeExpiresAt = user.EmailVerificationCodeExpiresAt, EmailVerificationCodeAttempts = user.EmailVerificationCodeAttempts,
        PasswordResetCodeHash = user.PasswordResetCodeHash, PasswordResetCodeExpiresAt = user.PasswordResetCodeExpiresAt,
        PasswordResetCodeAttempts = user.PasswordResetCodeAttempts, ContractSigningCodeHash = user.ContractSigningCodeHash,
        ContractSigningCodeExpiresAt = user.ContractSigningCodeExpiresAt, ContractSigningCodeAttempts = user.ContractSigningCodeAttempts
    };
    private static AuthRefreshSessionData? Map(RefreshSession? session) => session is null ? null : new()
    {
        Id = session.Id, CreatedAtUtc = session.CreatedAtUtc, UpdatedAtUtc = session.UpdatedAtUtc,
        UserId = session.UserId, TokenHash = session.TokenHash, ExpiresAtUtc = session.ExpiresAt,
        RevokedAtUtc = session.RevokedAt, ReplacedBySessionId = session.ReplacedBySessionId,
        CreatedFromIp = session.CreatedFromIp, UserAgent = session.UserAgent
    };
    private static ManagedUserData? ToManagedDataOrNull(User? user) => user is null ? null : ToManagedData(user);
    private static ManagedUserData ToManagedData(User user) => new(user.Id, user.Email, user.FullName, user.Phone,
        user.AvatarUrl, user.Role, user.CreatedAtUtc, user.UpdatedAtUtc);
    private static RefreshSession ToEntity(AuthRefreshSessionData session) => new()
    {
        Id = session.Id == Guid.Empty ? Guid.NewGuid() : session.Id, UserId = session.UserId,
        TokenHash = session.TokenHash, ExpiresAt = session.ExpiresAtUtc, RevokedAt = session.RevokedAtUtc,
        ReplacedBySessionId = session.ReplacedBySessionId, CreatedFromIp = session.CreatedFromIp,
        UserAgent = session.UserAgent, CreatedAtUtc = session.CreatedAtUtc == default ? DateTime.UtcNow : session.CreatedAtUtc,
        UpdatedAtUtc = session.UpdatedAtUtc == default ? DateTime.UtcNow : session.UpdatedAtUtc
    };
}
