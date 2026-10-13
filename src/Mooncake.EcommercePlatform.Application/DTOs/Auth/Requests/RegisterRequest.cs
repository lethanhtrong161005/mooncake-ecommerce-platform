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

    [Phone, MaxLength(20)]
    public string? Phone { get; init; }

    [Required]
    [RegularExpression("^(Customer|Supplier)$", ErrorMessage = "Role must be Customer or Supplier.")]
    public string Role { get; init; } = nameof(UserRole.Customer);
}
