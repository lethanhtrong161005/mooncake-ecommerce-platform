namespace Mooncake.EcommercePlatform.Application.DTOs.Auth.Requests;

using System.ComponentModel.DataAnnotations;

public sealed record ResetPasswordRequest
{
    [Required, EmailAddress, MaxLength(255)] public string Email { get; init; } = string.Empty;
    [Required, RegularExpression("^[0-9]{6}$")] public string Code { get; init; } = string.Empty;
    [Required, MinLength(8), MaxLength(100)] public string NewPassword { get; init; } = string.Empty;
}
