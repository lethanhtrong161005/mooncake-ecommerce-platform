namespace Mooncake.EcommercePlatform.Infrastructure.Helpers;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

internal static class ShopTradingLockHelper
{
    // Call inside the business transaction, after locking the supplier account/profile.
    internal static async Task<bool> CanTradeAsync(ApplicationDbContext context, Guid profileId, CancellationToken ct)
    {
        var shop = await context.Shops.FromSqlInterpolated(
            $"SELECT * FROM shops WHERE supplier_id = {profileId} FOR SHARE").AsNoTracking().SingleOrDefaultAsync(ct);
        return shop is { IsDeleted: false, Status: ShopStatus.Active };
    }
}
