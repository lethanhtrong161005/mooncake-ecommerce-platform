namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Persistence;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Responses;
using C = Mooncake.EcommercePlatform.Application.Common.Constant.ShopConstants;
using H = ShopPolicyHelper;

public sealed record ShopCommandResult<T>(T Response, string AuditJson);

/// <summary>Atomically commits a business decision, its audit, and its replayable result.</summary>
public static class ShopCommandHelper
{
    public static Task<T> ExecuteAsync<T>(IShopRepository repository, ShopLockScopeDto scope, bool administrator,
        string operation, string key, object payload, DateTime now,
        Func<IShopTransaction, ShopAccountStateDto, CancellationToken, Task<ShopCommandResult<T>>> execute, CancellationToken ct) where T : notnull
    {
        var parsedKey = H.Key(key);
        var hash = H.Hash(payload);
        return repository.InTransactionAsync(scope, async (transaction, token) => {
            var account = await transaction.GetAccountAsync(new(scope.ActorUserId), token);
            if (administrator) H.Admin(account); else H.Supplier(account);
            var resource = operation == C.Create ? account!.ProfileId!.Value :
                scope.ShopId ?? scope.TemplateId ?? (administrator ? Guid.Empty : account!.ProfileId!.Value);
            var replayScope = new ShopReplayQueryDto(scope.ActorUserId, operation, resource, parsedKey);
            var replay = await transaction.GetReplayAsync(replayScope, token);
            if (replay != null && replay.ExpiresAtUtc > now)
            {
                H.Require(replay.RequestHash == hash, 409, C.KeyMismatch);
                var previous = H.Parse<T>(replay.ResponseJson);
                return previous switch {
                    OwnerShopResponse owner => (T)(object)(owner with { IdempotentReplay = true }),
                    ShopTemplateResponse template => (T)(object)(template with { IdempotentReplay = true }),
                    _ => previous
                };
            }
            H.Require(await transaction.CountCommandsAsync(new(scope.ActorUserId), "*", now.AddMinutes(-1), token) < 30, 429, C.RateLimited);
            if (operation == C.Create) H.Require(await transaction.CountCommandsAsync(new(scope.ActorUserId), operation, now.AddHours(-1), token) < 5, 429, C.RateLimited);
            var result = await execute(transaction, account!, token);
            var auditId = result.Response switch { OwnerShopResponse owner => owner.ShopId, ShopTemplateResponse template => template.Id, _ => resource };
            await transaction.AppendAuditAsync(new(scope.ActorUserId, auditId, operation, result.AuditJson, now), token);
            await transaction.StoreReplayAsync(new(replayScope, new(hash, H.Serialize(result.Response),
                operation is C.Create or C.TemplateCreate ? 201 : 200, now.AddHours(24)), now), token);
            return result.Response;
        }, ct);
    }

    public static async Task<ShopStateDto> MineAsync(IShopTransaction repository, Guid actor, CancellationToken ct)
    {
        var shop = await repository.GetOwnerShopAsync(new(actor), ct);
        H.Require(shop is { IsDeleted: false }, 404, C.NotFound);
        return shop!;
    }

    public static async Task<ShopStateDto> InformationAsync(IShopTransaction repository, ShopStateDto shop,
        Mooncake.EcommercePlatform.Application.DTOs.Shops.Requests.ShopInformationRequest input, CancellationToken ct)
    {
        var assets = new Dictionary<Guid, ShopMediaStateDto?>();
        foreach (var id in new[] { input.LogoAssetId, input.BannerAssetId }.Where(x => x.HasValue).Select(x => H.Id(x)).Distinct().Order())
            assets[id] = await repository.GetMediaAsync(new(id), ct);
        string? Media(Guid? id, string purpose) {
            if (!id.HasValue) return null;
            var asset = assets[id.Value];
            H.Require(asset is { IsDeleted: false, Status: "Ready" } && asset.OwnerUserId == shop.OwnerUserId && asset.Purpose == purpose, 409, C.MediaNotReady);
            return asset!.PublicUrl;
        }
        return shop with { Name = input.Name, Description = input.Description, LogoAssetId = input.LogoAssetId,
            BannerAssetId = input.BannerAssetId, LogoUrl = Media(input.LogoAssetId, "Logo"),
            BannerUrl = Media(input.BannerAssetId, "Banner"), PublicContactEmail = input.PublicContactEmail,
            PublicContactPhone = input.PublicContactPhone, PublicAddress = input.PublicAddress, RegionCode = input.RegionCode };
    }

    public static async Task<TemplateRevisionDto> SelectRevisionAsync(IShopTransaction repository, Guid templateId,
        Guid revisionId, ShopStateDto? shop, CancellationToken ct)
    {
        var head = await repository.GetTemplateAsync(new(templateId), ct);
        var pinned = shop?.TemplateId == templateId && shop.TemplateRevisionId == revisionId;
        H.Require(head is { IsDeleted: false } && (head.IsActive || pinned), 409, C.TemplateUnavailable);
        H.Require(pinned || head!.CurrentRevision?.Id == revisionId, 409, C.RevisionChanged);
        var revision = await repository.GetRevisionAsync(new(revisionId), ct);
        H.Require(revision?.TemplateId == templateId, 409, C.RevisionChanged);
        return revision!;
    }
}
