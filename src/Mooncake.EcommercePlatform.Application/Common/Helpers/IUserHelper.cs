namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Application.DTOs.Users.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Defines helper methods for User operations.</summary>
public interface IUserHelper
{
    /// <summary>Maps a User entity to a UserResponse DTO.</summary>
    UserResponse ToResponse(User user);
}
