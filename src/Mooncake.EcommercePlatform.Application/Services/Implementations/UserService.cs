namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.DTOs.Users.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Users.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;

/// <summary>
/// Implements user management business logic.
/// Throws <see cref="HttpException"/> for expected domain errors.
/// </summary>
public class UserService(IUserRepository userRepository, IUserHelper userHelper) : IUserService
{
    public async Task<IEnumerable<UserResponse>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await userRepository.GetAllAsync(cancellationToken);
        return users.Select(u => userHelper.ToResponse(u));
    }

    public async Task<UserResponse> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(id, cancellationToken)
                   ?? throw new HttpException(404, $"User with id '{id}' was not found.");
        return userHelper.ToResponse(user);
    }

    public async Task<UserResponse> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existing is not null)
            throw new HttpException(409, $"A user with email '{request.Email}' already exists.");

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            FullName = request.FullName,
            // NOTE: Replace with a proper hashing library (e.g. BCrypt.Net) in production.
            PasswordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(request.Password))
        };

        var created = await userRepository.CreateAsync(user, cancellationToken);
        return userHelper.ToResponse(created);
    }

    public async Task<UserResponse> UpdateUserAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(id, cancellationToken)
                   ?? throw new HttpException(404, $"User with id '{id}' was not found.");

        user.FullName = request.FullName;
        user.Email = request.Email;
        user.UpdatedAtUtc = DateTime.UtcNow;

        var updated = await userRepository.UpdateAsync(user, cancellationToken);
        return userHelper.ToResponse(updated);
    }

    public async Task DeleteUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(id, cancellationToken)
                   ?? throw new HttpException(404, $"User with id '{id}' was not found.");

        await userRepository.DeleteAsync(user.Id, cancellationToken);
    }
}
