using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mooncake.EcommercePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CategoryCloudinaryImageUrls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE categories
                SET icon_url = CASE id
                    WHEN '7f5e3f83-2d3b-4f43-9dd1-8f8d32b9a101' THEN 'https://res.cloudinary.com/t8rh8x0i/image/upload/mooncake/images/public/category-banh-nuong.jpg'
                    WHEN '7f5e3f83-2d3b-4f43-9dd1-8f8d32b9a102' THEN 'https://res.cloudinary.com/t8rh8x0i/image/upload/mooncake/images/public/category-banh-deo.jpg'
                    WHEN '7f5e3f83-2d3b-4f43-9dd1-8f8d32b9a103' THEN 'https://res.cloudinary.com/t8rh8x0i/image/upload/mooncake/images/public/category-hop-son-mai.jpg'
                    WHEN '7f5e3f83-2d3b-4f43-9dd1-8f8d32b9a104' THEN 'https://res.cloudinary.com/t8rh8x0i/image/upload/mooncake/images/public/category-banh-dinh-duong.jpg'
                    ELSE icon_url
                END,
                updated_at = now()
                WHERE id IN (
                    '7f5e3f83-2d3b-4f43-9dd1-8f8d32b9a101',
                    '7f5e3f83-2d3b-4f43-9dd1-8f8d32b9a102',
                    '7f5e3f83-2d3b-4f43-9dd1-8f8d32b9a103',
                    '7f5e3f83-2d3b-4f43-9dd1-8f8d32b9a104'
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE categories
                SET icon_url = CASE id
                    WHEN '7f5e3f83-2d3b-4f43-9dd1-8f8d32b9a101' THEN '/categories/banh-nuong.jpg'
                    WHEN '7f5e3f83-2d3b-4f43-9dd1-8f8d32b9a102' THEN '/categories/banh-deo.jpg'
                    WHEN '7f5e3f83-2d3b-4f43-9dd1-8f8d32b9a103' THEN '/categories/hop-son-mai.jpg'
                    WHEN '7f5e3f83-2d3b-4f43-9dd1-8f8d32b9a104' THEN '/categories/banh-dinh-duong.jpg'
                    ELSE icon_url
                END,
                updated_at = now()
                WHERE id IN (
                    '7f5e3f83-2d3b-4f43-9dd1-8f8d32b9a101',
                    '7f5e3f83-2d3b-4f43-9dd1-8f8d32b9a102',
                    '7f5e3f83-2d3b-4f43-9dd1-8f8d32b9a103',
                    '7f5e3f83-2d3b-4f43-9dd1-8f8d32b9a104'
                );
                """);
        }
    }
}
