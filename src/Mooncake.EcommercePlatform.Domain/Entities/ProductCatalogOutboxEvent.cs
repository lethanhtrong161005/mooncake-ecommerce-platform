namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>Transactional signal used to refresh the public catalog search projection.</summary>
public sealed class ProductCatalogOutboxEvent : BaseEntity
{
    public Guid ProductId { get; set; }
    public long SourceVersion { get; set; }
    public string EventType { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
    public DateTime AvailableAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public string? LastErrorCode { get; set; }
}
