namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Represents the Shop domain entity.</summary>
public class Shop : BaseEntity
{
    public Guid OwnerUserId { get; set; }
    public long Version { get; set; } = 1;
    public Guid TemplateRevisionId { get; set; }
    public Guid? LogoAssetId { get; set; }
    public Guid? BannerAssetId { get; set; }
    public string? PublicContactEmail { get; set; }
    public string? PublicContactPhone { get; set; }
    public string? PublicAddress { get; set; }
    public string? RegionCode { get; set; }
    public ShopStatus? PreSuspensionStatus { get; set; }
    public DateTime? SuspendedAtUtc { get; set; }
    public Guid? SuspendedByUserId { get; set; }
    public string? SuspensionReason { get; set; }
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
