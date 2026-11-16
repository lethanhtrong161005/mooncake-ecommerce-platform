namespace Mooncake.EcommercePlatform.Verification.Tests;

using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Files.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Infrastructure.Services;
using Mooncake.EcommercePlatform.WebApi.Controllers;
using SkiaSharp;
using Xunit;

public sealed class CustomPackagingLogoUploadTests
{
    [Fact]
    public async Task UploadLogoAsync_TextFileRenamedToPng_Throws400ValidationError()
    {
        var mockStorage = new Mock<ICloudinaryStorageService>();
        var controller = CreateController(mockStorage);
        var textBytes = Encoding.UTF8.GetBytes("This is plain text pretending to be a PNG image file.");
        var file = CreateFormFile(textBytes, "hacked-logo.png", "image/png");

        var ex = await Assert.ThrowsAsync<HttpException>(() => controller.UploadLogoAsync(file, default));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal(OrderErrorCodes.ValidationError, ex.ErrorCode);
        mockStorage.Verify(x => x.UploadImageAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadLogoAsync_SvgFileWithSvgExtension_Throws400ValidationError()
    {
        var mockStorage = new Mock<ICloudinaryStorageService>();
        var controller = CreateController(mockStorage);
        var svgBytes = Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"100\" height=\"100\"><circle cx=\"50\" cy=\"50\" r=\"40\"/></svg>");
        var file = CreateFormFile(svgBytes, "vector-logo.svg", "image/svg+xml");

        var ex = await Assert.ThrowsAsync<HttpException>(() => controller.UploadLogoAsync(file, default));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal(OrderErrorCodes.ValidationError, ex.ErrorCode);
        mockStorage.Verify(x => x.UploadImageAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadLogoAsync_SvgFileRenamedToPng_Throws400ValidationError()
    {
        var mockStorage = new Mock<ICloudinaryStorageService>();
        var controller = CreateController(mockStorage);
        var svgBytes = Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"100\" height=\"100\"><rect width=\"100\" height=\"100\" fill=\"red\"/></svg>");
        var file = CreateFormFile(svgBytes, "vector-logo.png", "image/png");

        var ex = await Assert.ThrowsAsync<HttpException>(() => controller.UploadLogoAsync(file, default));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal(OrderErrorCodes.ValidationError, ex.ErrorCode);
        mockStorage.Verify(x => x.UploadImageAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadLogoAsync_GifFileRenamedToPng_Throws400ValidationError()
    {
        var mockStorage = new Mock<ICloudinaryStorageService>();
        var controller = CreateController(mockStorage);
        var gifBytes = CreateGifBytes();
        var file = CreateFormFile(gifBytes, "animated.png", "image/png");

        var ex = await Assert.ThrowsAsync<HttpException>(() => controller.UploadLogoAsync(file, default));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal(OrderErrorCodes.ValidationError, ex.ErrorCode);
        mockStorage.Verify(x => x.UploadImageAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadLogoAsync_DecompressionBombDimensions_Throws400ValidationError()
    {
        var mockStorage = new Mock<ICloudinaryStorageService>();
        var controller = CreateController(mockStorage);
        var bombBytes = CreatePngBytes(width: 5000, height: 10, withAlpha: false);
        var file = CreateFormFile(bombBytes, "bomb.png", "image/png");

        var ex = await Assert.ThrowsAsync<HttpException>(() => controller.UploadLogoAsync(file, default));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal(OrderErrorCodes.ValidationError, ex.ErrorCode);
        mockStorage.Verify(x => x.UploadImageAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadLogoAsync_ValidPngWithAlpha_ReencodesAsPngAndUploadsWithGeneratedFileName()
    {
        var mockStorage = new Mock<ICloudinaryStorageService>();
        Stream? passedStream = null;
        string? passedFileName = null;
        byte[]? reencodedBytes = null;

        mockStorage.Setup(x => x.UploadImageAsync(It.IsAny<Stream>(), It.IsAny<string>(), false, It.IsAny<CancellationToken>()))
            .Callback<Stream, string, bool, CancellationToken>((s, fn, _, _) =>
            {
                passedFileName = fn;
                using var ms = new MemoryStream();
                s.CopyTo(ms);
                reencodedBytes = ms.ToArray();
                passedStream = s;
            })
            .ReturnsAsync(new CloudinaryUploadResponse("https://res.cloudinary.com/demo/image/upload/v1/logo_123.png", "logo_123", "image", "upload", 1234));

        var controller = CreateController(mockStorage);
        var rawPng = CreatePngBytes(width: 120, height: 80, withAlpha: true);
        var file = CreateFormFile(rawPng, "my-company-logo.png", "image/png");

        var result = await controller.UploadLogoAsync(file, default);

        Assert.IsType<OkObjectResult>(result);
        mockStorage.Verify(x => x.UploadImageAsync(It.IsAny<Stream>(), It.IsAny<string>(), false, It.IsAny<CancellationToken>()), Times.Once);

        Assert.NotNull(passedFileName);
        Assert.NotEqual("my-company-logo.png", passedFileName);
        Assert.StartsWith("logo_", passedFileName);
        Assert.EndsWith(".png", passedFileName);

        Assert.NotNull(reencodedBytes);
        using var decoded = SKCodec.Create(SKData.CreateCopy(reencodedBytes));
        Assert.NotNull(decoded);
        Assert.Equal(120, decoded.Info.Width);
        Assert.Equal(80, decoded.Info.Height);
        Assert.Equal(SKEncodedImageFormat.Png, decoded.EncodedFormat);
    }

    [Fact]
    public async Task UploadLogoAsync_ValidOpaqueJpeg_ReencodesAsJpegAndUploadsWithGeneratedFileName()
    {
        var mockStorage = new Mock<ICloudinaryStorageService>();
        string? passedFileName = null;
        byte[]? reencodedBytes = null;

        mockStorage.Setup(x => x.UploadImageAsync(It.IsAny<Stream>(), It.IsAny<string>(), false, It.IsAny<CancellationToken>()))
            .Callback<Stream, string, bool, CancellationToken>((s, fn, _, _) =>
            {
                passedFileName = fn;
                using var ms = new MemoryStream();
                s.CopyTo(ms);
                reencodedBytes = ms.ToArray();
            })
            .ReturnsAsync(new CloudinaryUploadResponse("https://res.cloudinary.com/demo/image/upload/v1/logo_123.jpg", "logo_123", "image", "upload", 2345));

        var controller = CreateController(mockStorage);
        var rawJpeg = CreateJpegBytes(width: 200, height: 150);
        var file = CreateFormFile(rawJpeg, "original-photo.jpg", "image/jpeg");

        var result = await controller.UploadLogoAsync(file, default);

        Assert.IsType<OkObjectResult>(result);
        mockStorage.Verify(x => x.UploadImageAsync(It.IsAny<Stream>(), It.IsAny<string>(), false, It.IsAny<CancellationToken>()), Times.Once);

        Assert.NotNull(passedFileName);
        Assert.NotEqual("original-photo.jpg", passedFileName);
        Assert.StartsWith("logo_", passedFileName);
        Assert.EndsWith(".jpg", passedFileName);

        Assert.NotNull(reencodedBytes);
        using var decoded = SKCodec.Create(SKData.CreateCopy(reencodedBytes));
        Assert.NotNull(decoded);
        Assert.Equal(200, decoded.Info.Width);
        Assert.Equal(150, decoded.Info.Height);
        Assert.Equal(SKEncodedImageFormat.Jpeg, decoded.EncodedFormat);
    }

    [Fact]
    public void LogoImageSanitizer_RejectsInvalidOrMismatchedInputs()
    {
        var sanitizer = new LogoImageSanitizer();
        var validBytes = CreatePngBytes(width: 50, height: 50, withAlpha: false);

        // Null/empty bytes
        var ex1 = Assert.Throws<HttpException>(() => sanitizer.Sanitize([], "logo.png", "image/png", 10_000_000));
        Assert.Equal(400, ex1.StatusCode);
        Assert.Equal(OrderErrorCodes.ValidationError, ex1.ErrorCode);

        // Disallowed extension
        var ex2 = Assert.Throws<HttpException>(() => sanitizer.Sanitize(validBytes, "logo.bmp", "image/bmp", 10_000_000));
        Assert.Equal(400, ex2.StatusCode);
        Assert.Equal(OrderErrorCodes.ValidationError, ex2.ErrorCode);

        // Mismatched content-type
        var ex3 = Assert.Throws<HttpException>(() => sanitizer.Sanitize(validBytes, "logo.png", "image/jpeg", 10_000_000));
        Assert.Equal(400, ex3.StatusCode);
        Assert.Equal(OrderErrorCodes.ValidationError, ex3.ErrorCode);

        // MaxBytes exceeded after encoding
        var ex4 = Assert.Throws<HttpException>(() => sanitizer.Sanitize(validBytes, "logo.png", "image/png", 10));
        Assert.Equal(400, ex4.StatusCode);
        Assert.Equal(OrderErrorCodes.ValidationError, ex4.ErrorCode);
    }

    private static CustomPackagingController CreateController(
        Mock<ICloudinaryStorageService>? mockStorage = null,
        ILogoImageSanitizer? sanitizer = null)
    {
        var mockPackagingService = new Mock<ICustomPackagingService>();
        var controller = new CustomPackagingController(
            mockPackagingService.Object,
            mockStorage?.Object,
            sanitizer);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.ControllerContext.HttpContext.Request.Path = "/api/v1/custom-packaging/upload-logo";
        controller.ControllerContext.HttpContext.Items["TraceId"] = Guid.NewGuid().ToString("N");

        return controller;
    }

    private static IFormFile CreateFormFile(byte[] content, string fileName, string contentType)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, content.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private static byte[] CreatePngBytes(int width = 100, int height = 100, bool withAlpha = true)
    {
        using var bitmap = new SKBitmap(new SKImageInfo(
            width,
            height,
            SKColorType.Rgba8888,
            withAlpha ? SKAlphaType.Premul : SKAlphaType.Opaque));
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(withAlpha ? new SKColor(255, 100, 50, 180) : SKColors.DarkGreen);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static byte[] CreateJpegBytes(int width = 100, int height = 100)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Blue);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);
        return data.ToArray();
    }

    private static byte[] CreateGifBytes()
    {
        return
        [
            0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x01, 0x00, 0x01, 0x00, 0x80, 0x00, 0x00,
            0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x21, 0xF9, 0x04, 0x01, 0x00, 0x00, 0x00,
            0x00, 0x2C, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0x02, 0x02,
            0x44, 0x01, 0x00, 0x3B
        ];
    }
}
