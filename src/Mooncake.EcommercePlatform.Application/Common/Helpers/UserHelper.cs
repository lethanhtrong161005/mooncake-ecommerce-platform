namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Application.DTOs.Users.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Helper methods for User operations.</summary>
public class UserHelper : IUserHelper
{
    /// <summary>Maps a User entity to a UserResponse DTO.</summary>
    public UserResponse ToResponse(User user) =>
        new(user.Id, user.Email, user.FullName, user.Phone, user.AvatarUrl, user.Role, user.CreatedAtUtc, user.UpdatedAtUtc);
}
