namespace Mooncake.EcommercePlatform.Infrastructure.Options;

/// <summary>Cloudinary settings bound from the Cloudinary configuration section.</summary>
public sealed class CloudinaryOptions
{
    public const string SectionName = "Cloudinary";
    public const string InvalidConfigurationMessage = "Cloudinary configuration is invalid.";

    /// <summary>Cloudinary product environment name.</summary>
    public string CloudName { get; set; } = null!;
    /// <summary>Server-side API key.</summary>
    public string ApiKey { get; set; } = null!;
    /// <summary>Server-side API secret; never return or log this value.</summary>
    public string ApiSecret { get; set; } = null!;
    /// <summary>Requires HTTPS delivery URLs.</summary>
    public bool Secure { get; set; }
    /// <summary>The product environment's existing Dynamic or Fixed folder mode.</summary>
    public string FolderMode { get; set; } = null!;
    /// <summary>Media Library folder for public images.</summary>
    public string PublicImagesFolder { get; set; } = null!;
    /// <summary>Media Library folder for protected images.</summary>
    public string PrivateImagesFolder { get; set; } = null!;
    /// <summary>Media Library folder for raw files; all files are protected.</summary>
    public string FilesFolder { get; set; } = null!;
    /// <summary>Maximum actual image byte count.</summary>
    public long MaxImageSizeBytes { get; set; }
    /// <summary>Maximum actual raw file byte count.</summary>
    public long MaxFileSizeBytes { get; set; }
    /// <summary>Total time budget for reading and uploading content.</summary>
    public int UploadTimeoutSeconds { get; set; }
    /// <summary>Allowed image extensions, compared without case.</summary>
    public string[] AllowedImageExtensions { get; set; } = null!;
    /// <summary>Allowed raw file extensions, compared without case.</summary>
    public string[] AllowedFileExtensions { get; set; } = null!;

    /// <summary>Validates configuration without making network requests.</summary>
    public bool IsValid() => HasCredential(CloudName) && HasCredential(ApiKey) && HasCredential(ApiSecret)
        && CloudName.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_')
        && Secure && FolderMode is "Dynamic" or "Fixed"
        && HasValidFolder(PublicImagesFolder) && HasValidFolder(PrivateImagesFolder) && HasValidFolder(FilesFolder)
        && new[] { PublicImagesFolder, PrivateImagesFolder, FilesFolder }.Distinct(StringComparer.OrdinalIgnoreCase).Count() == 3
        && MaxImageSizeBytes is > 0 and <= 64 * 1024 * 1024
        && MaxFileSizeBytes is > 0 and <= 64 * 1024 * 1024 && UploadTimeoutSeconds is > 0 and <= 300
        && HasValidExtensions(AllowedImageExtensions) && HasValidExtensions(AllowedFileExtensions);

    private static bool HasCredential(string? value) => !string.IsNullOrWhiteSpace(value)
        && value == value.Trim() && !value.Contains('<') && !value.Contains('>')
        && !value.Contains("your_", StringComparison.OrdinalIgnoreCase)
        && !value.Equals("changeme", StringComparison.OrdinalIgnoreCase);

    private static bool HasValidFolder(string? value) => !string.IsNullOrWhiteSpace(value)
        && value == value.Trim() && value.Split('/').All(segment => segment.Length > 0 && segment is not ("." or "..")
            && segment.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_'));

    private static bool HasValidExtensions(string[]? values) => values is { Length: > 0 }
        && values.All(value => value is { Length: > 1 } && value[0] == '.' && value[1..].All(char.IsAsciiLetterOrDigit));
}
