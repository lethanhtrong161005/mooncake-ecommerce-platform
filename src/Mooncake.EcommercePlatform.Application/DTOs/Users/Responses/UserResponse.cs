namespace Mooncake.EcommercePlatform.Application.DTOs.Users.Responses;

using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Projection of a User returned to the caller.</summary>
public record UserResponse(
    long Id,
    string Email,
    string FullName,
    string? Phone,
    bool IsActive,
    UserRole Role,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc
);
