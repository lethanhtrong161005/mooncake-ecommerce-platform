namespace Mooncake.EcommercePlatform.Verification.Tests;

using Microsoft.Extensions.Options;
using Moq;
using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.Common.Options;
using Mooncake.EcommercePlatform.Application.Common.Utils;
using Mooncake.EcommercePlatform.Application.DTOs.Auth.Models;
using Mooncake.EcommercePlatform.Application.DTOs.Authen.Requests;
using Mooncake.EcommercePlatform.Application.Services.Implementations;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Xunit;

/// <summary>Tests security policies (lockout, email verification, active status) on phone login.</summary>
public sealed class PhoneLoginSecurityTests
{
    private const string ValidPhone = "0987654321";
    private const string RawPassword = "Password123@";
    private const string WrongPassword = "WrongPassword123@";
    private const string ExpectedAccountLockedMessage = "The account is temporarily locked. Try again later.";
    private const string ExpectedVerificationRequiredMessage = "Verify your email address before signing in.";
    private const string ExpectedAccountInactiveMessage = "The account is inactive.";

    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ISupplierRepository> _supplierRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IAccessTokenService> _accessTokenService = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();
    private readonly Mock<IEmailSender> _emailSender = new();
    private readonly Mock<IGoogleAuthRepository> _googleAuth = new();
    private readonly Mock<IGoogleIdentityClient> _googleClient = new();

    private readonly DateTime _utcNow = new(2026, 11, 15, 12, 0, 0, DateTimeKind.Utc);

