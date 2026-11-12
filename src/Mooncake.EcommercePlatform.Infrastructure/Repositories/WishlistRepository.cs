namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Products.Responses;
using Mooncake.EcommercePlatform.Application.DTOs.Wishlist.Persistence;
using Mooncake.EcommercePlatform.Application.DTOs.Wishlist.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;
using Npgsql;

/// <summary>EF Core repository for customer product wishlists with keyset cursor pagination.</summary>
public sealed class WishlistRepository(ApplicationDbContext db) : IWishlistRepository
{
    public async Task<WishlistPageResultDto> GetPageAsync(
        Guid userId,
        WishlistCursorDto? after,
        int pageSize,
        CancellationToken ct = default)
    {
        var baseQuery =
            from wishlist in db.ProductWishlists.AsNoTracking()
            join product in db.Products.AsNoTracking() on wishlist.ProductId equals product.Id
            join shop in db.Shops.AsNoTracking() on product.ShopId equals shop.Id
            join category in db.Categories.AsNoTracking() on product.CategoryId equals category.Id
            join profile in db.SupplierProfiles.AsNoTracking() on shop.SupplierId equals profile.Id into profiles
            from profile in profiles.DefaultIfEmpty()
            where wishlist.UserId == userId
                && !wishlist.IsDeleted
                && !product.IsDeleted
                && !shop.IsDeleted
            select new { wishlist, product, shop, category, profile };

        var totalCount = await baseQuery.CountAsync(ct);

        var filtered = baseQuery;
        if (after is not null)
        {
            filtered = filtered.Where(row =>
                row.wishlist.CreatedAt < after.SavedAtUtc ||
                (row.wishlist.CreatedAt == after.SavedAtUtc && row.wishlist.Id.CompareTo(after.WishlistId) < 0));
        }

        var rows = await filtered
            .OrderByDescending(row => row.wishlist.CreatedAt)
            .ThenByDescending(row => row.wishlist.Id)
            .Take(pageSize + 1)
            .Select(row => new
            {
                WishlistId = row.wishlist.Id,
                SavedAtUtc = row.wishlist.CreatedAt,
                ProductId = row.product.Id,
                ProductName = row.product.Name,
                row.product.BasePrice,
                row.product.MinOrderQty,
                Unit = row.product.Unit ?? "hộp",
                row.product.SupportsCustomPackaging,
                row.product.IsPublished,
                ProductStatus = row.product.Status,
                ShopId = row.shop.Id,
                ShopSlug = row.shop.Slug,
                ShopName = row.shop.Name ?? string.Empty,
                ReputationScore = row.profile != null ? row.profile.ReputationScore : 0m,
                IsVerified = row.profile != null && row.profile.VerificationStatus == SupplierVerificationStatus.Verified,
                row.shop.RegionCode,
                CategoryId = row.category.Id,
                CategoryName = row.category.Name,
                PrimaryImageUrl = db.ProductImages
                    .Where(image => image.ProductId == row.product.Id && !image.IsDeleted)
                    .OrderByDescending(image => image.IsPrimary)
                    .ThenBy(image => image.SortOrder)
                    .ThenBy(image => image.Id)
                    .Select(image => image.ImageUrl)
                    .FirstOrDefault(),
                MinPrice = db.ProductVariants
                    .Where(variant => variant.ProductId == row.product.Id && !variant.IsDeleted)
                    .Select(variant => (decimal?)(row.product.BasePrice + variant.PriceAdjustment))
                    .Min() ?? row.product.BasePrice,
                MaxPrice = db.ProductVariants
                    .Where(variant => variant.ProductId == row.product.Id && !variant.IsDeleted)
                    .Select(variant => (decimal?)(row.product.BasePrice + variant.PriceAdjustment))
                    .Max() ?? row.product.BasePrice,
                HasVariants = db.ProductVariants
                    .Any(variant => variant.ProductId == row.product.Id && !variant.IsDeleted),
                AnyInStock = db.ProductVariants
                    .Any(variant => variant.ProductId == row.product.Id && !variant.IsDeleted && variant.StockQty > 0),
                AverageRating = db.ProductReviews
                    .Where(review => review.ProductId == row.product.Id && !review.IsDeleted &&
                        review.IsPublic && !review.IsHiddenByAdmin && review.Rating.HasValue)
                    .Average(review => (decimal?)review.Rating),
                ReviewCount = db.ProductReviews
                    .Count(review => review.ProductId == row.product.Id && !review.IsDeleted &&
                        review.IsPublic && !review.IsHiddenByAdmin),
            })
            .ToArrayAsync(ct);

        var hasMore = rows.Length > pageSize;
        var items = (hasMore ? rows.Take(pageSize) : rows)
            .Select(row =>
            {
                var effectiveStatus = !row.IsPublished || row.ProductStatus == ProductStatus.Inactive
                    ? ProductStatus.Inactive
                    : row.HasVariants
                        ? (row.AnyInStock ? ProductStatus.Active : ProductStatus.OutOfStock)
                        : row.ProductStatus;

                return new WishlistItemResponse(
                    row.WishlistId,
                    row.ProductId,
                    row.ProductName,
                    row.PrimaryImageUrl,
                    row.BasePrice,
                    row.MinPrice,
                    row.MaxPrice,
                    row.MinOrderQty,
                    row.Unit,
                    row.SupportsCustomPackaging,
                    effectiveStatus,
                    new ProductShopSummaryResponse(
                        row.ShopId,
                        row.ShopSlug,
                        row.ShopName,
                        row.ReputationScore,
                        row.IsVerified,
                        row.RegionCode),
                    new ProductCategorySummaryResponse(row.CategoryId, row.CategoryName),
                    row.AverageRating,
                    row.ReviewCount,
                    row.SavedAtUtc);
            })
            .ToArray();

        WishlistCursorDto? nextCursor = hasMore && items.Length > 0
            ? new WishlistCursorDto(userId, items[^1].WishlistId, items[^1].SavedAtUtc)
            : null;

        return new WishlistPageResultDto(items, hasMore, nextCursor, totalCount);
    }

