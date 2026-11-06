namespace Mooncake.EcommercePlatform.Application.DTOs.Users.Requests;

using Mooncake.EcommercePlatform.Application.Common.DTOs;
using Mooncake.EcommercePlatform.Domain.Enums;

public sealed record UserPageQueryRequest : PagedQuery
{
    public UserRole? Role { get; init; }
    public bool? IsActive { get; init; }
}
