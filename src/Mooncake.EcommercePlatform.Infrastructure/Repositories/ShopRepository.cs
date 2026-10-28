namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using System.Data;
using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Persistence;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Helpers;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;
using Npgsql;
using C = Mooncake.EcommercePlatform.Application.Common.Constant.ShopConstants;
using H = Mooncake.EcommercePlatform.Application.Common.Helpers.ShopPolicyHelper;
using Map = Mooncake.EcommercePlatform.Infrastructure.Helpers.ShopPersistenceHelper;

/// <summary>Parameterized persistence with bounded whole-transaction retry and DTO-only boundaries.</summary>
public sealed partial class ShopRepository(ApplicationDbContext db) : IShopRepository
{
    private bool _write;
    private bool _catalogLocked;
    public async Task<T> InTransactionAsync<T>(ShopLockScopeDto scope, Func<IShopTransaction, CancellationToken, Task<T>> work, CancellationToken ct)
    {
        for (var attempt = 0; ; attempt++)
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            try {
                _write = true; _catalogLocked = scope.CatalogGuard;
                await ShopLockHelper.AcquireAsync(db, scope, ct);
                var result = await work(this, ct);
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return result;
            }
            catch (Exception failure) {
                await transaction.RollbackAsync(CancellationToken.None);
                db.ChangeTracker.Clear();
                var postgres = ShopLockHelper.Postgres(failure);
                if (postgres?.SqlState is "40P01" or "40001" && attempt < 2) {
                    await Task.Delay(TimeSpan.FromMilliseconds(50 * (attempt + 1) + Random.Shared.Next(30)), ct);
                    continue;
                }
                if (postgres?.SqlState is "40P01" or "40001" or "55P03" or "57014") throw new HttpException(503, C.Error, C.Busy);
                if (failure is DbUpdateConcurrencyException) throw new HttpException(409, C.Error, C.Conflict);
                if (postgres?.SqlState == "23505") {
                    var code = postgres.ConstraintName switch {
                        "shops_supplier_id_key" => C.Exists, "shops_slug_key" => C.SlugInUse,
                        "shop_templates_code_key" => C.TemplateCodeInUse, _ => C.Conflict };
                    throw new HttpException(409, C.Error, code);
                }
                if (postgres?.SqlState == "23503") throw new HttpException(409, C.Error, C.StateConflict);
                throw;
            }
            finally { _write = false; _catalogLocked = false; }
        }
    }
    public async Task<T> InReadSnapshotAsync<T>(Func<IShopTransaction, CancellationToken, Task<T>> work, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        await db.Database.ExecuteSqlRawAsync("SET TRANSACTION READ ONLY", ct);
        var result = await work(this, ct);
        await transaction.CommitAsync(ct);
        return result;
    }
    public Task<ShopAccountStateDto?> GetAccountAsync(ShopAccountQueryDto query, CancellationToken ct) =>
        (from user in db.Users.AsNoTracking()
         join profile in db.SupplierProfiles.AsNoTracking() on user.Id equals profile.UserId into profiles
         from profile in profiles.DefaultIfEmpty()
         where user.Id == query.UserId
         select new ShopAccountStateDto(user.Id, user.Role, user.IsActive, user.IsDeleted,
             profile == null ? null : profile.Id, profile == null ? null : profile.VerificationStatus,
             profile != null && profile.IsDeleted, profile == null ? 0 : profile.ReputationScore)).SingleOrDefaultAsync(ct);
    public async Task<ShopStateDto?> GetOwnerShopAsync(ShopOwnerQueryDto query, CancellationToken ct)
    {
        var row = await db.Shops.AsNoTracking().SingleOrDefaultAsync(x => x.OwnerUserId == query.OwnerUserId, ct);
        return row == null ? null : Map.Shop(row);
    }
    public async Task<ShopStateDto?> GetShopAsync(ShopIdQueryDto query, CancellationToken ct)
    {
        var row = await db.Shops.AsNoTracking().SingleOrDefaultAsync(x => x.Id == query.Id, ct);
        return row == null ? null : Map.Shop(row);
    }
    public async Task<ShopStateDto?> GetShopBySlugAsync(ShopSlugQueryDto query, CancellationToken ct)
    {
        var row = await db.Shops.AsNoTracking().SingleOrDefaultAsync(x => x.Slug == query.Slug, ct);
        return row == null ? null : Map.Shop(row);
    }
    public async Task<TemplateStateDto?> GetTemplateAsync(ShopIdQueryDto query, CancellationToken ct)
    {
        var row = await (from head in db.ShopTemplates.AsNoTracking()
                         join revision in db.ShopTemplateRevisions.AsNoTracking() on head.CurrentRevisionId equals revision.Id into revisions
                         from revision in revisions.DefaultIfEmpty()
                         where head.Id == query.Id select new { head, revision }).SingleOrDefaultAsync(ct);
        return row == null ? null : Map.Template(row.head, row.revision);
    }
    public async Task<TemplateRevisionDto?> GetRevisionAsync(ShopRevisionQueryDto query, CancellationToken ct)
    {
        var row = await db.ShopTemplateRevisions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == query.Id, ct);
        return row == null ? null : Map.Revision(row);
    }
    public async Task<ShopCatalogStateDto> GetCatalogAsync(CancellationToken ct)
    {
        var row = await db.ShopTemplateCatalogStates.AsNoTracking().SingleAsync(x => x.Id == 1, ct);
        var count = await db.ShopTemplates.CountAsync(x => !x.IsDeleted && x.IsActive && x.CurrentRevisionId != null, ct);
        return new(row.DefaultTemplateId, row.Version, count);
    }
    public async Task<ShopMediaStateDto?> GetMediaAsync(ShopMediaQueryDto query, CancellationToken ct)
    {
        var row = _write ? await db.ShopMediaAssets.FromSqlInterpolated($"SELECT * FROM shop_media_assets WHERE id = {query.Id} FOR UPDATE").AsNoTracking().SingleOrDefaultAsync(ct) :
            await db.ShopMediaAssets.AsNoTracking().SingleOrDefaultAsync(x => x.Id == query.Id, ct);
        return row == null ? null : Map.Media(row);
    }
    public async Task<ShopMediaStateDto?> GetUploadAsync(ShopUploadQueryDto query, CancellationToken ct)
    {
        var row = await db.ShopMediaAssets.AsNoTracking().SingleOrDefaultAsync(x => x.OwnerUserId == query.OwnerUserId && x.UploadOperationId == query.OperationId, ct);
        return row == null ? null : Map.Media(row);
    }
    public async Task<ShopUploadUsageDto> GetUploadUsageAsync(ShopActorDto actor, DateTime since, DateTime activeSince, CancellationToken ct)
    {
        var data = await db.ShopMediaAssets.Where(x => x.OwnerUserId == actor.UserId && !x.IsDeleted && x.CreatedAt >= since)
            .GroupBy(x => x.OwnerUserId).Select(g => new ShopUploadUsageDto(g.Count(),
                g.Count(x => x.Status == "Uploading" && x.UpdatedAt >= activeSince))).SingleOrDefaultAsync(ct);
        return data ?? new(0, 0);
    }
    public Task<int> CountCommandsAsync(ShopActorDto actor, string operation, DateTime since, CancellationToken ct) =>
        db.AuditLogs.CountAsync(x => x.ActorUserId == actor.UserId && x.EntityName == "ShopManagement" && x.CreatedAt >= since &&
            (operation == "*" || x.Action == operation), ct);
    public async Task<ShopReplayDto?> GetReplayAsync(ShopReplayQueryDto query, CancellationToken ct)
    {
        var row = await db.ShopIdempotencyRecords.AsNoTracking().SingleOrDefaultAsync(x => x.ActorUserId == query.ActorUserId &&
            x.Operation == query.Operation && x.ResourceId == query.ResourceId && x.Key == query.Key, ct);
        return row == null ? null : new(row.RequestHash, row.ResponseJson, row.HttpStatus, row.ExpiresAtUtc);
    }
    public async Task WriteShopAsync(ShopWriteDto command, CancellationToken ct)
    {
        Shop row;
        if (!command.ExpectedVersion.HasValue) {
            row = new Shop { Id = command.State.Id, CreatedAt = command.Now, CreatedBy = command.ActorUserId };
            db.Shops.Add(row);
        } else {
            row = await db.Shops.SingleAsync(x => x.Id == command.State.Id, ct);
            if (row.Version != command.ExpectedVersion) throw new DbUpdateConcurrencyException();
        }
        Map.Apply(row, command);
        await db.SaveChangesAsync(ct);
    }
    public async Task WriteTemplateAsync(TemplateWriteDto command, CancellationToken ct)
    {
        var state = command.State;
        ShopTemplate row;
        if (!command.ExpectedVersion.HasValue) {
            row = new ShopTemplate { Id = state.Id, Code = state.Code, Name = state.Name, IsActive = false,
                CreatedAt = command.Now, CreatedBy = command.ActorUserId, Version = 1 };
            db.ShopTemplates.Add(row);
            await db.SaveChangesAsync(ct);
        } else {
            row = await db.ShopTemplates.SingleAsync(x => x.Id == state.Id, ct);
            if (row.Version != command.ExpectedVersion) throw new DbUpdateConcurrencyException();
        }
        if (command.NewRevision is { } revision) {
            db.ShopTemplateRevisions.Add(new() { Id = revision.Id, TemplateId = state.Id, Revision = revision.Revision,
                RendererKey = revision.RendererKey, SchemaVersion = revision.SchemaVersion,
                CssVariables = H.Serialize(revision.Defaults), LayoutConfig = H.Serialize(revision.Layout),
                CreatedAt = command.Now, UpdatedAt = command.Now, CreatedBy = command.ActorUserId });
            await db.SaveChangesAsync(ct);
        }
        row.Name = state.Name; row.Description = state.Description; row.SortOrder = state.SortOrder;
        row.PreviewAssetId = state.PreviewAssetId; row.PreviewImageUrl = state.PreviewImageUrl;
        row.IsActive = state.IsActive; row.IsPremium = state.IsPremium; row.Version = state.Version;
        row.CurrentRevisionId = state.CurrentRevision?.Id; row.UpdatedAt = command.Now; row.UpdatedBy = command.ActorUserId;
        if (_catalogLocked) await db.ShopTemplateCatalogStates.Where(x => x.Id == 1)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Version, x => x.Version + 1), ct);
        await db.SaveChangesAsync(ct);
    }
    public async Task WriteMediaAsync(ShopMediaWriteDto command, CancellationToken ct)
    {
        var state = command.State;
        var row = await db.ShopMediaAssets.SingleOrDefaultAsync(x => x.Id == state.Id, ct);
        if (row == null) { row = new() { Id = state.Id, CreatedAt = command.Now }; db.ShopMediaAssets.Add(row); }
        row.OwnerUserId = state.OwnerUserId; row.UploadOperationId = state.UploadOperationId;
        row.Purpose = state.Purpose; row.RequestHash = state.RequestHash; row.Status = state.Status;
        row.PublicUrl = state.PublicUrl; row.ProviderPublicId = state.ProviderPublicId; row.Sha256 = state.Sha256;
        row.ContentType = state.ContentType; row.ByteCount = state.ByteCount; row.Width = state.Width; row.Height = state.Height;
        row.UpdatedAt = command.Now; row.IsDeleted = state.IsDeleted; row.LeaseToken = state.LeaseToken;
        await db.SaveChangesAsync(ct);
    }
    public Task AppendAuditAsync(ShopAuditWriteDto command, CancellationToken ct)
    {
        db.AuditLogs.Add(new() { Id = Guid.NewGuid(), ActorUserId = command.ActorUserId, EntityName = "ShopManagement",
            EntityId = command.ResourceId.ToString(), Action = command.Action, MetadataJson = command.MetadataJson, CreatedAt = command.Now });
        return Task.CompletedTask;
    }
    public async Task StoreReplayAsync(ShopReplayWriteDto command, CancellationToken ct)
    {
        var query = command.Scope;
        var row = await db.ShopIdempotencyRecords.SingleOrDefaultAsync(x => x.ActorUserId == query.ActorUserId && x.Operation == query.Operation && x.ResourceId == query.ResourceId && x.Key == query.Key, ct);
        if (row == null) { row = new() { Id = Guid.NewGuid(), ActorUserId = query.ActorUserId, Operation = query.Operation, ResourceId = query.ResourceId, Key = query.Key, CreatedAt = command.Now }; db.ShopIdempotencyRecords.Add(row); }
        row.RequestHash = command.Result.RequestHash; row.ResponseJson = command.Result.ResponseJson;
        row.HttpStatus = command.Result.HttpStatus; row.ExpiresAtUtc = command.Result.ExpiresAtUtc; row.UpdatedAt = command.Now;
    }
}
