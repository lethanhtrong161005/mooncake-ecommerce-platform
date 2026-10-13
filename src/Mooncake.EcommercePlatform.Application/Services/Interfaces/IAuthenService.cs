namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Authen.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Authen.Responses;

/// <summary>Phone login and revocable token lifecycle.</summary>
public interface IAuthenService
{
    /// <summary>Authenticates a user and issues a new token pair.</summary>
    Task<AuthenResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

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
