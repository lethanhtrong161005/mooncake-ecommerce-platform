namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>Represents a custom packaging template managed by a supplier workshop.</summary>
public class PackagingTemplate : BaseEntity
{
    public Guid ShopId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Material { get; set; }

    public string? Dimensions { get; set; }

    public int Capacity { get; set; } = 4;

    public decimal BasePrice { get; set; }

    public string? SampleImageUrl { get; set; }

    public string? Technique { get; set; }

    public int MinOrderQty { get; set; } = 20;

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; } = 0;
}

