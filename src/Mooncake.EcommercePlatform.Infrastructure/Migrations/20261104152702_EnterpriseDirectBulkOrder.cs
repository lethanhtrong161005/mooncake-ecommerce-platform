using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mooncake.EcommercePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnterpriseDirectBulkOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS payments_one_open_payment_per_order_key;");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:ai_conversation_type", "ProductInfo,MooncakeHistory,SalesAnalytics,General")
                .Annotation("Npgsql:Enum:ai_message_role", "User,Assistant,System")
                .Annotation("Npgsql:Enum:ai_report_status", "Pending,Processing,Completed,Failed")
                .Annotation("Npgsql:Enum:ai_report_type", "SalesTrend,SupplierPerformance,DemandForecast,ProductAnalysis,RevenueSummary")
                .Annotation("Npgsql:Enum:bid_status", "Submitted,Accepted,Cancelled")
                .Annotation("Npgsql:Enum:contract_status", "Draft,PendingSignature,Signed,PendingDeposit,Active,CompletedOnTime,CompletedLate,Breached,Disputed,Cancelled")
                .Annotation("Npgsql:Enum:custom_packaging_status", "Pending,DesignReview,RevisionRequested,Approved,InProduction,Completed,Rejected")
                .Annotation("Npgsql:Enum:delivery_status", "Pending,Confirmed,PickedUp,InTransit,Delivered,Failed,Returned,AwaitingCustomerConfirmation,Disputed")
                .Annotation("Npgsql:Enum:delivery_type", "PlatformManaged,SelfArranged")
                .Annotation("Npgsql:Enum:discount_type", "Percentage,FixedAmount,FixedPrice")
                .Annotation("Npgsql:Enum:milestone_status", "Pending,AwaitingPayment,Paid,InProgress,PendingReview,CompletedOnTime,CompletedLate,Overdue,Failed")
                .Annotation("Npgsql:Enum:order_status", "Pending,Confirmed,Processing,Shipped,Delivered,Cancelled,Expired,AwaitingCustomerDeliveryApproval,CancellationRequested,Disputed")
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

            migrationBuilder.AlterColumn<decimal>(
                name: "refunded_amount",
                table: "payments",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldPrecision: 12,
                oldScale: 2,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "amount",
                table: "payments",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldPrecision: 12,
                oldScale: 2);

            migrationBuilder.AddColumn<Guid>(
                name: "part_id",
                table: "payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "total_amount",
                table: "orders",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldPrecision: 12,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "tax_amount",
                table: "orders",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldPrecision: 12,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "subtotal",
                table: "orders",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldPrecision: 12,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "shipping_fee",
                table: "orders",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldPrecision: 12,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "packaging_fee",
                table: "orders",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldPrecision: 12,
                oldScale: 2,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "discount_amount",
                table: "orders",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldPrecision: 12,
                oldScale: 2);

            migrationBuilder.AddColumn<DateTime>(
                name: "confirmed_at",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "confirmed_delivery_date",
                table: "orders",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "delivery_approval_deadline_at",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "delivery_proposal_reason",
                table: "orders",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "deposit_percent_snapshot",
                table: "orders",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "deposit_required_snapshot",
                table: "orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "price_snapshot_version",
                table: "orders",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "proposed_delivery_date",
                table: "orders",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "reservation_expires_at",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "response_deadline_at",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipping_zone_code",
                table: "orders",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipping_zone_name_snapshot",
                table: "orders",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "tax_included_snapshot",
                table: "orders",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<decimal>(
                name: "tax_rate_snapshot",
                table: "orders",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "terms_snapshot_json",
                table: "orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "version",
                table: "orders",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.CreateTable(
                name: "inventory_reservations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    released_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    release_reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_reservations", x => x.id);
                    table.CheckConstraint("ck_inventory_reservations_qty", "quantity > 0");
                    table.ForeignKey(
                        name: "FK_inventory_reservations_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_reservations_product_variants_variant_id",
                        column: x => x.variant_id,
                        principalTable: "product_variants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_cancellation_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_by_role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    order_status_snapshot = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    review_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    proposed_refund_amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_cancellation_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_order_cancellation_requests_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_delivery_disputes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    delivery_id = table.Column<Guid>(type: "uuid", nullable: true),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    issue_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    evidence_urls_json = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    resolution_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    resolved_by_admin_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_delivery_disputes", x => x.id);
                    table.ForeignKey(
                        name: "FK_order_delivery_disputes_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_financial_ledger_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    refund_case_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payout_request_id = table.Column<Guid>(type: "uuid", nullable: true),
                    entry_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    balance_bucket = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_financial_ledger_entries", x => x.id);
                    table.ForeignKey(
                        name: "FK_order_financial_ledger_entries_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_payment_schedule",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence_no = table.Column<int>(type: "integer", nullable: false),
                    obligation_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    paid_amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    is_required_before_production = table.Column<bool>(type: "boolean", nullable: false),
                    is_required_before_shipping = table.Column<bool>(type: "boolean", nullable: false),
                    due_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    source_reference_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_payment_schedule", x => x.id);
                    table.CheckConstraint("ck_order_payment_schedule_amounts", "total_amount > 0 AND paid_amount >= 0");
                    table.ForeignKey(
                        name: "FK_order_payment_schedule_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_refund_cases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cancellation_request_id = table.Column<Guid>(type: "uuid", nullable: true),
                    case_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    requested_amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    approved_amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    sandbox_simulated = table.Column<bool>(type: "boolean", nullable: false),
                    processed_by_admin_id = table.Column<Guid>(type: "uuid", nullable: true),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    admin_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    refund_reference_code = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_refund_cases", x => x.id);
                    table.ForeignKey(
                        name: "FK_order_refund_cases_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "packaging_fee_change_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    custom_packaging_id = table.Column<Guid>(type: "uuid", nullable: false),
                    revision_id = table.Column<Guid>(type: "uuid", nullable: false),
                    old_packaging_fee = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    delta_amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    new_packaging_fee = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    old_order_total = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    new_order_total = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    order_version_snapshot = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    customer_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    generated_obligation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_packaging_fee_change_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_packaging_fee_change_requests_custom_packaging_revisions_re~",
                        column: x => x.revision_id,
                        principalTable: "custom_packaging_revisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_packaging_fee_change_requests_custom_packagings_custom_pack~",
                        column: x => x.custom_packaging_id,
                        principalTable: "custom_packagings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_packaging_fee_change_requests_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS packaging_templates (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    shop_id uuid NOT NULL,
    name character varying(255) NOT NULL,
    description text NULL,
    material character varying(150) NULL,
    dimensions character varying(100) NULL,
    capacity integer NOT NULL,
    base_price numeric(12,2) NOT NULL,
    sample_image_url text NULL,
    technique character varying(150) NULL,
    min_order_qty integer NOT NULL,
    is_active boolean NOT NULL DEFAULT TRUE,
    sort_order integer NOT NULL DEFAULT 0,
    is_deleted boolean NOT NULL DEFAULT FALSE,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    updated_at timestamp with time zone NOT NULL DEFAULT now(),
    created_by uuid NULL,
    updated_by uuid NULL,
    CONSTRAINT packaging_templates_pkey PRIMARY KEY (id)
);");

            migrationBuilder.CreateTable(
                name: "shop_order_policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    deposit_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    minimum_order_total_for_deposit = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    deposit_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    tax_included = table.Column<bool>(type: "boolean", nullable: false),
                    tax_rate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    allow_self_pickup = table.Column<bool>(type: "boolean", nullable: false),
                    require_shipping_zone_selection = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shop_order_policies", x => x.id);
                    table.CheckConstraint("ck_shop_order_policies_deposit_pct", "deposit_percent >= 10 AND deposit_percent <= 50");
                    table.CheckConstraint("ck_shop_order_policies_tax_rate", "tax_rate >= 0 AND tax_rate <= 100");
                    table.ForeignKey(
                        name: "FK_shop_order_policies_shops_shop_id",
                        column: x => x.shop_id,
                        principalTable: "shops",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shop_shipping_rates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    region_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    region_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    fee_amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    free_shipping_threshold = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    estimated_days = table.Column<int>(type: "integer", nullable: false),
                    is_supported = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shop_shipping_rates", x => x.id);
                    table.CheckConstraint("ck_shop_shipping_rates_fee", "fee_amount >= 0 AND estimated_days >= 0");
                    table.ForeignKey(
                        name: "FK_shop_shipping_rates_shops_shop_id",
                        column: x => x.shop_id,
                        principalTable: "shops",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplier_payout_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bank_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    bank_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    account_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    account_holder_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    branch_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    is_verified_simulated = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supplier_payout_accounts", x => x.id);
                    table.ForeignKey(
                        name: "FK_supplier_payout_accounts_users_supplier_user_id",
                        column: x => x.supplier_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_parts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    obligation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    part_index = table.Column<int>(type: "integer", nullable: false),
                    total_parts = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_parts", x => x.id);
                    table.CheckConstraint("ck_payment_parts_amount", "amount > 0 AND part_index >= 1 AND total_parts >= 1");
                    table.ForeignKey(
                        name: "FK_payment_parts_order_payment_schedule_obligation_id",
                        column: x => x.obligation_id,
                        principalTable: "order_payment_schedule",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payment_parts_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplier_payout_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payout_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    sandbox_simulated = table.Column<bool>(type: "boolean", nullable: false),
                    bank_snapshot_json = table.Column<string>(type: "text", nullable: false),
                    processed_by_admin_id = table.Column<Guid>(type: "uuid", nullable: true),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    admin_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    payout_reference_code = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supplier_payout_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_supplier_payout_requests_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_payout_requests_supplier_payout_accounts_payout_ac~",
                        column: x => x.payout_account_id,
                        principalTable: "supplier_payout_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_pay_part_id",
                table: "payments",
                column: "part_id",
                filter: "(part_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "payments_one_open_payment_per_order_legacy_key",
                table: "payments",
                column: "order_id",
                unique: true,
                filter: "(order_id IS NOT NULL AND part_id IS NULL AND status IN ('Pending', 'Processing', 'Completed') AND is_deleted = false)");

            migrationBuilder.CreateIndex(
                name: "payments_one_open_payment_per_part_key",
                table: "payments",
                column: "part_id",
                unique: true,
                filter: "(part_id IS NOT NULL AND status IN ('Pending', 'Processing', 'Completed') AND is_deleted = false)");

            migrationBuilder.CreateIndex(
                name: "idx_orders_customer_cursor",
                table: "orders",
                columns: new[] { "customer_id", "created_at", "id" },
                descending: new[] { false, true, true },
                filter: "(is_deleted = false)");

            migrationBuilder.CreateIndex(
                name: "idx_orders_reservation_expiry",
                table: "orders",
                columns: new[] { "status", "reservation_expires_at" },
                filter: "(is_deleted = false AND reservation_expires_at IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "idx_orders_shop_cursor",
                table: "orders",
                columns: new[] { "shop_id", "created_at", "id" },
                descending: new[] { false, true, true },
                filter: "(is_deleted = false)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_orders_deposit_percent_range",
                table: "orders",
                sql: "deposit_percent_snapshot >= 0 AND deposit_percent_snapshot <= 100");

            migrationBuilder.AddCheckConstraint(
                name: "ck_orders_version_positive",
                table: "orders",
                sql: "version > 0");

            migrationBuilder.CreateIndex(
                name: "idx_inventory_reservations_status_expires",
                table: "inventory_reservations",
                columns: new[] { "status", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_reservations_variant_id",
                table: "inventory_reservations",
                column: "variant_id");

            migrationBuilder.CreateIndex(
                name: "uq_inventory_reservations_order_variant",
                table: "inventory_reservations",
                columns: new[] { "order_id", "variant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_order_cancel_requests_order_status",
                table: "order_cancellation_requests",
                columns: new[] { "order_id", "status" });

            migrationBuilder.CreateIndex(
                name: "idx_order_delivery_disputes_order_status",
                table: "order_delivery_disputes",
                columns: new[] { "order_id", "status" });

            migrationBuilder.CreateIndex(
                name: "idx_order_financial_ledger_order_created",
                table: "order_financial_ledger_entries",
                columns: new[] { "order_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "idx_order_financial_ledger_shop_bucket",
                table: "order_financial_ledger_entries",
                columns: new[] { "shop_id", "balance_bucket" });

            migrationBuilder.CreateIndex(
                name: "uq_order_financial_ledger_idempotency",
                table: "order_financial_ledger_entries",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_order_payment_schedule_order_seq",
                table: "order_payment_schedule",
                columns: new[] { "order_id", "sequence_no" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "idx_order_refund_cases_order_id",
                table: "order_refund_cases",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "uq_order_refund_cases_number",
                table: "order_refund_cases",
                column: "case_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_pkg_fee_change_order_id",
                table: "packaging_fee_change_requests",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_packaging_fee_change_requests_custom_packaging_id",
                table: "packaging_fee_change_requests",
                column: "custom_packaging_id");

            migrationBuilder.CreateIndex(
                name: "uq_pkg_fee_change_revision_id",
                table: "packaging_fee_change_requests",
                column: "revision_id",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS idx_packaging_templates_shop_id ON packaging_templates (shop_id);");

            migrationBuilder.CreateIndex(
                name: "idx_payment_parts_order_id",
                table: "payment_parts",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "uq_payment_parts_obligation_index",
                table: "payment_parts",
                columns: new[] { "obligation_id", "part_index" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "uq_shop_order_policies_shop_id",
                table: "shop_order_policies",
                column: "shop_id",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "uq_shop_shipping_rates_shop_region",
                table: "shop_shipping_rates",
                columns: new[] { "shop_id", "region_code" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "uq_supplier_payout_accounts_user_id",
                table: "supplier_payout_accounts",
                column: "supplier_user_id",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "idx_supplier_payout_requests_order_id",
                table: "supplier_payout_requests",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_payout_requests_payout_account_id",
                table: "supplier_payout_requests",
                column: "payout_account_id");

            migrationBuilder.CreateIndex(
                name: "uq_supplier_payout_requests_number",
                table: "supplier_payout_requests",
                column: "request_number",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_payments_payment_parts_part_id",
                table: "payments",
                column: "part_id",
                principalTable: "payment_parts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(@"
-- Idempotent backfill: 1. Create order_payment_schedule rows for existing active/paid orders without a schedule
INSERT INTO order_payment_schedule (
    id, order_id, sequence_no, obligation_type, title, total_amount, paid_amount,
    status, is_required_before_production, is_required_before_shipping, due_at, paid_at,
    is_deleted, created_at, updated_at, created_by
)
SELECT
    gen_random_uuid(),
    o.id,
    1,
    'FullUpfront',
    'Backfilled full order payment obligation',
    GREATEST(o.total_amount, 1.00),
    CASE WHEN EXISTS (
        SELECT 1 FROM payments p
        WHERE p.order_id = o.id AND p.status = 'Completed'::payment_status AND p.is_deleted = FALSE
    ) THEN GREATEST(o.total_amount, 1.00) ELSE 0 END,
    CASE
        WHEN EXISTS (
            SELECT 1 FROM payments p
            WHERE p.order_id = o.id AND p.status = 'Completed'::payment_status AND p.is_deleted = FALSE
        ) THEN 'Paid'
        WHEN o.status = 'Cancelled'::order_status THEN 'Cancelled'
        ELSE 'Pending'
    END,
    TRUE,
    TRUE,
    o.created_at + INTERVAL '24 hours',
    (SELECT MAX(p.paid_at) FROM payments p WHERE p.order_id = o.id AND p.status = 'Completed'::payment_status AND p.is_deleted = FALSE),
    FALSE,
    o.created_at,
    now(),
    o.customer_id
FROM orders o
WHERE o.is_deleted = FALSE
  AND o.total_amount > 0
  AND (
      o.status IN ('Confirmed'::order_status, 'Processing'::order_status, 'Shipped'::order_status, 'Delivered'::order_status)
      OR EXISTS (SELECT 1 FROM payments p WHERE p.order_id = o.id AND p.is_deleted = FALSE)
  )
  AND NOT EXISTS (
      SELECT 1 FROM order_payment_schedule s WHERE s.order_id = o.id AND s.is_deleted = FALSE
  );

-- Idempotent backfill: 2. Create 1 payment_parts row per backfilled obligation without parts
INSERT INTO payment_parts (
    id, order_id, obligation_id, part_index, total_parts, amount, status, paid_at,
    is_deleted, created_at, updated_at, created_by
)
SELECT
    gen_random_uuid(),
    s.order_id,
    s.id,
    1,
    1,
    s.total_amount,
    s.status,
    s.paid_at,
    FALSE,
    s.created_at,
    now(),
    s.created_by
FROM order_payment_schedule s
WHERE s.is_deleted = FALSE
  AND NOT EXISTS (
      SELECT 1 FROM payment_parts pp WHERE pp.obligation_id = s.id AND pp.is_deleted = FALSE
  );

-- Idempotent backfill: 3. Link existing order-level payments (part_id IS NULL) to part 1
UPDATE payments p
SET part_id = pp.id
FROM payment_parts pp
JOIN order_payment_schedule s ON s.id = pp.obligation_id
WHERE p.order_id = pp.order_id
  AND p.order_id IS NOT NULL
  AND p.part_id IS NULL
  AND p.is_deleted = FALSE
  AND s.sequence_no = 1
  AND pp.part_index = 1;

-- Idempotent backfill: 4. Create Captured/Held ledger entries for existing Completed order payments
INSERT INTO order_financial_ledger_entries (
    id, order_id, shop_id, customer_id, payment_id, entry_type, amount, balance_bucket,
    description, idempotency_key, is_deleted, created_at, updated_at, created_by
)
SELECT
    gen_random_uuid(),
    o.id,
    o.shop_id,
    o.customer_id,
    p.id,
    'Captured',
    p.amount,
    'Held',
    'Backfilled captured order payment into Held balance.',
    'payment-captured:' || p.id::text,
    FALSE,
    COALESCE(p.paid_at, p.created_at),
    now(),
    p.customer_id
FROM payments p
JOIN orders o ON o.id = p.order_id
WHERE p.order_id IS NOT NULL
  AND p.status = 'Completed'::payment_status
  AND p.is_deleted = FALSE
  AND o.is_deleted = FALSE
  AND NOT EXISTS (
      SELECT 1 FROM order_financial_ledger_entries l
      WHERE l.idempotency_key = ('payment-captured:' || p.id::text)
  );

-- Idempotent backfill: 5. Create inventory_reservations for existing active orders
INSERT INTO inventory_reservations (
    id, order_id, product_id, variant_id, shop_id, quantity, status,
    expires_at, is_deleted, created_at, updated_at, created_by
)
SELECT
    gen_random_uuid(),
    o.id,
    oi.product_id,
    oi.variant_id,
    o.shop_id,
    SUM(oi.quantity)::integer,
    CASE WHEN o.status = 'Pending'::order_status THEN 'Active' ELSE 'Committed' END,
    CASE WHEN o.status = 'Pending'::order_status THEN o.created_at + INTERVAL '24 hours' ELSE NULL END,
    FALSE,
    o.created_at,
    now(),
    o.customer_id
FROM orders o
JOIN order_items oi ON oi.order_id = o.id
WHERE o.is_deleted = FALSE
  AND oi.is_deleted = FALSE
  AND o.status IN ('Pending'::order_status, 'Confirmed'::order_status, 'Processing'::order_status, 'Shipped'::order_status, 'Delivered'::order_status)
  AND NOT EXISTS (
      SELECT 1 FROM inventory_reservations ir WHERE ir.order_id = o.id AND ir.variant_id = oi.variant_id
  )
GROUP BY o.id, oi.product_id, oi.variant_id, o.shop_id, o.status, o.created_at, o.customer_id;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_payments_payment_parts_part_id",
                table: "payments");

            migrationBuilder.DropTable(
                name: "inventory_reservations");

            migrationBuilder.DropTable(
                name: "order_cancellation_requests");

            migrationBuilder.DropTable(
                name: "order_delivery_disputes");

            migrationBuilder.DropTable(
                name: "order_financial_ledger_entries");

            migrationBuilder.DropTable(
                name: "order_refund_cases");

            migrationBuilder.DropTable(
                name: "packaging_fee_change_requests");



            migrationBuilder.DropTable(
                name: "payment_parts");

            migrationBuilder.DropTable(
                name: "shop_order_policies");

            migrationBuilder.DropTable(
                name: "shop_shipping_rates");

            migrationBuilder.DropTable(
                name: "supplier_payout_requests");

            migrationBuilder.DropTable(
                name: "order_payment_schedule");

            migrationBuilder.DropTable(
                name: "supplier_payout_accounts");

            migrationBuilder.DropIndex(
                name: "idx_pay_part_id",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "payments_one_open_payment_per_order_legacy_key",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "payments_one_open_payment_per_part_key",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "idx_orders_customer_cursor",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "idx_orders_reservation_expiry",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "idx_orders_shop_cursor",
                table: "orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_orders_deposit_percent_range",
                table: "orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_orders_version_positive",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "part_id",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "confirmed_at",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "confirmed_delivery_date",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "delivery_approval_deadline_at",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "delivery_proposal_reason",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "deposit_percent_snapshot",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "deposit_required_snapshot",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "price_snapshot_version",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "proposed_delivery_date",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "reservation_expires_at",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "response_deadline_at",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "shipping_zone_code",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "shipping_zone_name_snapshot",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "tax_included_snapshot",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "tax_rate_snapshot",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "terms_snapshot_json",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "version",
                table: "orders");

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
                .OldAnnotation("Npgsql:Enum:delivery_status", "Pending,Confirmed,PickedUp,InTransit,Delivered,Failed,Returned,AwaitingCustomerConfirmation,Disputed")
                .OldAnnotation("Npgsql:Enum:delivery_type", "PlatformManaged,SelfArranged")
                .OldAnnotation("Npgsql:Enum:discount_type", "Percentage,FixedAmount,FixedPrice")
                .OldAnnotation("Npgsql:Enum:milestone_status", "Pending,AwaitingPayment,Paid,InProgress,PendingReview,CompletedOnTime,CompletedLate,Overdue,Failed")
                .OldAnnotation("Npgsql:Enum:order_status", "Pending,Confirmed,Processing,Shipped,Delivered,Cancelled,Expired,AwaitingCustomerDeliveryApproval,CancellationRequested,Disputed")
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

            migrationBuilder.AlterColumn<decimal>(
                name: "refunded_amount",
                table: "payments",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(14,2)",
                oldPrecision: 14,
                oldScale: 2,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "amount",
                table: "payments",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(14,2)",
                oldPrecision: 14,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "total_amount",
                table: "orders",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(14,2)",
                oldPrecision: 14,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "tax_amount",
                table: "orders",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(14,2)",
                oldPrecision: 14,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "subtotal",
                table: "orders",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(14,2)",
                oldPrecision: 14,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "shipping_fee",
                table: "orders",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(14,2)",
                oldPrecision: 14,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "packaging_fee",
                table: "orders",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(14,2)",
                oldPrecision: 14,
                oldScale: 2,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "discount_amount",
                table: "orders",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(14,2)",
                oldPrecision: 14,
                oldScale: 2);

            migrationBuilder.CreateIndex(
                name: "payments_one_open_payment_per_order_key",
                table: "payments",
                column: "order_id",
                unique: true,
                filter: "(order_id IS NOT NULL AND status IN ('Pending', 'Processing', 'Completed') AND is_deleted = false)");
        }
    }
}
