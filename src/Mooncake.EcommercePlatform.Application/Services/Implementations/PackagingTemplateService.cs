namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Packaging;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;

public sealed class PackagingTemplateService(
    IPackagingTemplateRepository repository,
    IDateTimeProvider dateTimeProvider) : IPackagingTemplateService
{
    public async Task<IReadOnlyList<PackagingTemplateResponse>> GetByShopIdAsync(Guid shopId, CancellationToken ct = default)
    {
        await repository.EnsureTableAndSeedAsync(ct);
        var templates = await repository.GetByShopIdAsync(shopId, ct);
        return templates.Select(ToResponse).ToList();
    }

    public async Task<IReadOnlyList<PackagingTemplateResponse>> GetMineAsync(Guid actorUserId, CancellationToken ct = default)
    {
        await repository.EnsureTableAndSeedAsync(ct);
        var templates = await repository.GetMineAsync(actorUserId, ct);
        return templates.Select(ToResponse).ToList();
    }

    public async Task<PackagingTemplateResponse> CreateAsync(Guid actorUserId, CreatePackagingTemplateRequest request, CancellationToken ct = default)
    {
        await repository.EnsureTableAndSeedAsync(ct);
        var shopId = await repository.GetShopIdForUserAsync(actorUserId, ct);
        if (!shopId.HasValue)
        {
            throw new HttpException(404, "Không tìm thấy gian hàng của nhà cung cấp này.");
        }

        var now = dateTimeProvider.UtcNow;
        var entity = new PackagingTemplate
        {
            Id = Guid.NewGuid(),
            ShopId = shopId.Value,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Material = request.Material?.Trim(),
            Dimensions = request.Dimensions?.Trim(),
            Capacity = request.Capacity > 0 ? request.Capacity : 4,
            BasePrice = Math.Max(0, request.BasePrice),
            SampleImageUrl = request.SampleImageUrl?.Trim(),
            Technique = request.Technique?.Trim(),
            MinOrderQty = Math.Max(1, request.MinOrderQty),
            IsActive = true,
            SortOrder = request.SortOrder,
            CreatedAt = now,
            UpdatedAt = now
        };

        var created = await repository.CreateAsync(entity, ct);
        return ToResponse(created);
    }

    public async Task<PackagingTemplateResponse> UpdateAsync(Guid actorUserId, Guid id, UpdatePackagingTemplateRequest request, CancellationToken ct = default)
    {
        await repository.EnsureTableAndSeedAsync(ct);
        var shopId = await repository.GetShopIdForUserAsync(actorUserId, ct);
        if (!shopId.HasValue)
        {
            throw new HttpException(404, "Không tìm thấy gian hàng của nhà cung cấp này.");
        }

        var existing = await repository.GetByIdAsync(id, ct);
        if (existing == null || existing.ShopId != shopId.Value)
        {
            throw new HttpException(404, "Mẫu bao bì không tồn tại hoặc bạn không có quyền sửa đổi.");
        }

        existing.Name = request.Name.Trim();
        existing.Description = request.Description?.Trim();
        existing.Material = request.Material?.Trim();
        existing.Dimensions = request.Dimensions?.Trim();
        existing.Capacity = request.Capacity > 0 ? request.Capacity : 4;
        existing.BasePrice = Math.Max(0, request.BasePrice);
        existing.SampleImageUrl = request.SampleImageUrl?.Trim();
        existing.Technique = request.Technique?.Trim();
        existing.MinOrderQty = Math.Max(1, request.MinOrderQty);
        existing.IsActive = request.IsActive;
        existing.SortOrder = request.SortOrder;
        existing.UpdatedAt = dateTimeProvider.UtcNow;

        var updated = await repository.UpdateAsync(existing, ct);
        return ToResponse(updated);
    }

    public async Task DeleteAsync(Guid actorUserId, Guid id, CancellationToken ct = default)
    {
        await repository.EnsureTableAndSeedAsync(ct);
        var shopId = await repository.GetShopIdForUserAsync(actorUserId, ct);
        if (!shopId.HasValue)
        {
            throw new HttpException(404, "Không tìm thấy gian hàng của nhà cung cấp này.");
        }

        var existing = await repository.GetByIdAsync(id, ct);
        if (existing == null || existing.ShopId != shopId.Value)
        {
            throw new HttpException(404, "Mẫu bao bì không tồn tại hoặc bạn không có quyền xóa.");
        }

        await repository.DeleteAsync(existing, ct);
    }

    private static PackagingTemplateResponse ToResponse(PackagingTemplate x) =>
        new(
            x.Id,
            x.ShopId,
            x.Name,
            x.Description,
            x.Material,
            x.Dimensions,
            x.Capacity,
            x.BasePrice,
            x.SampleImageUrl,
            x.Technique,
            x.MinOrderQty,
            x.IsActive,
            x.SortOrder,
            x.CreatedAt,
            x.UpdatedAt
        );
}

