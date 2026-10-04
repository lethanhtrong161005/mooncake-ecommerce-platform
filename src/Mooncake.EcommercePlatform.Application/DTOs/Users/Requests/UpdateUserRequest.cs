namespace Mooncake.EcommercePlatform.Application.DTOs.Users.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>Payload used to update an existing user's profile.</summary>
public record UpdateUserRequest
{
    [MaxLength(100)]
    public string? FullName { get; init; }

    [Required][EmailAddress][MaxLength(255)]
    public string Email { get; init; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; init; }

    [MaxLength(500)]
    public string? AvatarUrl { get; init; }
}
