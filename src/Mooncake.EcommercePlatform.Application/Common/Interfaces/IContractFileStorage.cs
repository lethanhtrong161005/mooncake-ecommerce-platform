namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

public interface IContractFileStorage
{
    Task UploadAsync(string objectPath, Stream content, string contentType,
        CancellationToken cancellationToken = default);
    Task<string> CreateSignedDownloadUrlAsync(string objectPath,
        CancellationToken cancellationToken = default);
    Task DeleteAsync(string objectPath, CancellationToken cancellationToken = default);
}
