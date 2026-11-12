namespace Mooncake.EcommercePlatform.Application.DTOs.Users.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>Editable fields of the authenticated user's personal profile.</summary>
public sealed record UpdateOwnProfileRequest
{
    [Required]
    [MaxLength(100)]
    [RegularExpression(@".*\S.*", ErrorMessage = "Full name is required.")]
    public string FullName { get; init; } = string.Empty;

    [MaxLength(25)]
    [RegularExpression(@"^(?:\+?84|0)[\s.\-()]*[35789](?:[\s.\-()]*[0-9]){8}$",
        ErrorMessage = "Phone number is invalid.")]
    public string? Phone { get; init; }

    [MaxLength(500)]
    [Url]
    public string? AvatarUrl { get; init; }
}
