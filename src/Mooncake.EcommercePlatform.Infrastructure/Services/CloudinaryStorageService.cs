namespace Mooncake.EcommercePlatform.Infrastructure.Services;

using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Files.Responses;
using Mooncake.EcommercePlatform.Infrastructure.Helpers;
using Mooncake.EcommercePlatform.Infrastructure.Options;

/// <summary>Uploads images and protected raw files to the existing Cloudinary folders.</summary>
public sealed class CloudinaryStorageService(ICloudinaryUploadApi client, IOptions<CloudinaryOptions> options) : ICloudinaryStorageService
{
    /// <inheritdoc />
    public Task<CloudinaryUploadResponse> UploadImageAsync(Stream content, string fileName, bool isPrivate,
        CancellationToken cancellationToken = default) => UploadAsync(content, fileName, true, isPrivate, cancellationToken);

    /// <inheritdoc />
    public Task<CloudinaryUploadResponse> UploadFileAsync(Stream content, string fileName,
        CancellationToken cancellationToken = default) => UploadAsync(content, fileName, false, true, cancellationToken);

    private async Task<CloudinaryUploadResponse> UploadAsync(Stream content, string fileName, bool image, bool isPrivate,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var configuration = options.Value;
        if (!configuration.IsValid()) throw new InvalidOperationException(CloudinaryOptions.InvalidConfigurationMessage);
        var safeFileName = CloudinaryUploadHelper.NormalizeFileName(content, fileName,
            image ? configuration.AllowedImageExtensions : configuration.AllowedFileExtensions);
        var folder = image
            ? isPrivate ? configuration.PrivateImagesFolder : configuration.PublicImagesFolder
            : configuration.FilesFolder;
        var deliveryType = isPrivate ? CloudinaryUploadHelper.PrivateDeliveryType : CloudinaryUploadHelper.PublicDeliveryType;
        var resourceType = image ? CloudinaryUploadHelper.ImageResourceType : CloudinaryUploadHelper.FileResourceType;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(configuration.UploadTimeoutSeconds));
        try
        {
            using var buffered = await CloudinaryUploadHelper.ReadBoundedAsync(content,
                image ? configuration.MaxImageSizeBytes : configuration.MaxFileSizeBytes, timeout.Token);
            var expectedBytes = buffered.Length;
            RawUploadParams parameters = image ? new ImageUploadParams() : new RawUploadParams();
            CloudinaryUploadHelper.ConfigureParameters(parameters, buffered, safeFileName, folder, deliveryType, image, configuration);
            var expectedPublicId = configuration.FolderMode == "Dynamic" ? parameters.PublicId : $"{folder}/{parameters.PublicId}";
            RawUploadResult result;
            try
            {
                result = image
                    ? await client.UploadAsync((ImageUploadParams)parameters, timeout.Token)
                    : await client.UploadAsync(parameters, CloudinaryUploadHelper.FileResourceType, timeout.Token);
            }
            catch (OperationCanceledException) { throw; }
            catch { throw new InvalidOperationException(CloudinaryUploadHelper.UploadFailedMessage); }
            timeout.Token.ThrowIfCancellationRequested();
            return CloudinaryUploadHelper.ToResponse(result, resourceType, deliveryType, expectedBytes, expectedPublicId, configuration.CloudName);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException(CloudinaryUploadHelper.TimeoutMessage);
        }
    }
}
