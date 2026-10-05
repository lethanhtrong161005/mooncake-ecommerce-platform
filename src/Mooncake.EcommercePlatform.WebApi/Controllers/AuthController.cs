namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.DTOs.Auth.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;

/// <summary>Public account registration and sign-in endpoints.</summary>
[AllowAnonymous]
[Route("api/v1/auth")]
public sealed class AuthController(IAuthService authService, IUserService userService) : BaseApiController
{
    /// <summary>Registers a customer account.</summary>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.RegisterAsync(request, cancellationToken);
        return Created(response, "Account registered successfully.");
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
