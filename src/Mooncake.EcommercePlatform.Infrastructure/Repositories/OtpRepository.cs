namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;
using Npgsql;

/// <summary>Serializes each OTP state transition in PostgreSQL.</summary>
public class OtpRepository(ApplicationDbContext context) : IOtpRepository
{
    private const string Purpose = "RegisterAccount";

    public async Task IssueAsync(string phone, string otpHash, DateTime nowUtc, bool resend, CancellationToken cancellationToken)
    {
        try
        {
            await using var transaction = await context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable, cancellationToken);
            var record = await context.OtpVerifications.SingleOrDefaultAsync(
                x => x.Phone == phone && x.Purpose == Purpose, cancellationToken);
            if (record is null)
            {
                if (resend) throw new HttpException(400, MessageConstant.OtpSessionNotFound);
                record = new OtpVerification
                {
                    Id = Guid.NewGuid(), Phone = phone, Purpose = Purpose,
                    WindowStartedAtUtc = nowUtc, CreatedAtUtc = nowUtc
                };
                context.OtpVerifications.Add(record);
            }
            else
            {
                var seconds = 60 - (int)Math.Floor((nowUtc - record.LastSentAtUtc).TotalSeconds);
                if (seconds > 0)
                    throw new HttpException(429, $"Vui lòng chờ {seconds} giây trước khi yêu cầu gửi lại mã OTP.");
                if (record.WindowStartedAtUtc <= nowUtc.AddHours(-1))
                {
                    record.WindowStartedAtUtc = nowUtc;
                    record.ResendCount = 0;
                }
                else if (record.ResendCount >= 4)
                    throw new HttpException(429, MessageConstant.OtpSendQuotaExceeded);
                record.ResendCount++;
            }

            record.OtpHash = otpHash;
            record.ExpiresAtUtc = nowUtc.AddMinutes(5);
            record.LastSentAtUtc = nowUtc;
            record.FailedAttempts = 0;
            record.IsVerified = false;
            record.VerifiedAtUtc = null;
            record.VerificationToken = null;
            record.TokenExpiresAtUtc = null;
            record.ConsumedAtUtc = null;
            record.IsDeleted = false;
            record.UpdatedAtUtc = nowUtc;
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex) when (IsConcurrencyConflict(ex))
        {
            throw new HttpException(429, MessageConstant.OtpRequestConflict);
        }
    }

    /// <returns>Remaining attempts; -1 for success.</returns>
    public async Task<int> VerifyAsync(string phone, string otpHash, string tokenHash, DateTime nowUtc, CancellationToken cancellationToken)
    {
        try
        {
            await using var transaction = await context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable, cancellationToken);
            var record = await context.OtpVerifications.SingleOrDefaultAsync(
                x => x.Phone == phone && x.Purpose == Purpose, cancellationToken);
            if (record is null || record.IsDeleted || record.ExpiresAtUtc <= nowUtc || record.ConsumedAtUtc is not null)
                throw new HttpException(400, MessageConstant.OtpExpiredOrNotFound);
            if (record.FailedAttempts >= 5)
                throw new HttpException(400, MessageConstant.OtpMaxAttemptsExceeded);
            if (record.IsVerified)
                throw new HttpException(400, MessageConstant.VerificationSessionExpired);

            var matches = CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(otpHash), Convert.FromHexString(record.OtpHash));
            if (!matches)
            {
                record.FailedAttempts++;
                if (record.FailedAttempts >= 5) record.IsDeleted = true;
            }
            else
            {
                record.IsVerified = true;
                record.VerifiedAtUtc = nowUtc;
                record.VerificationToken = tokenHash;
                record.TokenExpiresAtUtc = nowUtc.AddMinutes(10);
            }
            record.UpdatedAtUtc = nowUtc;
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return matches ? -1 : 5 - record.FailedAttempts;
        }
        catch (Exception ex) when (IsConcurrencyConflict(ex))
        {
            throw new HttpException(429, MessageConstant.OtpRequestConflict);
        }
    }

    public async Task<User> RegisterAsync(string phone, string tokenHash, string fullName,
        string passwordHash, DateTime nowUtc, CancellationToken cancellationToken)
    {
        try
        {
            await using var transaction = await context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable, cancellationToken);
            var record = await context.OtpVerifications.SingleOrDefaultAsync(
                x => x.VerificationToken == tokenHash && x.Purpose == Purpose, cancellationToken);
            if (record is null || record.Phone != phone || !record.IsVerified || record.IsDeleted ||
                record.TokenExpiresAtUtc <= nowUtc || record.ConsumedAtUtc is not null)
                throw new HttpException(400, MessageConstant.VerificationSessionExpired);
            if (await context.Users.AnyAsync(x => x.Phone == phone && !x.IsDeleted, cancellationToken))
                throw new HttpException(409, MessageConstant.PhoneAlreadyRegistered);

            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId, Phone = phone,
                Email = $"customer-{userId:N}@mooncake.local",
                FullName = fullName, PasswordHash = passwordHash,
                IsActive = true, Role = UserRole.Customer, RequestedRole = UserRole.Customer,
                CreatedAtUtc = nowUtc, UpdatedAtUtc = nowUtc
            };
            context.Users.Add(user);
            record.ConsumedAtUtc = nowUtc;
            record.UpdatedAtUtc = nowUtc;
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return user;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new HttpException(409, MessageConstant.PhoneAlreadyRegistered);
        }
        catch (Exception ex) when (IsConcurrencyConflict(ex))
        {
            throw new HttpException(400, MessageConstant.VerificationSessionExpired);
        }
    }

    private static bool IsConcurrencyConflict(Exception ex) =>
        ex is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.UniqueViolation } ||
        ex is DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.UniqueViolation } };
}
