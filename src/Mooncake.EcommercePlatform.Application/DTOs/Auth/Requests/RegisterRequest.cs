namespace Mooncake.EcommercePlatform.Application.DTOs.Auth.Requests;

using System.ComponentModel.DataAnnotations;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Request payload for registering a new user as Customer or Supplier.</summary>
public record RegisterRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MinLength(6)]
    [MaxLength(100)]
    public string Password { get; init; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string FullName { get; init; } = string.Empty;

    [MaxLength(30)]
    public string? Phone { get; init; }

    /// <summary>Role to register: Customer or Supplier.</summary>
    public UserRole Role { get; init; } = UserRole.Customer;

    // ── Customer specific fields ──
    public CustomerType CustomerType { get; init; } = CustomerType.Individual;
    public string? CompanyName { get; init; }
    public string? TaxCode { get; init; }
    public string? DefaultAddress { get; init; }

    // ── Supplier specific fields ──
    public string? BusinessName { get; init; }
    public string? Description { get; init; }
    public string? Address { get; init; }
}
