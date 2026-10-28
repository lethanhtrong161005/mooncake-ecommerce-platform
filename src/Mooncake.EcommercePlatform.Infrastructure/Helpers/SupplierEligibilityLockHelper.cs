namespace Mooncake.EcommercePlatform.Infrastructure.Helpers;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Shared User-before-Profile lock order for commands competing with verification changes.</summary>
public static class SupplierEligibilityLockHelper
{
    public static async Task<(User? Owner, SupplierProfile? Profile)> LockAsync(ApplicationDbContext db,
        Guid profileId, IReadOnlyList<Guid> otherUserIds, CancellationToken ct)
    {
        var ownerId = await db.SupplierProfiles.AsNoTracking().Where(x => x.Id == profileId)
            .Select(x => (Guid?)x.UserId).SingleOrDefaultAsync(ct);
        if (ownerId == null) return (null, null);
        var ids = otherUserIds.Append(ownerId.Value).Distinct().Order().ToArray();
        var accounts = await db.Users.FromSqlInterpolated(
            $"SELECT * FROM users WHERE id = ANY ({ids}) ORDER BY id FOR UPDATE").AsNoTracking().ToListAsync(ct);
        var profile = await db.SupplierProfiles.FromSqlInterpolated(
            $"SELECT * FROM supplier_profiles WHERE id = {profileId} FOR UPDATE").AsNoTracking().SingleOrDefaultAsync(ct);
        return (accounts.FirstOrDefault(x => x.Id == ownerId), profile);
    }
}
