namespace Mooncake.EcommercePlatform.Application.DTOs.Shops.Requests;

/// <summary>Request to apply or customize template layout for a shop.</summary>
public record ApplyShopTemplateRequest(
    long? TemplateId,
    string TemplateOverrides
);
