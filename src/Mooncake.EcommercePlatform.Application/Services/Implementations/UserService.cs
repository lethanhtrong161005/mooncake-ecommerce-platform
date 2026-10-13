namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Utils;
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
    private const string PhoneConflictMessage = "Phone number is already in use.";

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

        var phone = string.IsNullOrWhiteSpace(request.Phone)
            ? null : PhoneNumberUtils.Normalize(request.Phone);
        if (phone is not null && !PhoneNumberUtils.IsValid(phone))
            throw new HttpException(400, MessageConstant.PhoneInvalid);
        if (phone is not null && await userRepository.GetByPhoneAsync(phone, cancellationToken) is not null)
            throw new HttpException(409, PhoneConflictMessage);

        var user = new User
        {
            Email = request.Email,
            FullName = request.FullName,
            Phone = phone,
            Role = request.Role,
            IsActive = true,
            PasswordHash = PasswordHashUtils.Hash(request.Password)
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
        var phone = string.IsNullOrWhiteSpace(request.Phone)
            ? null : PhoneNumberUtils.Normalize(request.Phone);
        if (phone is not null && !PhoneNumberUtils.IsValid(phone))
            throw new HttpException(400, MessageConstant.PhoneInvalid);
        var phoneOwner = phone is null ? null : await userRepository.GetByPhoneAsync(phone, cancellationToken);
        if (phoneOwner is not null && phoneOwner.Id != id)
            throw new HttpException(409, PhoneConflictMessage);
        user.Phone = phone;
        user.AvatarUrl = request.AvatarUrl;
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
