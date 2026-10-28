namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>An immutable, deployable storefront definition pinned by shops.</summary>
public sealed class ShopTemplateRevision : BaseEntity
{
    public Guid TemplateId { get; set; }
    public int Revision { get; set; }
    public string RendererKey { get; set; } = string.Empty;
    public int SchemaVersion { get; set; } = 1;
    public string CssVariables { get; set; } = "{}";
    public string LayoutConfig { get; set; } = "{}";
}
