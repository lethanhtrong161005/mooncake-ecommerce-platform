namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using Mooncake.EcommercePlatform.Application.Common.Constant;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Utils;
using Mooncake.EcommercePlatform.Application.DTOs.Authen.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;

/// <summary>Phone login and access token lifecycle endpoints.</summary>
[Route("api/v1/auth")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AuthenController(IAuthenService authenService) : BaseApiController
{
    [HttpPost("register/send-otp")]
    [EnableRateLimiting("auth-login")]
    public async Task<IActionResult> SendRegisterOtpAsync(
        [FromBody] SendRegisterOtpRequest? request, CancellationToken cancellationToken)
    {
        if (request is null) throw new HttpException(400, MessageConstant.InvalidRegisterData);
        var result = await authenService.SendRegisterOtpAsync(request, cancellationToken);
        return Success(result, MessageConstant.OtpSent);
    }

    [HttpPost("register/resend-otp")]
    [EnableRateLimiting("auth-login")]
    public async Task<IActionResult> ResendRegisterOtpAsync(
        [FromBody] SendRegisterOtpRequest? request, CancellationToken cancellationToken)
    {
        if (request is null) throw new HttpException(400, MessageConstant.InvalidRegisterData);
        var result = await authenService.ResendRegisterOtpAsync(request, cancellationToken);
        return Success(result, MessageConstant.OtpResent);
    }

    [HttpPost("register/verify-otp")]
    [EnableRateLimiting("auth-login")]
    public async Task<IActionResult> VerifyRegisterOtpAsync(
        [FromBody] VerifyRegisterOtpRequest? request, CancellationToken cancellationToken)
    {
        if (request is null) throw new HttpException(400, MessageConstant.InvalidRegisterData);
        var result = await authenService.VerifyRegisterOtpAsync(request, cancellationToken);
        return Success(result, MessageConstant.OtpVerified);
    }

    [HttpPost("register")]
    [EnableRateLimiting("auth-login")]
    public async Task<IActionResult> RegisterAccountAsync(
        [FromBody] RegisterAccountRequest? request, CancellationToken cancellationToken)
    {
        if (request is null) throw new HttpException(400, MessageConstant.InvalidRegisterData);
        var result = await authenService.RegisterAccountAsync(request, cancellationToken);
        return Created(result, MessageConstant.RegisterSuccess);
    }
    /// <summary>Creates a Google authorization URL with state and PKCE.</summary>
    [HttpPost("google/authorization-url")]
    [EnableRateLimiting("auth-login")]
    public async Task<IActionResult> GoogleAuthorizationUrlAsync(CancellationToken cancellationToken)
    {
        var result = await authenService.CreateGoogleAuthorizationUrlAsync(cancellationToken);
        return Success(result, MessageConstant.GoogleUrlCreated);
    }

    /// <summary>Exchanges a Google code for Mooncake access and refresh tokens.</summary>
    [HttpPost("google/login")]
    [EnableRateLimiting("auth-login")]
    public async Task<IActionResult> GoogleLoginAsync(
        [FromBody] GoogleLoginRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
            throw new HttpException(400, MessageConstant.GoogleInvalidRequest);
        var result = await authenService.GoogleLoginAsync(request, cancellationToken);
        return Success(result, MessageConstant.LoginSuccess);
    }

    /// <summary>Signs in with a phone number and password.</summary>
    [HttpPost("login")]
    [EnableRateLimiting("auth-login")]
    public async Task<IActionResult> LoginAsync(
        [FromBody] LoginRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
            throw new HttpException(400, MessageConstant.InvalidRequest);
        var result = await authenService.LoginAsync(request, cancellationToken);
        return Success(result, MessageConstant.LoginSuccess);
    }

    /// <summary>Exchanges a refresh token for a rotated token pair.</summary>
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshAsync(
        [FromBody] RefreshTokenRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
            throw new HttpException(401, MessageConstant.SessionExpired);
        var result = await authenService.RefreshAsync(request, cancellationToken);
        return Success(result, MessageConstant.RefreshSuccess);
    }

    /// <summary>Revokes the current refresh token.</summary>
    [HttpPost("logout")]
    public async Task<IActionResult> LogoutAsync(
        [FromBody] RefreshTokenRequest? request, CancellationToken cancellationToken)
    {
        if (request is not null)
            await authenService.LogoutAsync(request, cancellationToken);
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
        var result = await authenService.GetCurrentUserAsync(userId, cancellationToken);
        return Success(result, MessageConstant.LoginSuccess);
    }
}
