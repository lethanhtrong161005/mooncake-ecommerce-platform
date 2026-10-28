namespace Mooncake.EcommercePlatform.Verification.Tests;

using System.Net;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Mooncake.EcommercePlatform.Infrastructure.Options;
using Mooncake.EcommercePlatform.Infrastructure.Services;

public sealed class RawDocumentStorageTests
{
    [Theory]
    [InlineData("archive.zip")]
    [InlineData("report.docx")]
    [InlineData("program.exe")]
    [InlineData("README")]
    [InlineData("data.unrecognized")]
    public async Task RawDocumentsIgnoreExtensionAllowlistAndRemainPrivate(string name)
    {
        var client = new Mock<ICloudinaryUploadApi>();
        client.Setup(x => x.UploadAsync(It.IsAny<RawUploadParams>(), "raw", It.IsAny<CancellationToken>()))
            .Returns((RawUploadParams parameters, string _, CancellationToken _) => Task.FromResult(new RawUploadResult {
                PublicId = parameters.PublicId, ResourceType = "raw", Type = "authenticated", Bytes = 3,
                StatusCode = HttpStatusCode.OK, SecureUrl = new Uri($"https://res.cloudinary.com/test-cloud/raw/authenticated/{parameters.PublicId}")
            }));
        var storage = new CloudinaryStorageService(client.Object, Options.Create(Configuration()));
        using var bytes = new MemoryStream([0, 1, 2]);
        var result = await storage.UploadRawFileAsync(bytes, name);
        Assert.Equal("raw", result.ResourceType); Assert.Equal("authenticated", result.DeliveryType);
        Assert.Equal(3, result.SizeBytes);
        client.Verify(x => x.UploadAsync(It.IsAny<RawUploadParams>(), "raw", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProviderUploadIsNotCalledWhenRawDocumentExceedsByteLimit()
    {
        var client = new Mock<ICloudinaryUploadApi>();
        var storage = new CloudinaryStorageService(client.Object, Options.Create(Configuration()));
        using var bytes = new MemoryStream(new byte[1025]);
        await Assert.ThrowsAsync<ArgumentException>(() => storage.UploadRawFileAsync(bytes, "data.custom"));
        client.Verify(x => x.UploadAsync(It.IsAny<RawUploadParams>(), "raw", It.IsAny<CancellationToken>()), Times.Never);
    }

    private static CloudinaryOptions Configuration() => new() {
        CloudName = "test-cloud", ApiKey = "test-api-key", ApiSecret = "test-api-secret", Secure = true,
        FolderMode = "Dynamic", PublicImagesFolder = "test/images", PrivateImagesFolder = "test/private", FilesFolder = "test/files",
        MaxImageSizeBytes = 1024, MaxFileSizeBytes = 1024, UploadTimeoutSeconds = 10,
        AllowedImageExtensions = [".png"], AllowedFileExtensions = [".pdf"]
    };
}
