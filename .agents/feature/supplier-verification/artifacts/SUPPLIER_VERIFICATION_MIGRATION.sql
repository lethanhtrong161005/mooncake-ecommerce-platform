START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024035322_SupplierVerificationWorkflow') THEN
    ALTER TABLE users ADD notification_sequence bigint NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024035322_SupplierVerificationWorkflow') THEN
    ALTER TABLE supplier_verification_documents ADD owner_user_id uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024035322_SupplierVerificationWorkflow') THEN
    ALTER TABLE supplier_verification_documents ADD provider_delivery_type character varying(30);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024035322_SupplierVerificationWorkflow') THEN
    ALTER TABLE supplier_verification_documents ADD provider_public_id character varying(1000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024035322_SupplierVerificationWorkflow') THEN
    ALTER TABLE supplier_verification_documents ADD provider_resource_type character varying(20);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024035322_SupplierVerificationWorkflow') THEN
    ALTER TABLE supplier_verification_documents ADD sha256 character varying(64);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024035322_SupplierVerificationWorkflow') THEN
    ALTER TABLE supplier_verification_documents ADD submission_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024035322_SupplierVerificationWorkflow') THEN
    ALTER TABLE supplier_verification_documents ADD upload_operation_id uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024035322_SupplierVerificationWorkflow') THEN
    ALTER TABLE supplier_verification_documents ADD upload_request_hash character varying(64);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024035322_SupplierVerificationWorkflow') THEN
    ALTER TABLE supplier_verification_documents ADD upload_status character varying(30) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024035322_SupplierVerificationWorkflow') THEN
    ALTER TABLE supplier_verification_documents ADD validation_completed_at_utc timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024035322_SupplierVerificationWorkflow') THEN
    ALTER TABLE supplier_profiles ADD current_submission_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024035322_SupplierVerificationWorkflow') THEN
    ALTER TABLE supplier_profiles ADD last_approved_submission_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024035322_SupplierVerificationWorkflow') THEN
    ALTER TABLE supplier_profiles ADD last_submitted_at_utc timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024035322_SupplierVerificationWorkflow') THEN
    ALTER TABLE supplier_profiles ADD revocation_reason character varying(1000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024035322_SupplierVerificationWorkflow') THEN
    ALTER TABLE supplier_profiles ADD revoked_at_utc timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024035322_SupplierVerificationWorkflow') THEN
    ALTER TABLE supplier_profiles ADD revoked_by_user_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024035322_SupplierVerificationWorkflow') THEN
    ALTER TABLE supplier_profiles ADD version bigint NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024035322_SupplierVerificationWorkflow') THEN
    ALTER TABLE notifications ADD aggregate_version bigint;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024035322_SupplierVerificationWorkflow') THEN
    ALTER TABLE notifications ADD audience_kind character varying(20);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024035322_SupplierVerificationWorkflow') THEN
    ALTER TABLE notifications ADD read_at_utc timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024035322_SupplierVerificationWorkflow') THEN
    ALTER TABLE notifications ADD sequence bigint;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024035322_SupplierVerificationWorkflow') THEN
    ALTER TABLE notifications ADD source_event_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024035322_SupplierVerificationWorkflow') THEN
    CREATE TABLE supplier_verification_submissions (
        id uuid NOT NULL,
        supplier_profile_id uuid NOT NULL,
        revision integer NOT NULL,
        company_name character varying(255) NOT NULL,
        tax_code character varying(50) NOT NULL,
        address character varying(500) NOT NULL,
        description character varying(2000),
        status character varying(20) NOT NULL,
        submitted_at_utc timestamp with time zone NOT NULL,
        submitted_by_user_id uuid NOT NULL,
        reviewed_at_utc timestamp with time zone,
        reviewed_by_user_id uuid,
        rejection_reason character varying(1000),
        source text NOT NULL,
        is_deleted boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        created_by uuid,
        updated_by uuid,
        CONSTRAINT "PK_supplier_verification_submissions" PRIMARY KEY (id),
        CONSTRAINT "FK_supplier_verification_submissions_supplier_profiles_supplie~" FOREIGN KEY (supplier_profile_id) REFERENCES supplier_profiles (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024035322_SupplierVerificationWorkflow') THEN
    CREATE TABLE verification_idempotency_records (
        id uuid NOT NULL,
        actor_user_id uuid NOT NULL,
        operation character varying(100) NOT NULL,
        resource_id uuid NOT NULL,
        key uuid NOT NULL,
        request_hash character varying(64) NOT NULL,
        result_json text NOT NULL,
        expires_at_utc timestamp with time zone NOT NULL,
        is_deleted boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        created_by uuid,
        updated_by uuid,
        CONSTRAINT "PK_verification_idempotency_records" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024035322_SupplierVerificationWorkflow') THEN
    CREATE TABLE verification_outbox_messages (
        id uuid NOT NULL,
        event_id uuid NOT NULL,
        aggregate_id uuid NOT NULL,
        aggregate_version bigint NOT NULL,
        event_type text NOT NULL,
        channel text NOT NULL,
        owner_user_id uuid NOT NULL,
        status text NOT NULL,
        attempt_count integer NOT NULL,
        next_attempt_at_utc timestamp with time zone NOT NULL,
        lease_until_utc timestamp with time zone,
        lease_token uuid,
        last_error_code text,
        is_deleted boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        created_by uuid,
        updated_by uuid,
        CONSTRAINT "PK_verification_outbox_messages" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024035322_SupplierVerificationWorkflow') THEN
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024035322_SupplierVerificationWorkflow') THEN
    CREATE UNIQUE INDEX "IX_supplier_verification_documents_owner_user_id_upload_operat~" ON supplier_verification_documents (owner_user_id, upload_operation_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024035322_SupplierVerificationWorkflow') THEN
    CREATE INDEX "IX_supplier_verification_documents_submission_id" ON supplier_verification_documents (submission_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024035322_SupplierVerificationWorkflow') THEN
    CREATE INDEX "IX_supplier_profiles_verification_status_last_submitted_at_utc~" ON supplier_profiles (verification_status, last_submitted_at_utc, id) WHERE is_deleted = false AND current_submission_id IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024035322_SupplierVerificationWorkflow') THEN
    CREATE UNIQUE INDEX "IX_notifications_user_id_sequence" ON notifications (user_id, sequence) WHERE sequence IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024035322_SupplierVerificationWorkflow') THEN
    CREATE UNIQUE INDEX "IX_notifications_user_id_source_event_id_type" ON notifications (user_id, source_event_id, type) WHERE source_event_id IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024035322_SupplierVerificationWorkflow') THEN
    CREATE UNIQUE INDEX "IX_supplier_verification_submissions_supplier_profile_id" ON supplier_verification_submissions (supplier_profile_id) WHERE status = 'Pending' AND is_deleted = false;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024035322_SupplierVerificationWorkflow') THEN
    CREATE UNIQUE INDEX "IX_supplier_verification_submissions_supplier_profile_id_revis~" ON supplier_verification_submissions (supplier_profile_id, revision);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024035322_SupplierVerificationWorkflow') THEN
    CREATE UNIQUE INDEX "IX_verification_idempotency_records_actor_user_id_operation_re~" ON verification_idempotency_records (actor_user_id, operation, resource_id, key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024035322_SupplierVerificationWorkflow') THEN
    CREATE UNIQUE INDEX "IX_verification_outbox_messages_event_id_channel" ON verification_outbox_messages (event_id, channel);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024035322_SupplierVerificationWorkflow') THEN
    CREATE INDEX "IX_verification_outbox_messages_status_next_attempt_at_utc" ON verification_outbox_messages (status, next_attempt_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024035322_SupplierVerificationWorkflow') THEN
    ALTER TABLE supplier_verification_documents ADD CONSTRAINT "FK_supplier_verification_documents_supplier_verification_submi~" FOREIGN KEY (submission_id) REFERENCES supplier_verification_submissions (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024035322_SupplierVerificationWorkflow') THEN
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024035322_SupplierVerificationWorkflow') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261024035322_SupplierVerificationWorkflow', '8.0.11');
    END IF;
END $EF$;
COMMIT;

