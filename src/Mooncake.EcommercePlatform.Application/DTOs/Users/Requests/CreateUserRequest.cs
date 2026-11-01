namespace Mooncake.EcommercePlatform.Application.DTOs.Users.Requests;

using System.ComponentModel.DataAnnotations;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Payload used to register a new user.</summary>
public record CreateUserRequest
{
    [Required][EmailAddress][MaxLength(255)]
    public string Email { get; init; } = string.Empty;

    [MaxLength(100)]
    public string? FullName { get; init; }

    [MaxLength(25)]
    [RegularExpression(@"^(?:\+?84|0)[\s.\-()]*[35789](?:[\s.\-()]*[0-9]){8}$", ErrorMessage = "Phone number is invalid.")]
    public string? Phone { get; init; }

    [Required][MinLength(8)]
    public string Password { get; init; } = string.Empty;

    public UserRole Role { get; init; } = UserRole.Customer;
}
