namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.DTOs;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Utils;
using Mooncake.EcommercePlatform.Application.DTOs.Users.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Users.Responses;
using Mooncake.EcommercePlatform.Application.DTOs.Auth.Models;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;

/// <summary>
/// Implements user management business logic.
/// Throws <see cref="HttpException"/> for expected domain errors.
/// </summary>
public class UserService(IUserRepository userRepository, IUserHelper userHelper) : IUserService
{
    private const string PhoneConflictMessage = "Phone number is already in use.";

    public async Task<PagedResult<UserResponse>> GetUsersPageAsync(UserPageQueryRequest query,
        CancellationToken cancellationToken = default)
    {
        PagedQueryHelper.Validate(query);
        PagedQueryHelper.ValidateSort(query.Sort, "createdAtDesc", "createdAtAsc", "fullNameAsc", "fullNameDesc");
        if (query.Role.HasValue && !Enum.IsDefined(query.Role.Value))
            throw new HttpException(400, "Invalid user role filter.", "VALIDATION_ERROR");
        var page = await userRepository.GetPageAsync(query, cancellationToken);
        return new PagedResult<UserResponse>(page.Items.Select(userHelper.ToResponse).ToList(),
            page.Page, page.PageSize, page.TotalCount);
    }

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

    public async Task<UserResponse> GetOwnProfileAsync(Guid userId, CancellationToken cancellationToken = default) =>
        userHelper.ToResponse(await GetActiveOwnUserAsync(userId, cancellationToken));

    public async Task<UserResponse> UpdateOwnProfileAsync(Guid userId, UpdateOwnProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await GetActiveOwnUserAsync(userId, cancellationToken);

        return await UpdateUserAsync(userId, new UpdateUserRequest
        {
            FullName = request.FullName.Trim(),
            Email = user.Email,
            Phone = request.Phone,
            AvatarUrl = request.AvatarUrl
        }, cancellationToken);
    }

    public async Task<UserResponse> UpdateOwnAvatarAsync(Guid userId, string? avatarUrl,
        CancellationToken cancellationToken = default)
    {
        var user = await GetActiveOwnUserAsync(userId, cancellationToken);
        var normalized = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl.Trim();
        if (normalized is not null && normalized.Length > 500)
            throw new HttpException(400, "Liên kết ảnh đại diện tối đa 500 ký tự.");

        user.AvatarUrl = normalized;
        user.UpdatedAtUtc = DateTime.UtcNow;

        var updated = await userRepository.UpdateAsync(user, cancellationToken);
        return userHelper.ToResponse(updated);
    }

    private async Task<AuthUserData> GetActiveOwnUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null || user.IsDeleted || !user.IsActive)
            throw new HttpException(401, MessageConstant.SessionExpired);
        return user;
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

        var user = new AuthUserData
        {
            Email = request.Email,
            FullName = request.FullName,
            Phone = phone,
            // Supplier authority is granted only by the verification workflow, including admin-created accounts.
            Role = request.Role == Mooncake.EcommercePlatform.Domain.Enums.UserRole.Supplier ?
                Mooncake.EcommercePlatform.Domain.Enums.UserRole.Customer : request.Role,
            RequestedRole = request.Role,
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
