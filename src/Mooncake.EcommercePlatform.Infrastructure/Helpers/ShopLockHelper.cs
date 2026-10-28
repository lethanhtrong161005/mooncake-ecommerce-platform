namespace Mooncake.EcommercePlatform.Infrastructure.Helpers;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Persistence;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;
using Npgsql;

/// <summary>One lock order shared by shop writes and existing supplier eligibility writers.</summary>
public static class ShopLockHelper
{
    public static async Task AcquireAsync(ApplicationDbContext db, ShopLockScopeDto scope, CancellationToken ct)
    {
        await db.Database.ExecuteSqlRawAsync("SET LOCAL lock_timeout = '5s'", ct);
        var owner = scope.ShopId.HasValue ? await db.Shops.AsNoTracking().Where(x => x.Id == scope.ShopId).Select(x => (Guid?)x.OwnerUserId).SingleOrDefaultAsync(ct) : scope.ActorUserId;
        var users = new[] { scope.ActorUserId, owner ?? scope.ActorUserId }.Distinct().Order().ToArray();
        await db.Users.FromSqlInterpolated($"SELECT * FROM users WHERE id = ANY ({users}) ORDER BY id FOR UPDATE").AsNoTracking().ToListAsync(ct);
        await db.SupplierProfiles.FromSqlInterpolated($"SELECT * FROM supplier_profiles WHERE user_id = ANY ({users}) ORDER BY id FOR UPDATE").AsNoTracking().ToListAsync(ct);
        var shops = scope.ShopId.HasValue ? await db.Shops.FromSqlInterpolated($"SELECT * FROM shops WHERE id = {scope.ShopId.Value} FOR UPDATE").AsNoTracking().ToListAsync(ct) :
            await db.Shops.FromSqlInterpolated($"SELECT * FROM shops WHERE owner_user_id = {scope.ActorUserId} ORDER BY id FOR UPDATE").AsNoTracking().ToListAsync(ct);
        if (scope.CatalogGuard) await db.ShopTemplateCatalogStates.FromSqlRaw("SELECT * FROM shop_template_catalog_state WHERE id = 1 FOR UPDATE").AsNoTracking().SingleAsync(ct);
        var templates = shops.Select(x => x.TemplateId).Concat(scope.TemplateId.HasValue ? [scope.TemplateId.Value] : Array.Empty<Guid>()).Distinct().Order().ToArray();
        if (templates.Length > 0) await db.ShopTemplates.FromSqlInterpolated($"SELECT * FROM shop_templates WHERE id = ANY ({templates}) ORDER BY id FOR UPDATE").AsNoTracking().ToListAsync(ct);
        if (scope.MediaId.HasValue) await db.ShopMediaAssets.FromSqlInterpolated($"SELECT * FROM shop_media_assets WHERE id = {scope.MediaId.Value} FOR UPDATE").AsNoTracking().SingleOrDefaultAsync(ct);
        // Discard middleware/read state so all later writes start from the locked database version.
        db.ChangeTracker.Clear();
    }
    public static PostgresException? Postgres(Exception exception)
    {
        for (Exception? current = exception; current != null; current = current.InnerException)
            if (current is PostgresException postgres) return postgres;
        return null;
    }
}
