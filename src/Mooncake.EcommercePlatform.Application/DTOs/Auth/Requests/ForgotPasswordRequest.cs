namespace Mooncake.EcommercePlatform.Application.DTOs.Auth.Requests;

using System.ComponentModel.DataAnnotations;

public sealed record ForgotPasswordRequest
{
    [Required, EmailAddress, MaxLength(255)] public string Email { get; init; } = string.Empty;
}
