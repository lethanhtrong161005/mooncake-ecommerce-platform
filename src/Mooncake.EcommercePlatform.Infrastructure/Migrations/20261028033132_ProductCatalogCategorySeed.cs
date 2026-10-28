using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mooncake.EcommercePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProductCatalogCategorySeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO categories (id, parent_id, name, description, icon_url, sort_order, is_deleted, created_at, updated_at)
                VALUES
                    ('7f5e3f83-2d3b-4f43-9dd1-8f8d32b9a101', NULL, 'Bánh nướng truyền thống', 'Thập cẩm, gà quay, vi cá và các dòng bánh nướng cao cấp.', '/categories/banh-nuong.jpg', 10, false, now(), now()),
                    ('7f5e3f83-2d3b-4f43-9dd1-8f8d32b9a102', NULL, 'Bánh dẻo truyền thống', 'Hạt sen, hoa bưởi, đậu xanh và cốm non.', '/categories/banh-deo.jpg', 20, false, now(), now()),
                    ('7f5e3f83-2d3b-4f43-9dd1-8f8d32b9a103', NULL, 'Hộp quà cao cấp', 'Hộp sơn mài, khóa đồng và tùy chỉnh logo doanh nghiệp.', '/categories/hop-son-mai.jpg', 30, false, now(), now()),
                    ('7f5e3f83-2d3b-4f43-9dd1-8f8d32b9a104', NULL, 'Bánh dinh dưỡng ít ngọt', 'Đường ăn kiêng tự nhiên và các loại hạt dinh dưỡng.', '/categories/banh-dinh-duong.jpg', 40, false, now(), now())
                ON CONFLICT (id) DO UPDATE SET
                    name = EXCLUDED.name,
                    description = EXCLUDED.description,
                    icon_url = EXCLUDED.icon_url,
                    sort_order = EXCLUDED.sort_order,
                    is_deleted = false,
                    updated_at = now();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM categories
                WHERE id IN (
                    '7f5e3f83-2d3b-4f43-9dd1-8f8d32b9a101',
                    '7f5e3f83-2d3b-4f43-9dd1-8f8d32b9a102',
                    '7f5e3f83-2d3b-4f43-9dd1-8f8d32b9a103',
                    '7f5e3f83-2d3b-4f43-9dd1-8f8d32b9a104'
                ) AND NOT EXISTS (SELECT 1 FROM products WHERE products.category_id = categories.id);
                """);
        }
    }
}
