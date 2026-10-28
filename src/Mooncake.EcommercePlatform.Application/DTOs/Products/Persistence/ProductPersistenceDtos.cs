namespace Mooncake.EcommercePlatform.Application.DTOs.Products.Persistence;

using Mooncake.EcommercePlatform.Domain.Enums;

public sealed record ProductAccountStateDto(Guid UserId, UserRole Role, bool IsActive, bool IsDeleted,
    Guid? ProfileId, SupplierVerificationStatus? VerificationStatus, bool ProfileDeleted,
    Guid? ShopId, ShopStatus? ShopStatus, bool ShopDeleted, decimal ReputationScore);
public sealed record ProductCategoryStateDto(Guid Id, string Name, bool IsDeleted);
public sealed record ProductVariantStateDto(Guid Id, Guid ProductId, Guid ShopId, string Name, string Sku,
    string NormalizedSku, string? CrustType, string? Filling, string? SizeLabel, int? WeightGram, bool IsGiftSet,
    decimal PriceAdjustment, int StockQty, long InventoryVersion, Guid? ImageAssetId, string? ImageUrl,
    bool IsDeleted, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
public sealed record ProductImageStateDto(Guid Id, Guid ProductId, Guid? MediaAssetId, string ImageUrl,
    bool IsPrimary, int SortOrder, bool MediaReady, bool IsDeleted, DateTime CreatedAtUtc);
public sealed record ProductStateDto(Guid Id, Guid ShopId, Guid CategoryId, string CategoryName, bool CategoryActive,
    long Version, string Name, string NormalizedName, string? Description, string? Ingredients, decimal BasePrice, int MinOrderQty,
    int? MaxOrderQty, string Unit, bool SupportsCustomPackaging, bool IsPublished, ProductStatus Status,
    DateTime? PublishedAtUtc, bool IsDeleted, DateTime CreatedAtUtc, DateTime UpdatedAtUtc,
    IReadOnlyList<ProductVariantStateDto> Variants, IReadOnlyList<ProductImageStateDto> Images);
public sealed record ProductMediaStateDto(Guid Id, Guid OwnerUserId, string Purpose, string Status,
    string? PublicUrl, long ByteCount, int Width, int Height, bool IsDeleted);
public sealed record ProductReplayDto(string RequestHash, string ResponseJson, DateTime ExpiresAtUtc);
public sealed record ProductLockScopeDto(Guid ActorUserId, Guid? ProductId = null, Guid? VariantId = null,
    IReadOnlyList<Guid>? MediaIds = null);
public sealed record ProductAggregateWriteDto(ProductStateDto State, long? ExpectedVersion,
    Guid ActorUserId, DateTime NowUtc);
public sealed record ProductInventoryWriteDto(Guid ProductId, Guid VariantId, Guid ShopId, Guid ActorUserId,
    long ExpectedProductVersion, int Delta, string Reason, string SourceId, DateTime NowUtc);
public sealed record ProductInventoryResultDto(Guid MovementId, int PreviousQty, int CurrentQty,
    long InventoryVersion, long ProductVersion, ProductStatus EffectiveStatus);
public sealed record ProductReplayQueryDto(Guid ActorUserId, string Operation, Guid ResourceId, Guid Key);
public sealed record ProductReplayWriteDto(ProductReplayQueryDto Scope, string RequestHash,
    string ResponseJson, DateTime ExpiresAtUtc, DateTime NowUtc);
public sealed record ProductAuditWriteDto(Guid ActorUserId, Guid ResourceId, string Action,
    string MetadataJson, DateTime NowUtc);
public sealed record ProductOutboxWriteDto(Guid ProductId, long SourceVersion, string EventType, DateTime NowUtc);
