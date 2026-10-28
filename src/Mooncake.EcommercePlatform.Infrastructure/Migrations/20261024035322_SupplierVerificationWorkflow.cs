using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mooncake.EcommercePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SupplierVerificationWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "notification_sequence",
                table: "users",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<Guid>(
                name: "owner_user_id",
                table: "supplier_verification_documents",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "provider_delivery_type",
                table: "supplier_verification_documents",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider_public_id",
                table: "supplier_verification_documents",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider_resource_type",
                table: "supplier_verification_documents",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sha256",
                table: "supplier_verification_documents",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "submission_id",
                table: "supplier_verification_documents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "upload_operation_id",
                table: "supplier_verification_documents",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "upload_request_hash",
                table: "supplier_verification_documents",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "upload_status",
                table: "supplier_verification_documents",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "validation_completed_at_utc",
                table: "supplier_verification_documents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "current_submission_id",
                table: "supplier_profiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "last_approved_submission_id",
                table: "supplier_profiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_submitted_at_utc",
                table: "supplier_profiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "revocation_reason",
                table: "supplier_profiles",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "revoked_at_utc",
                table: "supplier_profiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "revoked_by_user_id",
                table: "supplier_profiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "version",
                table: "supplier_profiles",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "aggregate_version",
                table: "notifications",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "audience_kind",
                table: "notifications",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "read_at_utc",
                table: "notifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "sequence",
                table: "notifications",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "source_event_id",
                table: "notifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "supplier_verification_submissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    revision = table.Column<int>(type: "integer", nullable: false),
                    company_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    tax_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    submitted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    submitted_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reviewed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    source = table.Column<string>(type: "text", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supplier_verification_submissions", x => x.id);
                    table.ForeignKey(
                        name: "FK_supplier_verification_submissions_supplier_profiles_supplie~",
                        column: x => x.supplier_profile_id,
                        principalTable: "supplier_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "verification_idempotency_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<Guid>(type: "uuid", nullable: false),
                    request_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    result_json = table.Column<string>(type: "text", nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_verification_idempotency_records", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "verification_outbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    aggregate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    aggregate_version = table.Column<long>(type: "bigint", nullable: false),
                    event_type = table.Column<string>(type: "text", nullable: false),
                    channel = table.Column<string>(type: "text", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    next_attempt_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    lease_until_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    lease_token = table.Column<Guid>(type: "uuid", nullable: true),
                    last_error_code = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_verification_outbox_messages", x => x.id);
                });

            // Import only observed legacy state. Unverified privacy/safety metadata is never inferred from an old URL.
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT regexp_replace(tax_code, '[[:space:]-]', '', 'g')
                        FROM supplier_profiles WHERE tax_code IS NOT NULL
                        GROUP BY regexp_replace(tax_code, '[[:space:]-]', '', 'g') HAVING count(*) > 1
                    ) THEN
                        RAISE EXCEPTION 'Resolve canonical supplier tax code duplicates before applying this migration.';
                    END IF;
                END $$;
                UPDATE supplier_profiles SET tax_code = regexp_replace(tax_code, '[[:space:]-]', '', 'g')
                    WHERE tax_code IS NOT NULL;
                INSERT INTO supplier_verification_submissions
                    (id, supplier_profile_id, revision, company_name, tax_code, address, description,
                     status, submitted_at_utc, submitted_by_user_id, reviewed_at_utc, reviewed_by_user_id,
                     rejection_reason, source, is_deleted, created_at, updated_at)
                SELECT id, id, 1, COALESCE(company_name, ''), COALESCE(tax_code, ''), COALESCE(address, ''),
                    description, verification_status, created_at, user_id, reviewed_at, reviewed_by_user_id,
                    rejection_reason, 'LegacyImport', false, created_at, updated_at
                FROM supplier_profiles WHERE is_deleted = false;
                UPDATE supplier_profiles SET current_submission_id = id, version = 1,
                    last_submitted_at_utc = created_at,
                    last_approved_submission_id = CASE WHEN verification_status IN ('Verified', 'Revoked') THEN id ELSE NULL END
                    WHERE is_deleted = false;
                UPDATE supplier_verification_documents d SET owner_user_id = p.user_id,
                    upload_operation_id = d."Id", upload_status = 'LegacyNeedsReconciliation',
                    submission_id = CASE WHEN p.is_deleted = false AND d."IsDeleted" = false THEN p.current_submission_id ELSE NULL END
                    FROM supplier_profiles p WHERE d."SupplierProfileId" = p.id;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_supplier_verification_documents_owner_user_id_upload_operat~",
                table: "supplier_verification_documents",
                columns: new[] { "owner_user_id", "upload_operation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_supplier_verification_documents_submission_id",
                table: "supplier_verification_documents",
                column: "submission_id");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_profiles_verification_status_last_submitted_at_utc~",
                table: "supplier_profiles",
                columns: new[] { "verification_status", "last_submitted_at_utc", "id" },
                filter: "is_deleted = false AND current_submission_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_user_id_sequence",
                table: "notifications",
                columns: new[] { "user_id", "sequence" },
                unique: true,
                filter: "sequence IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_user_id_source_event_id_type",
                table: "notifications",
                columns: new[] { "user_id", "source_event_id", "type" },
                unique: true,
                filter: "source_event_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_verification_submissions_supplier_profile_id",
                table: "supplier_verification_submissions",
                column: "supplier_profile_id",
                unique: true,
                filter: "status = 'Pending' AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_verification_submissions_supplier_profile_id_revis~",
                table: "supplier_verification_submissions",
                columns: new[] { "supplier_profile_id", "revision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_verification_idempotency_records_actor_user_id_operation_re~",
                table: "verification_idempotency_records",
                columns: new[] { "actor_user_id", "operation", "resource_id", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_verification_outbox_messages_event_id_channel",
                table: "verification_outbox_messages",
                columns: new[] { "event_id", "channel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_verification_outbox_messages_status_next_attempt_at_utc",
                table: "verification_outbox_messages",
                columns: new[] { "status", "next_attempt_at_utc" });

            migrationBuilder.AddForeignKey(
                name: "FK_supplier_verification_documents_supplier_verification_submi~",
                table: "supplier_verification_documents",
                column: "submission_id",
                principalTable: "supplier_verification_submissions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            // Composite references prevent cross-owner/cross-profile attachment even outside the application.
            // Deferred current-submission references allow atomic insertion of a profile and its first snapshot.
            migrationBuilder.Sql("""
                ALTER TABLE supplier_profiles ADD CONSTRAINT uq_sv_profile_owner UNIQUE (id, user_id);
                ALTER TABLE supplier_verification_submissions ADD CONSTRAINT uq_sv_submission_profile UNIQUE (supplier_profile_id, id);
                ALTER TABLE supplier_verification_submissions ADD CONSTRAINT fk_sv_submission_owner
                    FOREIGN KEY (supplier_profile_id, submitted_by_user_id) REFERENCES supplier_profiles(id, user_id);
                ALTER TABLE supplier_profiles ADD CONSTRAINT fk_sv_current_submission
                    FOREIGN KEY (id, current_submission_id) REFERENCES supplier_verification_submissions(supplier_profile_id, id)
                    DEFERRABLE INITIALLY DEFERRED;
                ALTER TABLE supplier_profiles ADD CONSTRAINT fk_sv_approved_submission
                    FOREIGN KEY (id, last_approved_submission_id) REFERENCES supplier_verification_submissions(supplier_profile_id, id)
                    DEFERRABLE INITIALLY DEFERRED;
                ALTER TABLE supplier_verification_documents ADD CONSTRAINT fk_sv_document_submission
                    FOREIGN KEY ("SupplierProfileId", submission_id) REFERENCES supplier_verification_submissions(supplier_profile_id, id)
                    DEFERRABLE INITIALLY DEFERRED;
                ALTER TABLE supplier_verification_documents ADD CONSTRAINT fk_sv_document_owner
                    FOREIGN KEY ("SupplierProfileId", owner_user_id) REFERENCES supplier_profiles(id, user_id);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE supplier_verification_documents DROP CONSTRAINT IF EXISTS fk_sv_document_owner;
                ALTER TABLE supplier_verification_documents DROP CONSTRAINT IF EXISTS fk_sv_document_submission;
                ALTER TABLE supplier_profiles DROP CONSTRAINT IF EXISTS fk_sv_current_submission;
                ALTER TABLE supplier_profiles DROP CONSTRAINT IF EXISTS fk_sv_approved_submission;
                ALTER TABLE supplier_verification_submissions DROP CONSTRAINT IF EXISTS fk_sv_submission_owner;
                ALTER TABLE supplier_profiles DROP CONSTRAINT IF EXISTS uq_sv_profile_owner;
                """);
            migrationBuilder.DropForeignKey(
                name: "FK_supplier_verification_documents_supplier_verification_submi~",
                table: "supplier_verification_documents");

            migrationBuilder.DropTable(
                name: "supplier_verification_submissions");

            migrationBuilder.DropTable(
                name: "verification_idempotency_records");

            migrationBuilder.DropTable(
                name: "verification_outbox_messages");

            migrationBuilder.DropIndex(
                name: "IX_supplier_verification_documents_owner_user_id_upload_operat~",
                table: "supplier_verification_documents");

            migrationBuilder.DropIndex(
                name: "IX_supplier_verification_documents_submission_id",
                table: "supplier_verification_documents");

            migrationBuilder.DropIndex(
                name: "IX_supplier_profiles_verification_status_last_submitted_at_utc~",
                table: "supplier_profiles");

            migrationBuilder.DropIndex(
                name: "IX_notifications_user_id_sequence",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "IX_notifications_user_id_source_event_id_type",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "notification_sequence",
                table: "users");

            migrationBuilder.DropColumn(
                name: "owner_user_id",
                table: "supplier_verification_documents");

            migrationBuilder.DropColumn(
                name: "provider_delivery_type",
                table: "supplier_verification_documents");

            migrationBuilder.DropColumn(
                name: "provider_public_id",
                table: "supplier_verification_documents");

            migrationBuilder.DropColumn(
                name: "provider_resource_type",
                table: "supplier_verification_documents");

            migrationBuilder.DropColumn(
                name: "sha256",
                table: "supplier_verification_documents");

            migrationBuilder.DropColumn(
                name: "submission_id",
                table: "supplier_verification_documents");

            migrationBuilder.DropColumn(
                name: "upload_operation_id",
                table: "supplier_verification_documents");

            migrationBuilder.DropColumn(
                name: "upload_request_hash",
                table: "supplier_verification_documents");

            migrationBuilder.DropColumn(
                name: "upload_status",
                table: "supplier_verification_documents");

            migrationBuilder.DropColumn(
                name: "validation_completed_at_utc",
                table: "supplier_verification_documents");

            migrationBuilder.DropColumn(
                name: "current_submission_id",
                table: "supplier_profiles");

            migrationBuilder.DropColumn(
                name: "last_approved_submission_id",
                table: "supplier_profiles");

            migrationBuilder.DropColumn(
                name: "last_submitted_at_utc",
                table: "supplier_profiles");

            migrationBuilder.DropColumn(
                name: "revocation_reason",
                table: "supplier_profiles");

            migrationBuilder.DropColumn(
                name: "revoked_at_utc",
                table: "supplier_profiles");

            migrationBuilder.DropColumn(
                name: "revoked_by_user_id",
                table: "supplier_profiles");

            migrationBuilder.DropColumn(
                name: "version",
                table: "supplier_profiles");

            migrationBuilder.DropColumn(
                name: "aggregate_version",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "audience_kind",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "read_at_utc",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "sequence",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "source_event_id",
                table: "notifications");
        }
    }
}
