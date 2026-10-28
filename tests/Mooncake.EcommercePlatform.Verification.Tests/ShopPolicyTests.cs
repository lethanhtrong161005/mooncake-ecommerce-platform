namespace Mooncake.EcommercePlatform.Verification.Tests;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using SkiaSharp;
using Xunit;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Persistence;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Requests;
using Mooncake.EcommercePlatform.Infrastructure.Options;
using Mooncake.EcommercePlatform.Infrastructure.Services;

public sealed class ShopPolicyTests
{
    [Theory]
    [InlineData("admin")]
    [InlineData("my shop")]
    [InlineData("Bakery")]
    [InlineData("bakery/other")]
    [InlineData("bakery%2Fother")]
    [InlineData("bakery'; DROP TABLE shops;--")]
    public void SlugIsCanonicalAndDoesNotAcceptSqlOrRouteSyntax(string slug) => Assert.Throws<HttpException>(() => ShopPolicyHelper.Slug(slug));

    [Fact]
    public void MvcValidatesNestedFieldsAndDistinguishesFalseFromMissing()
    {
        Assert.True(Validate(new SetTemplateAvailabilityRequest { ExpectedVersion = 1, IsActive = false }));
        Assert.False(Validate(new SetTemplateAvailabilityRequest { ExpectedVersion = 1 }));
        Assert.False(Validate(new SetMyShopVisibilityRequest { ExpectedVersion = 0 }));
        Assert.False(Validate(new UpdateShopAppearanceRequest { ExpectedVersion = 1,
            Overrides = new() { PrimaryColor = "url(https://invalid.test)" } }));
        Assert.False(Validate(new PublishTemplateRevisionRequest { ExpectedVersion = 1, Name = "Valid name",
            Defaults = new() { FontScale = 4 }, Layout = new() { ProductColumnsDesktop = 99 } }));
        Assert.False(Validate(new ShopProductQuery { PageSize = 101, MinPrice = -1 }));
    }

    [Fact]
    public void ThemeRejectsUnknownPropertiesUnsafeContrastAndExtraPrecision()
    {
        Assert.Throws<System.Text.Json.JsonException>(() => ShopPolicyHelper.Parse<ThemeOverridesRequest>("{\"customCss\":\"body{display:none}\"}"));
        Assert.Throws<HttpException>(() => ShopPolicyHelper.Tokens(new() { FontScale = 1.001m }));
        var invalid = new ThemeOverridesRequest { PrimaryColor = "#FFFFFF", ButtonTextColor = "#FFFFFF",
            TextColor = "#FFFFFF", BackgroundColor = "#FFFFFF", SurfaceColor = "#FFFFFF", BorderRadiusPx = 0, FontScale = 1 };
        Assert.Throws<HttpException>(() => ShopPolicyHelper.Merge(invalid, new()));
    }

    [Fact]
    public void CursorsRejectTamperingOtherShopOtherFiltersAndExpiry()
    {
        var now = DateTime.UtcNow; var clock = new Mock<IDateTimeProvider>(); clock.SetupGet(x => x.UtcNow).Returns(() => now);
        var protector = new ShopCursorProtector(new EphemeralDataProtectionProvider(), clock.Object);
        var scope = new ShopCursorScopeDto("products", Guid.NewGuid(), "priceAsc");
        var cursor = new ShopCursorDto(Guid.NewGuid(), now, 100);
        var token = protector.Protect(scope, cursor);
        Assert.Equal(cursor, protector.Unprotect(scope, token));
        foreach (var other in new[] { scope with { ResourceId = Guid.NewGuid() }, scope with { FilterHash = "priceDesc" }, scope with { Kind = "reviews" } })
            Assert.Equal("SHOP_CURSOR_INVALID", Assert.Throws<HttpException>(() => protector.Unprotect(other, token)).ErrorCode);
        Assert.Throws<HttpException>(() => protector.Unprotect(scope, "tampered" + token));
        now = now.AddMinutes(16); Assert.Throws<HttpException>(() => protector.Unprotect(scope, token));
    }

    [Fact]
    public async Task MediaDecodesWithoutScannerAndRejectsUnsupportedDimensions()
    {
        var gateway = MediaGateway();
        using var bitmap = new SKBitmap(128, 128); bitmap.Erase(SKColors.Coral);
        using var image = SKImage.FromBitmap(bitmap); using var encoded = image.Encode(SKEncodedImageFormat.Png, 90);
        var result = await gateway.PrepareAsync(encoded.ToArray(), "Logo", default);
        Assert.Equal(128, result.Width); Assert.Equal("image/png", result.ContentType);
        var dimensions = await Assert.ThrowsAsync<HttpException>(() => gateway.PrepareAsync(encoded.ToArray(), "Banner", default));
        Assert.Equal("SHOP_MEDIA_UNSUPPORTED", dimensions.ErrorCode);
        var unsupported = await Assert.ThrowsAsync<HttpException>(() => gateway.PrepareAsync("<svg onload='alert(1)'/>"u8.ToArray(), "Logo", default));
        Assert.Equal(415, unsupported.StatusCode);
    }

    [Fact]
    public async Task MediaByteLimitIsCheckedBeforeImageDecoding()
    {
        var error = await Assert.ThrowsAsync<HttpException>(() => MediaGateway().PrepareAsync(new byte[5 * 1024 * 1024 + 1], "Logo", default));
        Assert.Equal("SHOP_MEDIA_TOO_LARGE", error.ErrorCode); Assert.Equal(413, error.StatusCode);
    }

    private static ShopMediaGateway MediaGateway() => new(Mock.Of<ICloudinaryStorageService>(),
        Mock.Of<ICloudinaryUploadApi>(), Options.Create(new CloudinaryOptions()), NullLogger<ShopMediaGateway>.Instance);

    private static bool Validate(object model)
    {
        var services = new ServiceCollection(); services.AddLogging(); services.AddControllers();
        using var provider = services.BuildServiceProvider();
        var action = new ActionContext(new DefaultHttpContext { RequestServices = provider }, new RouteData(), new ActionDescriptor(), new ModelStateDictionary());
        provider.GetRequiredService<IObjectModelValidator>().Validate(action, null, string.Empty, model);
        return action.ModelState.IsValid;
    }
}
