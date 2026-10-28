namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Persistence;
using Mooncake.EcommercePlatform.Infrastructure.Helpers;

public sealed partial class ShopRepository
{
    public async Task<ShopDisplayDto?> GetDisplayAsync(ShopDisplayQueryDto query, CancellationToken ct)
    {
        var row = await (from shop in db.Shops.AsNoTracking()
            join profile in db.SupplierProfiles.AsNoTracking() on shop.SupplierId equals profile.Id
            join owner in db.Users.AsNoTracking() on shop.OwnerUserId equals owner.Id
            join template in db.ShopTemplates.AsNoTracking() on shop.TemplateId equals template.Id
            join revision in db.ShopTemplateRevisions.AsNoTracking() on shop.TemplateRevisionId equals revision.Id
            where (query.OwnerUserId == null || shop.OwnerUserId == query.OwnerUserId) &&
                (query.Slug == null || shop.Slug == query.Slug) && (query.ShopId == null || shop.Id == query.ShopId)
            select new { shop, template, revision, Account = new ShopAccountStateDto(owner.Id, owner.Role, owner.IsActive,
                owner.IsDeleted, profile.Id, profile.VerificationStatus, profile.IsDeleted, profile.ReputationScore) }).SingleOrDefaultAsync(ct);
        return row == null ? null : new(ShopPersistenceHelper.Shop(row.shop), row.Account,
            ShopPersistenceHelper.Template(row.template, row.revision), ShopPersistenceHelper.Revision(row.revision));
    }
}
