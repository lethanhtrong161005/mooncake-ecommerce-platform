namespace Mooncake.EcommercePlatform.Application.DTOs.Users.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>Payload to update a user's profile.</summary>
public record UpdateUserRequest
{
    [Required]
    [MaxLength(255)]
    public string FullName { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; init; } = string.Empty;

    [MaxLength(30)]
    public string? Phone { get; init; }
}
