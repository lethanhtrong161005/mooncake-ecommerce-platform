namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Auth.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Auth.Responses;
using Mooncake.EcommercePlatform.Application.DTOs.Authen.Responses;
using GoogleLoginRequest = Mooncake.EcommercePlatform.Application.DTOs.Authen.Requests.GoogleLoginRequest;
using PhoneLoginRequest = Mooncake.EcommercePlatform.Application.DTOs.Authen.Requests.LoginRequest;
using RefreshTokenRequest = Mooncake.EcommercePlatform.Application.DTOs.Authen.Requests.RefreshTokenRequest;

/// <summary>Account registration, verification, sign-in, and token lifecycle use cases.</summary>
public interface IAuthService
{
    Task RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default);
    Task ResendVerificationAsync(string email, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> RefreshAsync(string refreshToken, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);
    Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task RevokeAllRefreshTokensAsync(Guid userId, CancellationToken cancellationToken = default);
    Task RequestContractSignatureOtpAsync(Guid userId, Guid contractId, CancellationToken cancellationToken = default);
    Task VerifyContractSignatureOtpAsync(Guid userId, Guid contractId, string code, CancellationToken cancellationToken = default);

    /// <summary>Authenticates a user and issues a new token pair.</summary>
    Task<AuthenResponse> LoginAsync(PhoneLoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>Creates a one-use Google authorization URL.</summary>
    Task<GoogleAuthorizationUrlResponse> CreateGoogleAuthorizationUrlAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Exchanges a Google authorization code for local tokens.</summary>
    Task<AuthenResponse> GoogleLoginAsync(
        GoogleLoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>Rotates a valid refresh token and returns the replacement pair.</summary>
    Task<AuthenResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);

    /// <summary>Revokes the supplied refresh token.</summary>
    Task LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);

    /// <summary>Returns the active user represented by an access token.</summary>
    Task<AuthenUserResponse> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
