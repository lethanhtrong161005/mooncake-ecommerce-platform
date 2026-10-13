namespace Mooncake.EcommercePlatform.Application.DTOs.Authen.Requests;

/// <summary>Credentials for phone/password login.</summary>
public record LoginRequest(string? PhoneNumber, string? Password);
