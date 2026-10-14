using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mooncake.EcommercePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CompleteRfqBidWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "decline_reason",
                table: "rfq_invitations",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "awarded_at",
                table: "request_for_quotations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "awarded_bid_id",
                table: "request_for_quotations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancel_reason",
                table: "request_for_quotations",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "selection_note",
                table: "request_for_quotations",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "accepted_at",
                table: "bids",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancel_reason",
                table: "bids",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "bids_one_accepted_per_rfq_key",
                table: "bids",
                column: "rfq_id",
                unique: true,
                filter: "(status = 'Accepted' AND is_deleted = false)");

            migrationBuilder.CreateIndex(
                name: "bids_rfq_id_supplier_id_key",
                table: "bids",
                columns: new[] { "rfq_id", "supplier_id" },
                unique: true,
                filter: "(is_deleted = false)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "bids_one_accepted_per_rfq_key",
                table: "bids");

            migrationBuilder.DropIndex(
                name: "bids_rfq_id_supplier_id_key",
                table: "bids");

            migrationBuilder.DropColumn(
                name: "decline_reason",
                table: "rfq_invitations");

            migrationBuilder.DropColumn(
                name: "awarded_at",
                table: "request_for_quotations");

            migrationBuilder.DropColumn(
                name: "awarded_bid_id",
                table: "request_for_quotations");

            migrationBuilder.DropColumn(
                name: "cancel_reason",
                table: "request_for_quotations");

            migrationBuilder.DropColumn(
                name: "selection_note",
                table: "request_for_quotations");

            migrationBuilder.DropColumn(
                name: "accepted_at",
                table: "bids");

            migrationBuilder.DropColumn(
                name: "cancel_reason",
                table: "bids");
        }
    }
}
