namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>A committed result stored atomically with its shop command.</summary>
public sealed class ShopIdempotencyRecord : BaseEntity
{
    public Guid ActorUserId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public Guid ResourceId { get; set; }
    public Guid Key { get; set; }
    public string RequestHash { get; set; } = string.Empty;
    public string ResponseJson { get; set; } = string.Empty;
    public int HttpStatus { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}
