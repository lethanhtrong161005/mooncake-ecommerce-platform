namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Represents the Shop domain entity.</summary>
public class Shop : BaseEntity
{
    public Guid SupplierId { get; set; }

    public Guid TemplateId { get; set; }

    public string? Name { get; set; }

    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? BannerUrl { get; set; }

    public string? LogoUrl { get; set; }

    public string? CustomColors { get; set; }

    public string? CustomCss { get; set; }

    public ShopStatus Status { get; set; } = ShopStatus.Active;
}
