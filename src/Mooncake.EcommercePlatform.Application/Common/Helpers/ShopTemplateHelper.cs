namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Application.DTOs.ShopTemplates.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Maps ShopTemplate entities to DTOs.</summary>
public class ShopTemplateHelper : IShopTemplateHelper
{
    public ShopTemplateResponse ToResponse(ShopTemplate template) =>
        new(
            template.Id,
            template.Name,
            template.Description,
            template.PreviewUrl,
            template.Config,
            template.IsActive,
            template.CreatedAtUtc,
            template.UpdatedAtUtc
        );
}
