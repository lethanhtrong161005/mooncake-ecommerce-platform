using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mooncake.EcommercePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRefundBankAndTransferProof : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "customer_bank_snapshot_json",
                table: "order_refund_cases",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "transfer_proof_image_url",
                table: "order_refund_cases",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "refund_bank_snapshot_json",
                table: "order_delivery_disputes",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "customer_bank_snapshot_json",
                table: "order_refund_cases");

            migrationBuilder.DropColumn(
                name: "transfer_proof_image_url",
                table: "order_refund_cases");

            migrationBuilder.DropColumn(
                name: "refund_bank_snapshot_json",
                table: "order_delivery_disputes");
        }
    }
}
