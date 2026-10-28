namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Persistence;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Responses;
using Mooncake.EcommercePlatform.Domain.Enums;
using C = Mooncake.EcommercePlatform.Application.Common.Constant.ShopConstants;
using H = ShopPolicyHelper;

/// <summary>Builds explicit owner/public projections from persistence DTOs.</summary>
public static class ShopResponseHelper
{
    public static async Task<OwnerShopResponse> OwnerAsync(IShopTransaction repository, ShopStateDto shop, CancellationToken ct)
    {
        var head = await repository.GetTemplateAsync(new(shop.TemplateId), ct);
        var revision = await repository.GetRevisionAsync(new(shop.TemplateRevisionId), ct);
        H.Require(head != null && revision?.TemplateId == shop.TemplateId, 409, C.TemplateUnavailable);
        return Owner(shop, head!, revision!);
    }
    public static OwnerShopResponse Owner(ShopStateDto shop, TemplateStateDto head, TemplateRevisionDto revision)
    {
        var appearance = new ShopAppearanceResponse(head!.Id, revision!.Id, head.Code, head.Name,
            revision.RendererKey, revision.SchemaVersion, revision.Revision, head.IsActive, head.IsPremium,
            revision.Defaults, shop.Overrides, H.Merge(revision.Defaults, shop.Overrides), revision.Layout, shop.Version);
        return new(shop.Id, shop.SupplierId, shop.Version, shop.Name, shop.Slug, shop.Description, shop.Status,
            shop.LogoAssetId, shop.BannerAssetId, shop.LogoUrl, shop.BannerUrl, shop.PublicContactEmail,
            shop.PublicContactPhone, shop.PublicAddress, shop.RegionCode, shop.SuspensionReason, appearance,
            shop.Status == ShopStatus.Suspended ? [] : ["EditInformation", "EditAppearance", shop.Status == ShopStatus.Active ? "Deactivate" : "Activate"]);
    }
    public static ShopTemplateResponse Template(TemplateStateDto template) => new(template.Id, template.Code,
        template.Name, template.Description, template.Version, template.IsActive, template.IsPremium,
        template.SortOrder, template.PreviewAssetId, template.PreviewImageUrl, template.CurrentRevision);
    public static ShopMediaResponse Media(ShopMediaStateDto media) => new(media.Id, media.Purpose,
        media.Status, media.PublicUrl, media.ContentType, media.ByteCount, media.Width, media.Height);
    public static ShopCursorPageResponse<T> Page<T>(IReadOnlyList<T> rows, int pageSize, Func<T, string> cursor)
    {
        var items = rows.Take(pageSize).ToArray();
        return new(items, rows.Count > pageSize, rows.Count > pageSize && items.Length > 0 ? cursor(items[^1]) : null);
    }
}
