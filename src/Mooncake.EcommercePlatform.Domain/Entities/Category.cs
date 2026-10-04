namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>Represents the Category domain entity.</summary>
public class Category : BaseEntity
{
    public Guid? ParentId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? IconUrl { get; set; }

    public int SortOrder { get; set; }
}
