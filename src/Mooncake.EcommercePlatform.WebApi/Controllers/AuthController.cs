namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Mooncake.EcommercePlatform.Application.DTOs.Auth.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;

/// <summary>Account registration, sign-in, and authenticated account endpoints.</summary>
[Route("api/v1/auth")]
[Authorize]
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

}
