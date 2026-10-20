namespace Mooncake.EcommercePlatform.Infrastructure.Services;

using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.Common.Options;

public sealed class SupabaseContractFileStorage(
    HttpClient client,
    IOptions<SupabaseStorageOptions> options,
    ILogger<SupabaseContractFileStorage> logger) : IContractFileStorage
{
    private readonly SupabaseStorageOptions storage = options.Value;

    public async Task UploadAsync(string objectPath, Stream content, string contentType,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"object/{Encode(storage.ContractBucket)}/{EncodePath(objectPath)}");
        request.Headers.TryAddWithoutValidation("x-upsert", "false");
        request.Content = new StreamContent(content);
        request.Content.Headers.ContentType = new(contentType);
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw await StorageErrorAsync(response, "Không thể lưu file hợp đồng.", cancellationToken);
    }

    public async Task<string> CreateSignedDownloadUrlAsync(string objectPath,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.PostAsJsonAsync(
            $"object/sign/{Encode(storage.ContractBucket)}/{EncodePath(objectPath)}",
            new { expiresIn = storage.SignedUrlLifetimeSeconds, download = true }, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw await StorageErrorAsync(response, "Không thể tạo liên kết tải hợp đồng.", cancellationToken);
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        if (!payload.RootElement.TryGetProperty("signedURL", out var signedUrl)
            && !payload.RootElement.TryGetProperty("signedUrl", out signedUrl))
            throw new HttpException(502, "Supabase Storage trả về liên kết tải không hợp lệ.");
        var value = signedUrl.GetString();
        if (string.IsNullOrWhiteSpace(value))
            throw new HttpException(502, "Supabase Storage trả về liên kết tải không hợp lệ.");
        return Uri.TryCreate(value, UriKind.Absolute, out var absolute)
            ? absolute.ToString()
            : new Uri(client.BaseAddress!, value.TrimStart('/')).ToString();
    }

    public async Task DeleteAsync(string objectPath, CancellationToken cancellationToken = default)
    {
        using var response = await client.DeleteAsync(
            $"object/{Encode(storage.ContractBucket)}/{EncodePath(objectPath)}", cancellationToken);
        if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
            throw await StorageErrorAsync(response, "Không thể xóa file hợp đồng cũ.", cancellationToken);
    }

    private static string EncodePath(string value) =>
        string.Join('/', value.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(Encode));

    private static string Encode(string value) => Uri.EscapeDataString(value);

    private async Task<HttpException> StorageErrorAsync(
        HttpResponseMessage response, string message, CancellationToken cancellationToken)
    {
        var detail = await response.Content.ReadAsStringAsync(cancellationToken);
        if (detail.Length > 500) detail = detail[..500];
        logger.LogError("Supabase Storage returned {StatusCode}: {Detail}", (int)response.StatusCode, detail);
        return new HttpException(502, message);
    }
}
