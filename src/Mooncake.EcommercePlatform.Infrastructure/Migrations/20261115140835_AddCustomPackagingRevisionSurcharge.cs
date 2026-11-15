using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mooncake.EcommercePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomPackagingRevisionSurcharge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "RevisionSurcharge",
                table: "custom_packaging_revisions",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RevisionSurcharge",
                table: "custom_packaging_revisions");
        }
    }
}
