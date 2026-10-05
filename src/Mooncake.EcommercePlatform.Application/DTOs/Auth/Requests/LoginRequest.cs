namespace Mooncake.EcommercePlatform.Application.DTOs.Auth.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>Payload used to authenticate with email and password.</summary>
public sealed record LoginRequest
{
    [Required, EmailAddress, MaxLength(255)]
    public string Email { get; init; } = string.Empty;

    [Required, MaxLength(100)]
    public string Password { get; init; } = string.Empty;
}
