-- Storefront design revision 2. Existing shops remain pinned to their revision;
-- the sample Da Nang shop is moved to the new heritage revision below.
BEGIN;

INSERT INTO shop_template_revisions
  (id, template_id, revision, renderer_key, schema_version, css_variables, layout_config,
   is_deleted, created_at, updated_at)
VALUES
  ('92000000-0000-4000-8000-000000000001','90000000-0000-4000-8000-000000000001',2,'heritage',1,
   '{"primaryColor":"#751624","backgroundColor":"#F8F4EC","textColor":"#312322","surfaceColor":"#FFFFFF","buttonTextColor":"#FFFFFF","borderRadiusPx":8,"fontScale":1}'::jsonb,
   '{"sectionOrder":["Hero","Products","Story","Reviews","Contact"],"productColumnsDesktop":3}'::jsonb,false,now(),now()),
  ('92000000-0000-4000-8000-000000000002','90000000-0000-4000-8000-000000000002',2,'royal-gift',1,
   '{"primaryColor":"#501A28","backgroundColor":"#F8F1E8","textColor":"#2F2224","surfaceColor":"#FFFFFF","buttonTextColor":"#FFFFFF","borderRadiusPx":10,"fontScale":1}'::jsonb,
   '{"sectionOrder":["Hero","Story","Products","Reviews","Contact"],"productColumnsDesktop":3}'::jsonb,false,now(),now()),
  ('92000000-0000-4000-8000-000000000003','90000000-0000-4000-8000-000000000003',2,'minimal-grid',1,
   '{"primaryColor":"#292D2B","backgroundColor":"#F7F7F4","textColor":"#252725","surfaceColor":"#FFFFFF","buttonTextColor":"#FFFFFF","borderRadiusPx":4,"fontScale":1}'::jsonb,
   '{"sectionOrder":["Hero","Products","Story","Reviews","Contact"],"productColumnsDesktop":4}'::jsonb,false,now(),now()),
  ('92000000-0000-4000-8000-000000000004','90000000-0000-4000-8000-000000000004',2,'artisan-story',1,
   '{"primaryColor":"#60422F","backgroundColor":"#FAF6EF","textColor":"#332A25","surfaceColor":"#FFFFFF","buttonTextColor":"#FFFFFF","borderRadiusPx":12,"fontScale":1}'::jsonb,
   '{"sectionOrder":["Hero","Story","Products","Reviews","Contact"],"productColumnsDesktop":2}'::jsonb,false,now(),now()),
  ('92000000-0000-4000-8000-000000000005','90000000-0000-4000-8000-000000000005',2,'modern-catalog',1,
   '{"primaryColor":"#243C43","backgroundColor":"#F2F6F5","textColor":"#253238","surfaceColor":"#FFFFFF","buttonTextColor":"#FFFFFF","borderRadiusPx":6,"fontScale":1}'::jsonb,
   '{"sectionOrder":["Hero","Products","Story","Reviews","Contact"],"productColumnsDesktop":3}'::jsonb,false,now(),now())
ON CONFLICT (id) DO NOTHING;

UPDATE shop_templates t
SET current_revision_id = r.id, name = CASE t.code
  WHEN 'heritage' THEN 'Di sản Nguyệt Phố'
  WHEN 'royal-gift' THEN 'Quà tặng Hoàng gia'
  WHEN 'minimal-grid' THEN 'Tối giản Tinh tuyển'
  WHEN 'artisan-story' THEN 'Chuyện người làm bánh'
  WHEN 'modern-catalog' THEN 'Danh mục Đương đại' END,
  version = t.version + 1, updated_at = now()
FROM shop_template_revisions r
WHERE r.template_id = t.id AND r.revision = 2 AND t.current_revision_id IS DISTINCT FROM r.id;

UPDATE shops
SET template_revision_id = '92000000-0000-4000-8000-000000000001',
    version = version + 1, updated_at = now()
WHERE slug = 'test-nguyet-pho-shop-c-dn'
  AND template_id = '90000000-0000-4000-8000-000000000001'
  AND template_revision_id IS DISTINCT FROM '92000000-0000-4000-8000-000000000001';

COMMIT;
