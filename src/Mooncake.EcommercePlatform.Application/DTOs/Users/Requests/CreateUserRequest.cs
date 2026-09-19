namespace Mooncake.EcommercePlatform.Application.DTOs.Users.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>Payload used to register a new user.</summary>
public record CreateUserRequest
{
    [Required][MinLength(3)][MaxLength(50)]
    public string Username { get; init; } = string.Empty;

    [Required][EmailAddress][MaxLength(255)]
    public string Email { get; init; } = string.Empty;

    [Required][MaxLength(100)]
    public string FullName { get; init; } = string.Empty;

    [Required][MinLength(8)]
    public string Password { get; init; } = string.Empty;
}
