namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using System.Security.Cryptography;
using System.Text;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Auth.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Auth.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Implements account registration, verification and token lifecycle use cases.</summary>
public sealed class AuthService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IAccessTokenService accessTokenService,
    IUserHelper userHelper,
    IDateTimeProvider dateTimeProvider,
    IEmailSender emailSender) : IAuthService
{
    private const string InvalidCredentialsMessage = "The email or password is incorrect.";
    private const string AccountLockedMessage = "The account is temporarily locked. Try again later.";
    private const string AccountInactiveMessage = "The account is inactive.";
    private const string VerificationRequiredMessage = "Verify your email address before signing in.";
    private const string InvalidOtpMessage = "The verification code is invalid or expired.";
    private const string VerificationSubject = "Mooncake email verification code";
    private const string ResetSubject = "Mooncake password reset code";
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan RefreshLifetime = TimeSpan.FromDays(30);
    private const int MaxFailedAttempts = 5;
    private const int MaxOtpAttempts = 5;

    public async Task RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        if (await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken) is not null)
            throw new HttpException(409, "An account with this email already exists.");

        var now = dateTimeProvider.UtcNow;
        var user = new User
        {
            Email = normalizedEmail,
            PasswordHash = passwordHasher.Hash(request.Password),
            FullName = request.FullName?.Trim(),
            Phone = request.Phone?.Trim(),
            Role = UserRole.Customer,
            RequestedRole = Enum.Parse<UserRole>(request.Role, ignoreCase: false),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        var code = GenerateCode();
        SetEmailCode(user, code);
        await userRepository.CreateAsync(user, cancellationToken);
        await emailSender.SendAsync(user.Email, VerificationSubject, $"Your Mooncake verification code is {code}. It expires in 10 minutes.", cancellationToken);
    }

    public async Task<AuthResponse> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default)
    {
        var user = await GetUserForCodeAsync(request.Email, cancellationToken);
        if (!user.IsActive) throw new HttpException(403, AccountInactiveMessage);
        if (user.EmailVerifiedAt is not null) throw new HttpException(409, "Email address is already verified.");

        if (!IsCodeValid(user.EmailVerificationCodeHash, user.EmailVerificationCodeExpiresAt, user.EmailVerificationCodeAttempts,
                request.Email, "email", request.Code))
        {
            user.EmailVerificationCodeAttempts++;
            await userRepository.UpdateAsync(user, cancellationToken);
            throw new HttpException(400, InvalidOtpMessage);
        }

        user.EmailVerifiedAt = dateTimeProvider.UtcNow;
        user.EmailVerificationCodeHash = null;
        user.EmailVerificationCodeExpiresAt = null;
        user.EmailVerificationCodeAttempts = 0;
        await userRepository.UpdateAsync(user, cancellationToken);
        return await CreateResponseAsync(user, null, null, cancellationToken);
    }

    public async Task ResendVerificationAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByEmailAsync(NormalizeEmail(email), cancellationToken);
        if (user is null || user.EmailVerifiedAt is not null) return;
        var code = GenerateCode();
        SetEmailCode(user, code);
        await userRepository.UpdateAsync(user, cancellationToken);
        await emailSender.SendAsync(user.Email, VerificationSubject, $"Your Mooncake verification code is {code}. It expires in 10 minutes.", cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByEmailAsync(NormalizeEmail(request.Email), cancellationToken);
        if (user?.LockoutUntil is DateTime activeLockout && activeLockout > dateTimeProvider.UtcNow)
            throw new HttpException(423, AccountLockedMessage);
        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            if (user is not null)
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= MaxFailedAttempts)
                {
                    user.FailedLoginAttempts = 0;
                    user.LockoutUntil = dateTimeProvider.UtcNow.Add(LockoutDuration);
                }
                await userRepository.UpdateAsync(user, cancellationToken);
            }
            throw new HttpException(401, InvalidCredentialsMessage);
        }
        if (!user.IsActive) throw new HttpException(403, AccountInactiveMessage);
        if (user.EmailVerifiedAt is null) throw new HttpException(403, VerificationRequiredMessage);

        user.FailedLoginAttempts = 0;
        user.LockoutUntil = null;
        if (passwordHasher.NeedsRehash(user.PasswordHash)) user.PasswordHash = passwordHasher.Hash(request.Password);
        await userRepository.UpdateAsync(user, cancellationToken);
        return await CreateResponseAsync(user, null, null, cancellationToken);
    }

    public async Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByEmailAsync(NormalizeEmail(email), cancellationToken);
        if (user is null || !user.IsActive || user.EmailVerifiedAt is null) return;
        var code = GenerateCode();
        user.PasswordResetCodeHash = HashCode(user.Email, "reset", code);
        user.PasswordResetCodeExpiresAt = dateTimeProvider.UtcNow.Add(OtpLifetime);
        user.PasswordResetCodeAttempts = 0;
        await userRepository.UpdateAsync(user, cancellationToken);
        await emailSender.SendAsync(user.Email, ResetSubject, $"Your Mooncake password reset code is {code}. It expires in 10 minutes.", cancellationToken);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await GetUserForCodeAsync(request.Email, cancellationToken);
        if (!user.IsActive || user.EmailVerifiedAt is null) throw new HttpException(400, InvalidOtpMessage);
        if (!IsCodeValid(user.PasswordResetCodeHash, user.PasswordResetCodeExpiresAt, user.PasswordResetCodeAttempts,
                request.Email, "reset", request.Code))
        {
            user.PasswordResetCodeAttempts++;
            await userRepository.UpdateAsync(user, cancellationToken);
            throw new HttpException(400, InvalidOtpMessage);
        }
        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        user.FailedLoginAttempts = 0;
        user.LockoutUntil = null;
        user.PasswordResetCodeHash = null;
        user.PasswordResetCodeExpiresAt = null;
        user.PasswordResetCodeAttempts = 0;
        await userRepository.UpdateAsync(user, cancellationToken);
        await userRepository.RevokeAllRefreshSessionsAsync(user.Id, dateTimeProvider.UtcNow, cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(string refreshToken, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        var current = await userRepository.GetRefreshSessionByHashAsync(HashToken(refreshToken), cancellationToken);
        if (current is null || current.RevokedAt is not null || current.ExpiresAt <= dateTimeProvider.UtcNow)
            throw new HttpException(401, "The refresh token is invalid or expired.");
        var user = await userRepository.GetByIdAsync(current.UserId, cancellationToken);
        if (user is null || !user.IsActive || user.EmailVerifiedAt is null)
            throw new HttpException(401, "The account is unavailable.");
        var replacement = CreateRefreshSession(user.Id, ipAddress, userAgent);
        await userRepository.RotateRefreshSessionAsync(current, replacement.Session, cancellationToken);
        return new AuthResponse(accessTokenService.CreateToken(user), replacement.Token, accessTokenService.GetExpirationUtc(), userHelper.ToResponse(user));
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var session = await userRepository.GetRefreshSessionByHashAsync(HashToken(refreshToken), cancellationToken);
        if (session is not null) await userRepository.RevokeRefreshSessionAsync(session, cancellationToken);
    }

    public Task RevokeAllRefreshTokensAsync(Guid userId, CancellationToken cancellationToken = default) =>
        userRepository.RevokeAllRefreshSessionsAsync(userId, dateTimeProvider.UtcNow, cancellationToken);

    private async Task<AuthResponse> CreateResponseAsync(User user, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
    {
        var refresh = CreateRefreshSession(user.Id, ipAddress, userAgent);
        await userRepository.CreateRefreshSessionAsync(refresh.Session, cancellationToken);
        return new AuthResponse(accessTokenService.CreateToken(user), refresh.Token, accessTokenService.GetExpirationUtc(), userHelper.ToResponse(user));
    }

    private (string Token, RefreshSession Session) CreateRefreshSession(Guid userId, string? ipAddress, string? userAgent)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var session = new RefreshSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = HashToken(token),
            ExpiresAt = dateTimeProvider.UtcNow.Add(RefreshLifetime),
            CreatedFromIp = ipAddress,
            UserAgent = userAgent
        };
        return (token, session);
    }

    private async Task<User> GetUserForCodeAsync(string email, CancellationToken cancellationToken) =>
        await userRepository.GetByEmailAsync(NormalizeEmail(email), cancellationToken)
        ?? throw new HttpException(400, InvalidOtpMessage);

    private void SetEmailCode(User user, string code)
    {
        user.EmailVerificationCodeHash = HashCode(user.Email, "email", code);
        user.EmailVerificationCodeExpiresAt = dateTimeProvider.UtcNow.Add(OtpLifetime);
        user.EmailVerificationCodeAttempts = 0;
    }

    private bool IsCodeValid(string? storedHash, DateTime? expiresAt, int attempts, string email, string purpose, string code)
    {
        if (storedHash is null || expiresAt <= dateTimeProvider.UtcNow || attempts >= MaxOtpAttempts) return false;
        var expected = Encoding.UTF8.GetBytes(storedHash);
        var actual = Encoding.UTF8.GetBytes(HashCode(NormalizeEmail(email), purpose, code));
        return expected.Length == actual.Length && CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
    private static string GenerateCode() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
    private static string HashCode(string email, string purpose, string code)
    {
        var secret = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? throw new InvalidOperationException("JWT_SECRET_KEY is not configured.");
        return Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes($"{purpose}:{NormalizeEmail(email)}:{code}")));
    }
    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
