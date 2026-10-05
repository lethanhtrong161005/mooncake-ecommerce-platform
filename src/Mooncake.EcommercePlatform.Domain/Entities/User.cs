namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Represents the User domain entity.</summary>
public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string? FullName { get; set; }

    public string? Phone { get; set; }

    public string? AvatarUrl { get; set; }

    public bool IsActive { get; set; }

    public int FailedLoginAttempts { get; set; }

    public DateTime? LockoutUntil { get; set; }

    public DateTime? EmailVerifiedAt { get; set; }

    public UserRole Role { get; set; } = UserRole.Customer;
}
