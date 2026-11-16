using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mooncake.EcommercePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AllowSystemShopSuspensionForExpiredLicense : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_shop_suspension",
                table: "shops");

            migrationBuilder.AddCheckConstraint(
                name: "ck_shop_suspension",
                table: "shops",
                sql: "(status = 'Suspended' AND pre_suspension_status IN ('Active','Inactive') AND suspended_at_utc IS NOT NULL AND (suspended_by_user_id IS NOT NULL OR suspension_reason = 'Giấy ATTP hết hạn') AND length(suspension_reason) >= 10) OR (status <> 'Suspended' AND pre_suspension_status IS NULL AND suspended_at_utc IS NULL AND suspended_by_user_id IS NULL AND suspension_reason IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_shop_suspension",
                table: "shops");

            migrationBuilder.AddCheckConstraint(
                name: "ck_shop_suspension",
                table: "shops",
                sql: "(status = 'Suspended' AND pre_suspension_status IN ('Active','Inactive') AND suspended_at_utc IS NOT NULL AND suspended_by_user_id IS NOT NULL AND length(suspension_reason) >= 10) OR (status <> 'Suspended' AND pre_suspension_status IS NULL AND suspended_at_utc IS NULL AND suspended_by_user_id IS NULL AND suspension_reason IS NULL)");
        }
    }
}
