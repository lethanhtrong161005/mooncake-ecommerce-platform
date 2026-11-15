namespace Mooncake.EcommercePlatform.Infrastructure.Services;

using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using SkiaSharp;

/// <summary>Validates and re-encodes corporate logo images to prevent decompression bombs and metadata exploits.</summary>
public sealed class LogoImageSanitizer : ILogoImageSanitizer
{
    private const int MaxDimension = 4096;
    private const long MaxPixels = 4096L * 4096L;
    private const int JpegQuality = 90;
    private const int PngQuality = 100;

    private const string InvalidImageFormatMessage = "Only PNG, JPG, JPEG, and WEBP image formats are supported.";
    private const string ImageDimensionsTooLargeMessage = "Image dimensions must not exceed 4096x4096 pixels.";
    private const string InvalidImageContentMessage = "The provided file is not a valid image.";
    private const string EmptyImageContentMessage = "The uploaded image file is empty.";

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png",
        ".jpg",
        ".jpeg",
        ".webp"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png",
        "image/x-png",
        "image/jpeg",
        "image/jpg",
        "image/pjpeg",
        "image/webp"
    };

    /// <summary>Validates extension, MIME type, dimensions, and re-encodes the image stream.</summary>
    public (byte[] Bytes, string FileName) Sanitize(
        Stream inputStream,
        string? originalFileName,
        string? contentType,
        long maxBytes)
    {
        if (inputStream is null)
        {
            throw new HttpException(400, EmptyImageContentMessage, OrderErrorCodes.ValidationError);
        }

        using var memoryStream = new MemoryStream();
        inputStream.CopyTo(memoryStream);
        return Sanitize(memoryStream.ToArray(), originalFileName, contentType, maxBytes);
    }

    /// <summary>Validates extension, MIME type, dimensions, and re-encodes the image raw bytes.</summary>
    public (byte[] Bytes, string FileName) Sanitize(
        byte[] rawBytes,
        string? originalFileName,
        string? contentType,
        long maxBytes)
    {
        if (rawBytes is null || rawBytes.Length == 0)
        {
            throw new HttpException(400, EmptyImageContentMessage, OrderErrorCodes.ValidationError);
        }

        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            throw new HttpException(400, InvalidImageFormatMessage, OrderErrorCodes.ValidationError);
        }

        var extension = Path.GetExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            throw new HttpException(400, InvalidImageFormatMessage, OrderErrorCodes.ValidationError);
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new HttpException(400, InvalidImageFormatMessage, OrderErrorCodes.ValidationError);
        }

        var mediaType = contentType.Split(';')[0].Trim();
        if (!AllowedContentTypes.Contains(mediaType) || !IsContentTypeMatchingExtension(extension, mediaType))
        {
            throw new HttpException(400, InvalidImageFormatMessage, OrderErrorCodes.ValidationError);
        }

        using var data = SKData.CreateCopy(rawBytes);
        using var codec = SKCodec.Create(data);
        if (codec is null || codec.FrameCount > 1 ||
            codec.EncodedFormat is not (SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Png or SKEncodedImageFormat.Webp))
        {
            throw new HttpException(400, InvalidImageContentMessage, OrderErrorCodes.ValidationError);
        }

        var info = codec.Info;
        if (info.Width is < 1 or > MaxDimension || info.Height is < 1 or > MaxDimension ||
            (long)info.Width * info.Height > MaxPixels)
        {
            throw new HttpException(400, ImageDimensionsTooLargeMessage, OrderErrorCodes.ValidationError);
        }

        using var bitmap = SKBitmap.Decode(
            codec, new SKImageInfo(info.Width, info.Height, SKColorType.Rgba8888, SKAlphaType.Premul));
        if (bitmap is null)
        {
            throw new HttpException(400, InvalidImageContentMessage, OrderErrorCodes.ValidationError);
        }

        using var image = SKImage.FromBitmap(bitmap);
        var hasAlpha = codec.Info.AlphaType != SKAlphaType.Opaque;
        var format = hasAlpha ? SKEncodedImageFormat.Png : SKEncodedImageFormat.Jpeg;
        var quality = hasAlpha ? PngQuality : JpegQuality;
        var outputExtension = hasAlpha ? ".png" : ".jpg";

        using var encoded = image.Encode(format, quality);
        if (encoded is null)
        {
            throw new HttpException(400, InvalidImageContentMessage, OrderErrorCodes.ValidationError);
        }

        if (encoded.Size > maxBytes)
        {
            throw new HttpException(400, B2BOrderingMessages.LogoFileTooLarge, OrderErrorCodes.ValidationError);
        }

        var serverGeneratedFileName = $"logo_{Guid.NewGuid():N}{outputExtension}";
        return (encoded.ToArray(), serverGeneratedFileName);
    }

    private static bool IsContentTypeMatchingExtension(string extension, string mediaType) =>
        extension.ToLowerInvariant() switch
        {
            ".png" => mediaType.Equals("image/png", StringComparison.OrdinalIgnoreCase) ||
                      mediaType.Equals("image/x-png", StringComparison.OrdinalIgnoreCase),
            ".jpg" or ".jpeg" => mediaType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase) ||
                                 mediaType.Equals("image/jpg", StringComparison.OrdinalIgnoreCase) ||
                                 mediaType.Equals("image/pjpeg", StringComparison.OrdinalIgnoreCase),
            ".webp" => mediaType.Equals("image/webp", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
}
