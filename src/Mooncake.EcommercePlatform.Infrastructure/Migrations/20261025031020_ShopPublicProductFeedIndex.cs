using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mooncake.EcommercePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ShopPublicProductFeedIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "idx_shop_public_product_feed",
                table: "products",
                columns: new[] { "shop_id", "created_at", "id" },
                descending: new[] { false, true, false },
                filter: "is_deleted = false AND status IN ('Active','OutOfStock')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_shop_public_product_feed",
                table: "products");
        }
    }
}
