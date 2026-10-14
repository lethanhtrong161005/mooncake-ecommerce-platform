using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mooncake.EcommercePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueContractBid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "contracts_bid_id_key",
                table: "contracts",
                column: "bid_id",
                unique: true,
                filter: "(bid_id IS NOT NULL AND is_deleted = false)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "contracts_bid_id_key",
                table: "contracts");
        }
    }
}
