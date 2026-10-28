# Data, SQL, concurrency and security design

## 1. Data ownership and source of truth

Canonical write model remains PostgreSQL tables for Product, Variant, Image, Shop, Supplier and transactions. Search tables are derived read models and can be rebuilt. Product media provider state is referenced through database proof rows; a provider URL alone is never ownership proof.

The current schema already contains Product/Variant/Image/Category, a global unique SKU, product `vector(1536)`, a trigram product-name index and basic filters. Migration must inspect real data before changing constraints. Do not assume the initial migration or conceptual business ERD exactly matches the deployed database.

## 2. Proposed canonical schema changes

Names below are implementation targets subject to EF migration naming review.

### 2.1 `products`

Add:

- `version bigint NOT NULL DEFAULT 1` for metadata/variant/gallery/publication optimistic concurrency;
- `publication_state product_publication_state NOT NULL DEFAULT 'Draft'` (`Draft`, `Published`);
- `normalized_name varchar(255) NOT NULL` for deterministic owner lookup/diagnostics;
- `published_at_utc timestamptz NULL`;
- unique supporting key `(id, shop_id)` for composite child ownership FKs.

Retain existing `status` during compatibility rollout, but stop treating two fields as independent authority. New service maps `publication_state + live stock` to outward `Inactive/Active/OutOfStock`. Existing status writers are migrated before status becomes compatibility-only or is removed in a later separately reviewed migration.

Checks:

- `base_price >= 0`;
- `min_order_qty >= 1`;
- `max_order_qty IS NULL OR max_order_qty >= min_order_qty`;
- `version >= 1`;
- normalized name nonempty and length bounded.

FKs use `ON DELETE RESTRICT/NO ACTION`; lifecycle uses soft delete. Index public/owner predicates with partial indexes, e.g. `(shop_id, updated_at DESC, id) WHERE is_deleted=false` and `(category_id, publication_state, id) WHERE is_deleted=false` after EXPLAIN evidence.

### 2.2 `product_variants`

Add/change:

- `shop_id uuid NOT NULL` backfilled from Product;
- `normalized_sku varchar(100) NOT NULL`;
- `crust_type varchar(100) NULL`; migrate/review ambiguous legacy `flavor` data rather than blindly renaming;
- `is_gift_set boolean NOT NULL DEFAULT false`;
- `inventory_version bigint NOT NULL DEFAULT 1`;
- optional `image_asset_id uuid NULL` replacing arbitrary new image URL writes.

Constraints:

- composite FK `(product_id, shop_id) -> products(id, shop_id)`;
- partial unique `(shop_id, normalized_sku) WHERE is_deleted=false`;
- `stock_qty >= 0`, `inventory_version >= 1`, weight null or positive;
- price adjustment numeric bounds; final-price positivity is cross-table and therefore enforced in Service plus transactional update query/test, not a misleading row-only check.

The existing global `product_variants_sku_key` is dropped only after duplicate/preflight and new scoped constraint validation. Scoped SKU avoids unrelated Suppliers blocking each other while remaining stable within one shop.

### 2.3 `product_images`

Add `media_asset_id uuid NOT NULL` after backfill/reconciliation. New writes no longer accept `image_url`; URL is projected from the media asset. Preserve legacy URL temporarily for compatibility if required.

Constraints/indexes:

- FK to Product and ProductMediaAsset with restrict behavior;
- partial unique `(product_id) WHERE is_primary=true AND is_deleted=false`;
- partial unique `(product_id, media_asset_id) WHERE is_deleted=false`;
- partial unique `(product_id, sort_order) WHERE is_deleted=false`;
- sort order `0..9`.

Exactly one primary cannot be fully expressed as a simple check for an arbitrary child set; Service locks Product and validates count, while the partial unique index guarantees at most one. Publication transaction verifies at least one.

### 2.4 `product_media_assets`

Suggested fields: id, owner_user_id, supplier_profile_id, shop_id, purpose, status (`Pending`, `Ready`, `Bound`, `Failed`, `Deleted`), provider/public ID, URL, content type, byte count, width, height, SHA-256, lease/expires timestamps, bound product/variant IDs, version, failure/retry metadata, standard audit/soft-delete timestamps.

Key rules:

- provider public ID unique;
- Ready/Bound rows contain positive bounded dimensions/bytes and digest;
- one asset binds to at most one product image/variant purpose unless an explicit reusable-media policy is later approved;
- cleanup claims expired unattached rows in bounded batches, rechecks references under lock, deletes provider object, then marks database state; provider failures retry without erasing proof.

