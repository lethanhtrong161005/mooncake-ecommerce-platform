namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Auth.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Auth.Responses;

/// <summary>Account registration and sign-in use cases.</summary>
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
}
