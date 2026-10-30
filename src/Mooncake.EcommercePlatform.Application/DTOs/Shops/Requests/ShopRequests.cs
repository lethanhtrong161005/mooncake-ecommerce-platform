namespace Mooncake.EcommercePlatform.Application.DTOs.Shops.Requests;

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Editable public information. Ownership and authority are never accepted from the caller.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record ShopInformationRequest
{
    [Required, StringLength(255, MinimumLength = 2)] public string Name { get; init; } = string.Empty;
    [MaxLength(2000)] public string? Description { get; init; }
    public Guid? LogoAssetId { get; init; }
    public Guid? BannerAssetId { get; init; }
    [EmailAddress, MaxLength(255)] public string? PublicContactEmail { get; init; }
    [RegularExpression("^\\+[1-9][0-9]{7,14}$"), MaxLength(20)] public string? PublicContactPhone { get; init; }
    [MaxLength(500)] public string? PublicAddress { get; init; }
    [RegularExpression("^[A-Z0-9-]{2,20}$"), MaxLength(20)] public string? RegionCode { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateShopRequest : ShopInformationRequest
{
    [Required, StringLength(80, MinimumLength = 3), RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    public string Slug { get; init; } = string.Empty;
    [Required] public Guid? TemplateId { get; init; }
    [Required] public Guid? TemplateRevisionId { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record UpdateShopRequest : ShopInformationRequest
{
    [Required, Range(1, long.MaxValue)] public long? ExpectedVersion { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SetMyShopVisibilityRequest
{
    [Required, Range(1, long.MaxValue)] public long? ExpectedVersion { get; init; }
    [Required, EnumDataType(typeof(ShopStatus))] public ShopStatus? Status { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record AdminShopModerationRequest
{
    [Required, Range(1, long.MaxValue)] public long? ExpectedVersion { get; init; }
    [Required, RegularExpression("^(Suspend|Restore)$")] public string Action { get; init; } = string.Empty;
    [Required, StringLength(1000, MinimumLength = 10)] public string Reason { get; init; } = string.Empty;
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record ThemeOverridesRequest
{
    [RegularExpression("^#[0-9a-fA-F]{6}$")] public string? PrimaryColor { get; init; }
    [RegularExpression("^#[0-9a-fA-F]{6}$")] public string? BackgroundColor { get; init; }
    [RegularExpression("^#[0-9a-fA-F]{6}$")] public string? TextColor { get; init; }
    [RegularExpression("^#[0-9a-fA-F]{6}$")] public string? SurfaceColor { get; init; }
    [RegularExpression("^#[0-9a-fA-F]{6}$")] public string? ButtonTextColor { get; init; }
    [Range(0, 24)] public int? BorderRadiusPx { get; init; }
    [Range(0.85, 1.25)] public decimal? FontScale { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record TemplateLayoutRequest
{
    [Required, MinLength(2), MaxLength(5)] public List<string> SectionOrder { get; init; } = [];
    [Required, Range(2, 4)] public int? ProductColumnsDesktop { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record UpdateShopAppearanceRequest
{
    [Required, Range(1, long.MaxValue)] public long? ExpectedVersion { get; init; }
    [Required] public Guid? TemplateId { get; init; }
    [Required] public Guid? TemplateRevisionId { get; init; }
    [Required] public ThemeOverridesRequest Overrides { get; init; } = null!;
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record TemplateDefinitionRequest
{
    [Required, StringLength(100, MinimumLength = 2)] public string Name { get; init; } = string.Empty;
    [MaxLength(1000)] public string? Description { get; init; }
    public Guid? PreviewAssetId { get; init; }
    [Required, Range(0, 10000)] public int? SortOrder { get; init; }
    [Required, RegularExpression("^(heritage|royal-gift|minimal-grid|artisan-story|modern-catalog)$")]
    public string RendererKey { get; init; } = string.Empty;
    [Required, Range(1, 1)] public int? SchemaVersion { get; init; }
    [Required] public ThemeOverridesRequest Defaults { get; init; } = new();
    [Required] public TemplateLayoutRequest Layout { get; init; } = new();
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateShopTemplateRequest : TemplateDefinitionRequest
{
    [Required, StringLength(50, MinimumLength = 3), RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    public string Code { get; init; } = string.Empty;
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record PublishTemplateRevisionRequest : TemplateDefinitionRequest
{
    [Required, Range(1, long.MaxValue)] public long? ExpectedVersion { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SetTemplateAvailabilityRequest
{
    [Required, Range(1, long.MaxValue)] public long? ExpectedVersion { get; init; }
    [Required] public bool? IsActive { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SetTemplatePremiumRequest
{
    [Required, Range(1, long.MaxValue)] public long? ExpectedVersion { get; init; }
    [Required] public bool? IsPremium { get; init; }
}

/// <summary>Bounded queries are validated even when all values originate in the URL.</summary>
public record ShopPageQuery
{
    [Range(1, 1000)] public int Page { get; init; } = 1;
    [Range(1, 100)] public int PageSize { get; init; } = 20;
    [MaxLength(100)] public string? Q { get; init; }
    [MaxLength(2048)] public string? Cursor { get; init; }
}

public sealed record ShopProductQuery : ShopPageQuery
{
    public Guid? CategoryId { get; init; }
    [Range(0, 999999999999.99)] public decimal? MinPrice { get; init; }
    [Range(0, 999999999999.99)] public decimal? MaxPrice { get; init; }
    [MaxLength(100)] public string? Flavor { get; init; }
    [MaxLength(100)] public string? Filling { get; init; }
    [Range(1, 10000)] public int? WeightGram { get; init; }
    public bool InStockOnly { get; init; }
    [RegularExpression("^(newest|priceAsc|priceDesc)$")] public string Sort { get; init; } = "newest";
}

public sealed record ShopReviewQuery : ShopPageQuery
{
    [Range(1, 5)] public short? Rating { get; init; }
}

public sealed record AdminShopQuery : ShopPageQuery
{
    [EnumDataType(typeof(ShopStatus))] public ShopStatus? Status { get; init; }
    public Guid? OwnerProfileId { get; init; }
    [RegularExpression("^(createdAtDesc|nameAsc)$")] public string Sort { get; init; } = "createdAtDesc";
}

public sealed record AdminTemplateQuery : ShopPageQuery
{
    public bool? IsActive { get; init; }
    public bool? IsPremium { get; init; }
    [RegularExpression("^(sortOrderAsc|updatedAtDesc)$")] public string Sort { get; init; } = "sortOrderAsc";
}
