using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mooncake.EcommercePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemOperationsAndDisputes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:ai_conversation_type", "ProductInfo,MooncakeHistory,SalesAnalytics,General")
                .Annotation("Npgsql:Enum:ai_message_role", "User,Assistant,System")
                .Annotation("Npgsql:Enum:ai_report_status", "Pending,Processing,Completed,Failed")
                .Annotation("Npgsql:Enum:ai_report_type", "SalesTrend,SupplierPerformance,DemandForecast,ProductAnalysis,RevenueSummary")
                .Annotation("Npgsql:Enum:bid_status", "Submitted,Accepted,Cancelled")
                .Annotation("Npgsql:Enum:contract_status", "Draft,PendingSignature,Signed,PendingDeposit,Active,CompletedOnTime,CompletedLate,Breached,Disputed,Cancelled")
                .Annotation("Npgsql:Enum:custom_packaging_status", "Pending,DesignReview,RevisionRequested,Approved,InProduction,Completed,Rejected")
                .Annotation("Npgsql:Enum:delivery_status", "Pending,Confirmed,PickedUp,InTransit,Delivered,Failed,Returned")
                .Annotation("Npgsql:Enum:delivery_type", "PlatformManaged,SelfArranged")
                .Annotation("Npgsql:Enum:discount_type", "Percentage,FixedAmount,FixedPrice")
                .Annotation("Npgsql:Enum:milestone_status", "Pending,AwaitingPayment,Paid,InProgress,PendingReview,CompletedOnTime,CompletedLate,Overdue,Failed")
                .Annotation("Npgsql:Enum:order_status", "Pending,Confirmed,Processing,Shipped,Delivered,Cancelled")
                .Annotation("Npgsql:Enum:payment_method", "BankTransfer,Momo,VNPay,ZaloPay,Cash")
                .Annotation("Npgsql:Enum:payment_status", "Pending,Processing,Completed,Failed,Refunded")
                .Annotation("Npgsql:Enum:product_status", "Active,Inactive,OutOfStock")
                .Annotation("Npgsql:Enum:proof_type", "Pickup,InTransit,Delivery,FailedAttempt")
                .Annotation("Npgsql:Enum:reputation_event", "CompletedOnTime,CompletedLate,Breached,PositiveReview,NegativeReview,AdminPenalty,AdminBonus")
                .Annotation("Npgsql:Enum:rfq_invitation_status", "Invited,Viewed,BidSubmitted,Declined")
                .Annotation("Npgsql:Enum:rfq_status", "Draft,Open,InNegotiation,Awarded,Closed,Cancelled")
                .Annotation("Npgsql:Enum:shop_status", "Active,Inactive,Suspended")
                .Annotation("Npgsql:Enum:user_role", "Customer,Supplier,Admin")
                .Annotation("Npgsql:PostgresExtension:extensions.pgcrypto", ",,")
                .Annotation("Npgsql:PostgresExtension:extensions.uuid-ossp", ",,")
                .Annotation("Npgsql:PostgresExtension:vector", ",,")
                .OldAnnotation("Npgsql:Enum:ai_conversation_type", "ProductInfo,MooncakeHistory,SalesAnalytics,General")
                .OldAnnotation("Npgsql:Enum:ai_message_role", "User,Assistant,System")
                .OldAnnotation("Npgsql:Enum:ai_report_status", "Pending,Processing,Completed,Failed")
                .OldAnnotation("Npgsql:Enum:ai_report_type", "SalesTrend,SupplierPerformance,DemandForecast,ProductAnalysis,RevenueSummary")
                .OldAnnotation("Npgsql:Enum:bid_status", "Submitted,Accepted,Cancelled")
                .OldAnnotation("Npgsql:Enum:contract_status", "Draft,PendingSignature,Signed,PendingDeposit,Active,CompletedOnTime,CompletedLate,Breached,Disputed,Cancelled")
                .OldAnnotation("Npgsql:Enum:custom_packaging_status", "Pending,DesignReview,RevisionRequested,Approved,InProduction,Completed,Rejected")
                .OldAnnotation("Npgsql:Enum:delivery_status", "Pending,Confirmed,PickedUp,InTransit,Delivered,Failed,Returned")
                .OldAnnotation("Npgsql:Enum:delivery_type", "PlatformManaged,SelfArranged")
                .OldAnnotation("Npgsql:Enum:discount_type", "Percentage,FixedAmount,FixedPrice")
                .OldAnnotation("Npgsql:Enum:milestone_status", "Pending,AwaitingPayment,Paid,CompletedOnTime,CompletedLate,Overdue,Failed")
                .OldAnnotation("Npgsql:Enum:order_status", "Pending,Confirmed,Processing,Shipped,Delivered,Cancelled")
                .OldAnnotation("Npgsql:Enum:payment_method", "BankTransfer,Momo,VNPay,ZaloPay,Cash")
                .OldAnnotation("Npgsql:Enum:payment_status", "Pending,Processing,Completed,Failed,Refunded")
                .OldAnnotation("Npgsql:Enum:product_status", "Active,Inactive,OutOfStock")
                .OldAnnotation("Npgsql:Enum:proof_type", "Pickup,InTransit,Delivery,FailedAttempt")
                .OldAnnotation("Npgsql:Enum:reputation_event", "CompletedOnTime,CompletedLate,Breached,PositiveReview,NegativeReview,AdminPenalty,AdminBonus")
                .OldAnnotation("Npgsql:Enum:rfq_invitation_status", "Invited,Viewed,BidSubmitted,Declined")
                .OldAnnotation("Npgsql:Enum:rfq_status", "Draft,Open,InNegotiation,Awarded,Closed,Cancelled")
                .OldAnnotation("Npgsql:Enum:shop_status", "Active,Inactive,Suspended")
                .OldAnnotation("Npgsql:Enum:user_role", "Customer,Supplier,Admin")
                .OldAnnotation("Npgsql:PostgresExtension:extensions.pgcrypto", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:extensions.uuid-ossp", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.AddColumn<bool>(
                name: "is_anonymous",
                table: "reviews",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_hidden_by_admin",
                table: "reviews",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "media_urls",
                table: "reviews",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "supplier_replied_at",
                table: "reviews",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "supplier_reply",
                table: "reviews",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "evidence_notes",
                table: "payments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "evidence_url",
                table: "payments",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tamper_proof_hash",
                table: "delivery_proofs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "evidence_urls",
                table: "contract_milestones",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "progress_notes",
                table: "contract_milestones",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "contract_disputes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    raised_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dispute_reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    evidence_urls = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    admin_verdict = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    resolution_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    refund_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    raised_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("contract_disputes_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "contract_extensions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proposed_due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    requested_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    responded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    responded_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    response_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("contract_extensions_pkey", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_cdis_contract_id",
                table: "contract_disputes",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "idx_cdis_raised_by",
                table: "contract_disputes",
                column: "raised_by_user_id");

            migrationBuilder.CreateIndex(
                name: "idx_cdis_status",
                table: "contract_disputes",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_cext_contract_id",
                table: "contract_extensions",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "idx_cext_status",
                table: "contract_extensions",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contract_disputes");

            migrationBuilder.DropTable(
                name: "contract_extensions");

            migrationBuilder.DropColumn(
                name: "is_anonymous",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "is_hidden_by_admin",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "media_urls",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "supplier_replied_at",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "supplier_reply",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "evidence_notes",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "evidence_url",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "tamper_proof_hash",
                table: "delivery_proofs");

            migrationBuilder.DropColumn(
                name: "evidence_urls",
                table: "contract_milestones");

            migrationBuilder.DropColumn(
                name: "progress_notes",
                table: "contract_milestones");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:ai_conversation_type", "ProductInfo,MooncakeHistory,SalesAnalytics,General")
                .Annotation("Npgsql:Enum:ai_message_role", "User,Assistant,System")
                .Annotation("Npgsql:Enum:ai_report_status", "Pending,Processing,Completed,Failed")
                .Annotation("Npgsql:Enum:ai_report_type", "SalesTrend,SupplierPerformance,DemandForecast,ProductAnalysis,RevenueSummary")
                .Annotation("Npgsql:Enum:bid_status", "Submitted,Accepted,Cancelled")
                .Annotation("Npgsql:Enum:contract_status", "Draft,PendingSignature,Signed,PendingDeposit,Active,CompletedOnTime,CompletedLate,Breached,Disputed,Cancelled")
                .Annotation("Npgsql:Enum:custom_packaging_status", "Pending,DesignReview,RevisionRequested,Approved,InProduction,Completed,Rejected")
                .Annotation("Npgsql:Enum:delivery_status", "Pending,Confirmed,PickedUp,InTransit,Delivered,Failed,Returned")
                .Annotation("Npgsql:Enum:delivery_type", "PlatformManaged,SelfArranged")
                .Annotation("Npgsql:Enum:discount_type", "Percentage,FixedAmount,FixedPrice")
                .Annotation("Npgsql:Enum:milestone_status", "Pending,AwaitingPayment,Paid,CompletedOnTime,CompletedLate,Overdue,Failed")
                .Annotation("Npgsql:Enum:order_status", "Pending,Confirmed,Processing,Shipped,Delivered,Cancelled")
                .Annotation("Npgsql:Enum:payment_method", "BankTransfer,Momo,VNPay,ZaloPay,Cash")
                .Annotation("Npgsql:Enum:payment_status", "Pending,Processing,Completed,Failed,Refunded")
                .Annotation("Npgsql:Enum:product_status", "Active,Inactive,OutOfStock")
                .Annotation("Npgsql:Enum:proof_type", "Pickup,InTransit,Delivery,FailedAttempt")
                .Annotation("Npgsql:Enum:reputation_event", "CompletedOnTime,CompletedLate,Breached,PositiveReview,NegativeReview,AdminPenalty,AdminBonus")
                .Annotation("Npgsql:Enum:rfq_invitation_status", "Invited,Viewed,BidSubmitted,Declined")
                .Annotation("Npgsql:Enum:rfq_status", "Draft,Open,InNegotiation,Awarded,Closed,Cancelled")
                .Annotation("Npgsql:Enum:shop_status", "Active,Inactive,Suspended")
                .Annotation("Npgsql:Enum:user_role", "Customer,Supplier,Admin")
                .Annotation("Npgsql:PostgresExtension:extensions.pgcrypto", ",,")
                .Annotation("Npgsql:PostgresExtension:extensions.uuid-ossp", ",,")
                .Annotation("Npgsql:PostgresExtension:vector", ",,")
                .OldAnnotation("Npgsql:Enum:ai_conversation_type", "ProductInfo,MooncakeHistory,SalesAnalytics,General")
                .OldAnnotation("Npgsql:Enum:ai_message_role", "User,Assistant,System")
                .OldAnnotation("Npgsql:Enum:ai_report_status", "Pending,Processing,Completed,Failed")
                .OldAnnotation("Npgsql:Enum:ai_report_type", "SalesTrend,SupplierPerformance,DemandForecast,ProductAnalysis,RevenueSummary")
                .OldAnnotation("Npgsql:Enum:bid_status", "Submitted,Accepted,Cancelled")
                .OldAnnotation("Npgsql:Enum:contract_status", "Draft,PendingSignature,Signed,PendingDeposit,Active,CompletedOnTime,CompletedLate,Breached,Disputed,Cancelled")
                .OldAnnotation("Npgsql:Enum:custom_packaging_status", "Pending,DesignReview,RevisionRequested,Approved,InProduction,Completed,Rejected")
                .OldAnnotation("Npgsql:Enum:delivery_status", "Pending,Confirmed,PickedUp,InTransit,Delivered,Failed,Returned")
                .OldAnnotation("Npgsql:Enum:delivery_type", "PlatformManaged,SelfArranged")
                .OldAnnotation("Npgsql:Enum:discount_type", "Percentage,FixedAmount,FixedPrice")
                .OldAnnotation("Npgsql:Enum:milestone_status", "Pending,AwaitingPayment,Paid,InProgress,PendingReview,CompletedOnTime,CompletedLate,Overdue,Failed")
                .OldAnnotation("Npgsql:Enum:order_status", "Pending,Confirmed,Processing,Shipped,Delivered,Cancelled")
                .OldAnnotation("Npgsql:Enum:payment_method", "BankTransfer,Momo,VNPay,ZaloPay,Cash")
                .OldAnnotation("Npgsql:Enum:payment_status", "Pending,Processing,Completed,Failed,Refunded")
                .OldAnnotation("Npgsql:Enum:product_status", "Active,Inactive,OutOfStock")
                .OldAnnotation("Npgsql:Enum:proof_type", "Pickup,InTransit,Delivery,FailedAttempt")
                .OldAnnotation("Npgsql:Enum:reputation_event", "CompletedOnTime,CompletedLate,Breached,PositiveReview,NegativeReview,AdminPenalty,AdminBonus")
                .OldAnnotation("Npgsql:Enum:rfq_invitation_status", "Invited,Viewed,BidSubmitted,Declined")
                .OldAnnotation("Npgsql:Enum:rfq_status", "Draft,Open,InNegotiation,Awarded,Closed,Cancelled")
                .OldAnnotation("Npgsql:Enum:shop_status", "Active,Inactive,Suspended")
                .OldAnnotation("Npgsql:Enum:user_role", "Customer,Supplier,Admin")
                .OldAnnotation("Npgsql:PostgresExtension:extensions.pgcrypto", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:extensions.uuid-ossp", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");
        }
    }
}
