namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

/// <summary>Validates and re-encodes uploaded packaging sample images.</summary>
public interface IPackagingTemplateImageProcessor
{
    /// <summary>Decodes, validates dimensions/format, and strips metadata from an image byte payload.</summary>
    (byte[] Bytes, string FileName) SanitizeAndReencode(byte[] rawBytes, long maxBytes);
}

