namespace Mooncake.EcommercePlatform.Infrastructure.Services;

using CloudinaryDotNet;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Domain.Entities;
using C = Mooncake.EcommercePlatform.Application.Common.Constant.SupplierVerificationConstants;

/// <summary>Bounded, server-authorized private Cloudinary downloads.</summary>
public sealed class VerificationDocumentGateway(ICloudinaryUploadApi cloudinary, HttpClient http,
    IDateTimeProvider clock,
    Microsoft.Extensions.Options.IOptions<Mooncake.EcommercePlatform.Infrastructure.Options.CloudinaryOptions> options) : IVerificationDocumentGateway
{
    public long MaxFileBytes => Math.Min(C.MaxFileBytes, options.Value.MaxFileSizeBytes);
    public async Task<byte[]> ReadAsync(SupplierVerificationDocument document, CancellationToken cancellationToken)
    {
        var location = cloudinary.DownloadPrivate(document.ProviderPublicId!, attachment: true,
            type: "authenticated", expiresAt: new DateTimeOffset(clock.UtcNow.AddMinutes(1)).ToUnixTimeSeconds(), resourceType: "raw");
        var uri = new Uri(location);
        for (var redirects = 0; redirects < 4; redirects++)
        {
            SupplierVerificationHelper.Require(uri.Scheme == "https" &&
                (uri.Host == "cloudinary.com" || uri.Host.EndsWith(".cloudinary.com", StringComparison.OrdinalIgnoreCase)), 503, C.StorageUnavailable);
            using var response = await http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if ((int)response.StatusCode is >= 300 and < 400 && response.Headers.Location is { } redirect)
            {
                uri = new Uri(uri, redirect);
                continue;
            }
            SupplierVerificationHelper.Require(response.IsSuccessStatusCode, 503, C.StorageUnavailable);
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var content = await SupplierVerificationHelper.ReadBoundedAsync(stream, cancellationToken);
            SupplierVerificationHelper.Require(content.Length == document.FileSizeBytes &&
                Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(content)) == document.Sha256, 503, C.StorageUnavailable);
            return content;
        }
        throw new Mooncake.EcommercePlatform.Application.Common.Exceptions.HttpException(503, C.Error, C.StorageUnavailable);
    }
}
