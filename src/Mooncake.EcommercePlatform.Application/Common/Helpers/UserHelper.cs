namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Application.DTOs.Users.Responses;
using Mooncake.EcommercePlatform.Application.DTOs.Auth.Models;

/// <summary>Helper methods for User operations.</summary>
public class UserHelper : IUserHelper
{
    /// <summary>Maps a User entity to a UserResponse DTO.</summary>
    public UserResponse ToResponse(AuthUserData user) =>
        new(user.Id, user.Email, user.FullName, user.Phone, user.AvatarUrl, user.Role, user.CreatedAtUtc, user.UpdatedAtUtc);
}
