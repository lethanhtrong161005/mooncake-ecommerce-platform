namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Products.Persistence;
using Mooncake.EcommercePlatform.Application.DTOs.Products.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Products.Responses;

/// <summary>Catalog transaction boundary exposes DTO snapshots/commands and never tracked entities.</summary>
public interface IProductCatalogTransaction
{
    Task<ProductAccountStateDto?> GetAccountAsync(Guid actorUserId, CancellationToken ct);
    Task<ProductStateDto?> GetProductAsync(Guid productId, CancellationToken ct);
    Task<ProductCategoryStateDto?> GetCategoryAsync(Guid categoryId, CancellationToken ct);
    Task<IReadOnlyList<ProductMediaStateDto>> GetMediaAsync(IReadOnlyCollection<Guid> mediaIds, CancellationToken ct);
    Task<bool> IsMediaBoundAsync(Guid mediaId, Guid? excludingProductId, CancellationToken ct);
    Task<ProductReplayDto?> GetReplayAsync(ProductReplayQueryDto query, CancellationToken ct);
    Task SaveAggregateAsync(ProductAggregateWriteDto command, CancellationToken ct);
    Task<ProductInventoryResultDto> AdjustInventoryAsync(ProductInventoryWriteDto command, CancellationToken ct);
    Task AppendAuditAsync(ProductAuditWriteDto command, CancellationToken ct);
    Task AppendOutboxAsync(ProductOutboxWriteDto command, CancellationToken ct);
    Task StoreReplayAsync(ProductReplayWriteDto command, CancellationToken ct);
}

public interface IProductCatalogRepository : IProductCatalogTransaction
{
    Task<T> InTransactionAsync<T>(ProductLockScopeDto scope,
        Func<IProductCatalogTransaction, CancellationToken, Task<T>> work, CancellationToken ct);
    Task<ProductPageResponse<ProductOwnerCardResponse>> ListMineAsync(Guid actorUserId, OwnerProductQuery query, CancellationToken ct);
    Task<ProductStateDto?> GetMineAsync(Guid actorUserId, Guid productId, CancellationToken ct);
    Task<ProductPublicDetailResponse?> GetPublicAsync(Guid productId, CancellationToken ct);
    Task<ProductSearchResponse> SearchAsync(ProductSearchQuery query, CancellationToken ct);
    Task<IReadOnlyList<ProductAutocompleteResponse>> AutocompleteAsync(ProductAutocompleteQuery query, CancellationToken ct);
    Task<IReadOnlyList<ProductSearchCardResponse>> RelatedAsync(Guid productId, int limit, Guid? userId, CancellationToken ct);
    Task<IReadOnlyList<ProductSearchCardResponse>> RecommendationsAsync(int limit, Guid? userId, CancellationToken ct);
    Task RecordViewAsync(Guid productId, Guid? userId, Guid? sessionId, string source, DateTime nowUtc, CancellationToken ct);
    Task<IReadOnlyList<CategoryCatalogResponse>> CategoriesAsync(CancellationToken ct);
    Task<IReadOnlyList<RegionCatalogResponse>> RegionsAsync(CancellationToken ct);
    Task<int> ProjectPendingAsync(int batchSize, DateTime nowUtc, CancellationToken ct);
}
