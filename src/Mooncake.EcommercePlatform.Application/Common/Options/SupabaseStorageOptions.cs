namespace Mooncake.EcommercePlatform.Application.Common.Options;

public sealed class SupabaseStorageOptions
{
    public const string SectionName = "SupabaseStorage";

    public string ProjectUrl { get; set; } = string.Empty;
    public string PublishableKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string ContractBucket { get; set; } = "contracts";
    public long MaxContractFileBytes { get; set; } = 10 * 1024 * 1024;
    public int SignedUrlLifetimeSeconds { get; set; } = 300;
}
