namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Auth.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Auth.Responses;

/// <summary>Account registration and sign-in use cases.</summary>
public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
