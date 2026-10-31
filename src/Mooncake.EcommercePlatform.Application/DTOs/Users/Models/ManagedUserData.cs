namespace Mooncake.EcommercePlatform.Application.DTOs.Users.Models;

using Mooncake.EcommercePlatform.Application.DTOs.Users.Responses;
using Mooncake.EcommercePlatform.Domain.Enums;

public sealed record CreateManagedUserData(string Email, string? FullName, string? Phone, string PasswordHash, UserRole Role);
public sealed record UpdateManagedUserData(Guid Id, string Email, string? FullName, string? Phone, string? AvatarUrl);
public sealed record ManagedUserData(Guid Id, string Email, string? FullName, string? Phone, string? AvatarUrl,
    UserRole Role, DateTime CreatedAtUtc, DateTime UpdatedAtUtc)
{
    public UserResponse ToResponse() => new(Id, Email, FullName, Phone, AvatarUrl, Role, CreatedAtUtc, UpdatedAtUtc);
}
