namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Persistence;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Responses;
using C = Mooncake.EcommercePlatform.Application.Common.Constant.ShopConstants;
using H = Mooncake.EcommercePlatform.Application.Common.Helpers.ShopPolicyHelper;
using Cmd = Mooncake.EcommercePlatform.Application.Common.Helpers.ShopCommandHelper;
using R = Mooncake.EcommercePlatform.Application.Common.Helpers.ShopResponseHelper;

public sealed partial class ShopService
{
    public Task<PublicShopResponse> PublicAsync(string slug, CancellationToken ct) => repository.InReadSnapshotAsync(async (tx, token) => {
        var display = await ShopPublicHelper.DisplayAsync(tx, slug, token);
        var shop = display.Shop; var account = display.Account;
        var owner = R.Owner(shop, display.Template, display.PinnedRevision);
        var reviews = await tx.ReviewStatsAsync(new(shop.SupplierId), token);
        return new PublicShopResponse(shop.Id, shop.Name, shop.Slug, shop.Description, shop.LogoUrl, shop.BannerUrl,
            shop.PublicContactEmail, shop.PublicContactPhone, shop.PublicAddress, shop.RegionCode, true, account.ReputationScore,
            reviews.Count, reviews.Average, owner.Appearance);
    }, ct);

    public Task<OwnerShopPreviewResponse> PreviewAsync(Guid actor, CancellationToken ct) => repository.InReadSnapshotAsync(async (tx, token) => {
        H.Supplier(await tx.GetAccountAsync(new(actor), token));
        var display = await tx.GetDisplayAsync(new(OwnerUserId: actor), token);
        H.Require(display is { Shop.IsDeleted: false }, 404, C.NotFound);
        var shop = display!.Shop;
        return new OwnerShopPreviewResponse(R.Owner(shop, display.Template, display.PinnedRevision),
            (await tx.ListProductsAsync(new(shop.Id, new() { PageSize = 12 }, null), token)).Take(12).ToArray());
    }, ct);

    public Task<ShopCursorPageResponse<ShopProductCardResponse>> ProductsAsync(string slug, ShopProductQuery query, CancellationToken ct)
    {
        query = H.ProductQuery(query);
        return repository.InReadSnapshotAsync(async (tx, token) => {
            var (shop, _) = await ShopPublicHelper.GateAsync(tx, slug, token);
            var scope = new Mooncake.EcommercePlatform.Application.Common.Interfaces.ShopCursorScopeDto("products", shop.Id, H.Hash(query with { Cursor = null, Page = 1 }));
            var after = query.Cursor == null ? null : cursors.Unprotect(scope, query.Cursor);
            var rows = await tx.ListProductsAsync(new(shop.Id, query, after), token);
            return R.Page(rows, query.PageSize, item => cursors.Protect(scope, new(item.Id, item.CreatedAtUtc, item.MinPrice)));
        }, ct);
    }

    public Task<ShopCursorPageResponse<ShopCategoryResponse>> CategoriesAsync(string slug, ShopPageQuery query, CancellationToken ct)
    {
        H.Page(query);
        return repository.InReadSnapshotAsync(async (tx, token) => {
            var (shop, _) = await ShopPublicHelper.GateAsync(tx, slug, token);
            var scope = new Mooncake.EcommercePlatform.Application.Common.Interfaces.ShopCursorScopeDto("categories", shop.Id, H.Hash(query with { Cursor = null, Page = 1 }));
            var rows = await tx.ListCategoriesAsync(new(shop.Id, shop.SupplierId, query,
                query.Cursor == null ? null : cursors.Unprotect(scope, query.Cursor)), token);
            return R.Page(rows, query.PageSize, item => cursors.Protect(scope, new(item.Id, Name: item.Name)));
        }, ct);
    }

    public Task<ShopCursorPageResponse<ShopReviewResponse>> ReviewsAsync(string slug, ShopReviewQuery query, CancellationToken ct)
    {
        H.Page(query, 50); H.Require(query.Rating is null or >= 1 and <= 5);
        return repository.InReadSnapshotAsync(async (tx, token) => {
            var (shop, _) = await ShopPublicHelper.GateAsync(tx, slug, token);
            var scope = new Mooncake.EcommercePlatform.Application.Common.Interfaces.ShopCursorScopeDto("reviews", shop.Id, H.Hash(query with { Cursor = null, Page = 1 }));
            var rows = await tx.ListReviewsAsync(new(shop.Id, shop.SupplierId, query,
                query.Cursor == null ? null : cursors.Unprotect(scope, query.Cursor), query.Rating), token);
            return R.Page(rows, query.PageSize, item => cursors.Protect(scope, new(item.Id, item.CreatedAtUtc)));
        }, ct);
    }

    public async Task<ShopCursorPageResponse<ShopTemplateResponse>> CatalogAsync(Guid actor, ShopPageQuery query, CancellationToken ct)
    {
        H.Supplier(await repository.GetAccountAsync(new(actor), ct)); H.Page(query, 50);
        var scope = new Mooncake.EcommercePlatform.Application.Common.Interfaces.ShopCursorScopeDto("templates", Guid.Empty, H.Hash(query with { Cursor = null, Page = 1 }));
        var result = await repository.ListTemplatesAsync(new(new() { Q = query.Q, PageSize = query.PageSize }, true,
            query.Cursor == null ? null : cursors.Unprotect(scope, query.Cursor)), ct);
        return R.Page(result.Items.Select(R.Template).ToArray(), query.PageSize,
            item => cursors.Protect(scope, new(item.Id, Name: item.Code, SortOrder: item.SortOrder)));
    }

    public async Task<ShopPageResponse<ShopTemplateResponse>> TemplatesAdminAsync(Guid actor, AdminTemplateQuery query, CancellationToken ct)
    {
        H.Admin(await repository.GetAccountAsync(new(actor), ct)); H.Page(query);
        H.Require(query.Sort is "sortOrderAsc" or "updatedAtDesc");
        var result = await repository.ListTemplatesAsync(new(query, false, null), ct);
        return new(result.Items.Select(R.Template).ToArray(), result.Page, result.PageSize, result.TotalCount);
    }

    public async Task<ShopTemplateResponse> TemplateDetailAsync(Guid actor, Guid templateId, CancellationToken ct)
    {
        H.Admin(await repository.GetAccountAsync(new(actor), ct));
        var template = await repository.GetTemplateAsync(new(H.Id(templateId)), ct);
        H.Require(template is { IsDeleted: false }, 404, C.TemplateUnavailable);
        return R.Template(template!);
    }
}
