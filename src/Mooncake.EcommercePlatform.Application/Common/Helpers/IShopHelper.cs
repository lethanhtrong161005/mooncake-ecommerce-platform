namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Application.DTOs.Shops.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Contract for Shop mapping helpers.</summary>
public interface IShopHelper
{
    ShopResponse ToResponse(Shop shop);
    ShopDetailResponse ToDetailResponse(Shop shop);
}
