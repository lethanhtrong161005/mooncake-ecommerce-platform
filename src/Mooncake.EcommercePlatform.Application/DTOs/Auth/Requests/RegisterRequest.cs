namespace Mooncake.EcommercePlatform.Application.DTOs.Auth.Requests;

using System.ComponentModel.DataAnnotations;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Payload used to register a customer account.</summary>
public sealed record RegisterRequest
{
    [Required, EmailAddress, MaxLength(255)]
    public string Email { get; init; } = string.Empty;

    [Required, MinLength(8), MaxLength(100)]
    public string Password { get; init; } = string.Empty;

    [MaxLength(100)]
    public string? FullName { get; init; }

    [Required, Phone, MaxLength(20)]
    public string Phone { get; init; } = string.Empty;

    [Required]
    [RegularExpression("^Customer$", ErrorMessage = "New accounts must register as Customer. Supplier onboarding is available after sign-in.")]
    public string Role { get; init; } = nameof(UserRole.Customer);
}