Shop media gateway code can supply shared decoding/storage helpers, but product purposes/dimensions/ownership remain product-specific policy. No network/provider call is made while database locks are held.

### 2.5 `product_variant_inventory_movements`

Suggested fields: id, variant_id, product_id, shop_id, source_type (`Initial`, `SupplierAdjustment`, `OrderReservation`, `OrderRelease`, `SystemCorrection`), source_id/client_reference, delta, previous_qty, current_qty, reason, actor_user_id, idempotency_key, created_at_utc.

Constraints:

- delta nonzero except an explicit Initial movement policy;
- previous/current nonnegative and `current_qty = previous_qty + delta`;
- unique `(source_type, source_id, variant_id)` when source ID exists;
- unique `(actor_user_id, operation, idempotency_key)` through catalog idempotency table;
- immutable after insert; corrections append another movement.

Order reservation/release and Supplier adjustment write movement plus conditional stock update in the same transaction. Inventory commands increment `inventory_version`; they do not increment Product metadata `version`, avoiding order traffic causing unrelated editor conflicts. The adjustment request's `expectedVersion` still protects that the product/variant lifecycle has not changed, while atomic delta/idempotency protect stock; the response returns both current version values.

### 2.6 Idempotency, audit and outbox

Use a feature-scoped table or extend an existing generic mechanism only if its semantics match:

- `catalog_idempotency_records(actor_user_id, operation, key, request_hash, resource_id, response_snapshot/version, expires_at_utc)` unique on actor/operation/key;
- catalog audit records for create/core edit/variant lifecycle/publication/delete/inventory correction; same transaction as mutation;
- `catalog_outbox_events(id, aggregate_type, aggregate_id, source_version/sequence, event_type, payload_version, occurred_at_utc, available_at_utc, attempt_count, claimed_at_utc, processed_at_utc, last_error_code)`.

Outbox payload contains identifiers/version, not full descriptions, credentials or media bytes. Projector reloads the current canonical projection, making events small and idempotent.

## 3. Search read model

### 3.1 `product_search_documents`

One row per publicly eligible product:

- product/shop/supplier/category IDs and public labels/slug;
- normalized product/shop/category text and weighted `tsvector`;
- description excerpt, primary image URL;
- min/max matching price, min-order, unit, packaging support, effective availability;
- reputation, average rating, review count, delivered-units window, region code;
- publication/created timestamps, source product/shop/profile versions, projection sequence/time;
- optional existing `vector(1536)` or reference to canonical embedding.

Indexes proposed after benchmark:

- GIN search vector;
- `pg_trgm` GIN/GiST on normalized suggestion/product/shop terms;
- B-tree/partial indexes for category, region, packaging, availability, price/reputation/rating/published/sold sort paths;
- pgvector HNSW/IVFFlat only when embeddings have sufficient coverage/volume and recall/latency measurements justify it.

`unaccent` and `pg_trgm` extension/schema availability must be preflighted on the actual managed PostgreSQL instance. The projector may compute `to_tsvector('simple', unaccent(@text))` during parameterized UPSERT; do not create a generated column using a non-immutable expression without validating PostgreSQL requirements.

### 3.2 `product_search_variant_facets`

One row per live public variant: product_id, variant_id, final_price, in_stock, normalized crust/filling/size, weight, source inventory version. It enables exact “one variant satisfies price/availability” matching and avoids false positives from only comparing product min/max.

Indexes: `(product_id)`, `(final_price, product_id)` partial for live rows, `(product_id, in_stock)`. Keep this table bounded to public products/live variants and rebuildable.

### 3.3 `catalog_search_terms`

Rows for public Product/Shop/Category suggestions: type, target ID, normalized term, display label, secondary label, popularity score, source version and updated time. Unique `(type, target_id, normalized_term)`; trigram/prefix index; query always has minimum length and limit <=10.

### 3.4 View/recommendation data

- `product_view_events`: time-partitioning only if volume warrants; user ID nullable, pseudonymous session hash, product, source, occurred UTC, dedupe bucket. Proposed raw retention 90 days.
- Daily category/product affinity and co-purchase aggregates are rebuildable projections.
- Embedding status stores model/version/dimensions/source hash. A model change queues regeneration; incompatible dimensions never share an index/query.

## 4. Fixed lock order and transaction boundaries

All catalog write paths use one documented order:

`User -> SupplierProfile -> Shop -> Product -> ProductVariant(s ordered by UUID) -> ProductMediaAsset(s ordered by UUID) -> idempotency/audit/outbox writes`.

Rules:

- Begin transaction only after request validation and any external image preparation.
- Lock only rows needed. Batch/sort IDs before `FOR UPDATE` to avoid different iteration order.
- Re-read eligibility, ownership and expected versions after locks are acquired.
- Keep network calls, image decode, embedding calls and large JSON serialization outside the transaction.
- Conditional updates and constraints decide the race; prechecks only improve errors.
- Retry only recognized transient serialization/deadlock failures, bounded (for example max 3 with jitter), using a fresh DbContext and the same idempotency key. Do not retry validation/unique/version conflicts.
- Unknown commit outcome is resolved by idempotency lookup before replay.

Inventory-only path locks Product then one/sorted Variants and uses:

```sql
UPDATE product_variants
SET stock_qty = stock_qty + @delta,
    inventory_version = inventory_version + 1,
    updated_at = @nowUtc,
    updated_by = @actor
WHERE id = @variantId
  AND product_id = @productId
  AND is_deleted = false
  AND stock_qty + @delta >= 0
RETURNING stock_qty, inventory_version;
```

Values are parameters. This is illustrative SQL; Infrastructure owns it and maps only DTOs. A zero-row result is translated after a bounded diagnostic read into not-found, lifecycle conflict or insufficient stock.

## 5. Race-condition matrix

| Race | Required behavior |
| --- | --- |
| Two creates, same Idempotency-Key | One aggregate/audit/outbox; second returns same response |
| Same key, changed body | One commit; second 409 idempotency conflict |
| Two creates with normalized same SKU in one shop | Constraint allows one; loser 409 SKU conflict, no 500 |
| Core edit vs variant edit with same Product version | One commits/version increments; one 409, no lost update |
| Two tabs delete two remaining variants | Product lock serializes; only one may commit, at least one variant remains |
| Two gallery requests choose different primary | Product lock/version + partial unique; one commit, one conflict |
| Publish vs image/variant removal | Product lock/version yields a complete published aggregate or a rejected stale command, never incomplete public state |
| Publish vs shop suspension/Supplier revoke | Shared User/Profile/Shop lock order; later commit observes prior authoritative state; public defensive gate still blocks stale projection |
| Supplier adjustment vs order reservation | Atomic deltas serialize at variant row; no negative stock/lost update; movements reflect commit order |
| Duplicate order reservation/release | Unique source movement/idempotency applies delta once |
| Delete/unpublish vs new order | Order and catalog share Shop/Product/Variant locks; order after unpublish/delete cannot pass eligibility; existing order history remains |
| Projector receives events out of order | Source sequence/version guard prevents old event overwriting new document |
| Two projector workers claim same batch | `FOR UPDATE SKIP LOCKED`; idempotent UPSERT is secondary defense |
| Cleanup vs media binding | Lock/recheck binding; either bind wins and cleanup skips, or expired cleanup wins and bind returns conflict |

Concurrency tests use at least two independent PostgreSQL connections/DbContexts and synchronization barriers. EF InMemory/SQLite is not evidence for PostgreSQL locks, partial indexes, enum/FTS/vector behavior or SQLSTATE translation.

## 6. N+1 prevention and query budgets

No endpoint may load a page and then call repository/service once per product/variant/image/shop. Repository projections aggregate/batch on the server.

| Endpoint | Proposed query budget excluding auth middleware | Projection rule |
| --- | --- | --- |
| Supplier product list | <=2 | one page projection + optional total/count; variant/image counts and primary image via grouped/lateral projection |
| Supplier product detail | <=4 | aggregate root/capability plus variants, gallery/media and optional movements page; no per-child query |
| Public detail | <=4 | eligibility/root, ordered variants, ordered images, aggregate review/promotion display data |
| Global search | <=3 | page, bounded facets, optional metadata; no full child collections |
| Autocomplete | 1 | read `catalog_search_terms` with limit |
| Related/recommendations | <=3 | candidate IDs then one bulk card projection; never fetch each card separately |
| Projector batch | bounded independent of item count | bulk load affected aggregates with `WHERE id = ANY(@ids)` and bulk/upsert strategy |

Use `AsNoTracking`, narrow `Select`, pageSize+1 keyset pagination, and split queries only when their bounded count is deliberate. Add a DbCommandInterceptor in integration/performance tests to assert query counts at page sizes 1, 20 and maximum. The current Shop list/product feed implementation should be refactored to shared projections rather than copied.