    public async Task<ToggleWishlistResponse> ToggleAsync(
        Guid userId,
        Guid productId,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        var existing = await db.ProductWishlists
            .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId, ct);

        bool isSaved;
        if (existing is not null)
        {
            if (!existing.IsDeleted)
            {
                db.ProductWishlists.Remove(existing);
                await db.SaveChangesAsync(ct);
                isSaved = false;
            }
            else
            {
                existing.IsDeleted = false;
                existing.CreatedAt = nowUtc;
                existing.UpdatedAt = nowUtc;
                existing.UpdatedBy = userId;
                await db.SaveChangesAsync(ct);
                isSaved = true;
            }
        }
        else
        {
            var productExists = await db.Products.AsNoTracking()
                .AnyAsync(p => p.Id == productId && !p.IsDeleted, ct);
            if (!productExists)
                throw new HttpException(404, WishlistConstants.ProductNotFound);

            var entry = new ProductWishlist
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ProductId = productId,
                IsDeleted = false,
                CreatedAt = nowUtc,
                UpdatedAt = nowUtc,
                CreatedBy = userId,
                UpdatedBy = userId,
            };

            db.ProductWishlists.Add(entry);
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException failure) when (IsUniqueConstraintViolation(failure))
            {
                // Concurrent double-click already inserted the unique (user_id, product_id) pair.
                db.Entry(entry).State = EntityState.Detached;
            }

            isSaved = true;
        }

        var totalCount = await CountActiveAsync(userId, ct);
        return new ToggleWishlistResponse(productId, isSaved, totalCount);
    }

    public async Task<IReadOnlyList<Guid>> GetSavedProductIdsAsync(
        Guid userId,
        IReadOnlyList<Guid> productIds,
        CancellationToken ct = default)
    {
        return await (
            from wishlist in db.ProductWishlists.AsNoTracking()
            join product in db.Products.AsNoTracking() on wishlist.ProductId equals product.Id
            join shop in db.Shops.AsNoTracking() on product.ShopId equals shop.Id
            where wishlist.UserId == userId
                && !wishlist.IsDeleted
                && !product.IsDeleted
                && !shop.IsDeleted
                && productIds.Contains(wishlist.ProductId)
            select wishlist.ProductId)
            .Distinct()
            .ToArrayAsync(ct);
    }

    private Task<int> CountActiveAsync(Guid userId, CancellationToken ct) =>
        (from wishlist in db.ProductWishlists.AsNoTracking()
         join product in db.Products.AsNoTracking() on wishlist.ProductId equals product.Id
         join shop in db.Shops.AsNoTracking() on product.ShopId equals shop.Id
         where wishlist.UserId == userId
             && !wishlist.IsDeleted
             && !product.IsDeleted
             && !shop.IsDeleted
         select wishlist.Id).CountAsync(ct);

    private static bool IsUniqueConstraintViolation(DbUpdateException failure)
    {
        for (Exception? current = failure; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgres && postgres.SqlState == PostgresErrorCodes.UniqueViolation)
                return true;
        }

        return false;
    }
}
