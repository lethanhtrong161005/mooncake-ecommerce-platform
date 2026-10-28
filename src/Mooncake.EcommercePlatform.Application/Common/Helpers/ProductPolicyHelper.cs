namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.DTOs.Products.Persistence;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Pure catalog normalization and invariant checks shared by product commands.</summary>
public static partial class ProductPolicyHelper
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static void Require(bool condition, int status = 400, string code = ProductConstants.Invalid)
    {
        if (!condition) throw new HttpException(status, ProductConstants.Error, code);
    }

    public static ProductAccountStateDto Supplier(ProductAccountStateDto? account, bool requireActiveShop = false)
    {
        Require(account is { IsActive: true, IsDeleted: false, Role: UserRole.Supplier,
            ProfileId: not null, ProfileDeleted: false, VerificationStatus: SupplierVerificationStatus.Verified }, 403, ProductConstants.NotEligible);
        Require(account!.ShopId.HasValue && !account.ShopDeleted, 404, ProductConstants.ShopRequired);
        if (requireActiveShop) Require(account.ShopStatus == ShopStatus.Active, 409, ProductConstants.ShopInactive);
        return account;
    }

    public static string Text(string? value, int minimum, int maximum)
    {
        var normalized = Regex.Replace((value ?? string.Empty).Normalize(NormalizationForm.FormKC).Trim(), "\\s+", " ");
        Require(normalized.Length >= minimum && normalized.Length <= maximum && !normalized.Any(char.IsControl));
        return normalized;
    }

    public static string? OptionalText(string? value, int maximum)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return Text(value, 1, maximum);
    }

    public static string Sku(string? value)
    {
        var sku = Text(value, 1, 100).ToUpperInvariant();
        Require(SkuPattern().IsMatch(sku));
        return sku;
    }

    public static string Unit(string? value)
    {
        var unit = Text(value, 1, 50).ToLowerInvariant();
        Require(unit is "piece" or "box" or "set" or "tray");
        return unit;
    }

    public static decimal Price(decimal basePrice, decimal adjustment)
    {
        Require(decimal.Round(basePrice, 2) == basePrice && decimal.Round(adjustment, 2) == adjustment, 400, ProductConstants.PriceInvalid);
        decimal price;
        try { price = checked(basePrice + adjustment); }
        catch (OverflowException) { throw new HttpException(400, ProductConstants.Error, ProductConstants.PriceInvalid); }
        Require(price > 0 && price <= 9_999_999_999.99m, 400, ProductConstants.PriceInvalid);
        return price;
    }

    public static Guid Key(string? value)
    {
        Require(Guid.TryParse(value, out var key) && key != Guid.Empty);
        return key;
    }

    public static string Hash(object value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, Json))));
    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Json);
    public static T Deserialize<T>(string value) => JsonSerializer.Deserialize<T>(value, Json) ?? throw new InvalidOperationException("Stored catalog response is invalid.");

    public static ProductStatus Effective(bool published, IReadOnlyCollection<ProductVariantStateDto> variants) =>
        !published ? ProductStatus.Inactive : variants.Any(x => !x.IsDeleted && x.StockQty > 0) ? ProductStatus.Active : ProductStatus.OutOfStock;

    public static void ValidateAggregate(ProductStateDto state, bool publishing)
    {
        Require(state.MinOrderQty > 0 && (!state.MaxOrderQty.HasValue || state.MaxOrderQty >= state.MinOrderQty));
        var variants = state.Variants.Where(x => !x.IsDeleted).ToArray();
        Require(variants.Length is >= 1 and <= ProductConstants.MaxVariants, 409, ProductConstants.VariantRequired);
        Require(variants.Select(x => x.NormalizedSku).Distinct(StringComparer.Ordinal).Count() == variants.Length, 409, ProductConstants.SkuConflict);
        foreach (var variant in variants) Price(state.BasePrice, variant.PriceAdjustment);
        var images = state.Images.Where(x => !x.IsDeleted).ToArray();
        Require(images.Length is >= 1 and <= ProductConstants.MaxImages, 409, ProductConstants.GalleryInvalid);
        Require(images.Count(x => x.IsPrimary) == 1 && images.Select(x => x.SortOrder).Distinct().Count() == images.Length, 409, ProductConstants.GalleryInvalid);
        if (publishing) Require(state.CategoryActive && images.All(x => x.MediaReady), 409, ProductConstants.PublicationIncomplete);
    }

    [GeneratedRegex("^[A-Z0-9._-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex SkuPattern();
}
