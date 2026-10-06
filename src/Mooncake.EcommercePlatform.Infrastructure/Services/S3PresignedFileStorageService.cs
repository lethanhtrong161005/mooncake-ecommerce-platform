namespace Mooncake.EcommercePlatform.Infrastructure.Services;

using System.Security.Cryptography;
using System.Text;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Files;

/// <summary>Creates S3 Signature Version 4 presigned URLs without storing credentials in source configuration.</summary>
public sealed class S3PresignedFileStorageService(IDateTimeProvider dateTimeProvider) : IFileStorageService
{
    private const string Algorithm = "AWS4-HMAC-SHA256";
    private const string ServiceName = "s3";
    private const string RequestTerminator = "aws4_request";
    private const string UploadMethod = "PUT";
    private const string DownloadMethod = "GET";
    private const string PayloadMarker = "UNSIGNED-PAYLOAD";
    private static readonly TimeSpan UploadLifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan DownloadLifetime = TimeSpan.FromMinutes(5);
    private static readonly IReadOnlyDictionary<string, string> AllowedTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg", ["image/png"] = ".png", ["image/webp"] = ".webp", ["application/pdf"] = ".pdf"
    };

    private const long MaxFileSizeBytes = 25_000_000;

    public PresignedFileResponse CreateUploadUrl(Guid ownerId, string fileName, string contentType, long fileSizeBytes)
    {
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Length > 255)
            throw new HttpException(400, "A valid file name is required.");
        contentType = contentType.Trim().ToLowerInvariant();
        if (!AllowedTypes.TryGetValue(contentType, out var extension))
            throw new HttpException(400, "Only JPEG, PNG, WebP, and PDF uploads are supported.");
        if (fileSizeBytes is < 1 or > MaxFileSizeBytes)
            throw new HttpException(400, "File size must be between 1 byte and 25 MB.");
        var objectKey = $"uploads/{ownerId:D}/{Guid.NewGuid():N}{extension}";
        var url = CreateSignedUrl(UploadMethod, objectKey, UploadLifetime, contentType, fileSizeBytes);
        return new PresignedFileResponse(objectKey, url, dateTimeProvider.UtcNow.Add(UploadLifetime), UploadMethod, contentType, fileSizeBytes, GetPublicAssetUrl(objectKey));
    }

    public PresignedFileResponse CreateDownloadUrl(Guid requesterId, string objectKey, bool administrator)
    {
        var normalizedObjectKey = objectKey ?? string.Empty;
        var keyParts = normalizedObjectKey.Split('/');
        if (keyParts.Length != 3 || keyParts[0] != "uploads" || keyParts[1] == "." || keyParts[1] == ".." ||
            keyParts[2] == "." || keyParts[2] == ".." || !Guid.TryParse(keyParts[1], out var ownerId) ||
            (!administrator && ownerId != requesterId) ||
            string.IsNullOrWhiteSpace(keyParts[2]))
            throw new HttpException(404, "File was not found.");
        var url = CreateSignedUrl(DownloadMethod, normalizedObjectKey, DownloadLifetime, null);
        return new PresignedFileResponse(normalizedObjectKey, url, dateTimeProvider.UtcNow.Add(DownloadLifetime), DownloadMethod, null, null, null);
    }

    private string CreateSignedUrl(string method, string objectKey, TimeSpan lifetime, string? contentType, long? contentLength = null)
    {
        var bucket = RequiredSetting("S3_BUCKET");
        var region = RequiredSetting("S3_REGION");
        var accessKey = RequiredSetting("S3_ACCESS_KEY_ID");
        var secretKey = RequiredSetting("S3_SECRET_ACCESS_KEY");
        var endpoint = GetEndpoint(region);
        var now = dateTimeProvider.UtcNow;
        var timestamp = now.ToString("yyyyMMdd'T'HHmmss'Z'", System.Globalization.CultureInfo.InvariantCulture);
        var dateStamp = now.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
        var expiresSeconds = (int)lifetime.TotalSeconds;
        var scope = $"{dateStamp}/{region}/{ServiceName}/{RequestTerminator}";
        var canonicalUri = BuildCanonicalUri(endpoint, bucket, objectKey);
        var signedHeaders = contentType is null ? "host" : "content-length;content-type;host";
        var contentLengthHeader = contentLength?.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var canonicalHeaders = contentType is null
            ? $"host:{endpoint.Authority.ToLowerInvariant()}"
            : $"content-length:{contentLengthHeader}\ncontent-type:{contentType.Trim().ToLowerInvariant()}\nhost:{endpoint.Authority.ToLowerInvariant()}";
        var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["X-Amz-Algorithm"] = Algorithm,
            ["X-Amz-Credential"] = $"{accessKey}/{scope}",
            ["X-Amz-Date"] = timestamp,
            ["X-Amz-Expires"] = expiresSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["X-Amz-SignedHeaders"] = signedHeaders
        };
        var sessionToken = Environment.GetEnvironmentVariable("S3_SESSION_TOKEN");
        if (!string.IsNullOrWhiteSpace(sessionToken)) parameters["X-Amz-Security-Token"] = sessionToken;
        var canonicalQuery = string.Join("&", parameters.Select(pair => $"{Encode(pair.Key)}={Encode(pair.Value)}"));
        var canonicalRequest = $"{method}\n{canonicalUri}\n{canonicalQuery}\n{canonicalHeaders}\n{signedHeaders}\n{PayloadMarker}";
        var stringToSign = string.Join("\n", Algorithm, timestamp, scope, Sha256Hex(canonicalRequest));
        var signature = Convert.ToHexString(Sign(SigningKey(secretKey, dateStamp, region), stringToSign)).ToLowerInvariant();
        var absoluteUrl = $"{endpoint.GetLeftPart(UriPartial.Authority)}{canonicalUri}?{canonicalQuery}&X-Amz-Signature={signature}";
        return absoluteUrl;
    }

    private static Uri GetEndpoint(string region)
    {
        var configuredEndpoint = Environment.GetEnvironmentVariable("S3_ENDPOINT");
        var endpoint = string.IsNullOrWhiteSpace(configuredEndpoint)
            ? new Uri($"https://s3.{region}.amazonaws.com")
            : new Uri(configuredEndpoint, UriKind.Absolute);
        if (endpoint.Scheme != Uri.UriSchemeHttps && !endpoint.IsLoopback)
            throw new InvalidOperationException("S3_ENDPOINT must use HTTPS except for loopback development storage.");
        return endpoint;
    }

    private static string BuildCanonicalUri(Uri endpoint, string bucket, string objectKey)
    {
        var prefix = endpoint.AbsolutePath.TrimEnd('/');
        var path = string.Join('/', objectKey.Split('/').Select(Encode));
        return $"{prefix}/{Encode(bucket)}/{path}";
    }

    private static string RequiredSetting(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"{name} must be configured to use file storage.");

    private static string? GetPublicAssetUrl(string objectKey)
    {
        var baseUrl = Environment.GetEnvironmentVariable("S3_PUBLIC_BASE_URL");
        return string.IsNullOrWhiteSpace(baseUrl) ? null : $"{baseUrl.TrimEnd('/')}/{string.Join('/', objectKey.Split('/').Select(Encode))}";
    }

    private static string Encode(string value) => Uri.EscapeDataString(value).Replace("%7E", "~", StringComparison.Ordinal);
    private static string Sha256Hex(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static byte[] Sign(byte[] key, string value) => HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(value));

    private static byte[] SigningKey(string secretKey, string dateStamp, string region)
    {
        var dateKey = Sign(Encoding.UTF8.GetBytes($"AWS4{secretKey}"), dateStamp);
        var regionKey = Sign(dateKey, region);
        var serviceKey = Sign(regionKey, ServiceName);
        return Sign(serviceKey, RequestTerminator);
    }
}
