START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    DO $$ BEGIN
    IF EXISTS (SELECT 1 FROM shops s LEFT JOIN supplier_profiles p ON p.id=s.supplier_id
    LEFT JOIN users u ON u.id=p.user_id WHERE p.id IS NULL OR u.id IS NULL)
    THEN RAISE EXCEPTION 'Shop preflight: reconcile supplier profile ownership.'; END IF;
    IF EXISTS (SELECT 1 FROM shops WHERE name IS NULL OR length(trim(name))<2 OR length(description)>2000
    OR slug !~ '^[a-z0-9]+(-[a-z0-9]+)*$' OR length(slug) NOT BETWEEN 3 AND 80
    OR slug IN ('me','admin','api','shop','shops','templates','products','reviews','categories','support','auth'))
    THEN RAISE EXCEPTION 'Shop preflight: reconcile invalid names, descriptions or canonical slugs.'; END IF;
    IF EXISTS (SELECT 1 FROM shops WHERE status='Suspended')
    THEN RAISE EXCEPTION 'Shop preflight: legacy suspensions require reviewed moderation reconciliation.'; END IF;
    IF EXISTS (SELECT 1 FROM shops s LEFT JOIN shop_templates t ON t.id=s.template_id WHERE t.id IS NULL)
    THEN RAISE EXCEPTION 'Shop preflight: orphan template reference.'; END IF;
    IF EXISTS (SELECT 1 FROM shop_templates WHERE lower(coalesce(name,'')) NOT IN
    ('classic','heritage','royal-gift','minimal-grid','artisan-story','modern-catalog'))
    THEN RAISE EXCEPTION 'Shop preflight: review unknown legacy renderer mappings.'; END IF;
    END $$;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    ALTER TABLE shops ALTER COLUMN slug TYPE character varying(80);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    UPDATE shops SET name = '' WHERE name IS NULL;
    ALTER TABLE shops ALTER COLUMN name SET NOT NULL;
    ALTER TABLE shops ALTER COLUMN name SET DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    ALTER TABLE shops ALTER COLUMN description TYPE character varying(2000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    UPDATE shops SET custom_colors = '{}' WHERE custom_colors IS NULL;
    ALTER TABLE shops ALTER COLUMN custom_colors SET NOT NULL;
    ALTER TABLE shops ALTER COLUMN custom_colors SET DEFAULT '{}';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    ALTER TABLE shops ADD banner_asset_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    ALTER TABLE shops ADD logo_asset_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    ALTER TABLE shops ADD owner_user_id uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    ALTER TABLE shops ADD pre_suspension_status shop_status;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    ALTER TABLE shops ADD public_address character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    ALTER TABLE shops ADD public_contact_email character varying(255);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    ALTER TABLE shops ADD public_contact_phone character varying(20);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    ALTER TABLE shops ADD suspended_at_utc timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    ALTER TABLE shops ADD suspended_by_user_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    ALTER TABLE shops ADD suspension_reason character varying(1000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    ALTER TABLE shops ADD template_revision_id uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    ALTER TABLE shops ADD version bigint NOT NULL DEFAULT 1;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    ALTER TABLE shop_templates ADD code character varying(50) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    ALTER TABLE shop_templates ADD current_revision_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    ALTER TABLE shop_templates ADD is_premium boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    ALTER TABLE shop_templates ADD preview_asset_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    ALTER TABLE shop_templates ADD version bigint NOT NULL DEFAULT 1;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    CREATE TABLE shop_idempotency_records (
        id uuid NOT NULL,
        actor_user_id uuid NOT NULL,
        operation character varying(100) NOT NULL,
        resource_id uuid NOT NULL,
        key uuid NOT NULL,
        request_hash character varying(64) NOT NULL,
        response_json jsonb NOT NULL,
        http_status integer NOT NULL,
        expires_at_utc timestamp with time zone NOT NULL,
        is_deleted boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        created_by uuid,
        updated_by uuid,
        CONSTRAINT "PK_shop_idempotency_records" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    CREATE TABLE shop_media_assets (
        id uuid NOT NULL,
        owner_user_id uuid NOT NULL,
        purpose character varying(30) NOT NULL,
        upload_operation_id uuid NOT NULL,
        request_hash character varying(64) NOT NULL,
        status character varying(30) NOT NULL,
        lease_token uuid NOT NULL,
        sha256 character varying(64),
        provider_public_id text,
        public_url text,
        content_type character varying(100),
        byte_count bigint NOT NULL,
        width integer NOT NULL,
        height integer NOT NULL,
        is_deleted boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        created_by uuid,
        updated_by uuid,
        CONSTRAINT "PK_shop_media_assets" PRIMARY KEY (id),
        CONSTRAINT uq_shop_media_owner UNIQUE (id, owner_user_id),
        CONSTRAINT ck_shop_media_purpose CHECK (purpose IN ('Logo','Banner','TemplatePreview')),
        CONSTRAINT ck_shop_media_status CHECK (status IN ('Uploading','Ready','Failed','Quarantined','Deleting','Deleted')),
        CONSTRAINT "FK_shop_media_assets_users_owner_user_id" FOREIGN KEY (owner_user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    CREATE TABLE shop_template_catalog_state (
        id integer NOT NULL,
        default_template_id uuid NOT NULL,
        version bigint NOT NULL,
        CONSTRAINT "PK_shop_template_catalog_state" PRIMARY KEY (id),
        CONSTRAINT ck_shop_catalog_singleton CHECK (id = 1 AND version > 0),
        CONSTRAINT "FK_shop_template_catalog_state_shop_templates_default_template~" FOREIGN KEY (default_template_id) REFERENCES shop_templates (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    CREATE TABLE shop_template_revisions (
        id uuid NOT NULL,
        template_id uuid NOT NULL,
        revision integer NOT NULL,
        renderer_key character varying(50) NOT NULL,
        schema_version integer NOT NULL,
        css_variables jsonb NOT NULL,
        layout_config jsonb NOT NULL,
        is_deleted boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        created_by uuid,
        updated_by uuid,
        CONSTRAINT "PK_shop_template_revisions" PRIMARY KEY (id),
        CONSTRAINT uq_shop_revision_template UNIQUE (template_id, id),
        CONSTRAINT ck_shop_revision CHECK (revision > 0 AND schema_version = 1 AND jsonb_typeof(css_variables) = 'object' AND jsonb_typeof(layout_config) = 'object'),
        CONSTRAINT "FK_shop_template_revisions_shop_templates_template_id" FOREIGN KEY (template_id) REFERENCES shop_templates (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    UPDATE shops s SET owner_user_id=p.user_id FROM supplier_profiles p WHERE s.supplier_id=p.id;
    UPDATE shop_templates SET code='legacy-'||replace(id::text,'-','');
    INSERT INTO shop_template_revisions
    (id,template_id,revision,renderer_key,schema_version,css_variables,layout_config,is_deleted,created_at,updated_at)
    SELECT id,id,1,CASE WHEN lower(name)='classic' THEN 'heritage' ELSE lower(name) END,1,
    '{"primaryColor":"#8C1524","backgroundColor":"#FAF8F6","textColor":"#222222","surfaceColor":"#FFFFFF","buttonTextColor":"#FFFFFF","borderRadiusPx":12,"fontScale":1}'::jsonb,'{"sectionOrder":["Hero","Story","Products","Reviews","Contact"],"productColumnsDesktop":3}'::jsonb,false,now(),now() FROM shop_templates;
    UPDATE shop_templates SET current_revision_id=id;
    UPDATE shops s SET template_revision_id=t.current_revision_id FROM shop_templates t WHERE s.template_id=t.id;
    INSERT INTO audit_logs ("Id","ActorUserId","Action","EntityName","EntityId","MetadataJson","IsDeleted","CreatedAt","UpdatedAt")
    SELECT uuid_generate_v4(),NULL,'ShopLegacyThemeImported','ShopManagement',id::text,
    jsonb_build_object('source','LegacyImport','previousCustomColors',custom_colors),false,now(),now()
    FROM shops WHERE custom_colors IS NOT NULL AND custom_colors <> '{}'::jsonb;
    UPDATE shops SET custom_colors='{}'::jsonb;
    INSERT INTO shop_templates (id,code,name,is_active,is_premium,sort_order,version,is_deleted,created_at,updated_at)
    VALUES ('90000000-0000-4000-8000-000000000001','heritage','heritage',false,false,10,1,false,now(),now());
    INSERT INTO shop_template_revisions (id,template_id,revision,renderer_key,schema_version,css_variables,layout_config,is_deleted,created_at,updated_at)
    VALUES ('91000000-0000-4000-8000-000000000001','90000000-0000-4000-8000-000000000001',1,'heritage',1,'{"primaryColor":"#8C1524","backgroundColor":"#FAF8F6","textColor":"#222222","surfaceColor":"#FFFFFF","buttonTextColor":"#FFFFFF","borderRadiusPx":12,"fontScale":1}'::jsonb,'{"sectionOrder":["Hero","Story","Products","Reviews","Contact"],"productColumnsDesktop":3}'::jsonb,false,now(),now());
    UPDATE shop_templates SET current_revision_id='91000000-0000-4000-8000-000000000001',is_active=true WHERE id='90000000-0000-4000-8000-000000000001';
    INSERT INTO shop_templates (id,code,name,is_active,is_premium,sort_order,version,is_deleted,created_at,updated_at)
    VALUES ('90000000-0000-4000-8000-000000000002','royal-gift','royal-gift',false,false,20,1,false,now(),now());
    INSERT INTO shop_template_revisions (id,template_id,revision,renderer_key,schema_version,css_variables,layout_config,is_deleted,created_at,updated_at)
    VALUES ('91000000-0000-4000-8000-000000000002','90000000-0000-4000-8000-000000000002',1,'royal-gift',1,'{"primaryColor":"#8C1524","backgroundColor":"#FAF8F6","textColor":"#222222","surfaceColor":"#FFFFFF","buttonTextColor":"#FFFFFF","borderRadiusPx":12,"fontScale":1}'::jsonb,'{"sectionOrder":["Hero","Story","Products","Reviews","Contact"],"productColumnsDesktop":3}'::jsonb,false,now(),now());
    UPDATE shop_templates SET current_revision_id='91000000-0000-4000-8000-000000000002',is_active=true WHERE id='90000000-0000-4000-8000-000000000002';
    INSERT INTO shop_templates (id,code,name,is_active,is_premium,sort_order,version,is_deleted,created_at,updated_at)
    VALUES ('90000000-0000-4000-8000-000000000003','minimal-grid','minimal-grid',false,false,30,1,false,now(),now());
    INSERT INTO shop_template_revisions (id,template_id,revision,renderer_key,schema_version,css_variables,layout_config,is_deleted,created_at,updated_at)
    VALUES ('91000000-0000-4000-8000-000000000003','90000000-0000-4000-8000-000000000003',1,'minimal-grid',1,'{"primaryColor":"#8C1524","backgroundColor":"#FAF8F6","textColor":"#222222","surfaceColor":"#FFFFFF","buttonTextColor":"#FFFFFF","borderRadiusPx":12,"fontScale":1}'::jsonb,'{"sectionOrder":["Hero","Story","Products","Reviews","Contact"],"productColumnsDesktop":3}'::jsonb,false,now(),now());
    UPDATE shop_templates SET current_revision_id='91000000-0000-4000-8000-000000000003',is_active=true WHERE id='90000000-0000-4000-8000-000000000003';
    INSERT INTO shop_templates (id,code,name,is_active,is_premium,sort_order,version,is_deleted,created_at,updated_at)
    VALUES ('90000000-0000-4000-8000-000000000004','artisan-story','artisan-story',false,false,40,1,false,now(),now());
    INSERT INTO shop_template_revisions (id,template_id,revision,renderer_key,schema_version,css_variables,layout_config,is_deleted,created_at,updated_at)
    VALUES ('91000000-0000-4000-8000-000000000004','90000000-0000-4000-8000-000000000004',1,'artisan-story',1,'{"primaryColor":"#8C1524","backgroundColor":"#FAF8F6","textColor":"#222222","surfaceColor":"#FFFFFF","buttonTextColor":"#FFFFFF","borderRadiusPx":12,"fontScale":1}'::jsonb,'{"sectionOrder":["Hero","Story","Products","Reviews","Contact"],"productColumnsDesktop":3}'::jsonb,false,now(),now());
    UPDATE shop_templates SET current_revision_id='91000000-0000-4000-8000-000000000004',is_active=true WHERE id='90000000-0000-4000-8000-000000000004';
    INSERT INTO shop_templates (id,code,name,is_active,is_premium,sort_order,version,is_deleted,created_at,updated_at)
    VALUES ('90000000-0000-4000-8000-000000000005','modern-catalog','modern-catalog',false,false,50,1,false,now(),now());
    INSERT INTO shop_template_revisions (id,template_id,revision,renderer_key,schema_version,css_variables,layout_config,is_deleted,created_at,updated_at)
    VALUES ('91000000-0000-4000-8000-000000000005','90000000-0000-4000-8000-000000000005',1,'modern-catalog',1,'{"primaryColor":"#8C1524","backgroundColor":"#FAF8F6","textColor":"#222222","surfaceColor":"#FFFFFF","buttonTextColor":"#FFFFFF","borderRadiusPx":12,"fontScale":1}'::jsonb,'{"sectionOrder":["Hero","Story","Products","Reviews","Contact"],"productColumnsDesktop":3}'::jsonb,false,now(),now());
    UPDATE shop_templates SET current_revision_id='91000000-0000-4000-8000-000000000005',is_active=true WHERE id='90000000-0000-4000-8000-000000000005';
    INSERT INTO shop_template_catalog_state (id,default_template_id,version)
    VALUES (1,'90000000-0000-4000-8000-000000000001',1);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    CREATE INDEX "IX_shops_banner_asset_id_owner_user_id" ON shops (banner_asset_id, owner_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    CREATE INDEX "IX_shops_logo_asset_id_owner_user_id" ON shops (logo_asset_id, owner_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    CREATE INDEX "IX_shops_status_created_at_id" ON shops (status, created_at, id) WHERE is_deleted = false;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    CREATE INDEX "IX_shops_supplier_id_owner_user_id" ON shops (supplier_id, owner_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    CREATE INDEX "IX_shops_suspended_by_user_id" ON shops (suspended_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    CREATE INDEX "IX_shops_template_id_template_revision_id" ON shops (template_id, template_revision_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    ALTER TABLE shops ADD CONSTRAINT ck_shop_colors CHECK (jsonb_typeof(custom_colors) = 'object');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    ALTER TABLE shops ADD CONSTRAINT ck_shop_slug CHECK (slug ~ '^[a-z0-9]+(-[a-z0-9]+)*$' AND length(slug) BETWEEN 3 AND 80);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    ALTER TABLE shops ADD CONSTRAINT ck_shop_suspension CHECK ((status = 'Suspended' AND pre_suspension_status IN ('Active','Inactive') AND suspended_at_utc IS NOT NULL AND suspended_by_user_id IS NOT NULL AND length(suspension_reason) >= 10) OR (status <> 'Suspended' AND pre_suspension_status IS NULL AND suspended_at_utc IS NULL AND suspended_by_user_id IS NULL AND suspension_reason IS NULL));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    ALTER TABLE shops ADD CONSTRAINT ck_shop_version CHECK (version > 0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    CREATE INDEX "IX_shop_templates_id_current_revision_id" ON shop_templates (id, current_revision_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    CREATE INDEX "IX_shop_templates_is_active_sort_order_code_id" ON shop_templates (is_active, sort_order, code, id) WHERE is_deleted = false;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    CREATE INDEX "IX_shop_templates_preview_asset_id" ON shop_templates (preview_asset_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    CREATE UNIQUE INDEX shop_templates_code_key ON shop_templates (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    ALTER TABLE shop_templates ADD CONSTRAINT ck_template_current CHECK (NOT is_active OR current_revision_id IS NOT NULL);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    ALTER TABLE shop_templates ADD CONSTRAINT ck_template_version CHECK (version > 0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    CREATE UNIQUE INDEX "IX_shop_idempotency_records_actor_user_id_operation_resource_i~" ON shop_idempotency_records (actor_user_id, operation, resource_id, key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    CREATE INDEX "IX_shop_idempotency_records_expires_at_utc" ON shop_idempotency_records (expires_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    CREATE INDEX "IX_shop_media_assets_owner_user_id_created_at" ON shop_media_assets (owner_user_id, created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    CREATE UNIQUE INDEX "IX_shop_media_assets_owner_user_id_upload_operation_id" ON shop_media_assets (owner_user_id, upload_operation_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    CREATE INDEX "IX_shop_template_catalog_state_default_template_id" ON shop_template_catalog_state (default_template_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    CREATE UNIQUE INDEX "IX_shop_template_revisions_template_id_revision" ON shop_template_revisions (template_id, revision);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    ALTER TABLE shop_templates ADD CONSTRAINT "FK_shop_templates_shop_media_assets_preview_asset_id" FOREIGN KEY (preview_asset_id) REFERENCES shop_media_assets (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    ALTER TABLE shop_templates ADD CONSTRAINT fk_template_current_revision FOREIGN KEY (id, current_revision_id) REFERENCES shop_template_revisions (template_id, id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    ALTER TABLE shops ADD CONSTRAINT "FK_shops_users_suspended_by_user_id" FOREIGN KEY (suspended_by_user_id) REFERENCES users (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    ALTER TABLE shops ADD CONSTRAINT fk_shop_banner_owner FOREIGN KEY (banner_asset_id, owner_user_id) REFERENCES shop_media_assets (id, owner_user_id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    ALTER TABLE shops ADD CONSTRAINT fk_shop_logo_owner FOREIGN KEY (logo_asset_id, owner_user_id) REFERENCES shop_media_assets (id, owner_user_id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    ALTER TABLE shops ADD CONSTRAINT fk_shop_supplier_owner FOREIGN KEY (supplier_id, owner_user_id) REFERENCES supplier_profiles (id, user_id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    ALTER TABLE shops ADD CONSTRAINT fk_shop_template_revision FOREIGN KEY (template_id, template_revision_id) REFERENCES shop_template_revisions (template_id, id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    ALTER TABLE shop_templates ALTER CONSTRAINT fk_template_current_revision DEFERRABLE INITIALLY DEFERRED;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025023429_ShopManagement') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261025023429_ShopManagement', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025031020_ShopPublicProductFeedIndex') THEN
    CREATE INDEX idx_shop_public_product_feed ON products (shop_id, created_at DESC, id) WHERE is_deleted = false AND status IN ('Active','OutOfStock');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261025031020_ShopPublicProductFeedIndex') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261025031020_ShopPublicProductFeedIndex', '8.0.11');
    END IF;
END $EF$;
COMMIT;

