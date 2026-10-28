namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Persistence;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Responses;
using Mooncake.EcommercePlatform.Domain.Enums;
using Map = Mooncake.EcommercePlatform.Infrastructure.Helpers.ShopPersistenceHelper;

public sealed partial class ShopRepository
{
    public async Task<ShopPageResponse<AdminShopSummaryResponse>> ListShopsAsync(AdminShopQuery query, CancellationToken ct)
    {
        var rows = db.Shops.AsNoTracking().Where(x => !x.IsDeleted);
        if (query.Status.HasValue) rows = rows.Where(x => x.Status == query.Status);
        if (query.OwnerProfileId.HasValue) rows = rows.Where(x => x.SupplierId == query.OwnerProfileId);
        if (!string.IsNullOrWhiteSpace(query.Q)) rows = rows.Where(x => x.Name!.Contains(query.Q) || x.Slug.Contains(query.Q));
        var count = await rows.CountAsync(ct);
        rows = query.Sort == "nameAsc" ? rows.OrderBy(x => x.Name).ThenBy(x => x.Id) : rows.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id);
        var items = await rows.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(x => new AdminShopSummaryResponse(x.Id, x.SupplierId, x.Name!, x.Slug, x.Status, x.Version,
                db.Products.Count(p => p.ShopId == x.Id && !p.IsDeleted), x.CreatedAt)).ToArrayAsync(ct);
        return new(items, query.Page, query.PageSize, count);
    }

    public async Task<ShopPageResponse<TemplateStateDto>> ListTemplatesAsync(ShopTemplatePageQueryDto input, CancellationToken ct)
    {
        var query = input.Query;
        var rows = db.ShopTemplates.AsNoTracking().Where(x => !x.IsDeleted);
        if (input.SelectableOnly) rows = rows.Where(x => x.IsActive && x.CurrentRevisionId != null);
        if (query.IsActive.HasValue) rows = rows.Where(x => x.IsActive == query.IsActive);
        if (query.IsPremium.HasValue) rows = rows.Where(x => x.IsPremium == query.IsPremium);
        if (!string.IsNullOrWhiteSpace(query.Q)) rows = rows.Where(x => x.Name!.Contains(query.Q) || x.Code.Contains(query.Q));
        var count = input.SelectableOnly ? 0 : await rows.CountAsync(ct);
        if (input.After is { } after) rows = rows.Where(x => x.SortOrder > after.SortOrder || x.SortOrder == after.SortOrder &&
            (string.Compare(x.Code, after.Name) > 0 || x.Code == after.Name && x.Id.CompareTo(after.Id) > 0));
        rows = query.Sort == "updatedAtDesc" && !input.SelectableOnly ? rows.OrderByDescending(x => x.UpdatedAt).ThenBy(x => x.Id) :
            rows.OrderBy(x => x.SortOrder).ThenBy(x => x.Code).ThenBy(x => x.Id);
        var page = input.SelectableOnly ? rows.Take(query.PageSize + 1) : rows.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize);
        var data = await (from head in page
                          join revision in db.ShopTemplateRevisions.AsNoTracking() on head.CurrentRevisionId equals revision.Id into revisions
                          from revision in revisions.DefaultIfEmpty()
                          select new { head, revision }).ToArrayAsync(ct);
        var ordered = query.Sort == "updatedAtDesc" && !input.SelectableOnly ?
            data.OrderByDescending(x => x.head.UpdatedAt).ThenBy(x => x.head.Id) :
            data.OrderBy(x => x.head.SortOrder).ThenBy(x => x.head.Code, StringComparer.Ordinal).ThenBy(x => x.head.Id);
        return new(ordered.Select(x => Map.Template(x.head, x.revision)).ToArray(), query.Page, query.PageSize, count);
    }

    public async Task<IReadOnlyList<ShopProductCardResponse>> ListProductsAsync(ShopProductPageQueryDto input, CancellationToken ct)
    {
        var query = input.Query;
        var variants = from variant in db.ProductVariants.AsNoTracking()
                       join product in db.Products.AsNoTracking() on variant.ProductId equals product.Id
                       where product.ShopId == input.ShopId && !variant.IsDeleted && !product.IsDeleted
                         && (product.Status == ProductStatus.Active || product.Status == ProductStatus.OutOfStock)
                         && product.BasePrice + variant.PriceAdjustment >= 0
                         && (!query.CategoryId.HasValue || product.CategoryId == query.CategoryId)
                         && (query.Flavor == null || variant.Flavor == query.Flavor)
                         && (query.Filling == null || variant.Filling == query.Filling)
                         && (!query.WeightGram.HasValue || variant.WeightGram == query.WeightGram)
                         && (!query.MinPrice.HasValue || product.BasePrice + variant.PriceAdjustment >= query.MinPrice)
                         && (!query.MaxPrice.HasValue || product.BasePrice + variant.PriceAdjustment <= query.MaxPrice)
                         && (!query.InStockOnly || variant.StockQty > 0 && product.Status == ProductStatus.Active)
                       select new { variant.ProductId, Price = product.BasePrice + variant.PriceAdjustment,
                           InStock = variant.StockQty > 0 && product.Status == ProductStatus.Active };
        if (query.Sort == "newest")
        {
            // Select the bounded page first. Aggregate prices only for those products, rather than the entire shop.
            var newest = db.Products.AsNoTracking().Where(product => product.ShopId == input.ShopId && !product.IsDeleted &&
                (product.Status == ProductStatus.Active || product.Status == ProductStatus.OutOfStock) &&
                (query.Q == null || product.Name.Contains(query.Q)) && variants.Any(variant => variant.ProductId == product.Id));
            if (input.After is { } cursor) newest = newest.Where(product => product.CreatedAt < cursor.CreatedAtUtc ||
                product.CreatedAt == cursor.CreatedAtUtc && product.Id.CompareTo(cursor.Id) > 0);
            return await newest.OrderByDescending(product => product.CreatedAt).ThenBy(product => product.Id).Take(query.PageSize + 1)
                .Select(product => new ShopProductCardResponse(product.Id, product.Name, product.CategoryId,
                    db.ProductImages.Where(image => image.ProductId == product.Id && !image.IsDeleted)
                        .OrderByDescending(image => image.IsPrimary).ThenBy(image => image.SortOrder).ThenBy(image => image.Id)
                        .Select(image => image.ImageUrl).FirstOrDefault(),
                    variants.Where(variant => variant.ProductId == product.Id).Min(variant => variant.Price),
                    variants.Where(variant => variant.ProductId == product.Id).Max(variant => variant.Price),
                    variants.Any(variant => variant.ProductId == product.Id && variant.InStock),
                    product.MinOrderQty, product.CreatedAt)).ToArrayAsync(ct);
        }
        var prices = variants.GroupBy(x => x.ProductId).Select(g => new {
            Id = g.Key, MinPrice = g.Min(x => x.Price), MaxPrice = g.Max(x => x.Price), InStock = g.Any(x => x.InStock) });
        var rows = from product in db.Products.AsNoTracking()
                   join price in prices on product.Id equals price.Id
                   where query.Q == null || product.Name.Contains(query.Q)
                   select new { product.Id, product.Name, product.CategoryId,
                       ImageUrl = db.ProductImages.Where(i => i.ProductId == product.Id && !i.IsDeleted)
                         .OrderByDescending(i => i.IsPrimary).ThenBy(i => i.SortOrder).ThenBy(i => i.Id).Select(i => i.ImageUrl).FirstOrDefault(),
                       price.MinPrice, price.MaxPrice, IsInStock = price.InStock, product.MinOrderQty, CreatedAtUtc = product.CreatedAt };
        if (input.After is { } after) rows = query.Sort switch {
            "priceAsc" => rows.Where(x => x.MinPrice > after.Price || x.MinPrice == after.Price && x.Id.CompareTo(after.Id) > 0),
            "priceDesc" => rows.Where(x => x.MinPrice < after.Price || x.MinPrice == after.Price && x.Id.CompareTo(after.Id) > 0),
            _ => rows.Where(x => x.CreatedAtUtc < after.CreatedAtUtc || x.CreatedAtUtc == after.CreatedAtUtc && x.Id.CompareTo(after.Id) > 0)
        };
        rows = query.Sort switch {
            "priceAsc" => rows.OrderBy(x => x.MinPrice).ThenBy(x => x.Id),
            "priceDesc" => rows.OrderByDescending(x => x.MinPrice).ThenBy(x => x.Id),
            _ => rows.OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id)
        };
        return await rows.Take(query.PageSize + 1).Select(x => new ShopProductCardResponse(x.Id, x.Name, x.CategoryId,
            x.ImageUrl, x.MinPrice, x.MaxPrice, x.IsInStock, x.MinOrderQty, x.CreatedAtUtc)).ToArrayAsync(ct);
    }

    public async Task<IReadOnlyList<ShopCategoryResponse>> ListCategoriesAsync(ShopRelatedPageQueryDto input, CancellationToken ct)
    {
        var rows = db.Categories.AsNoTracking().Where(category => !category.IsDeleted &&
            (input.Query.Q == null || category.Name.Contains(input.Query.Q)) && db.Products.Any(p => p.ShopId == input.ShopId &&
                p.CategoryId == category.Id && !p.IsDeleted && (p.Status == ProductStatus.Active || p.Status == ProductStatus.OutOfStock) &&
                db.ProductVariants.Any(v => v.ProductId == p.Id && !v.IsDeleted && p.BasePrice + v.PriceAdjustment >= 0)));
        if (input.After is { } after) rows = rows.Where(x => string.Compare(x.Name, after.Name) > 0 || x.Name == after.Name && x.Id.CompareTo(after.Id) > 0);
        return await rows.OrderBy(x => x.Name).ThenBy(x => x.Id).Take(input.Query.PageSize + 1)
            .Select(x => new ShopCategoryResponse(x.Id, x.Name)).ToArrayAsync(ct);
    }

    public async Task<IReadOnlyList<ShopReviewResponse>> ListReviewsAsync(ShopRelatedPageQueryDto input, CancellationToken ct)
    {
        var rows = db.Reviews.AsNoTracking().Where(x => x.SupplierId == input.SupplierId && x.IsPublic && !x.IsDeleted &&
            (!input.Rating.HasValue || x.OverallRating == input.Rating));
        if (input.After is { } after) rows = rows.Where(x => x.CreatedAt < after.CreatedAtUtc || x.CreatedAt == after.CreatedAtUtc && x.Id.CompareTo(after.Id) > 0);
        return await rows.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).Take(input.Query.PageSize + 1)
            .Select(x => new ShopReviewResponse(x.Id, x.OverallRating, x.Comment, x.CreatedAt)).ToArrayAsync(ct);
    }

    public async Task<ShopReviewStatsDto> ReviewStatsAsync(ShopIdQueryDto supplierProfile, CancellationToken ct) =>
        await db.Reviews.AsNoTracking().Where(x => x.SupplierId == supplierProfile.Id && x.IsPublic && !x.IsDeleted)
            .GroupBy(x => x.SupplierId).Select(g => new ShopReviewStatsDto(g.Count(), g.Average(x => (decimal?)x.OverallRating)))
            .SingleOrDefaultAsync(ct) ?? new(0, null);
}
