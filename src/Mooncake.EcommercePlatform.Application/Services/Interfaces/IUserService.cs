namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Users.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Users.Responses;

/// <summary>Business logic contract for user management.</summary>
public interface IUserService
{
    /// <summary>Retrieves all users.</summary>
    Task<IEnumerable<UserResponse>> GetAllUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>Retrieves a single user by ID; throws 404 if not found.</summary>
    Task<UserResponse> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Creates a new user; throws 409 if email already exists.</summary>
    Task<UserResponse> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    /// <summary>Updates a user's profile; throws 404 if not found.</summary>
    Task<UserResponse> UpdateUserAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default);

    /// <summary>Deletes a user; throws 404 if not found.</summary>
    Task DeleteUserAsync(Guid id, CancellationToken cancellationToken = default);
}
