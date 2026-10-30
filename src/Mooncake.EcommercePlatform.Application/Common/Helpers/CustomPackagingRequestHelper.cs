namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using System.Text.Json;
using System.Text.RegularExpressions;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;

/// <summary>Validates custom packaging URLs and color JSON without trusting client metadata.</summary>
public static partial class CustomPackagingRequestHelper
{
    private const string UrlMessage = "Packaging assets must use an absolute HTTPS URL.";
    private const string ColorMessage = "Color scheme must be a JSON object containing valid HEX colors.";

    /// <summary>Returns a normalized HTTPS URL or throws a validation exception.</summary>
    public static string NormalizeHttpsUrl(string value)
    {
        var normalized = value.Trim();
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps || string.IsNullOrWhiteSpace(uri.Host))
            throw new HttpException(400, UrlMessage);
        return uri.AbsoluteUri;
    }

    /// <summary>Validates and normalizes a JSON object whose values are HEX colors.</summary>
    public static string? NormalizeColorScheme(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        try
        {
            using var document = JsonDocument.Parse(value);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new JsonException();
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (property.Value.ValueKind != JsonValueKind.String ||
                    !HexColorRegex().IsMatch(property.Value.GetString() ?? string.Empty))
                    throw new JsonException();
            }
            return JsonSerializer.Serialize(document.RootElement);
        }
        catch (JsonException)
        {
            throw new HttpException(400, ColorMessage);
        }
    }

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex HexColorRegex();
}
