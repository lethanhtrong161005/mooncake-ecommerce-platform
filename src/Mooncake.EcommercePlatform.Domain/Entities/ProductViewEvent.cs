namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>Bounded product discovery signal used for deterministic recommendations.</summary>
public sealed class ProductViewEvent : BaseEntity
{
    public Guid ProductId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? SessionId { get; set; }
    public string Source { get; set; } = string.Empty;
    public DateTime ViewedAtUtc { get; set; }
}
