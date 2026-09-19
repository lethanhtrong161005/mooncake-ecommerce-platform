namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.DTOs.Users.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;

/// <summary>REST endpoints for user management.</summary>
[Route("api/v1/users")]
public class UsersController(IUserService userService) : BaseApiController
{
    /// <summary>Returns all registered users.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken)
    {
        var users = await userService.GetAllUsersAsync(cancellationToken);
        return Success(users, "Users retrieved successfully.");
    }

    /// <summary>Returns a single user by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await userService.GetUserByIdAsync(id, cancellationToken);
        return Success(user, "User retrieved successfully.");
    }

    /// <summary>Creates a new user.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await userService.CreateUserAsync(request, cancellationToken);
        return Created(user, "User created successfully.");
    }

    /// <summary>Updates an existing user's profile.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await userService.UpdateUserAsync(id, request, cancellationToken);
        return Success(user, "User updated successfully.");
    }

    /// <summary>Deletes a user by ID.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await userService.DeleteUserAsync(id, cancellationToken);
        return Success("User deleted successfully.");
    }
}
