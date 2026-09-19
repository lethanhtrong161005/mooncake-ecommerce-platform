namespace Mooncake.EcommercePlatform.Application.DTOs.Users.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>Payload used to update an existing user's profile.</summary>
public record UpdateUserRequest
{
    [Required][MaxLength(100)]
    public string FullName { get; init; } = string.Empty;

    [Required][EmailAddress][MaxLength(255)]
    public string Email { get; init; } = string.Empty;
}
