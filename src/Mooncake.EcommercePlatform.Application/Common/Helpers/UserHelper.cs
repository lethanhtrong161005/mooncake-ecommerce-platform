namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Application.DTOs.Users.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Maps User entities to DTOs.</summary>
public class UserHelper : IUserHelper
{
    public UserResponse ToResponse(User user) =>
        new(user.Id, user.Email, user.FullName, user.Phone, user.IsActive, user.Role, user.CreatedAtUtc, user.UpdatedAtUtc);
}
