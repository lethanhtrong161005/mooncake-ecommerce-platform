namespace Mooncake.EcommercePlatform.Application.DTOs.Authen.Requests;

/// <summary>Refresh token submitted for rotation or revocation.</summary>
public record RefreshTokenRequest(string? RefreshToken);
