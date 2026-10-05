namespace Mooncake.EcommercePlatform.Application.DTOs.Catalog.Responses;

using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Public shop details.</summary>
public sealed record ShopResponse(Guid Id, Guid SupplierId, string? Name, string Slug, string? Description, string? BannerUrl, string? LogoUrl, ShopStatus Status);
