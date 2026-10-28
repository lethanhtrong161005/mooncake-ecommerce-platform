namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>Represents the ShopTemplate domain entity.</summary>
public class ShopTemplate : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public long Version { get; set; } = 1;
    public Guid? CurrentRevisionId { get; set; }
    public Guid? PreviewAssetId { get; set; }
    public bool IsPremium { get; set; }
    public string? Name { get; set; }

    public string? Description { get; set; }

    public string? PreviewImageUrl { get; set; }

    public string? CssVariables { get; set; }

    public string? LayoutConfig { get; set; }

    public bool IsActive { get; set; }

    public int SortOrder { get; set; }
}
