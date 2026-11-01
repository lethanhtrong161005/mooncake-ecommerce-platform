namespace Mooncake.EcommercePlatform.Application.DTOs.Auth.Models;

using Mooncake.EcommercePlatform.Domain.Enums;

public sealed class AuthUserData
{
    public Guid Id { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime CreatedAt { get => CreatedAtUtc; set => CreatedAtUtc = value; }
    public DateTime UpdatedAt { get => UpdatedAtUtc; set => UpdatedAtUtc = value; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockoutUntil { get; set; }
    public DateTime? EmailVerifiedAt { get; set; }
    public UserRole Role { get; set; }
    public UserRole RequestedRole { get; set; }
    public string? EmailVerificationCodeHash { get; set; }
    public DateTime? EmailVerificationCodeExpiresAt { get; set; }
    public int EmailVerificationCodeAttempts { get; set; }
    public string? PasswordResetCodeHash { get; set; }
    public DateTime? PasswordResetCodeExpiresAt { get; set; }
    public int PasswordResetCodeAttempts { get; set; }
    public string? ContractSigningCodeHash { get; set; }
    public DateTime? ContractSigningCodeExpiresAt { get; set; }
    public int ContractSigningCodeAttempts { get; set; }
}

public sealed class AuthRefreshSessionData
{
    public Guid Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public Guid? ReplacedBySessionId { get; set; }
    public string? CreatedFromIp { get; set; }
    public string? UserAgent { get; set; }
}

public sealed class AuthRefreshTokenData
{
    public Guid Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public bool IsDeleted { get; set; }
}

public sealed class GoogleLoginAttemptData
{
    public Guid Id { get; set; }
    public string StateHash { get; set; } = string.Empty;
    public string CodeVerifier { get; set; } = string.Empty;
    public string Nonce { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
