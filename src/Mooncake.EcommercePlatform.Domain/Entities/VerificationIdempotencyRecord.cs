namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>Committed command result scoped to an actor, operation and resource.</summary>
public sealed class VerificationIdempotencyRecord : BaseEntity
{
    public Guid ActorUserId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public Guid ResourceId { get; set; }
    public Guid Key { get; set; }
    public string RequestHash { get; set; } = string.Empty;
    public string ResultJson { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
}
