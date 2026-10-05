namespace Mooncake.EcommercePlatform.Application.DTOs.Auth.Responses;

using Mooncake.EcommercePlatform.Application.DTOs.Users.Responses;

/// <summary>Authenticated user and bearer access token.</summary>
public sealed record AuthResponse(string AccessToken, DateTime ExpiresAtUtc, UserResponse User);