## 7. SQL injection and input-abuse controls

- EF LINQ parameters or `NpgsqlParameter` only. No interpolation/concatenation in `FromSqlRaw`, `ExecuteSqlRaw`, FTS query, ORDER BY, table/index name or JSON path.
- Map public sort enum to separate known expression branches in code. A string such as `price desc; drop table...` fails DTO validation and never reaches SQL construction.
- Parse UUID/decimal/int arrays at MVC boundary; cap array counts, body size, query length, page size and cursor length.
- FTS receives a text parameter to `websearch_to_tsquery`/safe parser. Special operators are data. Catch syntax errors and return a validation error without falling back to unsafe SQL.
- If a literal `ILIKE` path is needed, parameterize it and escape `%`, `_` and escape character for literal semantics; do not implement search by concatenating `%${q}%` into SQL text.
- Cursor is authenticated and scope-bound. The repository receives typed cursor values, not raw clauses or offsets.
- Plain product text is never rendered as HTML; no rich text/Markdown in v1. Media disallows SVG/HTML and is decoded/re-encoded.
- Runtime DB role receives only required schema/table/function grants. Migration/projector roles are separate where deploy environment permits. Test permissions using the runtime role, not only a superuser.

## 8. Projection freshness and defensive public gate

Search is eventually consistent, but security/public eligibility is not allowed to lag open:

- Search document contains source eligibility versions and public flag.
- Query joins/checks a compact current public-eligibility source or uses a short-lived deny cache updated synchronously for suspend/revoke/unpublish. Baseline recommendation is a defensive join to Product/Shop/Profile/User status keys until benchmark proves a secure alternative.
- Search/document/detail always returns uniform 404/omission for unavailable resources.
- A stale card may disappear between search and detail; FE handles 404 by informing the user and returning to results.
- Projector lag target <=5 seconds; health check/metric tracks oldest pending age, throughput, retries and dead letters. Lag does not trigger mock data.

## 9. Migration and backfill plan

### 9.1 Preflight on a verified backup/test copy

- Record PostgreSQL/version/extensions/schema/search_path and migration head.
- Count Products/Variants/Images, null/orphan FKs, negative stock/final prices, products without live variants/images, duplicate primary/sort orders, duplicate SKU after normalization and legacy arbitrary URLs.
- Inspect semantics of `Flavor` before mapping to `crust_type`; produce a reconciliation report for ambiguous values.
- Verify Shop/Supplier/User public gate data and choose a structured region backfill policy. Do not infer region silently from arbitrary addresses without review.
- Benchmark current shop product query and candidate search sizes; save EXPLAIN plans without sensitive parameter logging.

### 9.2 Expand/backfill/validate/contract

1. Expand nullable/new columns, tables, outbox and non-conflicting indexes. Do not deploy binary requiring missing columns first.
2. Backfill in bounded resumable batches ordered by UUID/created time; checkpoint and expose counts. Populate shop ID/SKU normalization/publication intent/media reconciliation.
3. Resolve invalid rows through an explicit report/policy; do not delete products or invent stock/images.
4. Add/check constraints as `NOT VALID` where appropriate, validate after cleanup; create large indexes concurrently where supported and account for EF migration transaction limitations.
5. Deploy write path + outbox while old reads remain. Refactor Order inventory writer before declaring one-writer invariant.
6. Backfill search documents/terms/facets; compare counts and sampled cards to canonical public gate.
7. Enable new public search/detail behind a feature flag/canary; update Shop product adapter and FE.
8. Remove old mock/fallback and legacy writers. Contract/drop legacy global SKU/status/URL fields only in a later release after consumer/rollback window.

Rollback is forward-fix oriented: disable new endpoints/projector, retain additive schema/outbox and restore prior reads. Do not run destructive down migrations on live data or remove inventory/audit history.

## 10. Performance evidence required

Benchmark at minimum: 100k/1m products scenarios, 5–10 variants each, skewed hot categories/shops, 10m delivered item aggregates and realistic Vietnamese text/filter selectivity. Capture PostgreSQL version/hardware/data generator, cold/warm runs and EXPLAIN (ANALYZE, BUFFERS) for every sort/filter family.

Initial review targets (not claims): p95 <=500 ms for global search and facets, <=250 ms autocomplete, <=300 ms product detail at agreed load; projector p95 freshness <=5 seconds. Indexes are accepted based on measured plans/write cost, not merely added for every column.
