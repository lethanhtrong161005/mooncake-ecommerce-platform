namespace Mooncake.EcommercePlatform.Infrastructure.Migrations;

/// <summary>Fail-closed legacy preflight and deterministic storefront seed.</summary>
internal static class ShopManagementSql
{
    internal const string Preflight = """
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
        """;
    internal const string Backfill = """
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
        """;
}
