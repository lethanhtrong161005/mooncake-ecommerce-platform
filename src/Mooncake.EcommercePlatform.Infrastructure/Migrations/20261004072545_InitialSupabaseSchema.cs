using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Mooncake.EcommercePlatform.Domain.Enums;
using Pgvector;

#nullable disable

namespace Mooncake.EcommercePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialSupabaseSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateTable(
                name: "ai_analytics_reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    requested_by = table.Column<Guid>(type: "uuid", nullable: false),
                    parameters = table.Column<string>(type: "jsonb", nullable: true),
                    result_data = table.Column<string>(type: "jsonb", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    report_type = table.Column<AiReportType>(type: "ai_report_type", nullable: false),
                    status = table.Column<AiReportStatus>(type: "ai_report_status", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ai_analytics_reports_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ai_conversations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_token = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    conversation_type = table.Column<AiConversationType>(type: "ai_conversation_type", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ai_conversations_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ai_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    tokens_used = table.Column<int>(type: "integer", nullable: true),
                    model_used = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    role = table.Column<AiMessageRole>(type: "ai_message_role", nullable: false),
                    embedding = table.Column<Vector>(type: "vector(1536)", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ai_messages_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "bid_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    bid_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rfq_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("bid_items_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "bids",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    rfq_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bid_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    total_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    estimated_delivery_days = table.Column<int>(type: "integer", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    validity_days = table.Column<int>(type: "integer", nullable: false, defaultValue: 30),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    status = table.Column<BidStatus>(type: "bid_status", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("bids_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    icon_url = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("categories_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "contract_deposit_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    deposit_percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    is_mandatory = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    applies_to_all = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    min_contract_value = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    payment_deadline_hours = table.Column<int>(type: "integer", nullable: false, defaultValue: 48),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("contract_deposit_rules_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "contract_milestones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence_number = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deposit = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    completion_date = table.Column<DateOnly>(type: "date", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    status = table.Column<MilestoneStatus>(type: "milestone_status", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("contract_milestones_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "contracts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    rfq_id = table.Column<Guid>(type: "uuid", nullable: true),
                    bid_id = table.Column<Guid>(type: "uuid", nullable: true),
                    order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    deposit_rule_id = table.Column<Guid>(type: "uuid", nullable: true),
                    contract_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    total_value = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    delivery_deadline = table.Column<DateOnly>(type: "date", nullable: true),
                    terms_and_conditions = table.Column<string>(type: "text", nullable: true),
                    penalty_terms = table.Column<string>(type: "text", nullable: true),
                    deposit_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    deposit_paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    signed_by_customer_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    signed_by_supplier_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<ContractStatus>(type: "contract_status", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("contracts_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "custom_packagings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    company_logo_url = table.Column<string>(type: "text", nullable: true),
                    box_design_url = table.Column<string>(type: "text", nullable: true),
                    packaging_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    color_scheme = table.Column<string>(type: "jsonb", nullable: true),
                    special_message = table.Column<string>(type: "text", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<CustomPackagingStatus>(type: "custom_packaging_status", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("custom_packagings_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "customer_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    company_logo_url = table.Column<string>(type: "text", nullable: true),
                    tax_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    billing_address = table.Column<string>(type: "text", nullable: true),
                    contact_person = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("customer_profiles_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "deliveries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: true),
                    order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    delivery_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    scheduled_date = table.Column<DateOnly>(type: "date", nullable: true),
                    actual_delivery_date = table.Column<DateOnly>(type: "date", nullable: true),
                    from_address = table.Column<string>(type: "text", nullable: true),
                    to_address = table.Column<string>(type: "text", nullable: true),
                    recipient_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    recipient_phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    tracking_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    delivery_type = table.Column<DeliveryType>(type: "delivery_type", nullable: false),
                    status = table.Column<DeliveryStatus>(type: "delivery_status", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("deliveries_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "delivery_proofs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    delivery_id = table.Column<Guid>(type: "uuid", nullable: false),
                    photo_url = table.Column<string>(type: "text", nullable: true),
                    taken_by = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    taken_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    location_lat = table.Column<decimal>(type: "numeric(10,8)", precision: 10, scale: 8, nullable: true),
                    location_lng = table.Column<decimal>(type: "numeric(11,8)", precision: 11, scale: 8, nullable: true),
                    signature_url = table.Column<string>(type: "text", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    proof_type = table.Column<ProofType>(type: "proof_type", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("delivery_proofs_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    content = table.Column<string>(type: "text", nullable: true),
                    is_read = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    reference_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reference_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("notifications_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "order_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    promotion_rule_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("order_items_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    shipping_fee = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    delivery_address = table.Column<string>(type: "text", nullable: true),
                    delivery_date_expected = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<OrderStatus>(type: "order_status", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("orders_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: true),
                    milestone_id = table.Column<Guid>(type: "uuid", nullable: true),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    transaction_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    method = table.Column<PaymentMethod>(type: "payment_method", nullable: false),
                    status = table.Column<PaymentStatus>(type: "payment_status", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("payments_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "product_images",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    image_url = table.Column<string>(type: "text", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("product_images_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "product_reviews",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_variant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    order_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rating = table.Column<short>(type: "smallint", nullable: true),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    content = table.Column<string>(type: "text", nullable: true),
                    media_urls = table.Column<string>(type: "jsonb", nullable: true),
                    is_verified_purchase = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    like_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    supplier_reply = table.Column<string>(type: "text", nullable: true),
                    supplier_replied_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_public = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_hidden_by_admin = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("product_reviews_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "product_variants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    sku = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    flavor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    filling = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    size_label = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    weight_gram = table.Column<int>(type: "integer", nullable: true),
                    price_adjustment = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    stock_qty = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    image_url = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("product_variants_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    shop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    base_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    min_order_qty = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    max_order_qty = table.Column<int>(type: "integer", nullable: true),
                    unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    supports_custom_packaging = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    status = table.Column<ProductStatus>(type: "product_status", nullable: false),
                    embedding = table.Column<Vector>(type: "vector(1536)", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("products_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "promotion_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    shop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    min_qty = table.Column<int>(type: "integer", nullable: true),
                    max_qty = table.Column<int>(type: "integer", nullable: true),
                    discount_value = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    discount_type = table.Column<DiscountType>(type: "discount_type", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("promotion_rules_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "reputation_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    score_delta = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    reference_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reference_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    event_type = table.Column<ReputationEvent>(type: "reputation_event", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("reputation_logs_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "request_for_quotations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rfq_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    delivery_address = table.Column<string>(type: "text", nullable: true),
                    delivery_date_required = table.Column<DateOnly>(type: "date", nullable: true),
                    budget_range_min = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    budget_range_max = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    bid_deadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<RfqStatus>(type: "rfq_status", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("request_for_quotations_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "reviews",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: true),
                    order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    overall_rating = table.Column<short>(type: "smallint", nullable: true),
                    quality_rating = table.Column<short>(type: "smallint", nullable: true),
                    delivery_rating = table.Column<short>(type: "smallint", nullable: true),
                    service_rating = table.Column<short>(type: "smallint", nullable: true),
                    comment = table.Column<string>(type: "text", nullable: true),
                    is_public = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("reviews_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "rfq_invitations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    rfq_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invited_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    responded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<RfqInvitationStatus>(type: "rfq_invitation_status", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("rfq_invitations_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "rfq_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    rfq_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    target_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    specifications = table.Column<string>(type: "text", nullable: true),
                    packaging_requirements = table.Column<string>(type: "text", nullable: true),
                    needs_custom_packaging = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("rfq_items_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "shop_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    preview_image_url = table.Column<string>(type: "text", nullable: true),
                    css_variables = table.Column<string>(type: "jsonb", nullable: true),
                    layout_config = table.Column<string>(type: "jsonb", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("shop_templates_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "shops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    slug = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    banner_url = table.Column<string>(type: "text", nullable: true),
                    logo_url = table.Column<string>(type: "text", nullable: true),
                    custom_colors = table.Column<string>(type: "jsonb", nullable: true),
                    custom_css = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<ShopStatus>(type: "shop_status", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("shops_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "supplier_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    company_logo_url = table.Column<string>(type: "text", nullable: true),
                    tax_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    reputation_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    total_contracts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    contracts_on_time = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    contracts_late = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    contracts_breached = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    avg_rating = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: true),
                    verified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("supplier_profiles_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    full_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    avatar_url = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    email_verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    role = table.Column<UserRole>(type: "user_role", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("users_pkey", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_aar_report_type",
                table: "ai_analytics_reports",
                column: "report_type");

            migrationBuilder.CreateIndex(
                name: "idx_aar_requested_by",
                table: "ai_analytics_reports",
                column: "requested_by",
                filter: "(is_deleted = false)");

            migrationBuilder.CreateIndex(
                name: "idx_aar_status",
                table: "ai_analytics_reports",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ai_conversations_session_token_key",
                table: "ai_conversations",
                column: "session_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_aic_type",
                table: "ai_conversations",
                column: "conversation_type");

            migrationBuilder.CreateIndex(
                name: "idx_aic_user_id",
                table: "ai_conversations",
                column: "user_id",
                filter: "(is_deleted = false)");

            migrationBuilder.CreateIndex(
                name: "idx_aim_conversation_id",
                table: "ai_messages",
                columns: new[] { "conversation_id", "created_at" },
                filter: "(is_deleted = false)");

            migrationBuilder.CreateIndex(
                name: "idx_bi_bid_id",
                table: "bid_items",
                column: "bid_id");

            migrationBuilder.CreateIndex(
                name: "idx_bi_rfq_item_id",
                table: "bid_items",
                column: "rfq_item_id");

            migrationBuilder.CreateIndex(
                name: "bids_bid_number_key",
                table: "bids",
                column: "bid_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_bids_rfq_id",
                table: "bids",
                column: "rfq_id",
                filter: "(is_deleted = false)");

            migrationBuilder.CreateIndex(
                name: "idx_bids_status",
                table: "bids",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_bids_supplier_id",
                table: "bids",
                column: "supplier_id",
                filter: "(is_deleted = false)");

            migrationBuilder.CreateIndex(
                name: "idx_bids_total_price",
                table: "bids",
                column: "total_price");

            migrationBuilder.CreateIndex(
                name: "idx_categories_parent_id",
                table: "categories",
                column: "parent_id",
                filter: "(parent_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "idx_categories_sort_order",
                table: "categories",
                column: "sort_order");

            migrationBuilder.CreateIndex(
                name: "idx_cdr_is_mandatory",
                table: "contract_deposit_rules",
                column: "is_mandatory",
                filter: "(is_deleted = false)");

            migrationBuilder.CreateIndex(
                name: "idx_cm_contract_id",
                table: "contract_milestones",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "idx_cm_due_date",
                table: "contract_milestones",
                column: "due_date",
                filter: "(is_deleted = false)");

            migrationBuilder.CreateIndex(
                name: "idx_cm_is_deposit",
                table: "contract_milestones",
                columns: new[] { "contract_id", "is_deposit" });

            migrationBuilder.CreateIndex(
                name: "idx_cm_status",
                table: "contract_milestones",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "contracts_contract_number_key",
                table: "contracts",
                column: "contract_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_contracts_bid_id",
                table: "contracts",
                column: "bid_id",
                filter: "(bid_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "idx_contracts_customer_id",
                table: "contracts",
                column: "customer_id",
                filter: "(is_deleted = false)");

            migrationBuilder.CreateIndex(
                name: "idx_contracts_deposit_rule_id",
                table: "contracts",
                column: "deposit_rule_id",
                filter: "(deposit_rule_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "idx_contracts_order_id",
                table: "contracts",
                column: "order_id",
                filter: "(order_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "idx_contracts_pending_deposit",
                table: "contracts",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_contracts_rfq_id",
                table: "contracts",
                column: "rfq_id",
                filter: "(rfq_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "idx_contracts_status",
                table: "contracts",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_contracts_supplier_id",
                table: "contracts",
                column: "supplier_id",
                filter: "(is_deleted = false)");

            migrationBuilder.CreateIndex(
                name: "idx_cpkg_order_id",
                table: "custom_packagings",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "idx_cpkg_status",
                table: "custom_packagings",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_cp_user_id",
                table: "customer_profiles",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "deliveries_delivery_number_key",
                table: "deliveries",
                column: "delivery_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_del_contract_id",
                table: "deliveries",
                column: "contract_id",
                filter: "(contract_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "idx_del_order_id",
                table: "deliveries",
                column: "order_id",
                filter: "(order_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "idx_del_status",
                table: "deliveries",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_del_tracking_code",
                table: "deliveries",
                column: "tracking_code",
                filter: "(tracking_code IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "idx_dp_delivery_id",
                table: "delivery_proofs",
                column: "delivery_id");

            migrationBuilder.CreateIndex(
                name: "idx_dp_proof_type",
                table: "delivery_proofs",
                column: "proof_type");

            migrationBuilder.CreateIndex(
                name: "idx_notif_type",
                table: "notifications",
                column: "type",
                filter: "(is_deleted = false)");

            migrationBuilder.CreateIndex(
                name: "idx_notif_unread",
                table: "notifications",
                columns: new[] { "user_id", "is_read" },
                filter: "((is_deleted = false) AND (is_read = false))");

            migrationBuilder.CreateIndex(
                name: "idx_notif_user_id",
                table: "notifications",
                columns: new[] { "user_id", "created_at" },
                descending: new[] { false, true },
                filter: "(is_deleted = false)");

            migrationBuilder.CreateIndex(
                name: "idx_oi_order_id",
                table: "order_items",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "idx_oi_product_id",
                table: "order_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "idx_oi_promotion_rule_id",
                table: "order_items",
                column: "promotion_rule_id",
                filter: "(promotion_rule_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "idx_oi_variant_id",
                table: "order_items",
                column: "variant_id");

            migrationBuilder.CreateIndex(
                name: "idx_orders_created_at",
                table: "orders",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_orders_customer_id",
                table: "orders",
                column: "customer_id",
                filter: "(is_deleted = false)");

            migrationBuilder.CreateIndex(
                name: "idx_orders_shop_id",
                table: "orders",
                column: "shop_id",
                filter: "(is_deleted = false)");

            migrationBuilder.CreateIndex(
                name: "idx_orders_status",
                table: "orders",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "orders_order_number_key",
                table: "orders",
                column: "order_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_pay_contract_id",
                table: "payments",
                column: "contract_id",
                filter: "(contract_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "idx_pay_customer_id",
                table: "payments",
                column: "customer_id",
                filter: "(is_deleted = false)");

            migrationBuilder.CreateIndex(
                name: "idx_pay_milestone_id",
                table: "payments",
                column: "milestone_id",
                filter: "(milestone_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "idx_pay_order_id",
                table: "payments",
                column: "order_id",
                filter: "(order_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "idx_pay_paid_at",
                table: "payments",
                column: "paid_at",
                descending: new bool[0],
                filter: "(paid_at IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "idx_pay_status",
                table: "payments",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "payments_payment_number_key",
                table: "payments",
                column: "payment_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_pi_is_primary",
                table: "product_images",
                columns: new[] { "product_id", "is_primary" });

            migrationBuilder.CreateIndex(
                name: "idx_pi_product_id",
                table: "product_images",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "idx_prev_admin_hidden",
                table: "product_reviews",
                column: "is_hidden_by_admin",
                filter: "(is_hidden_by_admin = true)");

            migrationBuilder.CreateIndex(
                name: "idx_prev_customer_id",
                table: "product_reviews",
                column: "customer_id",
                filter: "(is_deleted = false)");

            migrationBuilder.CreateIndex(
                name: "idx_prev_like_count",
                table: "product_reviews",
                columns: new[] { "product_id", "like_count" },
                descending: new[] { false, true },
                filter: "(is_deleted = false)");

            migrationBuilder.CreateIndex(
                name: "idx_prev_product_feed",
                table: "product_reviews",
                columns: new[] { "product_id", "created_at" },
                descending: new[] { false, true },
                filter: "((is_deleted = false) AND (is_hidden_by_admin = false))");

            migrationBuilder.CreateIndex(
                name: "idx_prev_rating",
                table: "product_reviews",
                columns: new[] { "product_id", "rating" },
                filter: "(is_deleted = false)");

            migrationBuilder.CreateIndex(
                name: "idx_prev_variant_id",
                table: "product_reviews",
                column: "product_variant_id",
                filter: "((product_variant_id IS NOT NULL) AND (is_deleted = false))");

            migrationBuilder.CreateIndex(
                name: "product_reviews_customer_id_order_item_id_key",
                table: "product_reviews",
                columns: new[] { "customer_id", "order_item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_pv_product_id",
                table: "product_variants",
                column: "product_id",
                filter: "(is_deleted = false)");

            migrationBuilder.CreateIndex(
                name: "idx_pv_stock_qty",
                table: "product_variants",
                column: "stock_qty",
                filter: "(is_deleted = false)");

            migrationBuilder.CreateIndex(
                name: "product_variants_sku_key",
                table: "product_variants",
                column: "sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_products_base_price",
                table: "products",
                column: "base_price");

            migrationBuilder.CreateIndex(
                name: "idx_products_category_id",
                table: "products",
                column: "category_id",
                filter: "(is_deleted = false)");

            migrationBuilder.CreateIndex(
                name: "idx_products_name_trgm",
                table: "products",
                column: "name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "idx_products_shop_id",
                table: "products",
                column: "shop_id",
                filter: "(is_deleted = false)");

            migrationBuilder.CreateIndex(
                name: "idx_products_status",
                table: "products",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_pr_active",
                table: "promotion_rules",
                columns: new[] { "is_active", "start_date", "end_date" },
                filter: "(is_deleted = false)");

            migrationBuilder.CreateIndex(
                name: "idx_pr_product_id",
                table: "promotion_rules",
                column: "product_id",
                filter: "((product_id IS NOT NULL) AND (is_deleted = false))");

            migrationBuilder.CreateIndex(
                name: "idx_pr_shop_id",
                table: "promotion_rules",
                column: "shop_id",
                filter: "(is_deleted = false)");

            migrationBuilder.CreateIndex(
                name: "idx_rl_event_type",
                table: "reputation_logs",
                column: "event_type");

            migrationBuilder.CreateIndex(
                name: "idx_rl_reference",
                table: "reputation_logs",
                columns: new[] { "reference_id", "reference_type" },
                filter: "(reference_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "idx_rl_supplier_id",
                table: "reputation_logs",
                columns: new[] { "supplier_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_rfq_bid_deadline",
                table: "request_for_quotations",
                column: "bid_deadline",
                filter: "(is_deleted = false)");

            migrationBuilder.CreateIndex(
                name: "idx_rfq_customer_id",
                table: "request_for_quotations",
                column: "customer_id",
                filter: "(is_deleted = false)");

            migrationBuilder.CreateIndex(
                name: "idx_rfq_status",
                table: "request_for_quotations",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "request_for_quotations_rfq_number_key",
                table: "request_for_quotations",
                column: "rfq_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_rev_contract_id",
                table: "reviews",
                column: "contract_id",
                filter: "(contract_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "idx_rev_customer_id",
                table: "reviews",
                column: "customer_id",
                filter: "(is_deleted = false)");

            migrationBuilder.CreateIndex(
                name: "idx_rev_order_id",
                table: "reviews",
                column: "order_id",
                filter: "(order_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "idx_rev_overall_rating",
                table: "reviews",
                columns: new[] { "supplier_id", "overall_rating" },
                filter: "(is_deleted = false)");

            migrationBuilder.CreateIndex(
                name: "idx_rev_supplier_id",
                table: "reviews",
                column: "supplier_id",
                filter: "(is_deleted = false)");

            migrationBuilder.CreateIndex(
                name: "idx_rfqinv_rfq_id",
                table: "rfq_invitations",
                column: "rfq_id");

            migrationBuilder.CreateIndex(
                name: "idx_rfqinv_status",
                table: "rfq_invitations",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_rfqinv_supplier_id",
                table: "rfq_invitations",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "rfq_invitations_rfq_id_supplier_id_key",
                table: "rfq_invitations",
                columns: new[] { "rfq_id", "supplier_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_rfqi_rfq_id",
                table: "rfq_items",
                column: "rfq_id");

            migrationBuilder.CreateIndex(
                name: "idx_shops_slug",
                table: "shops",
                column: "slug");

            migrationBuilder.CreateIndex(
                name: "idx_shops_status",
                table: "shops",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_shops_supplier_id",
                table: "shops",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "idx_shops_template_id",
                table: "shops",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "shops_slug_key",
                table: "shops",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_sp_reputation_score",
                table: "supplier_profiles",
                column: "reputation_score",
                descending: new bool[0],
                filter: "(is_deleted = false)");

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
                name: "supplier_profiles_tax_code_key",
                table: "supplier_profiles",
                column: "tax_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_users_email",
                table: "users",
                column: "email",
                filter: "(is_deleted = false)");

            migrationBuilder.CreateIndex(
                name: "idx_users_is_active",
                table: "users",
                column: "is_active",
                filter: "(is_deleted = false)");

            migrationBuilder.CreateIndex(
                name: "idx_users_role",
                table: "users",
                column: "role");

            migrationBuilder.CreateIndex(
                name: "users_email_key",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_analytics_reports");

            migrationBuilder.DropTable(
                name: "ai_conversations");

            migrationBuilder.DropTable(
                name: "ai_messages");

            migrationBuilder.DropTable(
                name: "bid_items");

            migrationBuilder.DropTable(
                name: "bids");

            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropTable(
                name: "contract_deposit_rules");

            migrationBuilder.DropTable(
                name: "contract_milestones");

            migrationBuilder.DropTable(
                name: "contracts");

            migrationBuilder.DropTable(
                name: "custom_packagings");

            migrationBuilder.DropTable(
                name: "customer_profiles");

            migrationBuilder.DropTable(
                name: "deliveries");

            migrationBuilder.DropTable(
                name: "delivery_proofs");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "order_items");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "product_images");

            migrationBuilder.DropTable(
                name: "product_reviews");

            migrationBuilder.DropTable(
                name: "product_variants");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "promotion_rules");

            migrationBuilder.DropTable(
                name: "reputation_logs");

            migrationBuilder.DropTable(
                name: "request_for_quotations");

            migrationBuilder.DropTable(
                name: "reviews");

            migrationBuilder.DropTable(
                name: "rfq_invitations");

            migrationBuilder.DropTable(
                name: "rfq_items");

            migrationBuilder.DropTable(
                name: "shop_templates");

            migrationBuilder.DropTable(
                name: "shops");

            migrationBuilder.DropTable(
                name: "supplier_profiles");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
