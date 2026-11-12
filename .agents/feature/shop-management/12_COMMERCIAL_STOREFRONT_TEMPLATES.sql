-- Commercial storefront revision 3. Idempotent; shops remain pinned unless explicitly updated.
BEGIN;

WITH definitions(id, template_id, renderer_key, columns) AS (
  VALUES
    ('93000000-0000-4000-8000-000000000001'::uuid, '90000000-0000-4000-8000-000000000001'::uuid, 'heritage', 3),
    ('93000000-0000-4000-8000-000000000002'::uuid, '90000000-0000-4000-8000-000000000002'::uuid, 'royal-gift', 3),
    ('93000000-0000-4000-8000-000000000003'::uuid, '90000000-0000-4000-8000-000000000003'::uuid, 'minimal-grid', 4),
    ('93000000-0000-4000-8000-000000000004'::uuid, '90000000-0000-4000-8000-000000000004'::uuid, 'artisan-story', 2),
    ('93000000-0000-4000-8000-000000000005'::uuid, '90000000-0000-4000-8000-000000000005'::uuid, 'modern-catalog', 2)
)
INSERT INTO shop_template_revisions
  (id, template_id, revision, renderer_key, schema_version, css_variables, layout_config,
   is_deleted, created_at, updated_at)
SELECT d.id, d.template_id, 3, d.renderer_key, 1, previous.css_variables,
  jsonb_build_object('sectionOrder', jsonb_build_array('Hero','Products','Story','Reviews','Contact'),
                     'productColumnsDesktop', d.columns), false, now(), now()
FROM definitions d
JOIN shop_template_revisions previous ON previous.template_id = d.template_id AND previous.revision = 2
ON CONFLICT (id) DO NOTHING;

UPDATE shop_templates template
SET current_revision_id = revision.id, version = template.version + 1, updated_at = now()
FROM shop_template_revisions revision
WHERE revision.template_id = template.id AND revision.revision = 3
  AND template.current_revision_id IS DISTINCT FROM revision.id;

UPDATE shops
SET template_revision_id = revision.id, version = shops.version + 1, updated_at = now()
FROM shop_template_revisions revision
WHERE slug = 'test-nguyet-pho-shop-c-dn'
  AND revision.template_id = shops.template_id AND revision.revision = 3
  AND shops.template_revision_id IS DISTINCT FROM revision.id;

COMMIT;
