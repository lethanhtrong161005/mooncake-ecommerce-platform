namespace Mooncake.EcommercePlatform.Infrastructure.Helpers;

using System.Net;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Mooncake.EcommercePlatform.Application.DTOs.Files.Responses;
using Mooncake.EcommercePlatform.Infrastructure.Options;

/// <summary>Pure upload parameter mapping, input bounds and response validation.</summary>
internal static class CloudinaryUploadHelper
{
    internal const string InvalidInputMessage = "The upload requires a readable, non-empty stream and a valid file name.";
    internal const string UnsupportedExtensionMessage = "The file extension is not supported.";
    internal const string SizeLimitMessage = "The uploaded content exceeds the allowed size.";
    internal const string UploadFailedMessage = "Cloudinary upload failed.";
    internal const string InvalidResultMessage = "Cloudinary returned an invalid upload result.";
    internal const string TimeoutMessage = "Cloudinary upload timed out.";
    internal const string PublicDeliveryType = "upload";
    internal const string PrivateDeliveryType = "authenticated";
    internal const string ImageResourceType = "image";
    internal const string FileResourceType = "raw";

    internal static string NormalizeFileName(Stream content, string fileName, string[] allowedExtensions)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (!content.CanRead || string.IsNullOrWhiteSpace(fileName) || fileName.Any(char.IsControl))
            throw new ArgumentException(InvalidInputMessage);
        var normalized = Path.GetFileName(fileName.Replace('\\', '/').Trim());
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > 255)
            throw new ArgumentException(InvalidInputMessage, nameof(fileName));
        if (!allowedExtensions.Contains(Path.GetExtension(normalized), StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException(UnsupportedExtensionMessage, nameof(fileName));
        return normalized;
    }

    internal static async Task<MemoryStream> ReadBoundedAsync(Stream content, long limit, CancellationToken cancellationToken)
    {
        var buffered = new MemoryStream();
        try
        {
            var chunk = new byte[81920];
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var remaining = (int)Math.Min(chunk.Length, limit - buffered.Length + 1);
                var count = await content.ReadAsync(chunk.AsMemory(0, remaining), cancellationToken);
                if (count == 0) break;
                if (buffered.Length + count > limit) throw new ArgumentException(SizeLimitMessage);
                await buffered.WriteAsync(chunk.AsMemory(0, count), cancellationToken);
            }
            if (buffered.Length == 0) throw new ArgumentException(InvalidInputMessage);
            buffered.Position = 0;
            return buffered;
        }
        catch { buffered.Dispose(); throw; }
    }

    internal static void ConfigureParameters(RawUploadParams parameters, Stream content, string fileName,
        string folder, string deliveryType, bool image, CloudinaryOptions options)
    {
        var basename = Guid.NewGuid().ToString("N") + (image ? string.Empty : Path.GetExtension(fileName).ToLowerInvariant());
        parameters.File = new FileDescription(fileName, content);
        parameters.Type = deliveryType;
        parameters.Overwrite = false;
        parameters.UseFilename = false;
        if (options.FolderMode == "Dynamic")
        {
            parameters.AssetFolder = folder;
            parameters.PublicId = $"{folder}/{basename}";
            parameters.UseAssetFolderAsPublicIdPrefix = false;
        }
        else
        {
            parameters.Folder = folder;
            parameters.PublicId = basename;
        }
    }

    internal static CloudinaryUploadResponse ToResponse(RawUploadResult? result, string resourceType, string deliveryType,
        long expectedBytes, string expectedPublicId, string cloudName)
    {
        if (result is null || result.Error is not null || result.StatusCode < HttpStatusCode.OK || (int)result.StatusCode >= 300)
            throw new InvalidOperationException(UploadFailedMessage);
        if (!Uri.TryCreate(result.SecureUrl?.ToString(), UriKind.Absolute, out var url) || url.Scheme != Uri.UriSchemeHttps
            || !string.IsNullOrEmpty(url.UserInfo) || !string.IsNullOrEmpty(url.Query) || !string.IsNullOrEmpty(url.Fragment)
            || result.PublicId != expectedPublicId || result.ResourceType != resourceType || result.Type != deliveryType
            || result.Bytes <= 0 || resourceType == FileResourceType && result.Bytes != expectedBytes
            || url.Host != "res.cloudinary.com" || !url.IsDefaultPort)
            throw new InvalidOperationException(InvalidResultMessage);
        var deliveryPrefix = $"/{cloudName}/{resourceType}/{deliveryType}/";
        if (!url.AbsolutePath.StartsWith(deliveryPrefix, StringComparison.Ordinal))
            throw new InvalidOperationException(InvalidResultMessage);
        var suffix = url.AbsolutePath[deliveryPrefix.Length..];
        // Authenticated uploads can return a pre-signed secure_url. Persist an unsigned locator instead of a bearer URL.
        if (deliveryType == PrivateDeliveryType && suffix.StartsWith("s--", StringComparison.Ordinal))
        {
            var separator = suffix.IndexOf('/');
            if (separator < 6) throw new InvalidOperationException(InvalidResultMessage);
            var signature = suffix[..separator];
            if (!signature.EndsWith("--", StringComparison.Ordinal)
                || !signature[3..^2].All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_'))
                throw new InvalidOperationException(InvalidResultMessage);
            suffix = suffix[(separator + 1)..];
        }
        var canonicalUrl = url.GetLeftPart(UriPartial.Authority) + deliveryPrefix + suffix;
        return new(canonicalUrl, result.PublicId, result.ResourceType, result.Type, result.Bytes);
    }
}
