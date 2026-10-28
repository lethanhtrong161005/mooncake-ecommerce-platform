namespace Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Serializes changes to the selectable template catalog.</summary>
public sealed class ShopTemplateCatalogState
{
    public int Id { get; set; } = 1;
    public Guid DefaultTemplateId { get; set; }
    public long Version { get; set; } = 1;
}
