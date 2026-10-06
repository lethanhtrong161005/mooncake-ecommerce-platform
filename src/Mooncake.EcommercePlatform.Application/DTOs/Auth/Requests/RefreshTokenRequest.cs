namespace Mooncake.EcommercePlatform.Application.DTOs.Auth.Requests;

using System.ComponentModel.DataAnnotations;

public sealed record RefreshTokenRequest
{
    [Required] public string RefreshToken { get; init; } = string.Empty;
}
