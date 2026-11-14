namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using Mooncake.EcommercePlatform.Application.Common.DTOs;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Packaging;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;

public sealed class PackagingTemplateService(
    IPackagingTemplateRepository repository,
    IDateTimeProvider dateTimeProvider,
    ICloudinaryStorageService? cloudinaryStorage = null,
    IPackagingTemplateImageProcessor? imageProcessor = null) : IPackagingTemplateService
{
    private const long MaxImageSizeBytes = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };
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

    public async Task<PagedResult<PackagingTemplateResponse>> GetMinePageAsync(
        Guid actorUserId,
        PackagingTemplatePageQueryRequest query,
        CancellationToken ct = default)
    {
        PagedQueryHelper.Validate(new PagedQuery
        {
            Page = query.Page,
            PageSize = query.PageSize,
            Q = query.Q,
            Sort = query.Sort
        }, maxPageSize: 5);

        if (query.Material?.Length > 100 || (query.Capacity.HasValue && query.Capacity is < 1 or > 100))
        {
            throw new HttpException(400, "Invalid pagination or filter parameters.", "VALIDATION_ERROR");
        }

        PagedQueryHelper.ValidateSort(query.Sort, "createdAtDesc", "createdAtAsc", "priceAsc", "priceDesc", "nameAsc");

        await repository.EnsureTableAndSeedAsync(ct);
        var page = await repository.GetMinePageAsync(actorUserId, query, ct);
        return new PagedResult<PackagingTemplateResponse>(
            page.Items.Select(ToResponse).ToList(),
            page.Page,
            page.PageSize,
            page.TotalCount);
    }

    public async Task<PackagingTemplateImageUploadResponse> UploadImageAsync(
        Guid actorUserId,
        Stream? content,
        string? fileName,
        long length,
        CancellationToken ct = default)
    {
        if (cloudinaryStorage is null)
        {
            throw new HttpException(503, "Dịch vụ lưu trữ ảnh hiện không khả dụng. Vui lòng thử lại sau.");
        }

        var shopId = await repository.GetShopIdForUserAsync(actorUserId, ct);
        if (!shopId.HasValue)
        {
            throw new HttpException(404, "Không tìm thấy gian hàng của nhà cung cấp này.");
        }

        var (sanitizedBytes, normalizedFileName) = await ReadAndValidateImageAsync(content, fileName, length, ct);

        try
        {
            await using var uploadStream = new MemoryStream(sanitizedBytes, writable: false);
            var uploaded = await cloudinaryStorage.UploadImageAsync(
                uploadStream, normalizedFileName, isPrivate: false, ct);

            if (string.IsNullOrWhiteSpace(uploaded.Url))
            {
                throw new HttpException(502, "Không thể lấy đường dẫn ảnh sau khi tải lên.");
            }

            return new PackagingTemplateImageUploadResponse(uploaded.Url, uploaded.PublicId);
        }
        catch (HttpException)
        {
            throw;
        }
        catch (ArgumentException ex)
        {
            throw new HttpException(400, ex.Message);
        }
        catch (TimeoutException)
        {
            throw new HttpException(504, "Tải ảnh mẫu hộp quá thời gian chờ. Vui lòng thử lại.");
        }
        catch (InvalidOperationException)
        {
            throw new HttpException(502, "Không thể tải ảnh mẫu hộp lên dịch vụ lưu trữ. Vui lòng thử lại sau.");
        }
    }

    private async Task<(byte[] Bytes, string FileName)> ReadAndValidateImageAsync(
        Stream? content,
        string? fileName,
        long length,
        CancellationToken ct)
    {
        if (content is null || length <= 0)
        {
            throw new HttpException(400, "Vui lòng chọn tệp ảnh hợp lệ.");
        }

        if (length > MaxImageSizeBytes)
        {
            throw new HttpException(400, "Dung lượng ảnh không được vượt quá 5MB.");
        }

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            throw new HttpException(400, "Chỉ hỗ trợ ảnh định dạng JPG, JPEG, PNG hoặc WEBP.");
        }

        using var buffer = new MemoryStream((int)Math.Min(length, MaxImageSizeBytes));
        var chunk = new byte[16 * 1024];
        long totalRead = 0;

        while (true)
        {
            var read = await content.ReadAsync(chunk, ct);
            if (read == 0)
            {
                break;
            }

            totalRead += read;
            if (totalRead > MaxImageSizeBytes)
            {
                throw new HttpException(400, "Dung lượng ảnh không được vượt quá 5MB.");
            }

            buffer.Write(chunk, 0, read);
        }

        if (totalRead == 0)
        {
            throw new HttpException(400, "Tệp ảnh bị rỗng.");
        }

        var rawBytes = buffer.ToArray();
        return imageProcessor is not null
            ? imageProcessor.SanitizeAndReencode(rawBytes, MaxImageSizeBytes)
            : (rawBytes, Path.GetFileName(fileName) ?? "packaging-sample.jpg");
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

