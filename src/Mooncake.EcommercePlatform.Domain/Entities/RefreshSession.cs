namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>Represents a revocable user refresh-token session.</summary>
public class RefreshSession : BaseEntity
{
    public Guid UserId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public Guid? ReplacedBySessionId { get; set; }

    public string? CreatedFromIp { get; set; }

    public string? UserAgent { get; set; }
}
