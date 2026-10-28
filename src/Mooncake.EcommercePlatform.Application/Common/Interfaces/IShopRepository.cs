namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Shops.Persistence;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Responses;

/// <summary>Shop persistence exposes snapshots and commands, never tracked entities or queryables.</summary>
public interface IShopTransaction
{
    Task<ShopAccountStateDto?> GetAccountAsync(ShopAccountQueryDto query, CancellationToken ct);
    Task<ShopStateDto?> GetOwnerShopAsync(ShopOwnerQueryDto query, CancellationToken ct);
    Task<ShopStateDto?> GetShopAsync(ShopIdQueryDto query, CancellationToken ct);
    Task<ShopStateDto?> GetShopBySlugAsync(ShopSlugQueryDto query, CancellationToken ct);
    Task<ShopDisplayDto?> GetDisplayAsync(ShopDisplayQueryDto query, CancellationToken ct);
    Task<TemplateStateDto?> GetTemplateAsync(ShopIdQueryDto query, CancellationToken ct);
    Task<TemplateRevisionDto?> GetRevisionAsync(ShopRevisionQueryDto query, CancellationToken ct);
    Task<ShopCatalogStateDto> GetCatalogAsync(CancellationToken ct);
    Task<ShopMediaStateDto?> GetMediaAsync(ShopMediaQueryDto query, CancellationToken ct);
    Task<ShopMediaStateDto?> GetUploadAsync(ShopUploadQueryDto query, CancellationToken ct);
    Task<ShopUploadUsageDto> GetUploadUsageAsync(ShopActorDto actor, DateTime since, DateTime activeSince, CancellationToken ct);
    Task<int> CountCommandsAsync(ShopActorDto actor, string operation, DateTime since, CancellationToken ct);
    Task<ShopReplayDto?> GetReplayAsync(ShopReplayQueryDto query, CancellationToken ct);
    Task WriteShopAsync(ShopWriteDto command, CancellationToken ct);
    Task WriteTemplateAsync(TemplateWriteDto command, CancellationToken ct);
    Task WriteMediaAsync(ShopMediaWriteDto command, CancellationToken ct);
    Task AppendAuditAsync(ShopAuditWriteDto command, CancellationToken ct);
    Task StoreReplayAsync(ShopReplayWriteDto command, CancellationToken ct);
    Task<ShopPageResponse<AdminShopSummaryResponse>> ListShopsAsync(AdminShopQuery query, CancellationToken ct);
    Task<ShopPageResponse<TemplateStateDto>> ListTemplatesAsync(ShopTemplatePageQueryDto query, CancellationToken ct);
    Task<IReadOnlyList<ShopProductCardResponse>> ListProductsAsync(ShopProductPageQueryDto query, CancellationToken ct);
    Task<IReadOnlyList<ShopCategoryResponse>> ListCategoriesAsync(ShopRelatedPageQueryDto query, CancellationToken ct);
    Task<IReadOnlyList<ShopReviewResponse>> ListReviewsAsync(ShopRelatedPageQueryDto query, CancellationToken ct);
    Task<ShopReviewStatsDto> ReviewStatsAsync(ShopIdQueryDto supplierProfile, CancellationToken ct);
    Task<bool> IsMediaReferencedAsync(ShopMediaQueryDto query, CancellationToken ct);
}

public interface IShopRepository : IShopTransaction
{
    Task<IReadOnlyList<ShopMediaStateDto>> ListCleanupCandidatesAsync(ShopCleanupQueryDto query, CancellationToken ct);
    Task<int> DeleteExpiredReplaysAsync(ShopReplayCleanupDto query, CancellationToken ct);
    Task<T> InTransactionAsync<T>(ShopLockScopeDto scope, Func<IShopTransaction, CancellationToken, Task<T>> work, CancellationToken ct);
    Task<T> InReadSnapshotAsync<T>(Func<IShopTransaction, CancellationToken, Task<T>> work, CancellationToken ct);
}

public sealed record ShopCursorScopeDto(string Kind, Guid ResourceId, string FilterHash);
public interface IShopCursorProtector
{
    string Protect(ShopCursorScopeDto scope, ShopCursorDto cursor);
    ShopCursorDto Unprotect(ShopCursorScopeDto scope, string token);
}

public sealed record ShopProcessedMediaDto(byte[] Content, string FileName, string ContentType, int Width, int Height);
public sealed record ShopUploadedMediaDto(string Url, string PublicId);
public interface IShopMediaGateway
{
    Task<ShopProcessedMediaDto> PrepareAsync(byte[] content, string purpose, CancellationToken ct);
    Task<ShopUploadedMediaDto> UploadAsync(ShopProcessedMediaDto media, CancellationToken ct);
    Task DeleteAsync(string publicId, CancellationToken ct);
}
