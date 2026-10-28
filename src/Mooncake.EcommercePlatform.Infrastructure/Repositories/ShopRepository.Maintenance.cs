namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Persistence;
using Mooncake.EcommercePlatform.Infrastructure.Helpers;

public sealed partial class ShopRepository
{
    public async Task<IReadOnlyList<ShopMediaStateDto>> ListCleanupCandidatesAsync(ShopCleanupQueryDto query, CancellationToken ct)
    {
        var rows = await db.ShopMediaAssets.AsNoTracking().Where(asset => !asset.IsDeleted &&
            (asset.Status == "Deleting" ? asset.UpdatedAt < query.RetryBeforeUtc : asset.UpdatedAt < query.UnattachedBeforeUtc) &&
            !db.Shops.Any(shop => shop.LogoAssetId == asset.Id || shop.BannerAssetId == asset.Id) &&
            !db.ShopTemplates.Any(template => template.PreviewAssetId == asset.Id) &&
            !db.ProductImages.Any(image => image.MediaAssetId == asset.Id && !image.IsDeleted) &&
            !db.ProductVariants.Any(variant => variant.ImageAssetId == asset.Id && !variant.IsDeleted))
            .OrderBy(x => x.UpdatedAt).ThenBy(x => x.Id).Take(Math.Clamp(query.BatchSize, 1, 100)).ToListAsync(ct);
        return rows.Select(ShopPersistenceHelper.Media).ToArray();
    }
    public async Task<bool> IsMediaReferencedAsync(ShopMediaQueryDto query, CancellationToken ct) =>
        await db.Shops.AnyAsync(shop => shop.LogoAssetId == query.Id || shop.BannerAssetId == query.Id, ct) ||
        await db.ShopTemplates.AnyAsync(template => template.PreviewAssetId == query.Id, ct) ||
        await db.ProductImages.AnyAsync(image => image.MediaAssetId == query.Id && !image.IsDeleted, ct) ||
        await db.ProductVariants.AnyAsync(variant => variant.ImageAssetId == query.Id && !variant.IsDeleted, ct);

    public Task<int> DeleteExpiredReplaysAsync(ShopReplayCleanupDto query, CancellationToken ct)
    {
        var batch = Math.Clamp(query.BatchSize, 1, 1000);
        return db.Database.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM shop_idempotency_records WHERE id IN (
              SELECT id FROM shop_idempotency_records WHERE expires_at_utc < {query.ExpiredBeforeUtc}
              ORDER BY expires_at_utc, id LIMIT {batch} FOR UPDATE SKIP LOCKED)
            """, ct);
    }
}
