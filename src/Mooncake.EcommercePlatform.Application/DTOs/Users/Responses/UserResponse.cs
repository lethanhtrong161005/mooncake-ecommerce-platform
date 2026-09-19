namespace Mooncake.EcommercePlatform.Application.DTOs.Users.Responses;

using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Projection of a User returned to the caller.</summary>
public record UserResponse(
    Guid Id,
    string Username,
    string Email,
    string FullName,
    UserRole Role,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc
);
