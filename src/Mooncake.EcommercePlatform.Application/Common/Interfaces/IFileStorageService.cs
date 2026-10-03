namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

/// <summary>Contract for managing stored files (images, packaging logos, documents).</summary>
public interface IFileStorageService
{
    /// <summary>Saves a file stream to storage and returns its relative public URL.</summary>
    Task<string> SaveFileAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default);

    /// <summary>Deletes a file given its relative URL.</summary>
    Task DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default);
}
