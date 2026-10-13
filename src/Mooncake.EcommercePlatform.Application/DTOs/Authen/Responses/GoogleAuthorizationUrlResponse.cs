namespace Mooncake.EcommercePlatform.Application.DTOs.Authen.Responses;

/// <summary>Google redirect URL and browser-bound state.</summary>
public sealed record GoogleAuthorizationUrlResponse(string AuthorizationUrl, string State, int ExpiresIn);
