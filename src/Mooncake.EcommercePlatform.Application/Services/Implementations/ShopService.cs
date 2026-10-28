namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Persistence;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Enums;
using C = Mooncake.EcommercePlatform.Application.Common.Constant.ShopConstants;
using H = Mooncake.EcommercePlatform.Application.Common.Helpers.ShopPolicyHelper;
using Cmd = Mooncake.EcommercePlatform.Application.Common.Helpers.ShopCommandHelper;
using R = Mooncake.EcommercePlatform.Application.Common.Helpers.ShopResponseHelper;

/// <summary>Shop business decisions run against fresh DTO snapshots inside the persistence transaction.</summary>
public sealed partial class ShopService(IShopRepository repository, IDateTimeProvider clock,
    IShopCursorProtector cursors, IShopMediaGateway media) : IShopService
{
    public Task<OwnerShopResponse> GetMineAsync(Guid actor, CancellationToken ct) => repository.InReadSnapshotAsync(async (tx, token) => {
        H.Supplier(await tx.GetAccountAsync(new(actor), token));
        var display = await tx.GetDisplayAsync(new(OwnerUserId: actor), token);
        H.Require(display is { Shop.IsDeleted: false }, 404, C.NotFound);
        return R.Owner(display!.Shop, display.Template, display.PinnedRevision);
    }, ct);

    public Task<OwnerShopResponse> CreateAsync(Guid actor, string key, CreateShopRequest request, CancellationToken ct)
    {
        var information = H.Information(request);
        var slug = H.Slug(request.Slug);
        var templateId = H.Id(request.TemplateId); var revisionId = H.Id(request.TemplateRevisionId);
        return Cmd.ExecuteAsync<OwnerShopResponse>(repository, new(actor, TemplateId: templateId), false, C.Create, key,
            new { information, slug, templateId, revisionId }, clock.UtcNow, async (tx, account, token) => {
                H.Require(await tx.GetOwnerShopAsync(new(actor), token) == null, 409, C.Exists);
                await Cmd.SelectRevisionAsync(tx, templateId, revisionId, null, token);
                var state = new ShopStateDto { Id = Guid.NewGuid(), OwnerUserId = actor, SupplierId = account.ProfileId!.Value,
                    Name = information.Name, Slug = slug, Status = ShopStatus.Inactive, Version = 1,
                    TemplateId = templateId, TemplateRevisionId = revisionId, CreatedAtUtc = clock.UtcNow };
                state = await Cmd.InformationAsync(tx, state, information, token);
                await tx.WriteShopAsync(new(state, null, actor, clock.UtcNow), token);
                return new(await R.OwnerAsync(tx, state, token), H.Serialize(new { state.Version, state.Status, state.TemplateRevisionId }));
            }, ct);
    }

    public Task<OwnerShopResponse> UpdateAsync(Guid actor, string key, UpdateShopRequest request, CancellationToken ct)
    {
        var information = H.Information(request);
        return Cmd.ExecuteAsync<OwnerShopResponse>(repository, new(actor), false, C.Update, key, information, clock.UtcNow,
            async (tx, _, token) => {
                var current = await Cmd.MineAsync(tx, actor, token);
                H.Version(current.Version, request.ExpectedVersion); H.Editable(current);
                var state = await Cmd.InformationAsync(tx, current with { Version = current.Version + 1 }, information, token);
                await tx.WriteShopAsync(new(state, current.Version, actor, clock.UtcNow), token);
                return new(await R.OwnerAsync(tx, state, token), H.Serialize(new { previousVersion = current.Version, state.Version }));
            }, ct);
    }

    public Task<OwnerShopResponse> VisibilityAsync(Guid actor, string key, SetMyShopVisibilityRequest request, CancellationToken ct)
    {
        H.Require(request.Status is ShopStatus.Active or ShopStatus.Inactive);
        return Cmd.ExecuteAsync<OwnerShopResponse>(repository, new(actor), false, C.Visibility, key, request, clock.UtcNow,
            async (tx, _, token) => {
                var current = await Cmd.MineAsync(tx, actor, token);
                H.Version(current.Version, request.ExpectedVersion); H.Editable(current);
                await Cmd.SelectRevisionAsync(tx, current.TemplateId, current.TemplateRevisionId, current, token);
                var state = current with { Status = request.Status!.Value, Version = current.Version + 1 };
                await tx.WriteShopAsync(new(state, current.Version, actor, clock.UtcNow), token);
                return new(await R.OwnerAsync(tx, state, token), H.Serialize(new { previous = current.Status, state.Status, state.Version }));
            }, ct);
    }

    public Task<OwnerShopResponse> AppearanceAsync(Guid actor, string key, UpdateShopAppearanceRequest request, CancellationToken ct)
    {
        var templateId = H.Id(request.TemplateId); var revisionId = H.Id(request.TemplateRevisionId);
        var overrides = H.Tokens(request.Overrides);
        return Cmd.ExecuteAsync<OwnerShopResponse>(repository, new(actor, TemplateId: templateId), false, C.Appearance, key,
            request with { Overrides = overrides }, clock.UtcNow, async (tx, _, token) => {
                var current = await Cmd.MineAsync(tx, actor, token);
                H.Version(current.Version, request.ExpectedVersion); H.Editable(current);
                var revision = await Cmd.SelectRevisionAsync(tx, templateId, revisionId, current, token);
                H.Merge(revision.Defaults, overrides);
                var state = current with { TemplateId = templateId, TemplateRevisionId = revisionId,
                    Overrides = overrides, Version = current.Version + 1 };
                await tx.WriteShopAsync(new(state, current.Version, actor, clock.UtcNow), token);
                return new(await R.OwnerAsync(tx, state, token), H.Serialize(new { previousRevision = current.TemplateRevisionId, revisionId, state.Version }));
            }, ct);
    }

    public Task<OwnerShopResponse> ModerateAsync(Guid actor, Guid shopId, string key, AdminShopModerationRequest request, CancellationToken ct)
    {
        H.Id(shopId); H.Require(request.Action is "Suspend" or "Restore");
        request = request with { Reason = H.Reason(request.Reason) };
        return Cmd.ExecuteAsync<OwnerShopResponse>(repository, new(actor, shopId), true, C.Moderation, key, request, clock.UtcNow,
            async (tx, _, token) => {
                var current = await tx.GetShopAsync(new(shopId), token);
                H.Require(current is { IsDeleted: false }, 404, C.NotFound);
                H.Require(current!.OwnerUserId != actor, 403, C.Forbidden);
                H.Version(current.Version, request.ExpectedVersion);
                ShopStateDto state;
                if (request.Action == "Suspend") {
                    H.Require(current.Status != ShopStatus.Suspended, 409, C.StateConflict);
                    state = current with { Status = ShopStatus.Suspended, PreSuspensionStatus = current.Status,
                        SuspendedAtUtc = clock.UtcNow, SuspendedByUserId = actor, SuspensionReason = request.Reason };
                } else {
                    H.Require(current.Status == ShopStatus.Suspended && current.PreSuspensionStatus is ShopStatus.Active or ShopStatus.Inactive, 409, C.StateConflict);
                    H.Supplier(await tx.GetAccountAsync(new(current.OwnerUserId), token));
                    state = current with { Status = current.PreSuspensionStatus!.Value, PreSuspensionStatus = null,
                        SuspendedAtUtc = null, SuspendedByUserId = null, SuspensionReason = null };
                }
                state = state with { Version = current.Version + 1 };
                await tx.WriteShopAsync(new(state, current.Version, actor, clock.UtcNow), token);
                return new(await R.OwnerAsync(tx, state, token), H.Serialize(new {
                    previous = current.Status, state.Status, state.Version, request.Reason, previousReason = current.SuspensionReason }));
            }, ct);
    }

    public async Task<ShopPageResponse<AdminShopSummaryResponse>> ListAdminAsync(Guid actor, AdminShopQuery query, CancellationToken ct)
    {
        H.Admin(await repository.GetAccountAsync(new(actor), ct)); H.Page(query);
        H.Require(query.Status is null || Enum.IsDefined(query.Status.Value));
        H.Require(query.OwnerProfileId != Guid.Empty && query.Sort is "createdAtDesc" or "nameAsc");
        return await repository.ListShopsAsync(query, ct);
    }

    public Task<OwnerShopResponse> DetailAdminAsync(Guid actor, Guid shopId, CancellationToken ct) => repository.InReadSnapshotAsync(async (tx, token) => {
        H.Admin(await tx.GetAccountAsync(new(actor), token));
        var shop = await tx.GetShopAsync(new(H.Id(shopId)), token);
        H.Require(shop is { IsDeleted: false }, 404, C.NotFound);
        return await R.OwnerAsync(tx, shop!, token);
    }, ct);
}
