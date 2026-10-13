namespace Mooncake.EcommercePlatform.Application.DTOs.Authen.Requests;

/// <summary>Authorization code and one-use state returned to the frontend.</summary>
public sealed record GoogleLoginRequest(string? Code, string? State);
