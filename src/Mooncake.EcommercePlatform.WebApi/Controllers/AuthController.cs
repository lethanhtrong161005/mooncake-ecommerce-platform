namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using System.Security.Claims;
using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using GoogleLoginRequest = Mooncake.EcommercePlatform.Application.DTOs.Authen.Requests.GoogleLoginRequest;
using PhoneLoginRequest = Mooncake.EcommercePlatform.Application.DTOs.Authen.Requests.LoginRequest;
using RefreshTokenRequest = Mooncake.EcommercePlatform.Application.DTOs.Authen.Requests.RefreshTokenRequest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Mooncake.EcommercePlatform.Application.DTOs.Auth.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;

/// <summary>Account registration, sign-in, and authenticated account endpoints.</summary>
[Route("api/v1/auth")]
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AuthController(IAuthService authService) : BaseApiController
{
    /// <summary>Registers a customer account.</summary>
    [AllowAnonymous]
    [HttpPost("register")]
    [EnableRateLimiting("auth-otp")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        await authService.RegisterAsync(request, cancellationToken);
        return AcceptedResponse("Verification code sent to the registered email address.");
    }

    [AllowAnonymous]
    [HttpPost("verify-email")]
    [EnableRateLimiting("auth-otp")]
    public async Task<IActionResult> VerifyEmailAsync([FromBody] VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        await authService.VerifyEmailAsync(request, cancellationToken);
        return Success("Email verified successfully.");
    }

    [AllowAnonymous]
    [HttpPost("resend-verification")]
    [EnableRateLimiting("auth-otp")]
    public async Task<IActionResult> ResendVerificationAsync([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await authService.ResendVerificationAsync(request.Email, cancellationToken);
        return Success("If verification is pending, a code has been sent.");
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    [EnableRateLimiting("auth-otp")]
    public async Task<IActionResult> ForgotPasswordAsync([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await authService.RequestPasswordResetAsync(request.Email, cancellationToken);
        return Success("If the account exists, a reset code has been sent.");
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    [EnableRateLimiting("auth-otp")]
    public async Task<IActionResult> ResetPasswordAsync([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await authService.ResetPasswordAsync(request, cancellationToken);
        return Success("Password reset successfully.");
    }


    /// <summary>Creates a Google authorization URL with state and PKCE.</summary>
    [AllowAnonymous]
    [HttpPost("google/authorization-url")]
    [EnableRateLimiting("auth-login")]
    public async Task<IActionResult> GoogleAuthorizationUrlAsync(CancellationToken cancellationToken)
    {
        var result = await authService.CreateGoogleAuthorizationUrlAsync(cancellationToken);
        return Success(result, MessageConstant.GoogleUrlCreated);
    }

    /// <summary>Exchanges a Google code for Mooncake access and refresh tokens.</summary>
    [AllowAnonymous]
    [HttpPost("google/login")]
    [EnableRateLimiting("auth-login")]
    public async Task<IActionResult> GoogleLoginAsync(
        [FromBody] GoogleLoginRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
            throw new HttpException(400, MessageConstant.GoogleInvalidRequest);
        var result = await authService.GoogleLoginAsync(request, cancellationToken);
        return Success(result, MessageConstant.LoginSuccess);
    }

    /// <summary>Signs in with a phone number and password.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [EnableRateLimiting("auth-login")]
    public async Task<IActionResult> LoginAsync(
        [FromBody] PhoneLoginRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
            throw new HttpException(400, MessageConstant.InvalidRequest);
        var result = await authService.LoginAsync(request, cancellationToken);
        return Success(result, MessageConstant.LoginSuccess);
    }

    /// <summary>Exchanges a refresh token for a rotated token pair.</summary>
    [AllowAnonymous]
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshAsync(
        [FromBody] RefreshTokenRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
            throw new HttpException(401, MessageConstant.SessionExpired);
        var result = await authService.RefreshAsync(request, cancellationToken);
        return Success(result, MessageConstant.RefreshSuccess);
    }

    /// <summary>Revokes the current refresh token.</summary>
    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> LogoutAsync(
        [FromBody] RefreshTokenRequest? request, CancellationToken cancellationToken)
    {
        if (request is not null)
            await authService.LogoutAsync(request, cancellationToken);
        return Success(MessageConstant.LogoutSuccess);
    }

    /// <summary>Returns the user represented by a valid Bearer token.</summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> MeAsync(CancellationToken cancellationToken)
    {
        var rawId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(rawId, out var userId))
            throw new HttpException(401, MessageConstant.SessionExpired);
        var result = await authService.GetCurrentUserAsync(userId, cancellationToken);
        return Success(result, MessageConstant.LoginSuccess);
    }
}
