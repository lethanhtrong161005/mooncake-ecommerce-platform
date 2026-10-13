namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>A short-lived, single-use Google authorization attempt.</summary>
public class GoogleLoginAttempt : BaseEntity
{
    public string StateHash { get; set; } = string.Empty;
    public string CodeVerifier { get; set; } = string.Empty;
    public string Nonce { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }
}
