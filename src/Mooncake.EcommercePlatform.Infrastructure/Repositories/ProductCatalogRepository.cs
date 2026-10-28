namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using System.Data;
using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Products.Persistence;
using Mooncake.EcommercePlatform.Application.DTOs.Products.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Products.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;
using Npgsql;
using H = Mooncake.EcommercePlatform.Application.Common.Helpers.ProductPolicyHelper;

/// <summary>PostgreSQL catalog persistence with DTO-only boundaries and bounded transaction retry.</summary>
public sealed class ProductCatalogRepository(ApplicationDbContext db, IDateTimeProvider clock) : IProductCatalogRepository
{
    public async Task<T> InTransactionAsync<T>(ProductLockScopeDto scope,
        Func<IProductCatalogTransaction, CancellationToken, Task<T>> work, CancellationToken ct)
    {
        for (var attempt = 0; ; attempt++)
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            try
            {
                await LockAsync(scope, ct);
                var result = await work(this, ct);
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return result;
            }
            catch (Exception failure)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                db.ChangeTracker.Clear();
                var postgres = Postgres(failure);
                if (postgres?.SqlState is "40P01" or "40001" && attempt < 2)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(50 * (attempt + 1) + Random.Shared.Next(30)), ct);
                    continue;
                }
                if (failure is DbUpdateConcurrencyException)
                    throw new HttpException(409, ProductConstants.Error, ProductConstants.VersionConflict);
                if (postgres?.SqlState == "23505")
                {
                    var code = postgres.ConstraintName switch
                    {
                        "uq_product_variant_shop_sku" => ProductConstants.SkuConflict,
                        "uq_product_primary_image" or "uq_product_image_order" => ProductConstants.GalleryInvalid,
                        "uq_product_media_asset" => ProductConstants.MediaConflict,
                        _ => ProductConstants.KeyConflict
                    };
                    throw new HttpException(409, ProductConstants.Error, code);
                }
                if (postgres?.SqlState is "40P01" or "40001" or "55P03" or "57014")
                    throw new HttpException(503, ProductConstants.Error, ProductConstants.Busy);
                if (postgres?.SqlState == "23503")
                    throw new HttpException(409, ProductConstants.Error, ProductConstants.VersionConflict);
                throw;
            }
        }
    }

    public Task<ProductAccountStateDto?> GetAccountAsync(Guid actorUserId, CancellationToken ct) =>
        (from user in db.Users.AsNoTracking()
         join profile in db.SupplierProfiles.AsNoTracking() on user.Id equals profile.UserId into profiles
         from profile in profiles.DefaultIfEmpty()
         join shop in db.Shops.AsNoTracking() on user.Id equals shop.OwnerUserId into shops
         from shop in shops.DefaultIfEmpty()
         where user.Id == actorUserId
         select new ProductAccountStateDto(user.Id, user.Role, user.IsActive, user.IsDeleted,
             profile == null ? null : profile.Id, profile == null ? null : profile.VerificationStatus,
             profile != null && profile.IsDeleted, shop == null ? null : shop.Id, shop == null ? null : shop.Status,
             shop != null && shop.IsDeleted, profile == null ? 0 : profile.ReputationScore)).SingleOrDefaultAsync(ct);

    public async Task<ProductStateDto?> GetProductAsync(Guid productId, CancellationToken ct)
    {
        var product = await (from productRow in db.Products.AsNoTracking()
                             join category in db.Categories.AsNoTracking() on productRow.CategoryId equals category.Id
                             where productRow.Id == productId
                             select new { p = productRow, category }).SingleOrDefaultAsync(ct);
        if (product == null) return null;
        var variants = await (from variant in db.ProductVariants.AsNoTracking()
                              join media in db.ShopMediaAssets.AsNoTracking() on variant.ImageAssetId equals media.Id into mediaRows
                              from media in mediaRows.DefaultIfEmpty()
                              where variant.ProductId == productId
                              orderby variant.CreatedAt, variant.Id
                              select new ProductVariantStateDto(variant.Id, variant.ProductId, variant.ShopId,
                                  variant.Name, variant.Sku, variant.NormalizedSku, variant.CrustType ?? variant.Flavor,
                                  variant.Filling, variant.SizeLabel, variant.WeightGram, variant.IsGiftSet,
                                  variant.PriceAdjustment, variant.StockQty, variant.InventoryVersion,
                                  variant.ImageAssetId, media == null ? variant.ImageUrl : media.PublicUrl,
                                  variant.IsDeleted, variant.CreatedAt, variant.UpdatedAt)).ToArrayAsync(ct);
        var images = await (from image in db.ProductImages.AsNoTracking()
                            join media in db.ShopMediaAssets.AsNoTracking() on image.MediaAssetId equals media.Id into mediaRows
                            from media in mediaRows.DefaultIfEmpty()
                            where image.ProductId == productId
                            orderby image.SortOrder, image.Id
                            select new ProductImageStateDto(image.Id, image.ProductId, image.MediaAssetId,
                                media == null ? image.ImageUrl : media.PublicUrl ?? image.ImageUrl, image.IsPrimary, image.SortOrder,
                                media != null && media.Status == "Ready" && !media.IsDeleted, image.IsDeleted, image.CreatedAt)).ToArrayAsync(ct);
        var p = product.p;
        return new(p.Id, p.ShopId, p.CategoryId, product.category.Name, !product.category.IsDeleted,
            p.Version, p.Name, p.NormalizedName, p.Description, p.Ingredients, p.BasePrice, p.MinOrderQty, p.MaxOrderQty,
            p.Unit ?? "piece", p.SupportsCustomPackaging, p.IsPublished, p.Status, p.PublishedAtUtc,
            p.IsDeleted, p.CreatedAt, p.UpdatedAt, variants, images);
    }

    public async Task<ProductStateDto?> GetMineAsync(Guid actorUserId, Guid productId, CancellationToken ct)
    {
        var account = await GetAccountAsync(actorUserId, ct);
        var state = await GetProductAsync(productId, ct);
        return account?.ShopId == state?.ShopId ? state : null;
    }

    public async Task<ProductCategoryStateDto?> GetCategoryAsync(Guid categoryId, CancellationToken ct)
    {
        var row = await db.Categories.AsNoTracking().Where(x => x.Id == categoryId)
            .Select(x => new ProductCategoryStateDto(x.Id, x.Name, x.IsDeleted)).SingleOrDefaultAsync(ct);
        return row;
    }

    public async Task<IReadOnlyList<ProductMediaStateDto>> GetMediaAsync(IReadOnlyCollection<Guid> mediaIds, CancellationToken ct) =>
        await db.ShopMediaAssets.AsNoTracking().Where(x => mediaIds.Contains(x.Id))
            .Select(x => new ProductMediaStateDto(x.Id, x.OwnerUserId, x.Purpose, x.Status, x.PublicUrl,
                x.ByteCount, x.Width, x.Height, x.IsDeleted)).ToArrayAsync(ct);

    public async Task<bool> IsMediaBoundAsync(Guid mediaId, Guid? excludingProductId, CancellationToken ct)
    {
        if (await db.ProductImages.AnyAsync(x => x.MediaAssetId == mediaId && !x.IsDeleted &&
            (!excludingProductId.HasValue || x.ProductId != excludingProductId), ct)) return true;
        return await db.ProductVariants.AnyAsync(x => x.ImageAssetId == mediaId && !x.IsDeleted &&
            (!excludingProductId.HasValue || x.ProductId != excludingProductId), ct);
    }

    public async Task<ProductReplayDto?> GetReplayAsync(ProductReplayQueryDto query, CancellationToken ct)
    {
        var row = await db.ShopIdempotencyRecords.AsNoTracking().SingleOrDefaultAsync(x =>
            x.ActorUserId == query.ActorUserId && x.Operation == query.Operation && x.ResourceId == query.ResourceId && x.Key == query.Key, ct);
        return row == null ? null : new(row.RequestHash, row.ResponseJson, row.ExpiresAtUtc);
    }

    public async Task SaveAggregateAsync(ProductAggregateWriteDto command, CancellationToken ct)
    {
        var state = command.State;
        Product product;
        if (!command.ExpectedVersion.HasValue)
        {
            product = new Product { Id = state.Id, CreatedAt = command.NowUtc, CreatedBy = command.ActorUserId };
            db.Products.Add(product);
        }
        else
        {
            product = await db.Products.SingleAsync(x => x.Id == state.Id, ct);
            if (product.Version != command.ExpectedVersion.Value) throw new DbUpdateConcurrencyException();
        }
        product.ShopId = state.ShopId; product.CategoryId = state.CategoryId; product.Version = state.Version;
        product.Name = state.Name; product.NormalizedName = state.NormalizedName; product.Description = state.Description; product.Ingredients = state.Ingredients;
        product.BasePrice = state.BasePrice; product.MinOrderQty = state.MinOrderQty; product.MaxOrderQty = state.MaxOrderQty;
        product.Unit = state.Unit; product.SupportsCustomPackaging = state.SupportsCustomPackaging;
        product.IsPublished = state.IsPublished; product.Status = state.Status; product.PublishedAtUtc = state.PublishedAtUtc;
        product.IsDeleted = state.IsDeleted; product.UpdatedAt = command.NowUtc; product.UpdatedBy = command.ActorUserId;

        var variants = await db.ProductVariants.Where(x => x.ProductId == state.Id).ToDictionaryAsync(x => x.Id, ct);
        foreach (var source in state.Variants)
        {
            if (!variants.TryGetValue(source.Id, out var target))
            {
                target = new ProductVariant { Id = source.Id, ProductId = state.Id, ShopId = state.ShopId,
                    CreatedAt = source.CreatedAtUtc, CreatedBy = command.ActorUserId };
                db.ProductVariants.Add(target);
            }
            target.ShopId = state.ShopId; target.Name = source.Name; target.Sku = source.Sku;
            target.NormalizedSku = source.NormalizedSku; target.CrustType = source.CrustType;
            target.Flavor = source.CrustType; target.Filling = source.Filling; target.SizeLabel = source.SizeLabel;
            target.WeightGram = source.WeightGram; target.IsGiftSet = source.IsGiftSet;
            target.PriceAdjustment = source.PriceAdjustment; target.StockQty = source.StockQty;
            target.InventoryVersion = source.InventoryVersion; target.ImageAssetId = source.ImageAssetId;
            target.ImageUrl = source.ImageUrl; target.IsDeleted = source.IsDeleted;
            target.UpdatedAt = command.NowUtc; target.UpdatedBy = command.ActorUserId;
        }

        var images = await db.ProductImages.Where(x => x.ProductId == state.Id).ToDictionaryAsync(x => x.Id, ct);
        foreach (var source in state.Images)
        {
            if (!images.TryGetValue(source.Id, out var target))
            {
                target = new ProductImage { Id = source.Id, ProductId = state.Id, CreatedAt = source.CreatedAtUtc,
                    CreatedBy = command.ActorUserId };
                db.ProductImages.Add(target);
            }
            target.MediaAssetId = source.MediaAssetId; target.ImageUrl = source.ImageUrl;
            target.IsPrimary = source.IsPrimary; target.SortOrder = source.SortOrder; target.IsDeleted = source.IsDeleted;
            target.UpdatedAt = command.NowUtc; target.UpdatedBy = command.ActorUserId;
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task<ProductInventoryResultDto> AdjustInventoryAsync(ProductInventoryWriteDto command, CancellationToken ct)
    {
        var product = await db.Products.SingleOrDefaultAsync(x => x.Id == command.ProductId && x.ShopId == command.ShopId && !x.IsDeleted, ct);
        if (product == null) throw new HttpException(404, ProductConstants.Error, ProductConstants.NotFound);
        if (product.Version != command.ExpectedProductVersion) throw new HttpException(409, ProductConstants.Error, ProductConstants.VersionConflict);
        var duplicate = await db.ProductInventoryMovements.AsNoTracking().SingleOrDefaultAsync(x =>
            x.SourceType == "SupplierAdjustment" && x.SourceId == command.SourceId && x.VariantId == command.VariantId, ct);
        if (duplicate != null) throw new HttpException(409, ProductConstants.Error, ProductConstants.KeyConflict);
        var variant = await db.ProductVariants.SingleOrDefaultAsync(x => x.Id == command.VariantId &&
            x.ProductId == command.ProductId && !x.IsDeleted, ct);
        if (variant == null) throw new HttpException(404, ProductConstants.Error, ProductConstants.VariantNotFound);
        var previous = variant.StockQty;
        var current = (long)previous + command.Delta;
        if (current < 0 || current > 10_000_000) throw new HttpException(409, ProductConstants.Error, ProductConstants.InventoryInsufficient);
        variant.StockQty = (int)current; variant.InventoryVersion++; variant.UpdatedAt = command.NowUtc; variant.UpdatedBy = command.ActorUserId;
        var anyOtherStock = await db.ProductVariants.AsNoTracking().AnyAsync(x => x.ProductId == product.Id &&
            x.Id != variant.Id && !x.IsDeleted && x.StockQty > 0, ct);
        var effective = !product.IsPublished ? ProductStatus.Inactive : current > 0 || anyOtherStock ? ProductStatus.Active : ProductStatus.OutOfStock;
        product.Status = effective; product.UpdatedAt = command.NowUtc; product.UpdatedBy = command.ActorUserId;
        var movement = new ProductInventoryMovement
        {
            Id = Guid.NewGuid(), ProductId = product.Id, VariantId = variant.Id, ShopId = product.ShopId,
            ActorUserId = command.ActorUserId, SourceType = "SupplierAdjustment", SourceId = command.SourceId,
            Delta = command.Delta, PreviousQty = previous, CurrentQty = (int)current, Reason = command.Reason,
            CreatedAt = command.NowUtc, UpdatedAt = command.NowUtc, CreatedBy = command.ActorUserId
        };
        db.ProductInventoryMovements.Add(movement);
        await db.SaveChangesAsync(ct);
        return new(movement.Id, previous, (int)current, variant.InventoryVersion, product.Version, effective);
    }

    public Task AppendAuditAsync(ProductAuditWriteDto command, CancellationToken ct)
    {
        db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), ActorUserId = command.ActorUserId,
            EntityName = "ProductCatalog", EntityId = command.ResourceId.ToString(), Action = command.Action,
            MetadataJson = command.MetadataJson, CreatedAt = command.NowUtc, UpdatedAt = command.NowUtc });
        return Task.CompletedTask;
    }

    public Task AppendOutboxAsync(ProductOutboxWriteDto command, CancellationToken ct)
    {
        db.ProductCatalogOutboxEvents.Add(new ProductCatalogOutboxEvent { Id = Guid.NewGuid(), ProductId = command.ProductId,
            SourceVersion = command.SourceVersion, EventType = command.EventType, AvailableAtUtc = command.NowUtc,
            CreatedAt = command.NowUtc, UpdatedAt = command.NowUtc });
        return Task.CompletedTask;
    }

    public async Task StoreReplayAsync(ProductReplayWriteDto command, CancellationToken ct)
    {
        var scope = command.Scope;
        var row = await db.ShopIdempotencyRecords.SingleOrDefaultAsync(x => x.ActorUserId == scope.ActorUserId &&
            x.Operation == scope.Operation && x.ResourceId == scope.ResourceId && x.Key == scope.Key, ct);
        if (row == null)
        {
            row = new ShopIdempotencyRecord { Id = Guid.NewGuid(), ActorUserId = scope.ActorUserId,
                Operation = scope.Operation, ResourceId = scope.ResourceId, Key = scope.Key, CreatedAt = command.NowUtc };
            db.ShopIdempotencyRecords.Add(row);
        }
        row.RequestHash = command.RequestHash; row.ResponseJson = command.ResponseJson; row.HttpStatus = 200;
        row.ExpiresAtUtc = command.ExpiresAtUtc; row.UpdatedAt = command.NowUtc;
    }

    public async Task<ProductPageResponse<ProductOwnerCardResponse>> ListMineAsync(Guid actorUserId, OwnerProductQuery query, CancellationToken ct)
    {
        var account = await GetAccountAsync(actorUserId, ct);
        if (account?.ShopId == null) return new([], query.Page, query.PageSize, 0);
        var rows = db.Products.AsNoTracking().Where(x => x.ShopId == account.ShopId && !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(query.Q)) rows = rows.Where(x => x.Name.Contains(query.Q));
        if (query.CategoryId.HasValue) rows = rows.Where(x => x.CategoryId == query.CategoryId);
        if (query.Status.HasValue)
        {
            rows = query.Status.Value switch
            {
                ProductStatus.Inactive => rows.Where(x => !x.IsPublished),
                ProductStatus.Active => rows.Where(x => x.IsPublished && db.ProductVariants.Any(v => v.ProductId == x.Id && !v.IsDeleted && v.StockQty > 0)),
                ProductStatus.OutOfStock => rows.Where(x => x.IsPublished && !db.ProductVariants.Any(v => v.ProductId == x.Id && !v.IsDeleted && v.StockQty > 0)),
                _ => rows
            };
        }
        var total = await rows.CountAsync(ct);
        rows = query.Sort switch
        {
            "nameAsc" => rows.OrderBy(x => x.Name).ThenBy(x => x.Id),
            "stockAsc" => rows.OrderBy(x => db.ProductVariants.Where(v => v.ProductId == x.Id && !v.IsDeleted).Sum(v => v.StockQty)).ThenBy(x => x.Id),
            _ => rows.OrderByDescending(x => x.UpdatedAt).ThenBy(x => x.Id)
        };
        var items = await rows.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(x => new ProductOwnerCardResponse(x.Id, x.Version, x.Name, x.CategoryId,
                db.ProductImages.Where(i => i.ProductId == x.Id && !i.IsDeleted)
                    .OrderByDescending(i => i.IsPrimary).ThenBy(i => i.SortOrder).Select(i => i.ImageUrl).FirstOrDefault(),
                db.ProductVariants.Where(v => v.ProductId == x.Id && !v.IsDeleted).Min(v => (decimal?)(x.BasePrice + v.PriceAdjustment)) ?? x.BasePrice,
                db.ProductVariants.Where(v => v.ProductId == x.Id && !v.IsDeleted).Max(v => (decimal?)(x.BasePrice + v.PriceAdjustment)) ?? x.BasePrice,
                !x.IsPublished ? ProductStatus.Inactive : db.ProductVariants.Any(v => v.ProductId == x.Id && !v.IsDeleted && v.StockQty > 0) ? ProductStatus.Active : ProductStatus.OutOfStock,
                db.ProductVariants.Count(v => v.ProductId == x.Id && !v.IsDeleted),
                db.ProductVariants.Where(v => v.ProductId == x.Id && !v.IsDeleted).Sum(v => v.StockQty), x.UpdatedAt)).ToArrayAsync(ct);
        return new(items, query.Page, query.PageSize, total);
    }

    public async Task<ProductPublicDetailResponse?> GetPublicAsync(Guid productId, CancellationToken ct)
    {
        var root = await (from product in db.Products.AsNoTracking()
                          join shop in db.Shops.AsNoTracking() on product.ShopId equals shop.Id
                          join profile in db.SupplierProfiles.AsNoTracking() on shop.SupplierId equals profile.Id
                          join user in db.Users.AsNoTracking() on shop.OwnerUserId equals user.Id
                          join category in db.Categories.AsNoTracking() on product.CategoryId equals category.Id
                          where product.Id == productId && product.IsPublished && !product.IsDeleted && !shop.IsDeleted &&
                            shop.Status == ShopStatus.Active && !profile.IsDeleted && profile.VerificationStatus == SupplierVerificationStatus.Verified &&
                            user.IsActive && !user.IsDeleted && user.Role == UserRole.Supplier && !category.IsDeleted
                          select new { product, shop, profile, category }).SingleOrDefaultAsync(ct);
        if (root == null) return null;
        var variants = await (from variant in db.ProductVariants.AsNoTracking()
                              join media in db.ShopMediaAssets.AsNoTracking() on variant.ImageAssetId equals media.Id into mediaRows
                              from media in mediaRows.DefaultIfEmpty()
                              where variant.ProductId == productId && !variant.IsDeleted && root.product.BasePrice + variant.PriceAdjustment > 0
                              orderby variant.CreatedAt, variant.Id
                              select new ProductVariantPublicResponse(variant.Id, variant.Name, variant.Sku,
                                  variant.CrustType ?? variant.Flavor, variant.Filling, variant.SizeLabel, variant.WeightGram, variant.IsGiftSet,
                                  root.product.BasePrice + variant.PriceAdjustment, variant.StockQty > 0,
                                  media == null || media.IsDeleted || media.Status != "Ready" ? variant.ImageUrl : media.PublicUrl ?? variant.ImageUrl))
            .ToArrayAsync(ct);
        if (variants.Length == 0) return null;
        var images = await (from image in db.ProductImages.AsNoTracking()
                            join media in db.ShopMediaAssets.AsNoTracking() on image.MediaAssetId equals media.Id into mediaRows
                            from media in mediaRows.DefaultIfEmpty()
                            where image.ProductId == productId && !image.IsDeleted &&
                                (image.MediaAssetId == null || (media != null && !media.IsDeleted && media.Status == "Ready"))
                            orderby image.SortOrder, image.Id
                            select new ProductImageResponse(image.Id, image.MediaAssetId, media == null ? image.ImageUrl : media.PublicUrl ?? image.ImageUrl,
                                image.IsPrimary, image.SortOrder)).ToArrayAsync(ct);
        var stats = await db.ProductReviews.AsNoTracking().Where(x => x.ProductId == productId && !x.IsDeleted && x.IsPublic && !x.IsHiddenByAdmin && x.Rating.HasValue)
            .GroupBy(x => x.ProductId).Select(g => new { Count = g.Count(), Average = g.Average(x => (decimal?)x.Rating) }).SingleOrDefaultAsync(ct);
        var status = variants.Any(x => x.IsAvailable) ? ProductStatus.Active : ProductStatus.OutOfStock;
        return new(root.product.Id, root.product.Name, root.product.Description, root.product.Ingredients,
            new(root.category.Id, root.category.Name),
            new(root.shop.Id, root.shop.Slug, root.shop.Name!, root.profile.ReputationScore, true, root.shop.RegionCode),
            root.product.BasePrice, root.product.MinOrderQty, root.product.MaxOrderQty, root.product.Unit ?? "piece",
            root.product.SupportsCustomPackaging, status, variants, images, stats?.Average, stats?.Count ?? 0,
            root.product.PublishedAtUtc ?? root.product.CreatedAt);
    }

    public async Task<ProductSearchResponse> SearchAsync(ProductSearchQuery query, CancellationToken ct)
    {
        var rows = from document in db.ProductSearchDocuments.AsNoTracking()
                   join product in db.Products.AsNoTracking() on document.ProductId equals product.Id
                   join shop in db.Shops.AsNoTracking() on product.ShopId equals shop.Id
                   join profile in db.SupplierProfiles.AsNoTracking() on shop.SupplierId equals profile.Id
                   join user in db.Users.AsNoTracking() on shop.OwnerUserId equals user.Id
                   join category in db.Categories.AsNoTracking() on product.CategoryId equals category.Id
                   where document.SourceVersion <= product.Version && product.IsPublished && !product.IsDeleted &&
                     shop.Status == ShopStatus.Active && !shop.IsDeleted && profile.VerificationStatus == SupplierVerificationStatus.Verified &&
                     !profile.IsDeleted && user.IsActive && !user.IsDeleted && user.Role == UserRole.Supplier && !category.IsDeleted
                   select new { Document = document, Product = product, Shop = shop, Profile = profile, Category = category };

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var pattern = $"%{query.Q}%";
            rows = rows.Where(x => EF.Functions.ILike(x.Document.SearchText, pattern) ||
                EF.Functions.ILike(x.Shop.Name!, pattern));
        }
        if (query.CategoryIds.Length > 0) rows = rows.Where(x => query.CategoryIds.Contains(x.Product.CategoryId));
        if (query.MinPrice.HasValue) rows = rows.Where(x => db.ProductVariants.Any(v => v.ProductId == x.Product.Id && !v.IsDeleted &&
            x.Product.BasePrice + v.PriceAdjustment >= query.MinPrice && (!query.MaxPrice.HasValue || x.Product.BasePrice + v.PriceAdjustment <= query.MaxPrice)));
        else if (query.MaxPrice.HasValue) rows = rows.Where(x => db.ProductVariants.Any(v => v.ProductId == x.Product.Id && !v.IsDeleted &&
            x.Product.BasePrice + v.PriceAdjustment <= query.MaxPrice));
        if (query.MaxMinOrderQty.HasValue) rows = rows.Where(x => x.Product.MinOrderQty <= query.MaxMinOrderQty);
        if (query.SupportsCustomPackaging.HasValue) rows = rows.Where(x => x.Product.SupportsCustomPackaging == query.SupportsCustomPackaging);
        if (query.MinReputation.HasValue) rows = rows.Where(x => x.Profile.ReputationScore >= query.MinReputation);
        if (query.RegionCodes.Length > 0) rows = rows.Where(x => x.Shop.RegionCode != null && query.RegionCodes.Contains(x.Shop.RegionCode));
        if (query.InStockOnly) rows = rows.Where(x => db.ProductVariants.Any(v => v.ProductId == x.Product.Id && !v.IsDeleted && v.StockQty > 0));

        // Materialise the bounded result set once. This avoids EF trying to translate
        // grouping over the aggregate record projection and keeps facet/sort behaviour
        // deterministic across PostgreSQL provider versions.
        var projectedRows = rows.Select(x => new ProductSearchRow(x.Document, x.Product, x.Shop, x.Profile, x.Category,
            x.Document.MinPrice, x.Document.MaxPrice, x.Document.EffectiveStatus == ProductStatus.Active));
        var allRows = await projectedRows.ToArrayAsync(ct);
        var total = allRows.Length;
        var categoryFacets = allRows.GroupBy(x => new { x.Category.Id, x.Category.Name })
            .OrderByDescending(g => g.Count()).Take(50)
            .Select(g => new ProductFacetValueResponse(g.Key.Id.ToString(), g.Key.Name, g.Count())).ToArray();
        var regionFacets = allRows.Where(x => x.Shop.RegionCode != null).GroupBy(x => x.Shop.RegionCode!)
            .OrderByDescending(g => g.Count()).Take(50)
            .Select(g => new ProductFacetValueResponse(g.Key, g.Key, g.Count())).ToArray();
        var minFacet = allRows.Select(x => x.MinPrice).Where(x => x.HasValue).Select(x => x!.Value).DefaultIfEmpty().Min();
        var maxFacet = allRows.Select(x => x.MaxPrice).Where(x => x.HasValue).Select(x => x!.Value).DefaultIfEmpty().Max();
        var packagingCount = allRows.Count(x => x.Product.SupportsCustomPackaging);
        var stockCount = allRows.Count(x => x.InStock);

        IEnumerable<ProductSearchRow> sorted = query.Sort switch
        {
            "priceAsc" => allRows.OrderBy(x => x.MinPrice).ThenBy(x => x.Product.Id),
            "priceDesc" => allRows.OrderByDescending(x => x.MinPrice).ThenBy(x => x.Product.Id),
            "reputation" => allRows.OrderByDescending(x => x.Profile.ReputationScore).ThenBy(x => x.Product.Id),
            "rating" => allRows.OrderByDescending(x => x.Document.AverageRating).ThenByDescending(x => x.Document.ReviewCount).ThenBy(x => x.Product.Id),
            "bestSelling" => allRows.OrderByDescending(x => x.Document.SoldCount).ThenByDescending(x => x.Profile.ReputationScore).ThenBy(x => x.Product.Id),
            "newest" => allRows.OrderByDescending(x => x.Product.PublishedAtUtc ?? x.Product.CreatedAt).ThenBy(x => x.Product.Id),
            _ => allRows.OrderByDescending(x => x.Document.SoldCount).ThenByDescending(x => x.Profile.ReputationScore).ThenBy(x => x.Product.Id)
        };

        var items = sorted.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).Select(Card).ToArray();
        return new(items, query.Page, query.PageSize, total,
            new(categoryFacets, regionFacets, minFacet, maxFacet, packagingCount, stockCount), query.Sort, clock.UtcNow);
    }

    public async Task<IReadOnlyList<ProductAutocompleteResponse>> AutocompleteAsync(ProductAutocompleteQuery query, CancellationToken ct)
    {
        var products = await (from document in db.ProductSearchDocuments.AsNoTracking()
                              join product in db.Products.AsNoTracking() on document.ProductId equals product.Id
                              join shop in db.Shops.AsNoTracking() on product.ShopId equals shop.Id
                              join profile in db.SupplierProfiles.AsNoTracking() on shop.SupplierId equals profile.Id
                              join user in db.Users.AsNoTracking() on shop.OwnerUserId equals user.Id
                              where product.IsPublished && !product.IsDeleted && shop.Status == ShopStatus.Active && !shop.IsDeleted &&
                                profile.VerificationStatus == SupplierVerificationStatus.Verified && !profile.IsDeleted && user.IsActive && !user.IsDeleted &&
                                EF.Functions.ILike(document.Name, $"%{query.Q}%")
                              orderby document.SoldCount descending, document.Name
                              select new ProductAutocompleteResponse(document.ProductId, "Product", document.Name,
                                  shop.Name, $"san-pham/{document.ProductId}")).Take(query.Limit).ToArrayAsync(ct);
        if (products.Length >= query.Limit) return products;
        var shops = await (from shop in db.Shops.AsNoTracking()
                           join profile in db.SupplierProfiles.AsNoTracking() on shop.SupplierId equals profile.Id
                           join user in db.Users.AsNoTracking() on shop.OwnerUserId equals user.Id
                           where shop.Status == ShopStatus.Active && !shop.IsDeleted && profile.VerificationStatus == SupplierVerificationStatus.Verified &&
                             !profile.IsDeleted && user.IsActive && !user.IsDeleted && EF.Functions.ILike(shop.Name!, $"%{query.Q}%")
                           orderby profile.ReputationScore descending, shop.Name
                           select new ProductAutocompleteResponse(shop.Id, "Shop", shop.Name!, null, $"shop/{shop.Slug}"))
            .Take(query.Limit - products.Length).ToArrayAsync(ct);
        return products.Concat(shops).ToArray();
    }

    public async Task<IReadOnlyList<ProductSearchCardResponse>> RelatedAsync(Guid productId, int limit, Guid? userId, CancellationToken ct)
    {
        var category = await db.Products.AsNoTracking().Where(x => x.Id == productId && x.IsPublished && !x.IsDeleted)
            .Select(x => (Guid?)x.CategoryId).SingleOrDefaultAsync(ct);
        if (!category.HasValue) return [];
        var response = await SearchAsync(new ProductSearchQuery { CategoryIds = [category.Value], PageSize = Math.Min(limit + 1, 50), Sort = "bestSelling" }, ct);
        return response.Items.Where(x => x.ProductId != productId).Take(limit).ToArray();
    }

    public async Task<IReadOnlyList<ProductSearchCardResponse>> RecommendationsAsync(int limit, Guid? userId, CancellationToken ct)
    {
        if (userId.HasValue)
        {
            var categories = await (from view in db.ProductViewEvents.AsNoTracking()
                                    join product in db.Products.AsNoTracking() on view.ProductId equals product.Id
                                    where view.UserId == userId && view.ViewedAtUtc >= clock.UtcNow.AddDays(-30)
                                    group view by product.CategoryId into grouped
                                    orderby grouped.Count() descending
                                    select grouped.Key).Take(3).ToArrayAsync(ct);
            if (categories.Length > 0)
                return (await SearchAsync(new ProductSearchQuery { CategoryIds = categories, PageSize = Math.Min(limit, 50), Sort = "bestSelling" }, ct)).Items;
        }
        return (await SearchAsync(new ProductSearchQuery { PageSize = Math.Min(limit, 50), Sort = "bestSelling" }, ct)).Items;
    }

    public async Task RecordViewAsync(Guid productId, Guid? userId, Guid? sessionId, string source, DateTime nowUtc, CancellationToken ct)
    {
        var since = nowUtc.AddMinutes(-15);
        var exists = await db.ProductViewEvents.AnyAsync(x => x.ProductId == productId && x.ViewedAtUtc >= since &&
            (userId.HasValue ? x.UserId == userId : x.UserId == null && x.SessionId == sessionId), ct);
        if (exists) return;
        db.ProductViewEvents.Add(new ProductViewEvent { Id = Guid.NewGuid(), ProductId = productId, UserId = userId,
            SessionId = sessionId, Source = source, ViewedAtUtc = nowUtc, CreatedAt = nowUtc, UpdatedAt = nowUtc });
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<CategoryCatalogResponse>> CategoriesAsync(CancellationToken ct) =>
        await db.Categories.AsNoTracking().Where(x => !x.IsDeleted).OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(x => new CategoryCatalogResponse(x.Id, x.ParentId, x.Name, x.Description, x.IconUrl, x.SortOrder)).ToArrayAsync(ct);

    public Task<IReadOnlyList<RegionCatalogResponse>> RegionsAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<RegionCatalogResponse>>([
            new("VN-HN", "Hà Nội"), new("VN-HCM", "Thành phố Hồ Chí Minh"), new("VN-DN", "Đà Nẵng"),
            new("VN-HP", "Hải Phòng"), new("VN-CT", "Cần Thơ"), new("VN-BD", "Bình Dương")]);

    public async Task<int> ProjectPendingAsync(int batchSize, DateTime nowUtc, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var events = await db.ProductCatalogOutboxEvents.FromSqlInterpolated($"""
            SELECT * FROM product_catalog_outbox_events
            WHERE processed_at_utc IS NULL AND available_at_utc <= {nowUtc}
            ORDER BY created_at, id
            FOR UPDATE SKIP LOCKED
            LIMIT {batchSize}
            """).ToArrayAsync(ct);
        foreach (var item in events)
        {
            try
            {
                await ProjectAsync(item.ProductId, nowUtc, ct);
                item.ProcessedAtUtc = nowUtc; item.UpdatedAt = nowUtc; item.LastErrorCode = null;
            }
            catch (Exception failure)
            {
                item.AttemptCount++; item.LastErrorCode = failure.GetType().Name;
                item.AvailableAtUtc = nowUtc.AddSeconds(Math.Min(300, 5 * item.AttemptCount)); item.UpdatedAt = nowUtc;
            }
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return events.Length;
    }

    private async Task ProjectAsync(Guid productId, DateTime nowUtc, CancellationToken ct)
    {
        var root = await (from product in db.Products.AsNoTracking()
                          join shop in db.Shops.AsNoTracking() on product.ShopId equals shop.Id
                          join profile in db.SupplierProfiles.AsNoTracking() on shop.SupplierId equals profile.Id
                          join user in db.Users.AsNoTracking() on shop.OwnerUserId equals user.Id
                          join category in db.Categories.AsNoTracking() on product.CategoryId equals category.Id
                          where product.Id == productId
                          select new { product, shop, profile, user, category }).SingleOrDefaultAsync(ct);
        var document = await db.ProductSearchDocuments.SingleOrDefaultAsync(x => x.ProductId == productId, ct);
        var eligible = root != null && root.product.IsPublished && !root.product.IsDeleted && root.shop.Status == ShopStatus.Active &&
            !root.shop.IsDeleted && root.profile.VerificationStatus == SupplierVerificationStatus.Verified && !root.profile.IsDeleted &&
            root.user.IsActive && !root.user.IsDeleted && root.user.Role == UserRole.Supplier && !root.category.IsDeleted;
        if (!eligible)
        {
            if (document != null) db.ProductSearchDocuments.Remove(document);
            return;
        }
        var variants = await db.ProductVariants.AsNoTracking().Where(x => x.ProductId == productId && !x.IsDeleted && root!.product.BasePrice + x.PriceAdjustment > 0).ToArrayAsync(ct);
        if (variants.Length == 0)
        {
            if (document != null) db.ProductSearchDocuments.Remove(document);
            return;
        }
        var image = await (from productImage in db.ProductImages.AsNoTracking()
                           join media in db.ShopMediaAssets.AsNoTracking() on productImage.MediaAssetId equals media.Id
                           where productImage.ProductId == productId && !productImage.IsDeleted && media.Status == "Ready" && !media.IsDeleted
                           orderby productImage.IsPrimary descending, productImage.SortOrder
                           select media.PublicUrl).FirstOrDefaultAsync(ct);
        var stats = await db.ProductReviews.AsNoTracking().Where(x => x.ProductId == productId && !x.IsDeleted && x.IsPublic && !x.IsHiddenByAdmin && x.Rating.HasValue)
            .GroupBy(x => x.ProductId).Select(g => new { Count = g.Count(), Average = g.Average(x => (decimal?)x.Rating) }).SingleOrDefaultAsync(ct);
        var sold = await (from item in db.OrderItems.AsNoTracking()
                          join order in db.Orders.AsNoTracking() on item.OrderId equals order.Id
                          where item.ProductId == productId && !item.IsDeleted && !order.IsDeleted && order.Status == OrderStatus.Delivered
                          select (long)item.Quantity).SumAsync(ct);
        document ??= new ProductSearchDocument { ProductId = productId };
        if (db.Entry(document).State == EntityState.Detached) db.ProductSearchDocuments.Add(document);
        document.ShopId = root!.shop.Id; document.SupplierProfileId = root.profile.Id; document.CategoryId = root.category.Id;
        document.SourceVersion = root.product.Version; document.Name = root.product.Name; document.Description = root.product.Description;
        document.ShopName = root.shop.Name!; document.ShopSlug = root.shop.Slug; document.CategoryName = root.category.Name;
        document.SearchText = string.Join(' ', new[] { root.product.Name, root.product.Description, root.product.Ingredients, root.shop.Name, root.category.Name }
            .Concat(variants.SelectMany(x => new[] { x.Name, x.CrustType ?? x.Flavor, x.Filling, x.SizeLabel })).Where(x => !string.IsNullOrWhiteSpace(x)));
        document.PrimaryImageUrl = image; document.MinPrice = variants.Min(x => root.product.BasePrice + x.PriceAdjustment);
        document.MaxPrice = variants.Max(x => root.product.BasePrice + x.PriceAdjustment); document.MinOrderQty = root.product.MinOrderQty;
        document.Unit = root.product.Unit; document.SupportsCustomPackaging = root.product.SupportsCustomPackaging;
        document.EffectiveStatus = variants.Any(x => x.StockQty > 0) ? ProductStatus.Active : ProductStatus.OutOfStock;
        document.ReputationScore = root.profile.ReputationScore; document.AverageRating = stats?.Average;
        document.ReviewCount = stats?.Count ?? 0; document.SoldCount = sold; document.RegionCode = root.shop.RegionCode;
        document.CreatedAtUtc = root.product.CreatedAt; document.UpdatedAtUtc = root.product.UpdatedAt; document.ProjectedAtUtc = nowUtc;
    }

    private async Task LockAsync(ProductLockScopeDto scope, CancellationToken ct)
    {
        _ = await db.Users.FromSqlInterpolated($"SELECT * FROM users WHERE id = {scope.ActorUserId} FOR UPDATE")
            .AsNoTracking().Select(x => x.Id).ToArrayAsync(ct);
        _ = await db.SupplierProfiles.FromSqlInterpolated($"SELECT * FROM supplier_profiles WHERE user_id = {scope.ActorUserId} FOR UPDATE")
            .AsNoTracking().Select(x => x.Id).ToArrayAsync(ct);
        _ = await db.Shops.FromSqlInterpolated($"SELECT * FROM shops WHERE owner_user_id = {scope.ActorUserId} FOR UPDATE")
            .AsNoTracking().Select(x => x.Id).ToArrayAsync(ct);
        if (scope.ProductId.HasValue)
            _ = await db.Products.FromSqlInterpolated($"SELECT * FROM products WHERE id = {scope.ProductId.Value} FOR UPDATE")
                .AsNoTracking().Select(x => x.Id).ToArrayAsync(ct);
        if (scope.VariantId.HasValue)
            _ = await db.ProductVariants.FromSqlInterpolated($"SELECT * FROM product_variants WHERE id = {scope.VariantId.Value} FOR UPDATE")
                .AsNoTracking().Select(x => x.Id).ToArrayAsync(ct);
        foreach (var mediaId in (scope.MediaIds ?? []).Distinct().OrderBy(x => x))
            _ = await db.ShopMediaAssets.FromSqlInterpolated($"SELECT * FROM shop_media_assets WHERE id = {mediaId} FOR UPDATE")
                .AsNoTracking().Select(x => x.Id).ToArrayAsync(ct);
    }

    private static PostgresException? Postgres(Exception failure)
    {
        for (var current = failure; current != null; current = current.InnerException!)
            if (current is PostgresException postgres) return postgres;
        return null;
    }

    private static ProductSearchCardResponse Card(ProductSearchRow row) => new(row.Product.Id, row.Product.Name,
        new(row.Shop.Id, row.Shop.Slug, row.Shop.Name!, row.Profile.ReputationScore, true, row.Shop.RegionCode),
        new(row.Category.Id, row.Category.Name), row.Document.PrimaryImageUrl, row.MinPrice ?? row.Product.BasePrice,
        row.MaxPrice ?? row.Product.BasePrice, row.Product.MinOrderQty, row.Product.Unit ?? "piece",
        row.Product.SupportsCustomPackaging, row.InStock ? ProductStatus.Active : ProductStatus.OutOfStock,
        row.Document.AverageRating, row.Document.ReviewCount, row.Product.PublishedAtUtc ?? row.Product.CreatedAt);

    private sealed record ProductSearchRow(ProductSearchDocument Document, Product Product, Shop Shop,
        SupplierProfile Profile, Category Category, decimal? MinPrice, decimal? MaxPrice, bool InStock);
}
