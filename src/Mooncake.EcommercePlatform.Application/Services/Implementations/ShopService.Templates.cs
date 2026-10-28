namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using Mooncake.EcommercePlatform.Application.DTOs.Shops.Persistence;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Responses;
using C = Mooncake.EcommercePlatform.Application.Common.Constant.ShopConstants;
using H = Mooncake.EcommercePlatform.Application.Common.Helpers.ShopPolicyHelper;
using Cmd = Mooncake.EcommercePlatform.Application.Common.Helpers.ShopCommandHelper;
using R = Mooncake.EcommercePlatform.Application.Common.Helpers.ShopResponseHelper;

public sealed partial class ShopService
{
    public Task<ShopTemplateResponse> CreateTemplateAsync(Guid actor, string key, CreateShopTemplateRequest request, CancellationToken ct)
    {
        var input = H.Definition(request); var code = H.Slug(request.Code, true);
        return Cmd.ExecuteAsync<ShopTemplateResponse>(repository, new(actor), true, C.TemplateCreate, key, new { input, code }, clock.UtcNow,
            async (tx, _, token) => {
                var id = Guid.NewGuid();
                var revision = new TemplateRevisionDto(Guid.NewGuid(), id, 1, input.RendererKey, 1, input.Defaults, input.Layout);
                var state = new TemplateStateDto(id, code, input.Name, input.Description, 1, false, false,
                    input.SortOrder!.Value, input.PreviewAssetId, null, revision);
                if (input.PreviewAssetId.HasValue) {
                    var asset = await tx.GetMediaAsync(new(input.PreviewAssetId.Value), token);
                    H.Require(asset is { IsDeleted: false, Status: "Ready", Purpose: "TemplatePreview" } && asset.OwnerUserId == actor, 409, C.MediaNotReady);
                    state = state with { PreviewImageUrl = asset!.PublicUrl };
                }
                await tx.WriteTemplateAsync(new(state, null, actor, clock.UtcNow, revision), token);
                return new(R.Template(state), H.Serialize(new { code, state.Version, revision.Id }));
            }, ct);
    }

    public Task<ShopTemplateResponse> PublishTemplateAsync(Guid actor, Guid templateId, string key, PublishTemplateRevisionRequest request, CancellationToken ct)
    {
        H.Id(templateId); var input = H.Definition(request);
        return Cmd.ExecuteAsync<ShopTemplateResponse>(repository, new(actor, TemplateId: templateId), true, C.TemplatePublish, key, input, clock.UtcNow,
            async (tx, _, token) => {
                var current = await tx.GetTemplateAsync(new(templateId), token);
                H.Require(current is { IsDeleted: false, CurrentRevision: not null }, 404, C.TemplateUnavailable);
                H.Version(current!.Version, request.ExpectedVersion, C.TemplateConflict);
                var revision = new TemplateRevisionDto(Guid.NewGuid(), templateId, current.CurrentRevision!.Revision + 1,
                    input.RendererKey, 1, input.Defaults, input.Layout);
                var state = current with { Name = input.Name, Description = input.Description, SortOrder = input.SortOrder!.Value,
                    CurrentRevision = revision, PreviewAssetId = input.PreviewAssetId, PreviewImageUrl = null, Version = current.Version + 1 };
                if (input.PreviewAssetId.HasValue) {
                    var asset = await tx.GetMediaAsync(new(input.PreviewAssetId.Value), token);
                    H.Require(asset is { IsDeleted: false, Status: "Ready", Purpose: "TemplatePreview" }, 409, C.MediaNotReady);
                    H.Require(asset!.OwnerUserId == actor || input.PreviewAssetId == current.PreviewAssetId, 409, C.MediaNotReady);
                    state = state with { PreviewImageUrl = asset.PublicUrl };
                }
                await tx.WriteTemplateAsync(new(state, current.Version, actor, clock.UtcNow, revision), token);
                return new(R.Template(state), H.Serialize(new { previousRevision = current.CurrentRevision.Id, revision.Id, state.Version }));
            }, ct);
    }

    public Task<ShopTemplateResponse> TemplateAvailabilityAsync(Guid actor, Guid templateId, string key, SetTemplateAvailabilityRequest request, CancellationToken ct)
    {
        H.Id(templateId); H.Require(request.IsActive.HasValue);
        return Cmd.ExecuteAsync<ShopTemplateResponse>(repository, new(actor, TemplateId: templateId, CatalogGuard: true), true, C.Availability,
            key, request, clock.UtcNow, async (tx, _, token) => {
                var current = await tx.GetTemplateAsync(new(templateId), token);
                H.Require(current is { IsDeleted: false, CurrentRevision: not null }, 404, C.TemplateUnavailable);
                H.Version(current!.Version, request.ExpectedVersion, C.TemplateConflict);
                if (request.IsActive == false && current.IsActive) {
                    var catalog = await tx.GetCatalogAsync(token);
                    H.Require(catalog.DefaultTemplateId != templateId, 409, C.DefaultRequired);
                    H.Require(catalog.ActiveCount > 5, 409, C.TemplateMinimum);
                }
                var state = current with { IsActive = request.IsActive!.Value, Version = current.Version + 1 };
                await tx.WriteTemplateAsync(new(state, current.Version, actor, clock.UtcNow), token);
                return new(R.Template(state), H.Serialize(new { previous = current.IsActive, state.IsActive, state.Version }));
            }, ct);
    }

    public Task<ShopTemplateResponse> TemplatePremiumAsync(Guid actor, Guid templateId, string key, SetTemplatePremiumRequest request, CancellationToken ct)
    {
        H.Id(templateId); H.Require(request.IsPremium.HasValue);
        return Cmd.ExecuteAsync<ShopTemplateResponse>(repository, new(actor, TemplateId: templateId), true, C.Premium, key, request, clock.UtcNow,
            async (tx, _, token) => {
                var current = await tx.GetTemplateAsync(new(templateId), token);
                H.Require(current is { IsDeleted: false }, 404, C.TemplateUnavailable);
                H.Version(current!.Version, request.ExpectedVersion, C.TemplateConflict);
                var state = current with { IsPremium = request.IsPremium!.Value, Version = current.Version + 1 };
                await tx.WriteTemplateAsync(new(state, current.Version, actor, clock.UtcNow), token);
                return new(R.Template(state), H.Serialize(new { previous = current.IsPremium, state.IsPremium, state.Version }));
            }, ct);
    }
}
