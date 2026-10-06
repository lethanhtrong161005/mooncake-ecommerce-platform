namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Mooncake.EcommercePlatform.Application.DTOs.Auth.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;

/// <summary>Public account registration and sign-in endpoints.</summary>
[AllowAnonymous]
[Route("api/v1/auth")]
public sealed class AuthController(IAuthService authService, IUserService userService) : BaseApiController
{
    /// <summary>Registers a customer account.</summary>
    [HttpPost("register")]
    [EnableRateLimiting("auth-otp")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        await authService.RegisterAsync(request, cancellationToken);
        return AcceptedResponse("Verification code sent to the registered email address.");
    }

    [HttpPost("verify-email")]
    [EnableRateLimiting("auth-otp")]
    public async Task<IActionResult> VerifyEmailAsync([FromBody] VerifyEmailRequest request, CancellationToken cancellationToken) =>
        Success(await authService.VerifyEmailAsync(request, cancellationToken), "Email verified successfully.");

    [HttpPost("resend-verification")]
    [EnableRateLimiting("auth-otp")]
    public async Task<IActionResult> ResendVerificationAsync([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await authService.ResendVerificationAsync(request.Email, cancellationToken);
        return Success("If verification is pending, a code has been sent.");
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting("auth-otp")]
    public async Task<IActionResult> ForgotPasswordAsync([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await authService.RequestPasswordResetAsync(request.Email, cancellationToken);
        return Success("If the account exists, a reset code has been sent.");
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting("auth-otp")]
    public async Task<IActionResult> ResetPasswordAsync([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await authService.ResetPasswordAsync(request, cancellationToken);
        return Success("Password reset successfully.");
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshAsync([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken) =>
        Success(await authService.RefreshAsync(request.RefreshToken, HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(), cancellationToken), "Token refreshed successfully.");

    [HttpPost("revoke")]
    public async Task<IActionResult> RevokeAsync([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        await authService.RevokeRefreshTokenAsync(request.RefreshToken, cancellationToken);
        return Success("Refresh token revoked successfully.");
    }

    [Authorize]
    [HttpPost("revoke-all")]
    public async Task<IActionResult> RevokeAllAsync(CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        await authService.RevokeAllRefreshTokensAsync(userId, cancellationToken);
        return Success("All refresh tokens revoked successfully.");
    }

    /// <summary>Authenticates a user and returns a bearer access token.</summary>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.LoginAsync(request, cancellationToken);
        return Success(response, "Signed in successfully.");
    }

    /// <summary>Returns the authenticated user's profile.</summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var user = await userService.GetUserByIdAsync(userId, cancellationToken);
        return Success(user, "Account profile retrieved successfully.");
    }
}
