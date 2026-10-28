namespace Mooncake.EcommercePlatform.Infrastructure.Services;

using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Infrastructure.Options;
using SkiaSharp;
using C = Mooncake.EcommercePlatform.Application.Common.Constant.ShopConstants;
using H = Mooncake.EcommercePlatform.Application.Common.Helpers.ShopPolicyHelper;

/// <summary>Decodes bounded branding images and publishes re-encoded public media.</summary>
public sealed class ShopMediaGateway(ICloudinaryStorageService storage,
    ICloudinaryUploadApi cloudinary, IOptions<CloudinaryOptions> options, ILogger<ShopMediaGateway> logger) : IShopMediaGateway
{
    private static readonly SemaphoreSlim DecodeSlots = new(2);
    public async Task<ShopProcessedMediaDto> PrepareAsync(byte[] content, string purpose, CancellationToken ct)
    {
        H.Require(content.Length is > 0 and <= C.MaxMediaBytes, 413, C.MediaTooLarge);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        try {
            using var data = SKData.CreateCopy(content);
            using var codec = SKCodec.Create(data);
            H.Require(codec != null, 415, C.MediaUnsupported);
            var info = codec!.Info;
            var format = codec.EncodedFormat;
            H.Require(format is SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Png or SKEncodedImageFormat.Webp && codec.FrameCount <= 1, 415, C.MediaUnsupported);
            H.Require((long)info.Width * info.Height <= 20_000_000, 413, C.MediaTooLarge);
            var dimensions = purpose switch {
                "Logo" => info.Width is >= 128 and <= 2048 && info.Height is >= 128 and <= 2048,
                "Banner" => info.Width is >= 640 and <= 4096 && info.Height is >= 200 and <= 2048,
                "TemplatePreview" => info.Width is >= 320 and <= 2048 && info.Height is >= 180 and <= 2048,
                "ProductGallery" or "VariantImage" => info.Width is >= 320 and <= 4096 && info.Height is >= 320 and <= 4096,
                _ => false };
            H.Require(dimensions, 415, C.MediaUnsupported);
            await DecodeSlots.WaitAsync(timeout.Token);
            try {
                using var bitmap = SKBitmap.Decode(codec, new SKImageInfo(info.Width, info.Height, SKColorType.Rgba8888, SKAlphaType.Premul));
                H.Require(bitmap != null, 415, C.MediaUnsupported);
                using var image = SKImage.FromBitmap(bitmap);
                using var encoded = image.Encode(format == SKEncodedImageFormat.Jpeg ? SKEncodedImageFormat.Jpeg : SKEncodedImageFormat.Png, 85);
                timeout.Token.ThrowIfCancellationRequested();
                H.Require(encoded != null && encoded.Size <= C.MaxMediaBytes, 413, C.MediaTooLarge);
                return new(encoded!.ToArray(), format == SKEncodedImageFormat.Jpeg ? "branding.jpg" : "branding.png",
                    format == SKEncodedImageFormat.Jpeg ? "image/jpeg" : "image/png", info.Width, info.Height);
            }
            finally { DecodeSlots.Release(); }
        }
        catch (HttpException) { throw; }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception failure) {
            logger.LogWarning(C.MediaFailureLog, "Decode", failure.GetType().Name);
            throw new HttpException(415, C.Error, C.MediaUnsupported);
        }
    }
    public async Task<ShopUploadedMediaDto> UploadAsync(ShopProcessedMediaDto media, CancellationToken ct)
    {
        try {
            using var content = new MemoryStream(media.Content, false);
            var uploaded = await storage.UploadImageAsync(content, media.FileName, false, ct);
            H.Require(uploaded.ResourceType == "image" && uploaded.DeliveryType == "upload", 503, C.StorageUnavailable);
            return new(uploaded.Url, uploaded.PublicId);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception failure) {
            logger.LogWarning(C.MediaFailureLog, "Storage", failure.GetType().Name);
            throw new HttpException(503, C.Error, C.StorageUnavailable);
        }
    }
    public async Task DeleteAsync(string publicId, CancellationToken ct)
    {
        H.Require(publicId.StartsWith(options.Value.PublicImagesFolder + "/", StringComparison.Ordinal), 403, C.Forbidden);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        var result = await cloudinary.DestroyAsync(new DeletionParams(publicId) { ResourceType = ResourceType.Image, Type = "upload" }).WaitAsync(timeout.Token);
        H.Require(result.Result is "ok" or "not found", 503, C.StorageUnavailable);
    }
}
