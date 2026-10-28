namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using System.Security.Cryptography;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Persistence;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Responses;
using C = Mooncake.EcommercePlatform.Application.Common.Constant.ShopConstants;
using H = Mooncake.EcommercePlatform.Application.Common.Helpers.ShopPolicyHelper;
using R = Mooncake.EcommercePlatform.Application.Common.Helpers.ShopResponseHelper;

public sealed partial class ShopService
{
    public async Task<ShopMediaResponse> UploadAsync(Guid actor, bool administrator, string key, string purpose, Stream content, CancellationToken ct)
    {
        var parsedKey = H.Key(key);
        H.Require(administrator ? purpose == "TemplatePreview" : purpose is "Logo" or "Banner" or "ProductGallery" or "VariantImage");
        using var buffer = new MemoryStream();
        var chunk = new byte[65536];
        int count;
        while ((count = await content.ReadAsync(chunk, ct)) > 0) {
            H.Require(buffer.Length + count <= C.MaxMediaBytes, 413, C.MediaTooLarge);
            await buffer.WriteAsync(chunk.AsMemory(0, count), ct);
        }
        var bytes = buffer.ToArray(); H.Require(bytes.Length > 0, 415, C.MediaUnsupported);
        var fingerprint = H.Hash(new { purpose, hash = Convert.ToHexString(SHA256.HashData(bytes)) });
        var reserved = await repository.InTransactionAsync(new(actor), async (tx, token) => {
            var account = await tx.GetAccountAsync(new(actor), token);
            if (administrator) H.Admin(account); else {
                H.Supplier(account);
                var shop = await tx.GetOwnerShopAsync(new(actor), token);
                if (shop != null) H.Editable(shop);
            }
            var existing = await tx.GetUploadAsync(new(actor, parsedKey), token);
            if (existing != null) {
                H.Require(existing.RequestHash == fingerprint && !existing.IsDeleted, 409, C.KeyMismatch);
                if (existing.Status == "Ready") return existing;
                H.Require(existing.Status != "Uploading" || existing.UpdatedAtUtc < clock.UtcNow.AddMinutes(-5), 409, C.MediaNotReady);
            }
            var usage = await tx.GetUploadUsageAsync(new(actor), clock.UtcNow.AddHours(-1), clock.UtcNow.AddMinutes(-5), token);
            H.Require(usage.RecentCount < 20 && usage.ActiveCount < 2 &&
                await tx.CountCommandsAsync(new(actor), C.MediaAttempt, clock.UtcNow.AddHours(-1), token) < 20, 429, C.RateLimited);
            var state = existing ?? new ShopMediaStateDto(Guid.NewGuid(), actor, parsedKey, purpose, fingerprint,
                "Uploading", null, null, null, null, bytes.Length, 0, 0, clock.UtcNow);
            state = state with { Status = "Uploading", LeaseToken = Guid.NewGuid(), UpdatedAtUtc = clock.UtcNow };
            if (state.ProviderPublicId != null) {
                // Preserve the previous provider object for cleanup before a retry replaces its upload proof.
                await tx.WriteMediaAsync(new(state with { Id = Guid.NewGuid(), UploadOperationId = Guid.NewGuid(),
                    Status = "Failed", LeaseToken = Guid.NewGuid() }, clock.UtcNow), token);
                state = state with { ProviderPublicId = null, PublicUrl = null, Sha256 = null };
            }
            await tx.WriteMediaAsync(new(state, clock.UtcNow), token);
            await tx.AppendAuditAsync(new(actor, state.Id, C.MediaAttempt, H.Serialize(new { purpose, state.ByteCount }), clock.UtcNow), token);
            return state;
        }, ct);
        if (reserved.Status == "Ready") return R.Media(reserved);
        Mooncake.EcommercePlatform.Application.Common.Interfaces.ShopUploadedMediaDto? uploaded = null;
        try {
            var processed = await media.PrepareAsync(bytes, purpose, ct);
            uploaded = await media.UploadAsync(processed, ct);
            return await repository.InTransactionAsync(new(actor, MediaId: reserved.Id), async (tx, token) => {
                var account = await tx.GetAccountAsync(new(actor), token);
                if (administrator) H.Admin(account); else {
                    H.Supplier(account);
                    var shop = await tx.GetOwnerShopAsync(new(actor), token);
                    if (shop != null) H.Editable(shop);
                }
                var current = await tx.GetMediaAsync(new(reserved.Id), token);
                H.Require(current is { Status: "Uploading", IsDeleted: false } && current.LeaseToken == reserved.LeaseToken, 409, C.MediaNotReady);
                var ready = current! with { Status = "Ready", PublicUrl = uploaded.Url, ProviderPublicId = uploaded.PublicId,
                    Sha256 = Convert.ToHexString(SHA256.HashData(processed.Content)), ContentType = processed.ContentType,
                    ByteCount = processed.Content.Length, Width = processed.Width, Height = processed.Height, UpdatedAtUtc = clock.UtcNow };
                await tx.WriteMediaAsync(new(ready, clock.UtcNow), token);
                await tx.AppendAuditAsync(new(actor, ready.Id, C.MediaReady, H.Serialize(new { ready.Purpose, ready.ByteCount, ready.Width, ready.Height }), clock.UtcNow), token);
                return R.Media(ready);
            }, ct);
        }
        catch (Exception failure) {
            var cleanupPending = false;
            if (uploaded != null) {
                try { await media.DeleteAsync(uploaded.PublicId, CancellationToken.None); }
                catch { cleanupPending = true; }
            }
            await repository.InTransactionAsync(new(actor, MediaId: reserved.Id), async (tx, token) => {
                var current = await tx.GetMediaAsync(new(reserved.Id), token);
                if (current?.Status == "Uploading" && current.LeaseToken == reserved.LeaseToken)
                    await tx.WriteMediaAsync(new(current with {
                        Status = failure is HttpException { StatusCode: 415 } ? "Quarantined" : "Failed",
                        ProviderPublicId = cleanupPending ? uploaded!.PublicId : null,
                        PublicUrl = cleanupPending ? uploaded!.Url : null
                    }, clock.UtcNow), token);
                else if (cleanupPending)
                    await tx.WriteMediaAsync(new(reserved with { Id = Guid.NewGuid(), UploadOperationId = Guid.NewGuid(),
                        Status = "Failed", ProviderPublicId = uploaded!.PublicId, PublicUrl = uploaded.Url }, clock.UtcNow), token);
                return true;
            }, CancellationToken.None);
            if (failure is HttpException || failure is OperationCanceledException && ct.IsCancellationRequested) throw;
            throw new HttpException(503, C.Error, C.StorageUnavailable);
        }
    }
}
