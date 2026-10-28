namespace Mooncake.EcommercePlatform.Infrastructure.Helpers;

using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Persistence;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Requests;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Entity mapping remains entirely inside Infrastructure.</summary>
public static class ShopPersistenceHelper
{
    public static ShopStateDto Shop(Shop entity) => new() {
        Id = entity.Id, SupplierId = entity.SupplierId, OwnerUserId = entity.OwnerUserId, Version = entity.Version,
        IsDeleted = entity.IsDeleted, Name = entity.Name ?? "", Slug = entity.Slug, Description = entity.Description,
        Status = entity.Status, TemplateId = entity.TemplateId, TemplateRevisionId = entity.TemplateRevisionId,
        Overrides = ShopPolicyHelper.Parse<ThemeOverridesRequest>(entity.CustomColors ?? "{}"),
        LogoAssetId = entity.LogoAssetId, BannerAssetId = entity.BannerAssetId,
        LogoUrl = entity.LogoAssetId.HasValue ? entity.LogoUrl : null, BannerUrl = entity.BannerAssetId.HasValue ? entity.BannerUrl : null,
        PublicContactEmail = entity.PublicContactEmail, PublicContactPhone = entity.PublicContactPhone,
        PublicAddress = entity.PublicAddress, RegionCode = entity.RegionCode, PreSuspensionStatus = entity.PreSuspensionStatus,
        SuspensionReason = entity.SuspensionReason, SuspendedAtUtc = entity.SuspendedAtUtc,
        SuspendedByUserId = entity.SuspendedByUserId, CreatedAtUtc = entity.CreatedAt };
    public static void Apply(Shop entity, ShopWriteDto command)
    {
        var state = command.State;
        entity.OwnerUserId = state.OwnerUserId; entity.SupplierId = state.SupplierId;
        entity.Name = state.Name; entity.Slug = state.Slug; entity.Description = state.Description;
        entity.TemplateId = state.TemplateId; entity.TemplateRevisionId = state.TemplateRevisionId;
        entity.CustomColors = ShopPolicyHelper.Serialize(state.Overrides);
        entity.Status = state.Status; entity.Version = state.Version;
        entity.LogoAssetId = state.LogoAssetId; entity.BannerAssetId = state.BannerAssetId;
        entity.LogoUrl = state.LogoUrl; entity.BannerUrl = state.BannerUrl;
        entity.PublicContactEmail = state.PublicContactEmail; entity.PublicContactPhone = state.PublicContactPhone;
        entity.PublicAddress = state.PublicAddress; entity.RegionCode = state.RegionCode; entity.PreSuspensionStatus = state.PreSuspensionStatus;
        entity.SuspensionReason = state.SuspensionReason; entity.SuspendedByUserId = state.SuspendedByUserId;
        entity.SuspendedAtUtc = state.SuspendedAtUtc; entity.UpdatedAt = command.Now; entity.UpdatedBy = command.ActorUserId;
    }
    public static TemplateRevisionDto Revision(ShopTemplateRevision entity) => new(entity.Id, entity.TemplateId,
        entity.Revision, entity.RendererKey, entity.SchemaVersion,
        ShopPolicyHelper.Parse<ThemeOverridesRequest>(entity.CssVariables), ShopPolicyHelper.Parse<TemplateLayoutRequest>(entity.LayoutConfig));
    public static TemplateStateDto Template(ShopTemplate entity, ShopTemplateRevision? revision) => new(entity.Id,
        entity.Code, entity.Name ?? "", entity.Description, entity.Version, entity.IsActive, entity.IsPremium,
        entity.SortOrder, entity.PreviewAssetId, entity.PreviewAssetId.HasValue ? entity.PreviewImageUrl : null,
        revision == null ? null : Revision(revision), entity.IsDeleted);
    public static ShopMediaStateDto Media(ShopMediaAsset entity) => new(entity.Id, entity.OwnerUserId,
        entity.UploadOperationId, entity.Purpose, entity.RequestHash, entity.Status, entity.PublicUrl,
        entity.ProviderPublicId, entity.Sha256, entity.ContentType, entity.ByteCount, entity.Width, entity.Height,
        entity.UpdatedAt, entity.IsDeleted, entity.LeaseToken);
}
