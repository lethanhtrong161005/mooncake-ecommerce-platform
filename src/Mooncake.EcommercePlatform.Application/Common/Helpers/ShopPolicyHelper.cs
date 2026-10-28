namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Persistence;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Requests;
using Mooncake.EcommercePlatform.Domain.Enums;
using C = Mooncake.EcommercePlatform.Application.Common.Constant.ShopConstants;

/// <summary>Pure normalization and business predicates for shop commands.</summary>
public static class ShopPolicyHelper
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };
    public static readonly string[] Renderers = ["heritage", "royal-gift", "minimal-grid", "artisan-story", "modern-catalog"];
    public static readonly string[] Sections = ["Hero", "Story", "Products", "Reviews", "Contact"];
    private static readonly HashSet<string> Reserved = ["me", "admin", "api", "shop", "shops", "templates", "products", "reviews", "categories", "support", "auth"];
    public static void Require(bool condition, int status = 400, string code = C.Invalid)
    {
        if (!condition) throw new HttpException(status, C.Error, code);
    }
    public static bool CanManage(ShopAccountStateDto? user) => user is {
        IsActive: true, IsDeleted: false, Role: UserRole.Supplier,
        ProfileDeleted: false, ProfileId: not null, VerificationStatus: SupplierVerificationStatus.Verified };
    public static ShopAccountStateDto Supplier(ShopAccountStateDto? user)
    {
        Require(CanManage(user), 403, C.NotVerified);
        return user!;
    }
    public static void Admin(ShopAccountStateDto? user) => Require(user is { IsActive: true, IsDeleted: false, Role: UserRole.Admin }, 403, C.Forbidden);
    public static Guid Key(string value)
    {
        Require(Guid.TryParse(value, out var id) && id != Guid.Empty);
        return id;
    }
    public static Guid Id(Guid? value)
    {
        Require(value.HasValue && value.Value != Guid.Empty);
        return value!.Value;
    }
    public static string Slug(string? value, bool templateCode = false)
    {
        Require(value is not null && value.Length >= 3 && value.Length <= (templateCode ? 50 : 80) &&
            Regex.IsMatch(value, "^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1)) &&
            (templateCode || !Reserved.Contains(value)));
        return value!;
    }
    public static string? Text(string? value, int max, bool multiline = false)
    {
        var normalized = value?.Trim().Normalize(NormalizationForm.FormKC);
        if (string.IsNullOrEmpty(normalized)) return null;
        Require(normalized.Length <= max && !normalized.Any(c => char.IsControl(c) && (!multiline || c is not ('\n' or '\r' or '\t'))));
        return normalized.Replace("\r\n", "\n").Replace("\r", "\n");
    }
    public static ShopInformationRequest Information(ShopInformationRequest request)
    {
        var name = Text(request.Name, 255);
        Require(name?.Length >= 2);
        if (request.LogoAssetId.HasValue) Id(request.LogoAssetId);
        if (request.BannerAssetId.HasValue) Id(request.BannerAssetId);
        return request with { Name = name!, Description = Text(request.Description, 2000, true),
            PublicAddress = Text(request.PublicAddress, 500), PublicContactEmail = Text(request.PublicContactEmail, 255),
            PublicContactPhone = Text(request.PublicContactPhone, 20), RegionCode = Text(request.RegionCode, 20)?.ToUpperInvariant() };
    }
    public static string Reason(string? value)
    {
        var reason = Text(value, 1000, true);
        Require(reason?.Length >= 10);
        return reason!;
    }
    public static void Version(long current, long? expected, string code = C.Conflict) => Require(expected.HasValue && current == expected, 409, code);
    public static void Editable(ShopStateDto shop) => Require(shop.Status != ShopStatus.Suspended, 409, C.Suspended);
    public static string Hash(object data) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data, Json))));
    public static T Parse<T>(string json) => JsonSerializer.Deserialize<T>(json, Json)!;
    public static string Serialize(object value) => JsonSerializer.Serialize(value, Json);

    public static ThemeOverridesRequest Tokens(ThemeOverridesRequest input)
    {
        Require(input != null, 400, C.ThemeInvalid);
        string? Color(string? color) {
            Require(color is null || Regex.IsMatch(color, "^#[0-9a-fA-F]{6}$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1)), 400, C.ThemeInvalid);
            return color?.ToUpperInvariant();
        }
        Require(input!.BorderRadiusPx is null or >= 0 and <= 24 && input.FontScale is null or >= 0.85m and <= 1.25m, 400, C.ThemeInvalid);
        Require(!input.FontScale.HasValue || decimal.Round(input.FontScale.Value, 2) == input.FontScale, 400, C.ThemeInvalid);
        return input with { PrimaryColor = Color(input.PrimaryColor), BackgroundColor = Color(input.BackgroundColor),
            TextColor = Color(input.TextColor), SurfaceColor = Color(input.SurfaceColor), ButtonTextColor = Color(input.ButtonTextColor),
            FontScale = input.FontScale.HasValue ? decimal.Parse(input.FontScale.Value.ToString("0.##", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture) : null };
    }
    public static ThemeOverridesRequest Merge(ThemeOverridesRequest defaults, ThemeOverridesRequest overrides)
    {
        defaults = Tokens(defaults); overrides = Tokens(overrides);
        var effective = new ThemeOverridesRequest {
            PrimaryColor = overrides.PrimaryColor ?? defaults.PrimaryColor,
            BackgroundColor = overrides.BackgroundColor ?? defaults.BackgroundColor,
            TextColor = overrides.TextColor ?? defaults.TextColor, SurfaceColor = overrides.SurfaceColor ?? defaults.SurfaceColor,
            ButtonTextColor = overrides.ButtonTextColor ?? defaults.ButtonTextColor,
            BorderRadiusPx = overrides.BorderRadiusPx ?? defaults.BorderRadiusPx, FontScale = overrides.FontScale ?? defaults.FontScale };
        Require(effective.PrimaryColor != null && effective.BackgroundColor != null && effective.TextColor != null &&
            effective.SurfaceColor != null && effective.ButtonTextColor != null && effective.BorderRadiusPx.HasValue && effective.FontScale.HasValue, 400, C.ThemeInvalid);
        Require(Contrast(effective.TextColor!, effective.BackgroundColor!) >= 4.5 &&
            Contrast(effective.TextColor!, effective.SurfaceColor!) >= 4.5 &&
            Contrast(effective.ButtonTextColor!, effective.PrimaryColor!) >= 4.5, 400, C.ContrastInvalid);
        return effective;
    }
    public static double Contrast(string foreground, string background)
    {
        static double Luminance(string hex) {
            var channels = new[] { 1, 3, 5 }.Select(i => int.Parse(hex.Substring(i, 2), NumberStyles.HexNumber) / 255d)
                .Select(c => c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4)).ToArray();
            return channels[0] * 0.2126 + channels[1] * 0.7152 + channels[2] * 0.0722;
        }
        var a = Luminance(foreground); var b = Luminance(background);
        return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
    }
    public static TemplateDefinitionRequest Definition(TemplateDefinitionRequest request)
    {
        Require(Renderers.Contains(request.RendererKey) && request.SchemaVersion == 1 && request.SortOrder is >= 0 and <= 10000);
        var name = Text(request.Name, 100); Require(name?.Length >= 2);
        var defaults = Tokens(request.Defaults); Merge(defaults, new());
        Require(request.Layout is { SectionOrder: not null } && request.Layout.SectionOrder.Count is >= 2 and <= 5 &&
            request.Layout.ProductColumnsDesktop is >= 2 and <= 4 && request.Layout.SectionOrder.All(Sections.Contains) &&
            request.Layout.SectionOrder.Distinct().Count() == request.Layout.SectionOrder.Count &&
            request.Layout.SectionOrder.Contains("Products") && request.Layout.SectionOrder.Contains("Contact"));
        if (request.PreviewAssetId.HasValue) Id(request.PreviewAssetId);
        return request with { Name = name!, Description = Text(request.Description, 1000, true), Defaults = defaults };
    }
    public static void Page(ShopPageQuery query, int maxSize = 100)
    {
        Require(query.Page is >= 1 and <= 1000 && query.PageSize >= 1 && query.PageSize <= maxSize &&
            query.Q?.Length is null or <= 100 && query.Cursor?.Length is null or <= 2048);
    }
    public static ShopProductQuery ProductQuery(ShopProductQuery query)
    {
        Page(query, 50);
        Require(query.Sort is "newest" or "priceAsc" or "priceDesc" && query.CategoryId != Guid.Empty &&
            query.MinPrice is null or >= 0 and <= 999999999999.99m && query.MaxPrice is null or >= 0 and <= 999999999999.99m &&
            (!query.MinPrice.HasValue || !query.MaxPrice.HasValue || query.MinPrice <= query.MaxPrice) &&
            query.WeightGram is null or >= 1 and <= 10000);
        Require((!query.MinPrice.HasValue || decimal.Round(query.MinPrice.Value, 2) == query.MinPrice) &&
            (!query.MaxPrice.HasValue || decimal.Round(query.MaxPrice.Value, 2) == query.MaxPrice));
        return query with { Q = Text(query.Q, 100), Flavor = Text(query.Flavor, 100), Filling = Text(query.Filling, 100) };
    }
}
