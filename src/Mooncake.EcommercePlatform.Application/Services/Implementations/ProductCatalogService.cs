namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Products.Persistence;
using Mooncake.EcommercePlatform.Application.DTOs.Products.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Products.Responses;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Enums;
using H = Mooncake.EcommercePlatform.Application.Common.Helpers.ProductPolicyHelper;
using R = Mooncake.EcommercePlatform.Application.Common.Helpers.ProductResponseHelper;

/// <summary>Catalog business rules and orchestration over DTO-only persistence boundaries.</summary>
public sealed class ProductCatalogService(IProductCatalogRepository repository, IShopService shops,
    IDateTimeProvider clock) : IProductCatalogService
{
    public async Task<ProductPageResponse<ProductOwnerCardResponse>> ListMineAsync(Guid actor, OwnerProductQuery query, CancellationToken ct)
    {
        H.Require(query.Page > 0 && query.PageSize is > 0 and <= 100);
        H.Supplier(await repository.GetAccountAsync(actor, ct));
        return await repository.ListMineAsync(actor, query, ct);
    }

    public async Task<ProductOwnerResponse> GetMineAsync(Guid actor, Guid productId, CancellationToken ct)
    {
        H.Require(productId != Guid.Empty);
        var account = H.Supplier(await repository.GetAccountAsync(actor, ct));
        var state = await repository.GetMineAsync(actor, productId, ct);
        return R.Owner(Owned(state, account));
    }

    public Task<ProductOwnerResponse> CreateAsync(Guid actor, string key, CreateProductRequest request, CancellationToken ct)
    {
        var mediaIds = request.Images.Select(x => x.MediaAssetId!.Value)
            .Concat(request.Variants.Where(x => x.ImageAssetId.HasValue).Select(x => x.ImageAssetId!.Value)).Distinct().ToArray();
        return CommandAsync(actor, key, ProductConstants.Create, null, null, mediaIds, request, async (tx, account, now, token) =>
        {
            var category = await CategoryAsync(tx, request.CategoryId!.Value, token);
            await MediaAsync(tx, actor, request.Images.Select(x => x.MediaAssetId!.Value).ToArray(), null, token, "ProductGallery");
            await MediaAsync(tx, actor, request.Variants.Where(x => x.ImageAssetId.HasValue)
                .Select(x => x.ImageAssetId!.Value).ToArray(), null, token, "VariantImage");
            var id = Guid.NewGuid();
            var variants = request.Variants.Select(x => Variant(id, account.ShopId!.Value, x, x.InitialStockQty!.Value, now)).ToArray();
            var images = await ImagesAsync(tx, id, request.Images, now, token);
            var state = new ProductStateDto(id, account.ShopId.GetValueOrDefault(), category.Id, category.Name, true, 1,
                H.Text(request.Name, 2, 255), H.Text(request.Name, 2, 255).ToUpperInvariant(),
                H.OptionalText(request.Description, 10_000), H.OptionalText(request.Ingredients, 10_000), request.BasePrice!.Value, request.MinOrderQty!.Value,
                request.MaxOrderQty, H.Unit(request.Unit), request.SupportsCustomPackaging!.Value, false,
                ProductStatus.Inactive, null, false, now, now, variants, images);
            H.ValidateAggregate(state, false);
            await tx.SaveAggregateAsync(new(state, null, actor, now), token);
            return new ProductCommandResult<ProductOwnerResponse>(R.Owner(state), id, 1,
                H.Serialize(new { state.Name, variants = variants.Length, images = images.Count }));
        }, ct);
    }

    public Task<ProductOwnerResponse> UpdateAsync(Guid actor, Guid productId, string key, UpdateProductCoreRequest request, CancellationToken ct) =>
        CommandAsync(actor, key, ProductConstants.Update, productId, null, null, request, async (tx, account, now, token) =>
        {
            var current = Owned(await tx.GetProductAsync(productId, token), account);
            Version(current, request.ExpectedVersion!.Value);
            var category = await CategoryAsync(tx, request.CategoryId!.Value, token);
            var state = current with
            {
                Version = current.Version + 1,
                Name = H.Text(request.Name, 2, 255),
                NormalizedName = H.Text(request.Name, 2, 255).ToUpperInvariant(),
                Description = H.OptionalText(request.Description, 10_000),
                Ingredients = H.OptionalText(request.Ingredients, 10_000),
                CategoryId = category.Id,
                CategoryName = category.Name,
                CategoryActive = true,
                BasePrice = request.BasePrice!.Value,
                MinOrderQty = request.MinOrderQty!.Value,
                MaxOrderQty = request.MaxOrderQty,
                Unit = H.Unit(request.Unit),
                SupportsCustomPackaging = request.SupportsCustomPackaging!.Value,
                UpdatedAtUtc = now
            };
            H.ValidateAggregate(state, state.IsPublished);
            state = state with { Status = H.Effective(state.IsPublished, state.Variants) };
            await tx.SaveAggregateAsync(new(state, current.Version, actor, now), token);
            return Result(state, ProductConstants.Update);
        }, ct);

    public Task<ProductOwnerResponse> AddVariantAsync(Guid actor, Guid productId, string key, CreateProductVariantRequest request, CancellationToken ct)
    {
        var media = request.ImageAssetId.HasValue ? new[] { request.ImageAssetId.Value } : [];
        return CommandAsync(actor, key, ProductConstants.VariantCreate, productId, null, media, request, async (tx, account, now, token) =>
        {
            var current = Owned(await tx.GetProductAsync(productId, token), account);
            Version(current, request.ExpectedVersion!.Value);
            if (media.Length > 0) await MediaAsync(tx, actor, media, null, token, "VariantImage");
            H.Require(current.Variants.Count(x => !x.IsDeleted) < ProductConstants.MaxVariants);
            var variant = Variant(productId, current.ShopId, request, request.InitialStockQty!.Value, now);
            H.Require(current.Variants.Where(x => !x.IsDeleted).All(x => x.NormalizedSku != variant.NormalizedSku), 409, ProductConstants.SkuConflict);
            var state = current with { Version = current.Version + 1, UpdatedAtUtc = now, Variants = current.Variants.Append(variant).ToArray() };
            H.ValidateAggregate(state, state.IsPublished);
            state = state with { Status = H.Effective(state.IsPublished, state.Variants) };
            await tx.SaveAggregateAsync(new(state, current.Version, actor, now), token);
            return Result(state, ProductConstants.VariantCreate);
        }, ct);
    }

    public Task<ProductOwnerResponse> UpdateVariantAsync(Guid actor, Guid productId, Guid variantId, string key,
        UpdateProductVariantRequest request, CancellationToken ct)
    {
        var media = request.ImageAssetId.HasValue ? new[] { request.ImageAssetId.Value } : [];
        return CommandAsync(actor, key, ProductConstants.VariantUpdate, productId, variantId, media, request, async (tx, account, now, token) =>
        {
            var current = Owned(await tx.GetProductAsync(productId, token), account);
            Version(current, request.ExpectedVersion!.Value);
            var old = current.Variants.SingleOrDefault(x => x.Id == variantId && !x.IsDeleted);
            H.Require(old != null, 404, ProductConstants.VariantNotFound);
            if (media.Length > 0 && old!.ImageAssetId != request.ImageAssetId)
                await MediaAsync(tx, actor, media, null, token, "VariantImage");
            var sku = H.Sku(request.Sku);
            H.Require(current.Variants.Where(x => !x.IsDeleted && x.Id != variantId).All(x => x.NormalizedSku != sku), 409, ProductConstants.SkuConflict);
            var updated = old! with
            {
                Name = H.Text(request.Name, 1, 255), Sku = sku, NormalizedSku = sku,
                CrustType = H.OptionalText(request.CrustType, 100), Filling = H.OptionalText(request.Filling, 100),
                SizeLabel = H.OptionalText(request.SizeLabel, 50), WeightGram = request.WeightGram,
                IsGiftSet = request.IsGiftSet!.Value, PriceAdjustment = request.PriceAdjustment!.Value,
                ImageAssetId = request.ImageAssetId, UpdatedAtUtc = now
            };
            var variants = current.Variants.Select(x => x.Id == variantId ? updated : x).ToArray();
            var state = current with { Version = current.Version + 1, UpdatedAtUtc = now, Variants = variants };
            H.ValidateAggregate(state, state.IsPublished);
            state = state with { Status = H.Effective(state.IsPublished, variants) };
            await tx.SaveAggregateAsync(new(state, current.Version, actor, now), token);
            return Result(state, ProductConstants.VariantUpdate);
        }, ct);
    }

    public Task<ProductOwnerResponse> DeleteVariantAsync(Guid actor, Guid productId, Guid variantId,
        long expectedVersion, string key, CancellationToken ct) =>
        CommandAsync(actor, key, ProductConstants.VariantDelete, productId, variantId, null,
            new { expectedVersion, variantId }, async (tx, account, now, token) =>
            {
                var current = Owned(await tx.GetProductAsync(productId, token), account);
                Version(current, expectedVersion);
                var target = current.Variants.SingleOrDefault(x => x.Id == variantId && !x.IsDeleted);
                H.Require(target != null, 404, ProductConstants.VariantNotFound);
                H.Require(current.Variants.Count(x => !x.IsDeleted) > 1, 409, ProductConstants.VariantRequired);
                var variants = current.Variants.Select(x => x.Id == variantId ? x with { IsDeleted = true, UpdatedAtUtc = now } : x).ToArray();
                var state = current with { Version = current.Version + 1, UpdatedAtUtc = now, Variants = variants };
                H.ValidateAggregate(state, state.IsPublished);
                state = state with { Status = H.Effective(state.IsPublished, variants) };
                await tx.SaveAggregateAsync(new(state, current.Version, actor, now), token);
                return Result(state, ProductConstants.VariantDelete);
            }, ct);

    public Task<InventoryAdjustmentResponse> AdjustInventoryAsync(Guid actor, Guid productId, Guid variantId,
        string key, AdjustVariantInventoryRequest request, CancellationToken ct)
    {
        H.Require(request.Delta is not null and not 0);
        return CommandAsync(actor, key, ProductConstants.InventoryAdjust, productId, variantId, null, request,
            async (tx, account, now, token) =>
            {
                var current = Owned(await tx.GetProductAsync(productId, token), account);
                Version(current, request.ExpectedVersion!.Value);
                H.Require(current.Variants.Any(x => x.Id == variantId && !x.IsDeleted), 404, ProductConstants.VariantNotFound);
                var result = await tx.AdjustInventoryAsync(new(productId, variantId, current.ShopId, actor,
                    current.Version, request.Delta!.Value, H.Text(request.Reason, 5, 500),
                    request.ClientReference ?? H.Key(key).ToString(), now), token);
                var response = new InventoryAdjustmentResponse(result.MovementId, productId, variantId,
                    result.PreviousQty, request.Delta.Value, result.CurrentQty, result.InventoryVersion,
                    result.ProductVersion, result.EffectiveStatus);
                return new ProductCommandResult<InventoryAdjustmentResponse>(response, productId,
                    result.ProductVersion, H.Serialize(new { variantId, request.Delta, result.CurrentQty }));
            }, ct);
    }

    public Task<ProductOwnerResponse> ReplaceGalleryAsync(Guid actor, Guid productId, string key,
        ReplaceProductGalleryRequest request, CancellationToken ct)
    {
        var mediaIds = request.Images.Select(x => x.MediaAssetId!.Value).Distinct().ToArray();
        return CommandAsync(actor, key, ProductConstants.GalleryUpdate, productId, null, mediaIds, request,
            async (tx, account, now, token) =>
            {
                var current = Owned(await tx.GetProductAsync(productId, token), account);
                Version(current, request.ExpectedVersion!.Value);
                await MediaAsync(tx, actor, mediaIds, productId, token, "ProductGallery");
                var currentByMedia = current.Images.Where(x => x.MediaAssetId.HasValue)
                    .ToDictionary(x => x.MediaAssetId!.Value);
                var mediaStates = (await tx.GetMediaAsync(mediaIds, token)).ToDictionary(x => x.Id);
                var images = request.Images.Select(input =>
                {
                    var media = mediaStates[input.MediaAssetId!.Value];
                    var existing = currentByMedia.GetValueOrDefault(media.Id);
                    return new ProductImageStateDto(existing?.Id ?? Guid.NewGuid(), productId, media.Id,
                        media.PublicUrl!, input.IsPrimary!.Value, input.SortOrder!.Value, true, false,
                        existing?.CreatedAtUtc ?? now);
                }).Concat(current.Images.Where(x => !x.MediaAssetId.HasValue || !mediaIds.Contains(x.MediaAssetId.Value))
                    .Select(x => x with { IsDeleted = true })).ToArray();
                var state = current with { Version = current.Version + 1, UpdatedAtUtc = now, Images = images };
                H.ValidateAggregate(state, state.IsPublished);
                await tx.SaveAggregateAsync(new(state, current.Version, actor, now), token);
                return Result(state, ProductConstants.GalleryUpdate);
            }, ct);
    }

    public Task<ProductOwnerResponse> SetPublicationAsync(Guid actor, Guid productId, string key,
        SetProductPublicationRequest request, CancellationToken ct) =>
        CommandAsync(actor, key, ProductConstants.Publication, productId, null, null, request,
            async (tx, account, now, token) =>
            {
                var current = Owned(await tx.GetProductAsync(productId, token), account);
                Version(current, request.ExpectedVersion!.Value);
                if (request.IsPublished!.Value) H.Require(account.ShopStatus == ShopStatus.Active, 409, ProductConstants.ShopInactive);
                var state = current with
                {
                    Version = current.Version + 1,
                    IsPublished = request.IsPublished.Value,
                    PublishedAtUtc = request.IsPublished.Value ? current.PublishedAtUtc ?? now : current.PublishedAtUtc,
                    UpdatedAtUtc = now
                };
                H.ValidateAggregate(state, state.IsPublished);
                state = state with { Status = H.Effective(state.IsPublished, state.Variants) };
                await tx.SaveAggregateAsync(new(state, current.Version, actor, now), token);
                return Result(state, ProductConstants.Publication);
            }, ct);

    public Task<DeleteProductResponse> DeleteAsync(Guid actor, Guid productId, long expectedVersion,
        string key, CancellationToken ct) =>
        CommandAsync(actor, key, ProductConstants.Delete, productId, null, null, new { expectedVersion },
            async (tx, account, now, token) =>
            {
                var current = Owned(await tx.GetProductAsync(productId, token), account);
                Version(current, expectedVersion);
                var state = current with
                {
                    Version = current.Version + 1, IsPublished = false, Status = ProductStatus.Inactive,
                    IsDeleted = true, UpdatedAtUtc = now,
                    Variants = current.Variants.Select(x => x with { IsDeleted = true, UpdatedAtUtc = now }).ToArray(),
                    Images = current.Images.Select(x => x with { IsDeleted = true }).ToArray()
                };
                await tx.SaveAggregateAsync(new(state, current.Version, actor, now), token);
                return new ProductCommandResult<DeleteProductResponse>(new(productId, state.Version, true),
                    productId, state.Version, H.Serialize(new { deleted = true }));
            }, ct);

    public Task<ShopMediaResponse> UploadMediaAsync(Guid actor, string key, string purpose, Stream content, CancellationToken ct)
    {
        H.Require(purpose is "ProductGallery" or "VariantImage");
        return shops.UploadAsync(actor, false, key, purpose, content, ct);
    }

    public async Task<ProductPublicDetailResponse> PublicAsync(Guid productId, CancellationToken ct) =>
        await repository.GetPublicAsync(productId, ct) ?? throw new HttpException(404, ProductConstants.Error, ProductConstants.NotFound);

    public Task<ProductSearchResponse> SearchAsync(ProductSearchQuery query, CancellationToken ct)
    {
        H.Require(query.Page > 0 && query.PageSize is > 0 and <= 50 && query.CategoryIds.Distinct().Count() <= 20 &&
            query.RegionCodes.Distinct(StringComparer.OrdinalIgnoreCase).Count() <= 20 &&
            (!query.MinPrice.HasValue || !query.MaxPrice.HasValue || query.MinPrice <= query.MaxPrice));
        return repository.SearchAsync(query with
        {
            Q = H.OptionalText(query.Q, 100),
            RegionCodes = query.RegionCodes.Select(x => H.Text(x, 2, 20).ToUpperInvariant()).Distinct().ToArray()
        }, ct);
    }

    public Task<IReadOnlyList<ProductAutocompleteResponse>> AutocompleteAsync(ProductAutocompleteQuery query, CancellationToken ct) =>
        repository.AutocompleteAsync(query with { Q = H.Text(query.Q, 2, 100) }, ct);

    public Task<IReadOnlyList<ProductSearchCardResponse>> RelatedAsync(Guid productId, int limit, Guid? userId, CancellationToken ct)
    {
        H.Require(productId != Guid.Empty && limit is >= 1 and <= 20);
        return repository.RelatedAsync(productId, limit, userId, ct);
    }

    public Task<IReadOnlyList<ProductSearchCardResponse>> RecommendationsAsync(int limit, Guid? userId, CancellationToken ct)
    {
        H.Require(limit is >= 1 and <= 50);
        return repository.RecommendationsAsync(limit, userId, ct);
    }

    public async Task RecordViewAsync(Guid productId, Guid? userId, RecordProductViewRequest request, CancellationToken ct)
    {
        H.Require(productId != Guid.Empty && (userId.HasValue || request.SessionId.HasValue));
        _ = await repository.GetPublicAsync(productId, ct) ?? throw new HttpException(404, ProductConstants.Error, ProductConstants.NotFound);
        await repository.RecordViewAsync(productId, userId, request.SessionId, request.Source, clock.UtcNow, ct);
    }

    public Task<IReadOnlyList<CategoryCatalogResponse>> CategoriesAsync(CancellationToken ct) => repository.CategoriesAsync(ct);
    public Task<IReadOnlyList<RegionCatalogResponse>> RegionsAsync(CancellationToken ct) => repository.RegionsAsync(ct);

    private async Task<T> CommandAsync<T>(Guid actor, string key, string operation, Guid? productId, Guid? variantId,
        IReadOnlyList<Guid>? mediaIds, object payload,
        Func<IProductCatalogTransaction, ProductAccountStateDto, DateTime, CancellationToken, Task<ProductCommandResult<T>>> execute,
        CancellationToken ct) where T : notnull
    {
        var parsedKey = H.Key(key);
        var hash = H.Hash(payload);
        var now = clock.UtcNow;
        return await repository.InTransactionAsync(new(actor, productId, variantId, mediaIds), async (tx, token) =>
        {
            var account = H.Supplier(await tx.GetAccountAsync(actor, token));
            var resource = productId ?? account.ProfileId!.Value;
            var scope = new ProductReplayQueryDto(actor, operation, resource, parsedKey);
            var replay = await tx.GetReplayAsync(scope, token);
            if (replay is { ExpiresAtUtc: var expires } && expires > now)
            {
                H.Require(replay.RequestHash == hash, 409, ProductConstants.KeyConflict);
                return R.Replay(H.Deserialize<T>(replay.ResponseJson));
            }
            var result = await execute(tx, account, now, token);
            await tx.AppendAuditAsync(new(actor, result.ProductId, operation, result.AuditJson, now), token);
            await tx.AppendOutboxAsync(new(result.ProductId, result.SourceVersion, operation, now), token);
            await tx.StoreReplayAsync(new(scope, hash, H.Serialize(result.Response), now.AddHours(24), now), token);
            return result.Response;
        }, ct);
    }

    private static ProductStateDto Owned(ProductStateDto? state, ProductAccountStateDto account)
    {
        H.Require(state is { IsDeleted: false } && state.ShopId == account.ShopId, 404, ProductConstants.NotFound);
        return state!;
    }

    private static void Version(ProductStateDto state, long expected) =>
        H.Require(state.Version == expected, 409, ProductConstants.VersionConflict);

    private static async Task<ProductCategoryStateDto> CategoryAsync(IProductCatalogTransaction tx, Guid id, CancellationToken ct)
    {
        var category = await tx.GetCategoryAsync(id, ct);
        H.Require(category is { IsDeleted: false }, 400, ProductConstants.Invalid);
        return category!;
    }

    private static ProductVariantStateDto Variant(Guid productId, Guid shopId, ProductVariantInput request,
        int initialStockQty, DateTime now)
    {
        var sku = H.Sku(request.Sku);
        return new(Guid.NewGuid(), productId, shopId, H.Text(request.Name, 1, 255), sku, sku,
            H.OptionalText(request.CrustType, 100), H.OptionalText(request.Filling, 100),
            H.OptionalText(request.SizeLabel, 50), request.WeightGram, request.IsGiftSet!.Value,
            request.PriceAdjustment!.Value, initialStockQty, 1, request.ImageAssetId, null,
            false, now, now);
    }

    private static async Task<IReadOnlyList<ProductImageStateDto>> ImagesAsync(IProductCatalogTransaction tx,
        Guid productId, IReadOnlyList<ProductImageInput> inputs, DateTime now, CancellationToken ct)
    {
        var media = (await tx.GetMediaAsync(inputs.Select(x => x.MediaAssetId!.Value).ToArray(), ct)).ToDictionary(x => x.Id);
        return inputs.Select(x => new ProductImageStateDto(Guid.NewGuid(), productId, x.MediaAssetId!.Value,
            media[x.MediaAssetId.Value].PublicUrl!, x.IsPrimary!.Value, x.SortOrder!.Value, true, false, now)).ToArray();
    }

    private static async Task MediaAsync(IProductCatalogTransaction tx, Guid actor, IReadOnlyCollection<Guid> ids,
        Guid? allowProductId, CancellationToken ct, string? requiredPurpose = null)
    {
        H.Require(ids.Count == ids.Distinct().Count() && ids.All(x => x != Guid.Empty), 400, ProductConstants.GalleryInvalid);
        if (ids.Count == 0) return;
        var states = await tx.GetMediaAsync(ids, ct);
        H.Require(states.Count == ids.Count, 409, ProductConstants.MediaNotReady);
        foreach (var media in states)
        {
            H.Require(media.OwnerUserId == actor && !media.IsDeleted && media.Status == "Ready" && media.PublicUrl != null,
                409, ProductConstants.MediaNotReady);
            H.Require(requiredPurpose == null ? media.Purpose is "ProductGallery" or "VariantImage" : media.Purpose == requiredPurpose,
                409, ProductConstants.MediaNotReady);
            H.Require(!await tx.IsMediaBoundAsync(media.Id, allowProductId, ct), 409, ProductConstants.MediaConflict);
        }
    }

    private static ProductCommandResult<ProductOwnerResponse> Result(ProductStateDto state, string operation) =>
        new(R.Owner(state), state.Id, state.Version, H.Serialize(new { operation, state.Version }));

    private sealed record ProductCommandResult<T>(T Response, Guid ProductId, long SourceVersion, string AuditJson);
}
