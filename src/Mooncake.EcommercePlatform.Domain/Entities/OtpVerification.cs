namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>A persisted phone registration challenge.</summary>
public class OtpVerification : BaseEntity
{
    public string Phone { get; set; } = string.Empty;
    public string Purpose { get; set; } = "RegisterAccount";
    public string OtpHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime LastSentAtUtc { get; set; }
    public DateTime WindowStartedAtUtc { get; set; }
    public int ResendCount { get; set; }
    public int FailedAttempts { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }
    /// <summary>SHA-256 hash of the one-use verification token.</summary>
    public string? VerificationToken { get; set; }
    public DateTime? TokenExpiresAtUtc { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }
}
