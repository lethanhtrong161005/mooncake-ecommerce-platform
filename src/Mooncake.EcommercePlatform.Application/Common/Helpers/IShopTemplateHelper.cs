namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Application.DTOs.ShopTemplates.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Contract for ShopTemplate mapping helpers.</summary>
public interface IShopTemplateHelper
{
    ShopTemplateResponse ToResponse(ShopTemplate template);
}
