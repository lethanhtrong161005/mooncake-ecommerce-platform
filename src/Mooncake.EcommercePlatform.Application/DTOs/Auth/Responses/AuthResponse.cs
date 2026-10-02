namespace Mooncake.EcommercePlatform.Application.DTOs.Auth.Responses;

using Mooncake.EcommercePlatform.Application.DTOs.Users.Responses;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Authentication result containing JWT token and user profile details.</summary>
public record AuthResponse(
    string Token,
    UserResponse User,
    UserRole Role,
    long? CustomerId,
    long? SupplierId
);