    private AuthService CreateService()
    {
        _dateTimeProvider.Setup(d => d.UtcNow).Returns(_utcNow);
        _supplierRepository.Setup(s => s.GetProfileByUserIdAsync(It.IsAny<Guid>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SupplierProfile?)null);
        _refreshTokenRepository.Setup(r => r.CreateAsync(It.IsAny<AuthRefreshTokenData>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var jwtOptions = Options.Create(new JwtOptions
        {
            Key = "a-very-long-and-secure-test-secret-key-32-bytes-minimum!",
            Issuer = "test-issuer",
            Audience = "test-audience",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 7
        });
        var googleOptions = Options.Create(new GoogleOAuthOptions());

        return new AuthService(
            _userRepository.Object,
            _supplierRepository.Object,
            _passwordHasher.Object,
            _accessTokenService.Object,
            _refreshTokenRepository.Object,
            new UserHelper(),
            _dateTimeProvider.Object,
            _emailSender.Object,
            _googleAuth.Object,
            _googleClient.Object,
            jwtOptions,
            googleOptions);
    }

    private static AuthUserData CreateTestUser(
        bool isActive = true,
        DateTime? emailVerifiedAt = null,
        int failedAttempts = 0,
        DateTime? lockoutUntil = null)
    {
        return new AuthUserData
        {
            Id = Guid.NewGuid(),
            Email = "testuser@example.com",
            Phone = ValidPhone,
            FullName = "Test User",
            PasswordHash = PasswordHashUtils.Hash(RawPassword),
            Role = UserRole.Customer,
            RequestedRole = UserRole.Customer,
            IsActive = isActive,
            IsDeleted = false,
            EmailVerifiedAt = emailVerifiedAt,
            FailedLoginAttempts = failedAttempts,
            LockoutUntil = lockoutUntil,
            CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
    }

    [Fact]
    public async Task PhoneLogin_Throws403_WhenEmailIsNotVerified()
    {
        var user = CreateTestUser(isActive: true, emailVerifiedAt: null);
        _userRepository.Setup(r => r.GetByPhoneAsync(ValidPhone, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<HttpException>(() =>
            service.LoginAsync(new LoginRequest(ValidPhone, RawPassword)));

        Assert.Equal(403, exception.StatusCode);
        Assert.Equal(ExpectedVerificationRequiredMessage, exception.Message);
        _userRepository.Verify(r => r.UpdateAsync(It.IsAny<AuthUserData>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PhoneLogin_LocksAccountAfterMaxFailedAttempts_AndReturns423EvenWithCorrectPassword()
    {
        var user = CreateTestUser(isActive: true, emailVerifiedAt: _utcNow, failedAttempts: 0);
        _userRepository.Setup(r => r.GetByPhoneAsync(ValidPhone, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepository.Setup(r => r.UpdateAsync(It.IsAny<AuthUserData>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthUserData u, CancellationToken _) => u);
        var service = CreateService();

        // Attempts 1 to 4: should increment FailedLoginAttempts, throw 401, not lock account yet
        for (var attempt = 1; attempt <= 4; attempt++)
        {
            var ex = await Assert.ThrowsAsync<HttpException>(() =>
                service.LoginAsync(new LoginRequest(ValidPhone, WrongPassword)));

            Assert.Equal(401, ex.StatusCode);
            Assert.Equal(MessageConstant.InvalidCredentials, ex.Message);
            Assert.Equal(attempt, user.FailedLoginAttempts);
            Assert.Null(user.LockoutUntil);
        }

        // 5th attempt (MaxFailedAttempts reached): should lock account for 15 minutes, reset counter to 0, throw 401
        var fifthEx = await Assert.ThrowsAsync<HttpException>(() =>
            service.LoginAsync(new LoginRequest(ValidPhone, WrongPassword)));

        Assert.Equal(401, fifthEx.StatusCode);
        Assert.Equal(MessageConstant.InvalidCredentials, fifthEx.Message);
        Assert.Equal(0, user.FailedLoginAttempts);
        Assert.NotNull(user.LockoutUntil);
        Assert.Equal(_utcNow.AddMinutes(15), user.LockoutUntil);

        // 6th attempt: Even with the CORRECT password, should return 423 AccountLockedMessage
        var lockedEx = await Assert.ThrowsAsync<HttpException>(() =>
            service.LoginAsync(new LoginRequest(ValidPhone, RawPassword)));

        Assert.Equal(423, lockedEx.StatusCode);
        Assert.Equal(ExpectedAccountLockedMessage, lockedEx.Message);
    }

    [Fact]
    public async Task PhoneLogin_ResetsFailedLoginCounter_OnSuccessfulLogin()
    {
        var user = CreateTestUser(isActive: true, emailVerifiedAt: _utcNow, failedAttempts: 3);
        _userRepository.Setup(r => r.GetByPhoneAsync(ValidPhone, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepository.Setup(r => r.UpdateAsync(It.IsAny<AuthUserData>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthUserData u, CancellationToken _) => u);
        var service = CreateService();

        var response = await service.LoginAsync(new LoginRequest(ValidPhone, RawPassword));

        Assert.NotNull(response);
        Assert.NotEmpty(response.AccessToken);
        Assert.Equal(0, user.FailedLoginAttempts);
        Assert.Null(user.LockoutUntil);
        _userRepository.Verify(r => r.UpdateAsync(
            It.Is<AuthUserData>(u => u.FailedLoginAttempts == 0 && u.LockoutUntil == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PhoneLogin_Throws403_WhenAccountIsInactive()
    {
        var user = CreateTestUser(isActive: false, emailVerifiedAt: _utcNow);
        _userRepository.Setup(r => r.GetByPhoneAsync(ValidPhone, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<HttpException>(() =>
            service.LoginAsync(new LoginRequest(ValidPhone, RawPassword)));

        Assert.Equal(403, exception.StatusCode);
        Assert.Equal(ExpectedAccountInactiveMessage, exception.Message);
        _userRepository.Verify(r => r.UpdateAsync(It.IsAny<AuthUserData>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PhoneLogin_Throws401_WhenUserDoesNotExist()
    {
        _userRepository.Setup(r => r.GetByPhoneAsync(ValidPhone, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthUserData?)null);
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<HttpException>(() =>
            service.LoginAsync(new LoginRequest(ValidPhone, RawPassword)));

        Assert.Equal(401, exception.StatusCode);
        Assert.Equal(MessageConstant.InvalidCredentials, exception.Message);
        _userRepository.Verify(r => r.UpdateAsync(It.IsAny<AuthUserData>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
