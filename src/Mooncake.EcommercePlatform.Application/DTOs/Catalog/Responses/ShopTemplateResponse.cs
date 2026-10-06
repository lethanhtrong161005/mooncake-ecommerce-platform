namespace Mooncake.EcommercePlatform.Application.DTOs.Catalog.Responses;

/// <summary>Selectable shop template metadata.</summary>
public sealed record ShopTemplateResponse(Guid Id, string? Name, string? Description, string? PreviewImageUrl, string? CssVariables, string? LayoutConfig, bool IsPremium);
