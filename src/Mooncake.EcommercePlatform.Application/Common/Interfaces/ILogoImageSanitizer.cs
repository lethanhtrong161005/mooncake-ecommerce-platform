namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

/// <summary>Validates, sanitizes, and re-encodes corporate logo images for custom packaging.</summary>
public interface ILogoImageSanitizer
{
    /// <summary>
    /// Validates extension, MIME type, dimensions, and re-encodes the image stream.
    /// Returns the sanitized image bytes and a server-generated file name.
    /// </summary>
    (byte[] Bytes, string FileName) Sanitize(
        Stream inputStream,
        string? originalFileName,
        string? contentType,
        long maxBytes);

    /// <summary>
    /// Validates extension, MIME type, dimensions, and re-encodes the image byte payload.
    /// Returns the sanitized image bytes and a server-generated file name.
    /// </summary>
    (byte[] Bytes, string FileName) Sanitize(
        byte[] rawBytes,
        string? originalFileName,
        string? contentType,
        long maxBytes);
}
