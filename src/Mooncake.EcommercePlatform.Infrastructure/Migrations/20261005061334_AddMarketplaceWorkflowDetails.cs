using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mooncake.EcommercePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketplaceWorkflowDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_sp_user_id",
                table: "supplier_profiles");

            migrationBuilder.DropIndex(
                name: "idx_sp_verified",
                table: "supplier_profiles");

            migrationBuilder.DropIndex(
                name: "idx_shops_supplier_id",
                table: "shops");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "users");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "users");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "supplier_profiles");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "supplier_profiles");

            migrationBuilder.RenameColumn(
                name: "verified",
                table: "supplier_profiles",
                newName: "verified_legacy");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "shops");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "shops");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "shop_templates");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "shop_templates");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "rfq_items");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "rfq_items");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "rfq_invitations");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "rfq_invitations");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "request_for_quotations");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "request_for_quotations");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "reputation_logs");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "reputation_logs");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "promotion_rules");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "promotion_rules");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "products");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "products");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "product_reviews");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "product_reviews");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "product_images");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "product_images");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "delivery_proofs");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "delivery_proofs");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "deliveries");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "deliveries");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "customer_profiles");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "customer_profiles");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "custom_packagings");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "custom_packagings");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "contract_milestones");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "contract_milestones");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "contract_deposit_rules");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "contract_deposit_rules");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "bids");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "bids");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "bid_items");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "bid_items");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "ai_messages");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "ai_messages");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "ai_conversations");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "ai_conversations");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "ai_analytics_reports");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "ai_analytics_reports");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:ai_conversation_type.AiConversationType", "ProductInfo,MooncakeHistory,SalesAnalytics,General")
                .Annotation("Npgsql:Enum:ai_message_role.AiMessageRole", "User,Assistant,System")
                .Annotation("Npgsql:Enum:ai_report_status.AiReportStatus", "Pending,Processing,Completed,Failed")
                .Annotation("Npgsql:Enum:ai_report_type.AiReportType", "SalesTrend,SupplierPerformance,DemandForecast,ProductAnalysis,RevenueSummary")
                .Annotation("Npgsql:Enum:bid_status.BidStatus", "Submitted,Accepted,Cancelled")
                .Annotation("Npgsql:Enum:contract_status.ContractStatus", "Draft,PendingSignature,Signed,PendingDeposit,Active,CompletedOnTime,CompletedLate,Breached,Disputed,Cancelled")
                .Annotation("Npgsql:Enum:custom_packaging_status.CustomPackagingStatus", "Pending,DesignReview,Approved,InProduction,Rejected")
                .Annotation("Npgsql:Enum:delivery_status.DeliveryStatus", "Pending,Confirmed,PickedUp,InTransit,Delivered,Failed,Returned")
                .Annotation("Npgsql:Enum:delivery_type.DeliveryType", "PlatformManaged,SelfArranged")
                .Annotation("Npgsql:Enum:discount_type.DiscountType", "Percentage,FixedAmount,FixedPrice")
                .Annotation("Npgsql:Enum:milestone_status.MilestoneStatus", "Pending,AwaitingPayment,Paid,CompletedOnTime,CompletedLate,Overdue,Failed")
                .Annotation("Npgsql:Enum:order_status.OrderStatus", "Pending,Confirmed,Processing,Shipped,Delivered,Cancelled")
                .Annotation("Npgsql:Enum:payment_method.PaymentMethod", "BankTransfer,Momo,VNPay,ZaloPay,Cash")
                .Annotation("Npgsql:Enum:payment_status.PaymentStatus", "Pending,Processing,Completed,Failed,Refunded")
                .Annotation("Npgsql:Enum:product_status.ProductStatus", "Active,Inactive,OutOfStock")
                .Annotation("Npgsql:Enum:proof_type.ProofType", "Pickup,InTransit,Delivery,FailedAttempt")
                .Annotation("Npgsql:Enum:reputation_event.ReputationEvent", "CompletedOnTime,CompletedLate,Breached,PositiveReview,NegativeReview,AdminPenalty,AdminBonus")
                .Annotation("Npgsql:Enum:rfq_invitation_status.RfqInvitationStatus", "Invited,Viewed,BidSubmitted,Declined")
                .Annotation("Npgsql:Enum:rfq_status.RfqStatus", "Draft,Open,InNegotiation,Awarded,Closed,Cancelled")
                .Annotation("Npgsql:Enum:shop_status.ShopStatus", "Active,Inactive,Suspended")
                .Annotation("Npgsql:Enum:user_role.UserRole", "Customer,Supplier,Admin")
                .Annotation("Npgsql:PostgresExtension:extensions.pgcrypto", ",,")
                .Annotation("Npgsql:PostgresExtension:extensions.uuid-ossp", ",,")
                .Annotation("Npgsql:PostgresExtension:vector", ",,")
                .OldAnnotation("Npgsql:Enum:ai_conversation_type.ai_conversation_type", "product_info,mooncake_history,sales_analytics,general")
                .OldAnnotation("Npgsql:Enum:ai_message_role.ai_message_role", "user,assistant,system")
                .OldAnnotation("Npgsql:Enum:ai_report_status.ai_report_status", "pending,processing,completed,failed")
                .OldAnnotation("Npgsql:Enum:ai_report_type.ai_report_type", "sales_trend,supplier_performance,demand_forecast,product_analysis,revenue_summary")
                .OldAnnotation("Npgsql:Enum:bid_status.bid_status", "submitted,accepted,cancelled")
                .OldAnnotation("Npgsql:Enum:contract_status.contract_status", "draft,pending_signature,signed,pending_deposit,active,completed_on_time,completed_late,breached,disputed,cancelled")
                .OldAnnotation("Npgsql:Enum:custom_packaging_status.custom_packaging_status", "pending,design_review,approved,in_production,rejected")
                .OldAnnotation("Npgsql:Enum:delivery_status.delivery_status", "pending,confirmed,picked_up,in_transit,delivered,failed,returned")
                .OldAnnotation("Npgsql:Enum:delivery_type.delivery_type", "platform_managed,self_arranged")
                .OldAnnotation("Npgsql:Enum:discount_type.discount_type", "percentage,fixed_amount,fixed_price")
                .OldAnnotation("Npgsql:Enum:milestone_status.milestone_status", "pending,awaiting_payment,paid,completed_on_time,completed_late,overdue,failed")
                .OldAnnotation("Npgsql:Enum:order_status.order_status", "pending,confirmed,processing,shipped,delivered,cancelled")
                .OldAnnotation("Npgsql:Enum:payment_method.payment_method", "bank_transfer,momo,vnpay,zalopay,cash")
                .OldAnnotation("Npgsql:Enum:payment_status.payment_status", "pending,processing,completed,failed,refunded")
                .OldAnnotation("Npgsql:Enum:product_status.product_status", "active,inactive,out_of_stock")
                .OldAnnotation("Npgsql:Enum:proof_type.proof_type", "pickup,in_transit,delivery,failed_attempt")
                .OldAnnotation("Npgsql:Enum:reputation_event.reputation_event", "completed_on_time,completed_late,breached,positive_review,negative_review,admin_penalty,admin_bonus")
                .OldAnnotation("Npgsql:Enum:rfq_invitation_status.rfq_invitation_status", "invited,viewed,bid_submitted,declined")
                .OldAnnotation("Npgsql:Enum:rfq_status.rfq_status", "draft,open,in_negotiation,awarded,closed,cancelled")
                .OldAnnotation("Npgsql:Enum:shop_status.shop_status", "active,inactive,suspended")
                .OldAnnotation("Npgsql:Enum:user_role.user_role", "customer,supplier,admin")
                .OldAnnotation("Npgsql:PostgresExtension:extensions.pgcrypto", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:extensions.uuid-ossp", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.AddColumn<int>(
                name: "failed_login_attempts",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "lockout_until",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rejection_reason",
                table: "supplier_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "reviewed_at",
                table: "supplier_profiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "reviewed_by_user_id",
                table: "supplier_profiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "verification_status",
                table: "supplier_profiles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.Sql("UPDATE supplier_profiles SET verification_status = CASE WHEN verified_legacy THEN 'Verified' ELSE 'Pending' END");
            migrationBuilder.DropColumn(name: "verified_legacy", table: "supplier_profiles");

            migrationBuilder.AddColumn<string>(
                name: "currency",
                table: "payments",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "VND");

            migrationBuilder.AddColumn<string>(
                name: "idempotency_key",
                table: "payments",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider",
                table: "payments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancellation_reason",
                table: "orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "cancelled_at",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "cancelled_by_user_id",
                table: "orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "recipient_name",
                table: "orders",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "recipient_phone",
                table: "orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "packaging_fee",
                table: "order_items",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "product_name_snapshot",
                table: "order_items",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "variant_name_snapshot",
                table: "order_items",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "variant_sku_snapshot",
                table: "order_items",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "signed_document_sha256",
                table: "contracts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "signed_document_url",
                table: "contracts",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "custom_packaging_revisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    CustomPackagingId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    MockupUrl = table.Column<string>(type: "text", nullable: true),
                    CustomerNotes = table.Column<string>(type: "text", nullable: true),
                    SupplierNotes = table.Column<string>(type: "text", nullable: true),
                    DesignFee = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SubmittedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_custom_packaging_revisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_custom_packaging_revisions_custom_packagings_CustomPackagin~",
                        column: x => x.CustomPackagingId,
                        principalTable: "custom_packagings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_custom_packaging_revisions_users_SubmittedByUserId",
                        column: x => x.SubmittedByUserId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "product_review_likes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    ProductReviewId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_review_likes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_product_review_likes_customer_profiles_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "customer_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_product_review_likes_product_reviews_ProductReviewId",
                        column: x => x.ProductReviewId,
                        principalTable: "product_reviews",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReplacedBySessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedFromIp = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_refresh_sessions_refresh_sessions_ReplacedBySessionId",
                        column: x => x.ReplacedBySessionId,
                        principalTable: "refresh_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_refresh_sessions_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "supplier_verification_documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    SupplierProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileUrl = table.Column<string>(type: "text", nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ContentType = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    ExpiresOn = table.Column<DateOnly>(type: "date", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    RejectionReason = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supplier_verification_documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_supplier_verification_documents_supplier_profiles_SupplierP~",
                        column: x => x.SupplierProfileId,
                        principalTable: "supplier_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_supplier_verification_documents_users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "workflow_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    AggregateType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ToStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workflow_events_users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "idx_sp_verification_status",
                table: "supplier_profiles",
                column: "verification_status",
                filter: "(is_deleted = false)");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_profiles_reviewed_by_user_id",
                table: "supplier_profiles",
                column: "reviewed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "supplier_profiles_user_id_key",
                table: "supplier_profiles",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "shops_supplier_id_key",
                table: "shops",
                column: "supplier_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "payments_idempotency_key_key",
                table: "payments",
                column: "idempotency_key",
                unique: true,
                filter: "(idempotency_key IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_payments_single_payment_target",
                table: "payments",
                sql: "((order_id IS NOT NULL) <> (contract_id IS NOT NULL)) AND (order_id IS NULL OR milestone_id IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_deliveries_single_parent",
                table: "deliveries",
                sql: "(contract_id IS NOT NULL) <> (order_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "custom_packagings_order_id_key",
                table: "custom_packagings",
                column: "order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_custom_packaging_revisions_CustomPackagingId_RevisionNumber",
                table: "custom_packaging_revisions",
                columns: new[] { "CustomPackagingId", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_custom_packaging_revisions_SubmittedByUserId",
                table: "custom_packaging_revisions",
                column: "SubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_product_review_likes_CustomerId",
                table: "product_review_likes",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_product_review_likes_ProductReviewId_CustomerId",
                table: "product_review_likes",
                columns: new[] { "ProductReviewId", "CustomerId" },
                unique: true,
                filter: "(is_deleted = false)");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_sessions_ReplacedBySessionId",
                table: "refresh_sessions",
                column: "ReplacedBySessionId");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_sessions_TokenHash",
                table: "refresh_sessions",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_sessions_UserId_ExpiresAt",
                table: "refresh_sessions",
                columns: new[] { "UserId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_verification_documents_ReviewedByUserId",
                table: "supplier_verification_documents",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_verification_documents_SupplierProfileId_ReviewSta~",
                table: "supplier_verification_documents",
                columns: new[] { "SupplierProfileId", "ReviewStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_events_ActorUserId",
                table: "workflow_events",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_events_AggregateType_AggregateId_CreatedAt",
                table: "workflow_events",
                columns: new[] { "AggregateType", "AggregateId", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_supplier_profiles_users_reviewed_by_user_id",
                table: "supplier_profiles",
                column: "reviewed_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_supplier_profiles_users_user_id",
                table: "supplier_profiles",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_supplier_profiles_users_reviewed_by_user_id",
                table: "supplier_profiles");

            migrationBuilder.DropForeignKey(
                name: "FK_supplier_profiles_users_user_id",
                table: "supplier_profiles");

            migrationBuilder.DropTable(
                name: "custom_packaging_revisions");

            migrationBuilder.DropTable(
                name: "product_review_likes");

            migrationBuilder.DropTable(
                name: "refresh_sessions");

            migrationBuilder.DropTable(
                name: "supplier_verification_documents");

            migrationBuilder.DropTable(
                name: "workflow_events");

            migrationBuilder.DropIndex(
                name: "idx_sp_verification_status",
                table: "supplier_profiles");

            migrationBuilder.DropIndex(
                name: "IX_supplier_profiles_reviewed_by_user_id",
                table: "supplier_profiles");

            migrationBuilder.DropIndex(
                name: "supplier_profiles_user_id_key",
                table: "supplier_profiles");

            migrationBuilder.DropIndex(
                name: "shops_supplier_id_key",
                table: "shops");

            migrationBuilder.DropIndex(
                name: "payments_idempotency_key_key",
                table: "payments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_payments_single_payment_target",
                table: "payments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_deliveries_single_parent",
                table: "deliveries");

            migrationBuilder.DropIndex(
                name: "custom_packagings_order_id_key",
                table: "custom_packagings");

            migrationBuilder.DropColumn(
                name: "failed_login_attempts",
                table: "users");

            migrationBuilder.DropColumn(
                name: "lockout_until",
                table: "users");

            migrationBuilder.DropColumn(
                name: "rejection_reason",
                table: "supplier_profiles");

            migrationBuilder.DropColumn(
                name: "reviewed_at",
                table: "supplier_profiles");

            migrationBuilder.DropColumn(
                name: "reviewed_by_user_id",
                table: "supplier_profiles");

            migrationBuilder.RenameColumn(
                name: "verification_status",
                table: "supplier_profiles",
                newName: "verification_status_legacy");

            migrationBuilder.DropColumn(
                name: "currency",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "idempotency_key",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "provider",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "cancellation_reason",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "cancelled_at",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "cancelled_by_user_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "recipient_name",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "recipient_phone",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "packaging_fee",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "product_name_snapshot",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "variant_name_snapshot",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "variant_sku_snapshot",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "signed_document_sha256",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "signed_document_url",
                table: "contracts");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:ai_conversation_type.ai_conversation_type", "product_info,mooncake_history,sales_analytics,general")
                .Annotation("Npgsql:Enum:ai_message_role.ai_message_role", "user,assistant,system")
                .Annotation("Npgsql:Enum:ai_report_status.ai_report_status", "pending,processing,completed,failed")
                .Annotation("Npgsql:Enum:ai_report_type.ai_report_type", "sales_trend,supplier_performance,demand_forecast,product_analysis,revenue_summary")
                .Annotation("Npgsql:Enum:bid_status.bid_status", "submitted,accepted,cancelled")
                .Annotation("Npgsql:Enum:contract_status.contract_status", "draft,pending_signature,signed,pending_deposit,active,completed_on_time,completed_late,breached,disputed,cancelled")
                .Annotation("Npgsql:Enum:custom_packaging_status.custom_packaging_status", "pending,design_review,approved,in_production,rejected")
                .Annotation("Npgsql:Enum:delivery_status.delivery_status", "pending,confirmed,picked_up,in_transit,delivered,failed,returned")
                .Annotation("Npgsql:Enum:delivery_type.delivery_type", "platform_managed,self_arranged")
                .Annotation("Npgsql:Enum:discount_type.discount_type", "percentage,fixed_amount,fixed_price")
                .Annotation("Npgsql:Enum:milestone_status.milestone_status", "pending,awaiting_payment,paid,completed_on_time,completed_late,overdue,failed")
                .Annotation("Npgsql:Enum:order_status.order_status", "pending,confirmed,processing,shipped,delivered,cancelled")
                .Annotation("Npgsql:Enum:payment_method.payment_method", "bank_transfer,momo,vnpay,zalopay,cash")
                .Annotation("Npgsql:Enum:payment_status.payment_status", "pending,processing,completed,failed,refunded")
                .Annotation("Npgsql:Enum:product_status.product_status", "active,inactive,out_of_stock")
                .Annotation("Npgsql:Enum:proof_type.proof_type", "pickup,in_transit,delivery,failed_attempt")
                .Annotation("Npgsql:Enum:reputation_event.reputation_event", "completed_on_time,completed_late,breached,positive_review,negative_review,admin_penalty,admin_bonus")
                .Annotation("Npgsql:Enum:rfq_invitation_status.rfq_invitation_status", "invited,viewed,bid_submitted,declined")
                .Annotation("Npgsql:Enum:rfq_status.rfq_status", "draft,open,in_negotiation,awarded,closed,cancelled")
                .Annotation("Npgsql:Enum:shop_status.shop_status", "active,inactive,suspended")
                .Annotation("Npgsql:Enum:user_role.user_role", "customer,supplier,admin")
                .Annotation("Npgsql:PostgresExtension:extensions.pgcrypto", ",,")
                .Annotation("Npgsql:PostgresExtension:extensions.uuid-ossp", ",,")
                .Annotation("Npgsql:PostgresExtension:vector", ",,")
                .OldAnnotation("Npgsql:Enum:ai_conversation_type.AiConversationType", "ProductInfo,MooncakeHistory,SalesAnalytics,General")
                .OldAnnotation("Npgsql:Enum:ai_message_role.AiMessageRole", "User,Assistant,System")
                .OldAnnotation("Npgsql:Enum:ai_report_status.AiReportStatus", "Pending,Processing,Completed,Failed")
                .OldAnnotation("Npgsql:Enum:ai_report_type.AiReportType", "SalesTrend,SupplierPerformance,DemandForecast,ProductAnalysis,RevenueSummary")
                .OldAnnotation("Npgsql:Enum:bid_status.BidStatus", "Submitted,Accepted,Cancelled")
                .OldAnnotation("Npgsql:Enum:contract_status.ContractStatus", "Draft,PendingSignature,Signed,PendingDeposit,Active,CompletedOnTime,CompletedLate,Breached,Disputed,Cancelled")
                .OldAnnotation("Npgsql:Enum:custom_packaging_status.CustomPackagingStatus", "Pending,DesignReview,Approved,InProduction,Rejected")
                .OldAnnotation("Npgsql:Enum:delivery_status.DeliveryStatus", "Pending,Confirmed,PickedUp,InTransit,Delivered,Failed,Returned")
                .OldAnnotation("Npgsql:Enum:delivery_type.DeliveryType", "PlatformManaged,SelfArranged")
                .OldAnnotation("Npgsql:Enum:discount_type.DiscountType", "Percentage,FixedAmount,FixedPrice")
                .OldAnnotation("Npgsql:Enum:milestone_status.MilestoneStatus", "Pending,AwaitingPayment,Paid,CompletedOnTime,CompletedLate,Overdue,Failed")
                .OldAnnotation("Npgsql:Enum:order_status.OrderStatus", "Pending,Confirmed,Processing,Shipped,Delivered,Cancelled")
                .OldAnnotation("Npgsql:Enum:payment_method.PaymentMethod", "BankTransfer,Momo,VNPay,ZaloPay,Cash")
                .OldAnnotation("Npgsql:Enum:payment_status.PaymentStatus", "Pending,Processing,Completed,Failed,Refunded")
                .OldAnnotation("Npgsql:Enum:product_status.ProductStatus", "Active,Inactive,OutOfStock")
                .OldAnnotation("Npgsql:Enum:proof_type.ProofType", "Pickup,InTransit,Delivery,FailedAttempt")
                .OldAnnotation("Npgsql:Enum:reputation_event.ReputationEvent", "CompletedOnTime,CompletedLate,Breached,PositiveReview,NegativeReview,AdminPenalty,AdminBonus")
                .OldAnnotation("Npgsql:Enum:rfq_invitation_status.RfqInvitationStatus", "Invited,Viewed,BidSubmitted,Declined")
                .OldAnnotation("Npgsql:Enum:rfq_status.RfqStatus", "Draft,Open,InNegotiation,Awarded,Closed,Cancelled")
                .OldAnnotation("Npgsql:Enum:shop_status.ShopStatus", "Active,Inactive,Suspended")
                .OldAnnotation("Npgsql:Enum:user_role.UserRole", "Customer,Supplier,Admin")
                .OldAnnotation("Npgsql:PostgresExtension:extensions.pgcrypto", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:extensions.uuid-ossp", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "supplier_profiles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "supplier_profiles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "verified",
                table: "supplier_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("UPDATE supplier_profiles SET verified = (verification_status_legacy = 'Verified')");
            migrationBuilder.DropColumn(name: "verification_status_legacy", table: "supplier_profiles");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "shops",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "shops",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "shop_templates",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "shop_templates",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "rfq_items",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "rfq_items",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "rfq_invitations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "rfq_invitations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "reviews",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "reviews",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "request_for_quotations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "request_for_quotations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "reputation_logs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "reputation_logs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "promotion_rules",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "promotion_rules",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "products",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "products",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "product_variants",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "product_variants",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "product_reviews",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "product_reviews",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "product_images",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "product_images",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "payments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "payments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "orders",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "orders",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "order_items",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "order_items",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "notifications",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "notifications",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "delivery_proofs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "delivery_proofs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "deliveries",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "deliveries",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "customer_profiles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "customer_profiles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "custom_packagings",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "custom_packagings",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "contracts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "contracts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "contract_milestones",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "contract_milestones",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "contract_deposit_rules",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "contract_deposit_rules",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "categories",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "categories",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "bids",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "bids",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "bid_items",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "bid_items",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "ai_messages",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "ai_messages",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "ai_conversations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "ai_conversations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "ai_analytics_reports",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "ai_analytics_reports",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "idx_sp_user_id",
                table: "supplier_profiles",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_sp_verified",
                table: "supplier_profiles",
                column: "verified",
                filter: "(is_deleted = false)");

            migrationBuilder.CreateIndex(
                name: "idx_shops_supplier_id",
                table: "shops",
                column: "supplier_id");
        }
    }
}
