namespace Mooncake.EcommercePlatform.Infrastructure.Services;

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using CloudinaryDotNet;
using Microsoft.AspNetCore.DataProtection;
using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Chat.Persistence;
using Mooncake.EcommercePlatform.Application.DTOs.Chat.Requests;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using SkiaSharp;

/// <summary>Data-protected, user- and scope-bound keyset cursor for chat pagination.</summary>
public sealed class ChatCursorProtector(IDataProtectionProvider provider, IDateTimeProvider clock)
    : IChatCursorProtector
{
    private readonly IDataProtector _protector = provider.CreateProtector("Mooncake.Chat.Cursor.v1");

    private sealed record Envelope(Guid UserId, Guid ScopeId, DateTime AtUtc, Guid Id, DateTime ExpiresAtUtc);

    public string Protect(ChatCursorDto cursor) =>
        _protector.Protect(JsonSerializer.Serialize(new Envelope(
            cursor.UserId,
            cursor.ScopeId,
            cursor.AtUtc,
            cursor.Id,
            clock.UtcNow.AddMinutes(ChatConstants.CursorLifetimeMinutes))));

    public ChatCursorDto Unprotect(Guid expectedUserId, Guid expectedScopeId, string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token) || token.Length > ChatConstants.MaxCursorLength)
                throw new CryptographicException();

            var envelope = JsonSerializer.Deserialize<Envelope>(_protector.Unprotect(token))
                ?? throw new CryptographicException();

            if (envelope.UserId != expectedUserId
                || envelope.ScopeId != expectedScopeId
                || envelope.ExpiresAtUtc <= clock.UtcNow)
            {
                throw new CryptographicException();
            }

            return new ChatCursorDto(envelope.UserId, envelope.ScopeId, envelope.AtUtc, envelope.Id);
        }
        catch (Exception failure) when (failure is CryptographicException or JsonException or FormatException
            or ArgumentException)
        {
            throw new HttpException(400, ChatConstants.InvalidCursor);
        }
    }
}

/// <summary>Process-local connection counter per user; register as singleton.</summary>
public sealed class InMemoryPresenceTracker : IPresenceTracker
{
    private readonly ConcurrentDictionary<Guid, HashSet<string>> _connections = new();

    public bool Connect(Guid userId, string connectionId)
    {
        var set = _connections.GetOrAdd(userId, _ => []);
        lock (set)
        {
            set.Add(connectionId);
            return set.Count == 1;
        }
    }

    public bool Disconnect(Guid userId, string connectionId)
    {
        if (!_connections.TryGetValue(userId, out var set))
            return true;

        lock (set)
        {
            set.Remove(connectionId);
            return set.Count == 0;
        }
    }

    public bool IsOnline(Guid userId)
    {
        if (!_connections.TryGetValue(userId, out var set))
            return false;

        lock (set)
        {
            return set.Count > 0;
        }
    }
}

