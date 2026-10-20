namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.Common.Options;
using Mooncake.EcommercePlatform.Application.Common.Utils;
using Mooncake.EcommercePlatform.Application.DTOs.Authen.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Authen.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Validates credentials and manages single-use refresh tokens.</summary>
public class AuthenService(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IGoogleAuthRepository googleAuth,
    IGoogleIdentityClient googleClient,
    IOtpRepository otpRepository,
    IRegisterOtpSender otpSender,
    IDateTimeProvider clock,
    IOptions<JwtOptions> jwtOptions,
    IOptions<GoogleOAuthOptions> googleOptions) : IAuthenService
{
    private const string BearerTokenType = "Bearer";
    private readonly JwtOptions _jwt = jwtOptions.Value;
    private readonly GoogleOAuthOptions _google = googleOptions.Value;

    public Task<SendRegisterOtpResponse> SendRegisterOtpAsync(
        SendRegisterOtpRequest request, CancellationToken cancellationToken = default) =>
        IssueRegisterOtpAsync(request, false, cancellationToken);

    public Task<SendRegisterOtpResponse> ResendRegisterOtpAsync(
        SendRegisterOtpRequest request, CancellationToken cancellationToken = default) =>
        IssueRegisterOtpAsync(request, true, cancellationToken);

    private async Task<SendRegisterOtpResponse> IssueRegisterOtpAsync(
        SendRegisterOtpRequest request, bool resend, CancellationToken cancellationToken)
    {
        var phone = ValidatePhone(request.PhoneNumber);
        if (!otpSender.IsAvailable)
            throw new HttpException(503, MessageConstant.OtpProviderUnavailable);
        if (await users.GetByPhoneAsync(phone, cancellationToken) is not null)
            throw new HttpException(409, MessageConstant.PhoneAlreadyRegistered);

        var otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");
        await otpRepository.IssueAsync(phone, HashToken(otp), clock.UtcNow, resend, cancellationToken);
        await otpSender.SendAsync(phone, otp, cancellationToken);
        return new SendRegisterOtpResponse(phone, 60, 300);
    }

    public async Task<VerifyRegisterOtpResponse> VerifyRegisterOtpAsync(
        VerifyRegisterOtpRequest request, CancellationToken cancellationToken = default)
    {
        var phone = ValidatePhone(request.PhoneNumber);
        if (string.IsNullOrWhiteSpace(request.Otp))
            throw new HttpException(400, MessageConstant.OtpRequired);
        var otp = request.Otp.Trim();
        if (otp.Length != 6 || !otp.All(c => c is >= '0' and <= '9'))
            throw new HttpException(400, MessageConstant.OtpInvalid);
        var token = Microsoft.IdentityModel.Tokens.Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(48));
        var remaining = await otpRepository.VerifyAsync(phone, HashToken(otp), HashToken(token),
            clock.UtcNow, cancellationToken);
        if (remaining == 0)
            throw new HttpException(400, MessageConstant.OtpMaxAttemptsExceeded);
        if (remaining > 0)
            throw new HttpException(400, $"Mã OTP không chính xác. Bạn còn {remaining} lần thử.");
        return new VerifyRegisterOtpResponse(phone, token, 600);
    }

    public async Task<RegisterAccountResponse> RegisterAccountAsync(
        RegisterAccountRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.PhoneNumber) ||
            string.IsNullOrWhiteSpace(request.VerificationToken) ||
            string.IsNullOrWhiteSpace(request.FullName) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.ConfirmPassword))
            throw new HttpException(400, MessageConstant.InvalidRegisterData);
        var phone = ValidatePhone(request.PhoneNumber);
        var fullName = request.FullName.Trim();
        if (fullName.Length is < 2 or > 100)
            throw new HttpException(400, MessageConstant.FullNameInvalid);
        if (request.Password.Length < 6)
            throw new HttpException(400, MessageConstant.PasswordTooShort);
        if (request.Password != request.ConfirmPassword)
            throw new HttpException(400, MessageConstant.PasswordMismatch);
        if (request.VerificationToken.Length > 512)
            throw new HttpException(400, MessageConstant.VerificationSessionExpired);

        var user = await otpRepository.RegisterAsync(phone, HashToken(request.VerificationToken),
            fullName, PasswordHashUtils.Hash(request.Password), clock.UtcNow, cancellationToken);
        return new RegisterAccountResponse(user.Id, phone, fullName, user.Role);
    }

    private static string ValidatePhone(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new HttpException(400, MessageConstant.PhoneRequired);
        var phone = PhoneNumberUtils.Normalize(input);
        if (!PhoneNumberUtils.IsValid(phone))
            throw new HttpException(400, MessageConstant.PhoneInvalid);
        return phone;
    }

    /// <inheritdoc/>
    public async Task<GoogleAuthorizationUrlResponse> CreateGoogleAuthorizationUrlAsync(
        CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var state = GoogleOAuthHelper.RandomToken(32);
        var nonce = GoogleOAuthHelper.RandomToken(32);
        var verifier = GoogleOAuthHelper.RandomToken(64);
        await googleAuth.CreateAttemptAsync(new GoogleLoginAttempt
        {
            Id = Guid.NewGuid(),
            StateHash = GoogleOAuthHelper.Sha256(state),
            CodeVerifier = verifier,
            Nonce = nonce,
            ExpiresAtUtc = now.AddMinutes(_google.StateLifetimeMinutes),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        }, cancellationToken);
        return new GoogleAuthorizationUrlResponse(
            GoogleOAuthHelper.BuildUrl(_google, state, nonce, verifier), state,
            checked(_google.StateLifetimeMinutes * 60));
    }

    /// <inheritdoc/>
    public async Task<AuthenResponse> GoogleLoginAsync(
        GoogleLoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || request.Code.Length > 4096 ||
            string.IsNullOrWhiteSpace(request.State) || request.State.Length > 512)
            throw new HttpException(400, MessageConstant.GoogleInvalidRequest);

        var attempt = await googleAuth.ConsumeAttemptAsync(
            GoogleOAuthHelper.Sha256(request.State), clock.UtcNow, cancellationToken);
        if (attempt is null)
            throw new HttpException(401, MessageConstant.GoogleInvalidState);

        var identity = await googleClient.ExchangeAsync(
            request.Code, attempt.CodeVerifier, attempt.Nonce, cancellationToken);
        var user = await googleAuth.FindOrCreateUserAsync(
            identity.Subject, identity.Email, identity.Name, identity.Picture,
            clock.UtcNow, cancellationToken);
        return await IssueAsync(user, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<AuthenResponse> LoginAsync(
        LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            throw new HttpException(400, MessageConstant.PhoneRequired);

        var phone = PhoneNumberUtils.Normalize(request.PhoneNumber);
        if (!PhoneNumberUtils.IsValid(phone))
            throw new HttpException(400, MessageConstant.PhoneInvalid);
        if (string.IsNullOrWhiteSpace(request.Password))
            throw new HttpException(400, MessageConstant.PasswordRequired);

        var user = await users.GetByPhoneAsync(phone, cancellationToken);
        if (user is null || user.IsDeleted || !user.IsActive ||
            string.IsNullOrEmpty(user.PasswordHash) ||
            !PasswordHashUtils.Verify(request.Password, user.PasswordHash, out var needsUpgrade))
            throw new HttpException(401, MessageConstant.InvalidCredentials);

        if (needsUpgrade)
        {
            user.PasswordHash = PasswordHashUtils.Hash(request.Password);
            user.UpdatedAtUtc = clock.UtcNow;
            await users.UpdateAsync(user, cancellationToken);
        }

        return await IssueAsync(user, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<AuthenResponse> RefreshAsync(
        RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            throw new HttpException(401, MessageConstant.SessionExpired);

        var oldHash = HashToken(request.RefreshToken);
        var stored = await refreshTokens.GetByHashAsync(oldHash, cancellationToken);
        var now = clock.UtcNow;
        if (stored is null || stored.IsDeleted || stored.RevokedAtUtc is not null ||
            stored.ExpiresAtUtc <= now)
            throw new HttpException(401, MessageConstant.SessionExpired);

        var user = await users.GetByIdAsync(stored.UserId, cancellationToken);
        if (user is null || user.IsDeleted || !user.IsActive)
            throw new HttpException(401, MessageConstant.SessionExpired);

        var raw = CreateRefreshToken();
        var replacement = CreateRecord(user.Id, HashToken(raw), now);
        if (!await refreshTokens.RotateAsync(oldHash, replacement, now, cancellationToken))
            throw new HttpException(401, MessageConstant.SessionExpired);

        return CreateResponse(user, raw, now);
    }

    /// <inheritdoc/>
    public async Task LogoutAsync(
        RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
            await refreshTokens.RevokeAsync(
                HashToken(request.RefreshToken), clock.UtcNow, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<AuthenUserResponse> GetCurrentUserAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken);
        if (user is null || user.IsDeleted || !user.IsActive)
            throw new HttpException(401, MessageConstant.SessionExpired);
        return new AuthenUserResponse(user.Id, user.Phone, user.FullName, user.Role);
    }

    private async Task<AuthenResponse> IssueAsync(User user, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var raw = CreateRefreshToken();
        await refreshTokens.CreateAsync(
            CreateRecord(user.Id, HashToken(raw), now), cancellationToken);
        return CreateResponse(user, raw, now);
    }

    private AuthenResponse CreateResponse(User user, string refreshToken, DateTime nowUtc) =>
        new(
            JwtTokenUtils.Generate(user, _jwt, nowUtc),
            refreshToken,
            BearerTokenType,
            checked(_jwt.AccessTokenMinutes * 60),
            new AuthenUserResponse(user.Id, user.Phone, user.FullName, user.Role));

    private RefreshToken CreateRecord(Guid userId, string tokenHash, DateTime nowUtc) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAtUtc = nowUtc.AddDays(_jwt.RefreshTokenDays),
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        };

    private static string CreateRefreshToken() =>
        Microsoft.IdentityModel.Tokens.Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));

    private static string HashToken(string raw) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(raw)));
}
