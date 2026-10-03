namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.DTOs.Auth.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;

/// <summary>Authentication and account security endpoints.</summary>
[Route("api/v1/auth")]
public class AuthController(IAuthService authService) : BaseApiController
{
    /// <summary>Registers a new customer or supplier account.</summary>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAsync(request, cancellationToken);
        return Created(result, "Account registered successfully.");
    }

    /// <summary>Authenticates user credentials and returns a JWT token.</summary>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        return Success(result, "Login successful.");
    }

    /// <summary>Returns the current authenticated user's profile.</summary>
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfileAsync([FromQuery] long userId, CancellationToken cancellationToken)
    {
        var result = await authService.GetCurrentUserProfileAsync(userId, cancellationToken);
        return Success(result, "Profile retrieved successfully.");
    }

    /// <summary>Changes the current user's password.</summary>
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePasswordAsync([FromQuery] long userId, [FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        await authService.ChangePasswordAsync(userId, request, cancellationToken);
        return Success("Password changed successfully.");
    }
}
