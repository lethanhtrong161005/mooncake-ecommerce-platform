namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;
using Mooncake.EcommercePlatform.Domain.Enums;
using Pgvector;

/// <summary>Represents the Product domain entity.</summary>
public class Product : BaseEntity
{
    public long Version { get; set; } = 1;

    public Guid ShopId { get; set; }

    public Guid CategoryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string NormalizedName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Ingredients { get; set; }

    public decimal BasePrice { get; set; }

    public int MinOrderQty { get; set; }

    public int? MaxOrderQty { get; set; }

    public string? Unit { get; set; }

    public bool SupportsCustomPackaging { get; set; }

    public ProductStatus Status { get; set; } = ProductStatus.Active;

    public bool IsPublished { get; set; }

    public DateTime? PublishedAtUtc { get; set; }

    public Vector? Embedding { get; set; }
}
