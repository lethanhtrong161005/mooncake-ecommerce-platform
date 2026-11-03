namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Packaging;

public interface IPackagingTemplateService
{
    Task<IReadOnlyList<PackagingTemplateResponse>> GetByShopIdAsync(Guid shopId, CancellationToken ct = default);
    Task<IReadOnlyList<PackagingTemplateResponse>> GetMineAsync(Guid actorUserId, CancellationToken ct = default);
    Task<PackagingTemplateResponse> CreateAsync(Guid actorUserId, CreatePackagingTemplateRequest request, CancellationToken ct = default);
    Task<PackagingTemplateResponse> UpdateAsync(Guid actorUserId, Guid id, UpdatePackagingTemplateRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid actorUserId, Guid id, CancellationToken ct = default);
}

