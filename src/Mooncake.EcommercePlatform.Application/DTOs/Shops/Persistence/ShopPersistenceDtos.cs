namespace Mooncake.EcommercePlatform.Application.DTOs.Shops.Persistence;

using Mooncake.EcommercePlatform.Application.DTOs.Shops.Requests;
using Mooncake.EcommercePlatform.Domain.Enums;

public sealed record ShopActorDto(Guid UserId);
public sealed record ShopLockScopeDto(Guid ActorUserId, Guid? ShopId = null, Guid? TemplateId = null,
    bool CatalogGuard = false, Guid? MediaId = null);
public sealed record ShopAccountQueryDto(Guid UserId);
public sealed record ShopOwnerQueryDto(Guid OwnerUserId);
public sealed record ShopIdQueryDto(Guid Id);
public sealed record ShopSlugQueryDto(string Slug);
public sealed record ShopDisplayQueryDto(Guid? OwnerUserId = null, string? Slug = null, Guid? ShopId = null);
public sealed record ShopDisplayDto(ShopStateDto Shop, ShopAccountStateDto Account, TemplateStateDto Template, TemplateRevisionDto PinnedRevision);
public sealed record ShopRevisionQueryDto(Guid Id);
public sealed record ShopMediaQueryDto(Guid Id);
public sealed record ShopUploadQueryDto(Guid OwnerUserId, Guid OperationId);
public sealed record ShopAccountStateDto(Guid UserId, UserRole Role, bool IsActive, bool IsDeleted,
    Guid? ProfileId, SupplierVerificationStatus? VerificationStatus, bool ProfileDeleted, decimal ReputationScore);

/// <summary>Only this DTO crosses the persistence boundary; no tracked entity is exposed.</summary>
public record ShopStateDto
{
    public Guid Id { get; init; }
    public Guid SupplierId { get; init; }
    public Guid OwnerUserId { get; init; }
    public long Version { get; init; }
    public bool IsDeleted { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Description { get; init; }
    public ShopStatus Status { get; init; }
    public Guid TemplateId { get; init; }
    public Guid TemplateRevisionId { get; init; }
    public ThemeOverridesRequest Overrides { get; init; } = new();
    public Guid? LogoAssetId { get; init; }
    public Guid? BannerAssetId { get; init; }
    public string? LogoUrl { get; init; }
    public string? BannerUrl { get; init; }
    public string? PublicContactEmail { get; init; }
    public string? PublicContactPhone { get; init; }
    public string? PublicAddress { get; init; }
    public string? RegionCode { get; init; }
    public ShopStatus? PreSuspensionStatus { get; init; }
    public string? SuspensionReason { get; init; }
    public DateTime? SuspendedAtUtc { get; init; }
    public Guid? SuspendedByUserId { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}

public sealed record ShopWriteDto(ShopStateDto State, long? ExpectedVersion, Guid ActorUserId, DateTime Now);
public sealed record TemplateRevisionDto(Guid Id, Guid TemplateId, int Revision, string RendererKey,
    int SchemaVersion, ThemeOverridesRequest Defaults, TemplateLayoutRequest Layout);
public sealed record TemplateStateDto(Guid Id, string Code, string Name, string? Description, long Version,
    bool IsActive, bool IsPremium, int SortOrder, Guid? PreviewAssetId, string? PreviewImageUrl,
    TemplateRevisionDto? CurrentRevision, bool IsDeleted = false);
public sealed record TemplateWriteDto(TemplateStateDto State, long? ExpectedVersion, Guid ActorUserId,
    DateTime Now, TemplateRevisionDto? NewRevision = null);
public sealed record ShopCatalogStateDto(Guid DefaultTemplateId, long Version, int ActiveCount);
public sealed record ShopMediaStateDto(Guid Id, Guid OwnerUserId, Guid UploadOperationId, string Purpose,
    string RequestHash, string Status, string? PublicUrl, string? ProviderPublicId, string? Sha256,
    string? ContentType, long ByteCount, int Width, int Height, DateTime UpdatedAtUtc, bool IsDeleted = false, Guid LeaseToken = default);
public sealed record ShopMediaWriteDto(ShopMediaStateDto State, DateTime Now);
public sealed record ShopCleanupQueryDto(DateTime UnattachedBeforeUtc, DateTime RetryBeforeUtc, int BatchSize);
public sealed record ShopReplayCleanupDto(DateTime ExpiredBeforeUtc, int BatchSize);
public sealed record ShopUploadUsageDto(int RecentCount, int ActiveCount);
public sealed record ShopAuditWriteDto(Guid ActorUserId, Guid ResourceId, string Action, string MetadataJson, DateTime Now);
public sealed record ShopReplayQueryDto(Guid ActorUserId, string Operation, Guid ResourceId, Guid Key);
public sealed record ShopReplayDto(string RequestHash, string ResponseJson, int HttpStatus, DateTime ExpiresAtUtc);
public sealed record ShopReplayWriteDto(ShopReplayQueryDto Scope, ShopReplayDto Result, DateTime Now);
public sealed record ShopCursorDto(Guid Id, DateTime CreatedAtUtc = default, decimal Price = 0, string? Name = null, int SortOrder = 0);
public sealed record ShopProductPageQueryDto(Guid ShopId, ShopProductQuery Query, ShopCursorDto? After);
public sealed record ShopRelatedPageQueryDto(Guid ShopId, Guid SupplierId, ShopPageQuery Query, ShopCursorDto? After, short? Rating = null);
public sealed record ShopTemplatePageQueryDto(AdminTemplateQuery Query, bool SelectableOnly, ShopCursorDto? After);
