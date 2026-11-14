namespace Mooncake.EcommercePlatform.Infrastructure.Services;

using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using SkiaSharp;

/// <summary>SkiaSharp-backed image validator and re-encoder for packaging template samples.</summary>
public sealed class PackagingTemplateImageProcessor : IPackagingTemplateImageProcessor
{
    public (byte[] Bytes, string FileName) SanitizeAndReencode(byte[] rawBytes, long maxBytes)
    {
        using var data = SKData.CreateCopy(rawBytes);
        using var codec = SKCodec.Create(data);
        if (codec is null || codec.FrameCount > 1 ||
            codec.EncodedFormat is not (SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Png or SKEncodedImageFormat.Webp))
        {
            throw new HttpException(400, "Tệp tải lên không phải ảnh hợp lệ (JPG, PNG hoặc WEBP).");
        }

        var info = codec.Info;
        if (info.Width is < 1 or > 4096 || info.Height is < 1 or > 4096 || (long)info.Width * info.Height > 20_000_000)
        {
            throw new HttpException(400, "Kích thước ảnh quá lớn (tối đa 4096x4096px).");
        }

        // Re-encoding strips EXIF metadata and polyglot payloads before uploading to public storage.
        using var bitmap = SKBitmap.Decode(
            codec, new SKImageInfo(info.Width, info.Height, SKColorType.Rgba8888, SKAlphaType.Premul));
        if (bitmap is null)
        {
            throw new HttpException(400, "Không thể xử lý tệp ảnh đã chọn. Vui lòng thử ảnh khác.");
        }

        using var image = SKImage.FromBitmap(bitmap);
        var isJpeg = codec.EncodedFormat == SKEncodedImageFormat.Jpeg;
        using var encoded = image.Encode(isJpeg ? SKEncodedImageFormat.Jpeg : SKEncodedImageFormat.Png, 88);
        if (encoded is null || encoded.Size > maxBytes)
        {
            throw new HttpException(400, "Ảnh sau xử lý vượt quá giới hạn 5MB.");
        }

        return (encoded.ToArray(), isJpeg ? "packaging-sample.jpg" : "packaging-sample.png");
    }
}

