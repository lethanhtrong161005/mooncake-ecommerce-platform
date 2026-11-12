namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Users.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using SkiaSharp;

/// <summary>Personal profile endpoints scoped to the authenticated account.</summary>
[Authorize]
[Route("api/v1/users/me")]
public sealed class MyProfileController(
    IUserService userService,
    ICloudinaryStorageService? cloudinaryStorage = null) : BaseApiController
{
    private const long MaxAvatarSizeBytes = 5 * 1024 * 1024;
    private const long MaxAvatarRequestBytes = 6 * 1024 * 1024;
    private const string ProfileRetrievedMessage = "Profile retrieved successfully.";
    private const string ProfileUpdatedMessage = "Profile updated successfully.";
    private const string AvatarUploadedMessage = "Đã cập nhật ảnh đại diện.";
    private const string AvatarRemovedMessage = "Đã gỡ ảnh đại diện.";

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    /// <summary>Returns the authenticated user's personal profile.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken) =>
        Success(await userService.GetOwnProfileAsync(CurrentUserId, cancellationToken), ProfileRetrievedMessage);

    /// <summary>Updates the authenticated user's name, phone, and avatar URL.</summary>
    [HttpPut]
    public async Task<IActionResult> UpdateAsync([FromBody] UpdateOwnProfileRequest request,
        CancellationToken cancellationToken) =>
        Success(await userService.UpdateOwnProfileAsync(CurrentUserId, request, cancellationToken),
            ProfileUpdatedMessage);

    /// <summary>Uploads, sanitizes, and persists the authenticated user's avatar image.</summary>
    [HttpPost("avatar")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxAvatarRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxAvatarRequestBytes)]
    public async Task<IActionResult> UploadAvatarAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        if (cloudinaryStorage is null)
            throw new HttpException(503, "Dịch vụ lưu trữ ảnh hiện không khả dụng. Vui lòng thử lại sau.");

        var (sanitizedBytes, normalizedFileName) = await ReadAndSanitizeAvatarAsync(file, cancellationToken);

        try
        {
            await using var uploadStream = new MemoryStream(sanitizedBytes, writable: false);
            var uploaded = await cloudinaryStorage.UploadImageAsync(
                uploadStream, normalizedFileName, isPrivate: false, cancellationToken);

            if (string.IsNullOrWhiteSpace(uploaded.Url))
                throw new HttpException(502, "Không thể lấy đường dẫn ảnh đại diện sau khi tải lên.");

            return Success(
                await userService.UpdateOwnAvatarAsync(CurrentUserId, uploaded.Url, cancellationToken),
                AvatarUploadedMessage);
        }
        catch (HttpException)
        {
            throw;
        }
        catch (ArgumentException ex)
        {
            throw new HttpException(400, ex.Message);
        }
        catch (TimeoutException)
        {
            throw new HttpException(504, "Tải ảnh đại diện quá thời gian chờ. Vui lòng thử lại.");
        }
        catch (InvalidOperationException)
        {
            throw new HttpException(502, "Không thể tải ảnh đại diện lên dịch vụ lưu trữ. Vui lòng thử lại sau.");
        }
    }

    /// <summary>Removes the authenticated user's avatar URL.</summary>
    [HttpDelete("avatar")]
    public async Task<IActionResult> RemoveAvatarAsync(CancellationToken cancellationToken) =>
        Success(await userService.UpdateOwnAvatarAsync(CurrentUserId, null, cancellationToken),
            AvatarRemovedMessage);

    private static async Task<(byte[] Bytes, string FileName)> ReadAndSanitizeAvatarAsync(
        IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length <= 0)
            throw new HttpException(400, "Vui lòng chọn tệp ảnh đại diện.");

        if (file.Length > MaxAvatarSizeBytes)
            throw new HttpException(400, "Ảnh đại diện không được vượt quá 5MB.");

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
            throw new HttpException(400, "Chỉ hỗ trợ ảnh định dạng JPG, JPEG, PNG hoặc WEBP.");

        await using var sourceStream = file.OpenReadStream();
        using var buffer = new MemoryStream((int)Math.Min(file.Length, MaxAvatarSizeBytes));
        var chunk = new byte[16 * 1024];
        long totalRead = 0;

        while (true)
        {
            var read = await sourceStream.ReadAsync(chunk, cancellationToken);
            if (read == 0)
                break;

            totalRead += read;
            if (totalRead > MaxAvatarSizeBytes)
                throw new HttpException(400, "Ảnh đại diện không được vượt quá 5MB.");

            buffer.Write(chunk, 0, read);
        }

        if (totalRead == 0)
            throw new HttpException(400, "Tệp ảnh đại diện bị rỗng.");

        return ReencodeAvatarImage(buffer.ToArray());
    }

    private static (byte[] Bytes, string FileName) ReencodeAvatarImage(byte[] rawBytes)
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
            throw new HttpException(400, "Kích thước ảnh đại diện quá lớn (tối đa 4096x4096px).");

        // Re-encoding strips EXIF metadata and polyglot payloads before uploading to public storage.
        using var bitmap = SKBitmap.Decode(
            codec, new SKImageInfo(info.Width, info.Height, SKColorType.Rgba8888, SKAlphaType.Premul));
        if (bitmap is null)
            throw new HttpException(400, "Không thể xử lý tệp ảnh đã chọn. Vui lòng thử ảnh khác.");

        using var image = SKImage.FromBitmap(bitmap);
        var isJpeg = codec.EncodedFormat == SKEncodedImageFormat.Jpeg;
        using var encoded = image.Encode(isJpeg ? SKEncodedImageFormat.Jpeg : SKEncodedImageFormat.Png, 88);
        if (encoded is null || encoded.Size > MaxAvatarSizeBytes)
            throw new HttpException(400, "Ảnh đại diện sau xử lý vượt quá giới hạn 5MB.");

        return (encoded.ToArray(), isJpeg ? "avatar.jpg" : "avatar.png");
    }

    private Guid CurrentUserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
        ? userId : throw new HttpException(401, MessageConstant.SessionExpired);
}
