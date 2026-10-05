namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Auth.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Auth.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Implements customer registration and password sign-in.</summary>
public sealed class AuthService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IAccessTokenService accessTokenService,
    IUserHelper userHelper,
    IDateTimeProvider dateTimeProvider) : IAuthService
{
    private const string InvalidCredentialsMessage = "The email or password is incorrect.";
    private const string AccountLockedMessage = "The account is temporarily locked. Try again later.";
    private const string AccountInactiveMessage = "The account is inactive.";
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    private const int MaxFailedAttempts = 5;

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken) is not null)
            throw new HttpException(409, "An account with this email already exists.");

        var user = new User
        {
            Email = normalizedEmail,
            PasswordHash = passwordHasher.Hash(request.Password),
            FullName = request.FullName?.Trim(),
            Phone = request.Phone?.Trim(),
            Role = UserRole.Customer,
            IsActive = true
        };

        var created = await userRepository.CreateAsync(user, cancellationToken);
        return CreateResponse(created);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);
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

        if (!user.IsActive)
            throw new HttpException(403, AccountInactiveMessage);

        if (user.LockoutUntil is DateTime lockoutUntil && lockoutUntil > dateTimeProvider.UtcNow)
            throw new HttpException(423, AccountLockedMessage);

        user.FailedLoginAttempts = 0;
        user.LockoutUntil = null;
        if (passwordHasher.NeedsRehash(user.PasswordHash))
            user.PasswordHash = passwordHasher.Hash(request.Password);
        await userRepository.UpdateAsync(user, cancellationToken);
        return CreateResponse(user);
    }

    private AuthResponse CreateResponse(User user)
    {
        var expiresAtUtc = accessTokenService.GetExpirationUtc();
        return new AuthResponse(accessTokenService.CreateToken(user), expiresAtUtc, userHelper.ToResponse(user));
    }
}
