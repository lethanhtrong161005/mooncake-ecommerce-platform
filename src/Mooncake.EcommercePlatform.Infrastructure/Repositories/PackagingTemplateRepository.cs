namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

public class PackagingTemplateRepository(ApplicationDbContext db) : IPackagingTemplateRepository
{
    private static bool _tableEnsured = false;
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task EnsureTableAndSeedAsync(CancellationToken ct = default)
    {
        if (_tableEnsured) return;

        await _semaphore.WaitAsync(ct);
        try
        {
            if (_tableEnsured) return;

            const string createSql = """
                CREATE TABLE IF NOT EXISTS packaging_templates (
                    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                    shop_id UUID NOT NULL,
                    name VARCHAR(255) NOT NULL,
                    description TEXT,
                    material VARCHAR(150),
                    dimensions VARCHAR(100),
                    capacity INT NOT NULL DEFAULT 4,
                    base_price NUMERIC(12,2) NOT NULL DEFAULT 0,
                    sample_image_url TEXT,
                    technique VARCHAR(150),
                    min_order_qty INT NOT NULL DEFAULT 20,
                    is_active BOOLEAN NOT NULL DEFAULT TRUE,
                    sort_order INT NOT NULL DEFAULT 0,
                    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
                    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
                    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
                    created_by UUID,
                    updated_by UUID
                );
                ALTER TABLE packaging_templates ADD COLUMN IF NOT EXISTS created_by UUID;
                ALTER TABLE packaging_templates ADD COLUMN IF NOT EXISTS updated_by UUID;
                CREATE INDEX IF NOT EXISTS idx_packaging_templates_shop_id ON packaging_templates(shop_id);
            """;

            await db.Database.ExecuteSqlRawAsync(createSql, ct);
            _tableEnsured = true;
        }
        catch
        {
            _tableEnsured = true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<Guid?> GetShopIdForUserAsync(Guid actorUserId, CancellationToken ct = default)
    {
        return await db.Shops.AsNoTracking()
            .Where(s => s.OwnerUserId == actorUserId && !s.IsDeleted)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<PackagingTemplate>> GetByShopIdAsync(Guid shopId, CancellationToken ct = default)
    {
        var list = await db.PackagingTemplates.AsNoTracking()
            .Where(x => x.ShopId == shopId && !x.IsDeleted && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(ct);

        if (list.Count == 0)
        {
            await SeedDefaultTemplatesAsync(shopId, ct);
            list = await db.PackagingTemplates.AsNoTracking()
                .Where(x => x.ShopId == shopId && !x.IsDeleted && x.IsActive)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .ToListAsync(ct);
        }

        return list;
    }

    public async Task<IReadOnlyList<PackagingTemplate>> GetMineAsync(Guid actorUserId, CancellationToken ct = default)
    {
        var shopId = await GetShopIdForUserAsync(actorUserId, ct);
        if (!shopId.HasValue) return [];

        var list = await db.PackagingTemplates.AsNoTracking()
            .Where(x => x.ShopId == shopId.Value && !x.IsDeleted)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(ct);

        if (list.Count == 0)
        {
            await SeedDefaultTemplatesAsync(shopId.Value, ct);
            list = await db.PackagingTemplates.AsNoTracking()
                .Where(x => x.ShopId == shopId.Value && !x.IsDeleted)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.CreatedAt)
                .ToListAsync(ct);
        }

        return list;
    }

    public async Task<PackagingTemplate?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await db.PackagingTemplates
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<PackagingTemplate> CreateAsync(PackagingTemplate entity, CancellationToken ct = default)
    {
        db.PackagingTemplates.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<PackagingTemplate> UpdateAsync(PackagingTemplate entity, CancellationToken ct = default)
    {
        db.PackagingTemplates.Update(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task DeleteAsync(PackagingTemplate entity, CancellationToken ct = default)
    {
        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        db.PackagingTemplates.Update(entity);
        await db.SaveChangesAsync(ct);
    }

    private async Task SeedDefaultTemplatesAsync(Guid shopId, CancellationToken ct)
    {
        var defaults = new List<PackagingTemplate>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ShopId = shopId,
                Name = "Hộp Sơn Mài Thượng Hạng (4 bánh)",
                Description = "Gỗ phủ sơn mài nghệ nhân bóng mịn, khay nhung đỏ sang trọng, nắp hít nam châm kín khít.",
                Material = "Gỗ tự nhiên phủ sơn mài bóng",
                Dimensions = "32 x 32 x 8 cm",
                Capacity = 4,
                BasePrice = 180000,
                SampleImageUrl = "https://images.unsplash.com/photo-1578985545062-69928b1d9587?auto=format&fit=crop&w=600&q=80",
                Technique = "Ép kim vàng 24K & Khắc laser nắp gỗ",
                MinOrderQty = 20,
                IsActive = true,
                SortOrder = 1
            },
            new()
            {
                Id = Guid.NewGuid(),
                ShopId = shopId,
                Name = "Hộp VIP Dạ Nguyệt Quai Da (6 bánh + Trà)",
                Description = "Khung gỗ bọc da Ý cao cấp, quai xách kim loại mạ vàng, kèm hũ trà sen Tây Hồ và thiệp thư pháp.",
                Material = "Khung gỗ bọc da thật dập vân",
                Dimensions = "38 x 28 x 9.5 cm",
                Capacity = 6,
                BasePrice = 240000,
                SampleImageUrl = "https://images.unsplash.com/photo-1549465220-1a8b9238cd48?auto=format&fit=crop&w=600&q=80",
                Technique = "Ép kim vàng 24K & Phụ kiện mạ đồng",
                MinOrderQty = 30,
                IsActive = true,
                SortOrder = 2
            },
            new()
            {
                Id = Guid.NewGuid(),
                ShopId = shopId,
                Name = "Hộp Gấm Cung Đình (4 bánh)",
                Description = "Vải gấm dệt hoa văn hoàng gia cổ truyền, khóa đồng chạm khắc tỉ mỉ, lót lụa tơ tằm mềm mại.",
                Material = "Vải gấm dệt tơ tằm hoàng cung",
                Dimensions = "30 x 30 x 7.5 cm",
                Capacity = 4,
                BasePrice = 150000,
                SampleImageUrl = "https://images.unsplash.com/photo-1513519245088-0e12902e5a38?auto=format&fit=crop&w=600&q=80",
                Technique = "Thêu chỉ kim tuyến & Khóa đồng chạm trổ",
                MinOrderQty = 20,
                IsActive = true,
                SortOrder = 3
            },
            new()
            {
                Id = Guid.NewGuid(),
                ShopId = shopId,
                Name = "Hộp Giấy Mỹ Thuật Ép Kim (4 bánh)",
                Description = "Giấy mỹ thuật dập nổi hoa văn truyền thống, ép kim nhũ vàng, tối ưu ngân sách cho doanh nghiệp.",
                Material = "Giấy mỹ thuật bồi carton cứng",
                Dimensions = "28 x 28 x 7 cm",
                Capacity = 4,
                BasePrice = 85000,
                SampleImageUrl = "https://images.unsplash.com/photo-1528459801416-a9e53bbf4e17?auto=format&fit=crop&w=600&q=80",
                Technique = "Ép kim vàng nhũ & Dập nổi hoa văn",
                MinOrderQty = 50,
                IsActive = true,
                SortOrder = 4
            }
        };

        db.PackagingTemplates.AddRange(defaults);
        await db.SaveChangesAsync(ct);
    }
}

