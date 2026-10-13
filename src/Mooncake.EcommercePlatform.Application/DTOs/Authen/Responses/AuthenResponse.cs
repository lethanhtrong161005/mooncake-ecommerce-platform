namespace Mooncake.EcommercePlatform.Application.DTOs.Authen.Responses;

using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Public user fields returned with a new token pair.</summary>
public record AuthenUserResponse(Guid Id, string? Phone, string? FullName, UserRole Role);

/// <summary>A newly issued access/refresh token pair.</summary>
public record AuthenResponse(
    string AccessToken,
    string RefreshToken,
    string TokenType,
    int ExpiresIn,
    AuthenUserResponse User);
