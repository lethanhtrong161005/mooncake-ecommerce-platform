namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Persistence;
using Mooncake.EcommercePlatform.Domain.Enums;
using C = Mooncake.EcommercePlatform.Application.Common.Constant.ShopConstants;

public static class ShopPublicHelper
{
    public static async Task<ShopDisplayDto> DisplayAsync(IShopTransaction repository, string slug, CancellationToken ct)
    {
        ShopPolicyHelper.Slug(slug);
        var display = await repository.GetDisplayAsync(new(Slug: slug), ct);
        var shop = display?.Shop;
        ShopPolicyHelper.Require(shop is { IsDeleted: false, Status: ShopStatus.Active }, 404, C.Unavailable);
        var account = display!.Account;
        ShopPolicyHelper.Require(ShopPolicyHelper.CanManage(account) && account!.ProfileId == shop!.SupplierId, 404, C.Unavailable);
        return display;
    }
    public static async Task<(ShopStateDto Shop, ShopAccountStateDto Account)> GateAsync(IShopTransaction repository, string slug, CancellationToken ct)
    {
        var display = await DisplayAsync(repository, slug, ct);
        return (display.Shop, display.Account);
    }
}
