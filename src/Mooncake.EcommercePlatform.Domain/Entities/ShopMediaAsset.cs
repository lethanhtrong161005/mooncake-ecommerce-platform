namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>Owner-scoped proof of a decoded public branding upload.</summary>
public sealed class ShopMediaAsset : BaseEntity
{
    public Guid OwnerUserId { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public Guid UploadOperationId { get; set; }
    public string RequestHash { get; set; } = string.Empty;
    public string Status { get; set; } = "Uploading";
    public Guid LeaseToken { get; set; }
    public string? Sha256 { get; set; }
    public string? ProviderPublicId { get; set; }
    public string? PublicUrl { get; set; }
    public string? ContentType { get; set; }
    public long ByteCount { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}