/// <summary>
/// Verifies file signatures, re-encodes images to strip metadata, uploads to Cloudinary,
/// and signs short-lived links for private documents.
/// </summary>
public sealed class ChatAttachmentStorage(ICloudinaryStorageService storage, ICloudinaryUploadApi cloudinary)
    : IChatAttachmentStorage
{
    private const int MaxImageDimension = 8192;
    private const long MaxImagePixels = 40_000_000;

    private static readonly IReadOnlyDictionary<string, string> MimeTypes = new Dictionary<string, string>(
        StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".doc"] = "application/msword",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xls"] = "application/vnd.ms-excel",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
    };

    private static readonly byte[] PdfSignature = "%PDF-"u8.ToArray();
    private static readonly byte[] ZipSignature = [0x50, 0x4B, 0x03, 0x04];
    private static readonly byte[] OleSignature = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];

    public async Task<ChatStoredFileDto> StoreAsync(ChatUploadItem item, CancellationToken ct = default)
    {
        var extension = Path.GetExtension(item.FileName);
        var bytes = await ReadBoundedAsync(item, ct);
        var safeName = SafeFileName(item.FileName);

        return ChatConstants.ImageExtensions.Contains(extension)
            ? await StoreImageAsync(bytes, safeName, ct)
            : await StoreDocumentAsync(bytes, safeName, extension, ct);
    }

    public string CreateDownloadUrl(ChatAttachment attachment, DateTime expiresAt) =>
        cloudinary.DownloadPrivate(
            attachment.PublicId,
            attachment: true,
            type: "authenticated",
            expiresAt: new DateTimeOffset(DateTime.SpecifyKind(expiresAt, DateTimeKind.Utc)).ToUnixTimeSeconds(),
            resourceType: "raw");

    private async Task<ChatStoredFileDto> StoreImageAsync(byte[] raw, string fileName, CancellationToken ct)
    {
        using var data = SKData.CreateCopy(raw);
        using var codec = SKCodec.Create(data)
            ?? throw new HttpException(400, ChatConstants.InvalidFileContent);

        if (codec.EncodedFormat is not (SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Png
            or SKEncodedImageFormat.Webp or SKEncodedImageFormat.Gif))
        {
            throw new HttpException(400, ChatConstants.InvalidFileContent);
        }

        var info = codec.Info;
        if (info.Width is < 1 or > MaxImageDimension || info.Height is < 1 or > MaxImageDimension
            || (long)info.Width * info.Height > MaxImagePixels)
        {
            throw new HttpException(400, ChatConstants.InvalidFileContent);
        }

        // Re-encoding strips EXIF/GPS metadata and polyglot payloads; animated images keep the first frame.
        using var bitmap = SKBitmap.Decode(codec,
            new SKImageInfo(info.Width, info.Height, SKColorType.Rgba8888, SKAlphaType.Premul))
            ?? throw new HttpException(400, ChatConstants.InvalidFileContent);
        using var image = SKImage.FromBitmap(bitmap);
        var isJpeg = codec.EncodedFormat == SKEncodedImageFormat.Jpeg;
        using var encoded = image.Encode(isJpeg ? SKEncodedImageFormat.Jpeg : SKEncodedImageFormat.Png, 88)
            ?? throw new HttpException(400, ChatConstants.InvalidFileContent);

        var outputExtension = isJpeg ? ".jpg" : ".png";
        var displayName = $"{Path.GetFileNameWithoutExtension(fileName)}{outputExtension}";
        await using var stream = new MemoryStream(encoded.ToArray());
        var uploaded = await storage.UploadImageAsync(stream, $"chat{outputExtension}", isPrivate: false, ct);

        return new ChatStoredFileDto(
            ChatAttachmentKind.Image,
            uploaded.Url,
            uploaded.PublicId,
            displayName,
            isJpeg ? "image/jpeg" : "image/png",
            uploaded.SizeBytes,
            info.Width,
            info.Height);
    }

    private async Task<ChatStoredFileDto> StoreDocumentAsync(byte[] raw, string fileName, string extension,
        CancellationToken ct)
    {
        var signatureOk = extension.ToLowerInvariant() switch
        {
            ".pdf" => StartsWith(raw, PdfSignature),
            ".docx" or ".xlsx" => StartsWith(raw, ZipSignature),
            ".doc" or ".xls" => StartsWith(raw, OleSignature),
            _ => false
        };
        if (!signatureOk)
            throw new HttpException(400, ChatConstants.InvalidFileContent);

        await using var stream = new MemoryStream(raw);
        var uploaded = await storage.UploadRawFileAsync(stream, fileName, ct);

        return new ChatStoredFileDto(
            ChatAttachmentKind.File,
            string.Empty,
            uploaded.PublicId,
            fileName,
            MimeTypes[extension],
            uploaded.SizeBytes,
            null,
            null);
    }

    private static async Task<byte[]> ReadBoundedAsync(ChatUploadItem item, CancellationToken ct)
    {
        await using var source = item.OpenReadStream();
        using var buffer = new MemoryStream((int)Math.Min(item.Length, ChatConstants.MaxFileBytes));
        var chunk = new byte[81920];
        int read;
        while ((read = await source.ReadAsync(chunk, ct)) > 0)
        {
            if (buffer.Length + read > ChatConstants.MaxFileBytes)
                throw new HttpException(400, ChatConstants.FileTooLarge);
            buffer.Write(chunk, 0, read);
        }

        return buffer.Length == 0
            ? throw new HttpException(400, ChatConstants.InvalidFileContent)
            : buffer.ToArray();
    }

    private static bool StartsWith(byte[] data, byte[] signature) =>
        data.Length >= signature.Length && data.AsSpan(0, signature.Length).SequenceEqual(signature);

    private static string SafeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Select(c => invalid.Contains(c) || char.IsControl(c) ? '_' : c).ToArray()).Trim();
        return cleaned.Length > ChatConstants.MaxFileNameLength
            ? $"{Path.GetFileNameWithoutExtension(cleaned)[..(ChatConstants.MaxFileNameLength - 10)]}{Path.GetExtension(cleaned)}"
            : cleaned;
    }
}

