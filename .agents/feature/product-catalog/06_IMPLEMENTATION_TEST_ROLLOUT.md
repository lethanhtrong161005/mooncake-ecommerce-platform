# Implementation, test and rollout plan

## 1. Delivery principles

- This document is an ordered implementation plan, not evidence that code, migration, test or deployment already exists.
- No production/project database mutation occurs during planning. Implementation later starts with a disposable PostgreSQL database and an inspected backup/test copy for migration reconciliation.
- Backend follows existing Clean Architecture; frontend follows existing constants/services/hooks/components/pages layering and Ant Design v6.
- Request DTO validates shape. Application Service owns business policy. Infrastructure receives/returns persistence DTOs only and owns SQL/EF mapping.
- Keep one canonical writer for publication/inventory/media after cutover. Do not leave legacy direct writers that bypass version, idempotency, audit or outbox.
- Never use mock success/data as runtime fallback. Test fixtures are isolated to tests.

## 2. Ordered phases and exit gates

| Phase | Work | Exit gate |
| --- | --- | --- |
| P0 — Contract and preflight | Approve policy table; inventory all Product/Variant/Image/Order/Shop consumers; inspect deployed schema/data/extensions; freeze API/error DTO examples | No unresolved ownership/status/SKU/region/stock ambiguity; reconciliation report and baseline tests saved |
| P1 — Additive data foundation | Product/version/publication, scoped SKU, inventory movement, media proof, idempotency/audit/outbox, search projections, constraints/indexes | Migration up/backfill/validate tested on PostgreSQL copy; no pending model changes; no destructive contract step |
| P2 — Persistence DTO boundary | Product transaction/repository ports, mapping, fixed lock order, conditional inventory, media lifecycle, outbox writer | Service/controller cannot reference EF entity/DbContext/IQueryable/Npgsql; PostgreSQL integration tests pass |
| P3 — Feature #5 backend | Owner APIs, validation, create/edit/variant/gallery/publication/delete, shared Order inventory path, public detail | HTTP contract + business + concurrency tests pass; old stock/product writers removed or delegate to new port |
| P4 — Search read model | Projector/outbox, FTS/facets/terms, public search/autocomplete/related, defensive eligibility, shop-feed adapter | Backfill parity checks, lag/replay tests, SQL injection tests and query budgets pass |
| P5 — Frontend #5/#6 | Replace Supplier/Search/Detail mocks, product services/hooks/components/routes, responsive/error/a11y states | FE build/scoped lint/component tests/Playwright pass against fixtures and live test backend |
| P6 — Recommendations | View events, deterministic related/trending, optional personalized/vector provider and fallback | Privacy/retention approved; provider failure cannot affect main search/detail; quality/offline checks recorded |
| P7 — Performance and rollout | Benchmark, EXPLAIN/index tuning, staging reconciliation, canary/feature flags, runbooks/alerts | SLO/load/error/lag gates met in documented environment; backup/restore and rollback switch verified |

Dependencies: P0 -> P1 -> P2 -> P3. P4 depends on canonical/outbox work from P1–P3. FE can build against versioned fixtures after P0 but is only complete after live API contract verification. P6 can ship after deterministic recommendations; embeddings are not a blocker for P3–P5.

## 3. Expected source changes

### 3.1 Backend Domain/Application

- Extend `Domain/Entities/Product.cs`, `ProductVariant.cs`, `ProductImage.cs`; add ProductMediaAsset, ProductVariantInventoryMovement, CatalogOutboxEvent and CatalogIdempotencyRecord as approved.
- Add/adjust enums for publication, inventory source, media purpose/status. Keep public `ProductStatus` mapping compatible during transition.
- Add `Application/DTOs/Products/Requests`, `Responses`, `Persistence`. Retire/replace ambiguous legacy `DTOs/Catalog/Responses/ProductResponse.cs` after consumer inventory.
- Add `Application/Common/Constant/ProductConstants.cs` for limits, error codes/messages and allowlists.
- Add pure helpers: `ProductPolicyHelper`, `ProductSkuHelper`, `ProductSearchQueryHelper`, `ProductResponseHelper`; complex mapping/normalization does not stay inline in controller/service.
- Add interfaces: `IProductCatalogService`, `IProductPublicQueryService`/`IProductDiscoveryService`, `IProductCatalogRepository`, `IProductCatalogTransaction`, `IProductMediaGateway`, cursor/embedding/view ports.
- Add service implementations split by responsibility/partial files if needed: owner commands, public queries, search/recommendations. Services contain business behavior, not SQL or EF includes.
- Refactor `OrderService`/`IOrderRepository` inventory decrement/release to the shared conditional movement contract. Preserve order snapshots and idempotency.

### 3.2 Backend Infrastructure/WebApi

- Update EF configurations with FKs/checks/partial indexes/concurrency fields; create reviewed migrations and search model mappings.
- Implement ProductCatalogRepository/transaction with DTO projections and fixed locks; ProductDiscoveryRepository with fixed parameterized query branches.
- Implement CatalogSearchProjector/outbox claim/retry/dead-letter, media cleanup and optional embedding/retention workers in bounded batches.
- Reuse safe Cloudinary/image decode primitives without coupling product business rules to Supplier verification document storage.
- Add `SupplierProductsController`, public `ProductsController`, `CategoriesController`, `RegionsController`; controller methods stay thin.
- Add product-specific endpoint metadata/ModelState error mapping, rate-limit registration and health/metrics. No credentials/options changes are made implicitly; required options are documented and validated at startup.
- Refactor existing `ShopRepository.ListProductsAsync`/ShopService route to the shared public eligibility/projection semantics, then benchmark. Do not maintain duplicate product status logic.

### 3.3 Tests and benchmark projects

- Prefer a dedicated `tests/Mooncake.EcommercePlatform.Catalog.Tests` project for policy, HTTP and PostgreSQL integration tests, rather than growing the Supplier verification-named project further.
- Add `tests/Mooncake.EcommercePlatform.Catalog.Benchmarks` or equivalent reproducible harness with deterministic seed, query plans and result artifacts under this feature folder after implementation.
- Test database connection must be explicit loopback/disposable and verified before create/drop. Never seed/drop the configured project/production database.

### 3.4 Frontend

- Replace `productService.js` mock fallback with real methods and stable error propagation; update `apiEndpoints.js`/product constants.
- Refactor `ProductManage.jsx`, `SearchPage.jsx`, `ProductInformation.jsx`; add Product editor and reusable product components/hooks described in document 05.
- Update `App.jsx` hash parsing/routes and existing Customer/Supplier navigation without breaking Google callback, public shop and Customer→Supplier verification flows.
- Remove runtime `MOCK_PRODUCTS` and static product/review/supplier claims from production pages. Keep test fixture builders under `tests/` only.
- Add Playwright catalog specs and live-backend contract configuration on a non-conflicting test port.

## 4. Backend request/HTTP validation matrix

| ID | Case | Expected |
| --- | --- | --- |
| V01 | Null/malformed/oversized JSON, unknown fields, duplicate case-insensitive fields, excessive nesting | 400/413 correct envelope; service/repository not invoked |
| V02 | Empty/control/overlong name/description/unit, Guid.Empty category, invalid enum | Field 400, no normalization to a different unintended value |
| V03 | Decimal overflow, >2 fraction digits, base+adjustment <=0 | 400 `PRODUCT_PRICE_INVALID`; DB unchanged |
| V04 | min <1, max <min, false packaging flag, zero adjustment/stock valid | Missing distinguished from valid false/zero |
| V05 | Variants 0/101, images 0/11, duplicate SKU/media/sort, zero/two primaries | 400 shape or business conflict with exact field/path |
| V06 | SKU Unicode/case/space/special characters; normalized collision | Deterministic normalized validation/409 |
| V07 | Client submits shopId/supplierId/status/stockQty/metrics/URL/public ID | Unknown/overposting rejected; no authority mass assignment |
| V08 | Missing/zero/negative expectedVersion, invalid/changed Idempotency-Key | 400 or 409 before side effects |
| V09 | Empty/oversize/fake image/SVG/HTML/too many pixels/wrong purpose | 400/413/415; provider/binding state consistent |
| V10 | Search arrays >limit, invalid UUID/decimal, min>max, unknown sort, huge cursor/query | 400; repository receives no raw fragment/unbounded list |
| V11 | FTS special symbols, quotes, `%`, `_`, slash/encoded data, injection strings | Safe validation/literal search semantics; no SQL syntax/behavior change |
| V12 | MVC query/body actual binding for arrays, bool false, multipart and delete expectedVersion | HTTP tests prove metadata/binding, not only direct validator calls |

## 5. Application business matrix

| ID | Case | Expected |
| --- | --- | --- |
| B01 | Customer/pending/rejected/revoked/inactive/deleted user calls owner API | 403; no product/media/inventory write |
| B02 | Verified Supplier without shop | `SHOP_REQUIRED`; no implicit shop creation |
| B03 | Verified Supplier with Inactive shop creates | Draft succeeds; publish is blocked until shop Active |
| B04 | Valid create retry after lost response | Same product/version and one audit/outbox; replay flag true |
| B05 | Same key changed payload | 409; original resource unchanged |
| B06 | Update core makes an existing variant final price invalid | Whole update rejected; no partial price change |
| B07 | Delete last live variant | 409; at least one remains |
| B08 | Soft-delete variant/product referenced by order/review | New public/order use blocked; historical snapshot/join remains |
| B09 | Publish incomplete gallery/variant/category/media | 409 with completeness issues; no search document exposure |
| B10 | Publish with all stock zero | Published intent retained, outward OutOfStock |
| B11 | Replenish published out-of-stock variant | Effective Active without new publication command |
| B12 | Unpublish then replenish | Remains Inactive; stock does not republish Supplier draft |
| B13 | Cross-owner product/variant/media | Uniform 404/conflict; no existence/URL leakage |
| B14 | Public detail/search for suspended shop or revoked owner | 404/omitted even during projection lag |
| B15 | Product-level order attempt without variant | Order validation rejects; variant belongs to product/shop and is live |

## 6. PostgreSQL and race matrix

Run with independent connections and explicit barriers.

| ID | Race/failure | Expected |
| --- | --- | --- |
| D01 | Two creates same key/same payload | One product; replay returns same response |
| D02 | Two creates same normalized SKU in same shop | One commit, one 409; unique constraint translates cleanly |
| D03 | Same SKU in two shops | Both valid under scoped uniqueness |
| D04 | Two metadata commands same version | One commit, one `PRODUCT_VERSION_CONFLICT` |
| D05 | Concurrent deletion of two remaining variants | Fixed Product lock; only one deletion succeeds |
| D06 | Gallery primary/reorder race | At most/exactly one committed primary; no duplicate sort |
| D07 | Publish vs gallery/variant deletion | Complete published product or conflict, never partial public state |
| D08 | Publish vs shop suspension/Supplier revoke | Shared lock/gate prevents public exposure after authoritative block |
| D09 | Adjustment -80 vs order -30 from stock 100 | Only a valid commit order; stock never negative; movements exact |
| D10 | Duplicate order/adjustment replay | One delta movement/effect |
| D11 | Order release vs Supplier adjustment | Atomic deltas preserved; inventory version monotonic |
| D12 | Soft delete/unpublish vs order create | Later transaction observes block; existing fulfillment not erased |
| D13 | Failure after canonical write before audit/outbox/idempotency | Entire transaction rolls back |
| D14 | Connection loss/unknown commit | Idempotency lookup resolves; no duplicate aggregate/delta |
| D15 | Deadlock/serialization failure | Bounded whole-transaction retry, fresh context, fixed key; no provider replay under lock |
| D16 | Projector workers/out-of-order/replay | One latest projection; no stale overwrite; event eventually processed/dead-lettered |
| D17 | Cleanup vs media bind | Reference recheck protects bound asset; loser receives known conflict/retry state |
| D18 | Direct invalid SQL insert/update | FK/check/unique constraints block cross-shop/negative/duplicate-primary invariants |

## 7. Search correctness and security matrix

- [ ] Product name, shop name, description and variant terms match with documented weights.
- [ ] Vietnamese accent/no-accent test corpus behaves as approved; results/ranking snapshots are versioned, not brittle to unrelated DB order.
- [ ] Empty query uses bounded default discovery, not unbounded scan.
- [ ] Category multi-select is OR inside group; other filter groups are AND.
- [ ] Price + in-stock constraints match the same live variant; fixtures prove no min/max false positive.
- [ ] Region uses structured code; free-text address is not searched as authority.
- [ ] Reputation/rating/newest/best-selling sorts use correct evidence, null handling and stable Product ID tie-break.
- [ ] All cursor pages have no duplicate/skip for a stable dataset; modified/expired/wrong-filter/wrong-user cursor is rejected.
- [ ] Search/autocomplete/related/recommendation all apply active shop + verified active Supplier gate.
- [ ] Projector delay after suspend/revoke/unpublish cannot expose a stale candidate due defensive gate.
- [ ] SQL injection payloads in q/sort/arrays/cursor never alter SQL; no provider exception/raw SQL returned.
- [ ] Autocomplete minimum length/limit/type/public gate and 429 behavior.
- [ ] Personalized response is private/no-store as designed; anonymous cache cannot leak another user's history.
- [ ] Missing embeddings/provider failure uses deterministic fallback and main search/detail remains healthy.
- [ ] View event dedupe/retention and no sensitive free-text/token logging.

## 8. N+1 and performance tests

- Instrument EF commands and assert budgets from document 04 at page size 1, normal and maximum. Add child counts/variants/images without increasing queries linearly.
- Generate skewed datasets: 100k then 1m Products, 5–10 Variants/Product, multiple images, 10k shops, hot categories and Supplier reputation distribution, delivered order aggregates.
- Benchmark each search sort with empty/nonempty q and selective/nonselective filters; record cold/warm p50/p95/p99, throughput, database CPU/IO, rows read and result correctness.
- Save `EXPLAIN (ANALYZE, BUFFERS, FORMAT JSON)` artifacts on synthetic test data. Do not run ANALYZE on production queries from this task.
- Measure projector throughput, oldest-event lag, backfill duration, media cleanup, autocomplete and product detail separately.
- Initial review targets: p95 global search/facets <=500 ms, autocomplete <=250 ms, public detail <=300 ms at agreed concurrency; projector p95 freshness <=5 seconds. These are gates to measure, not current claims.
- Load test rate limits and graceful 429/503 behavior. Confirm connection pool and lock wait remain bounded during order + inventory + catalog load.

## 9. Frontend/browser acceptance matrix

- [ ] Customer and non-approved Supplier remain in Customer layout; owner route cannot be opened by menu or direct hash.
- [ ] Approved Supplier/no shop receives setup CTA; Inactive shop can save Draft but cannot publish.
- [ ] Create with image + variant, save Draft, reload, edit, publish, unpublish and soft delete all use real API responses.
- [ ] Add/edit/delete variants and gallery order/primary persist; last-variant conflict retains UI draft.
- [ ] Inventory Drawer sends delta/reason and shows committed stock; conflict refresh preserves user input.
- [ ] Two-tab metadata conflict offers reconciliation and never silently overwrites.
- [ ] Search query/multi-filter/sort serializes URL, resets cursor, aborts old calls and restores back navigation/scroll.
- [ ] Facet counts come from backend; filter empty state differs from outage.
- [ ] Autocomplete keyboard and failure fallback; Enter search remains available.
- [ ] Product detail loads by ID, requires concrete variant before order action and handles unavailable variant/product.
- [ ] Search/detail cards have real Supplier/reputation/rating values only; no placeholder ISO/VIP/discount claims.
- [ ] 400/401/403/404/409/413/415/429/503 states are mapped; no runtime mock card or premature success toast.
- [ ] 390 px mobile, desktop, keyboard-only, focus restoration, labels/error announcement and WCAG AA contrast.
- [ ] Existing auth callback, public shop, Supplier verification and Shop setup routes do not regress.

## 10. Observability and operations

### Metrics

- API latency/error/rate-limit by operation (not raw query text).
- Product create/update/publication/delete/version conflicts/idempotent replay/mismatch.
- Inventory adjustment/reservation/release conflicts, insufficient stock, movement duplicates.
- Media upload/failure/orphan/cleanup backlog and provider latency.
- Search latency/result count/zero-result rate by normalized aggregate dimensions, not personal raw q; facet timeout.
- Outbox pending age/count, claimed/retry/dead-letter, projector throughput/document parity.
- Recommendation strategy/fallback/provider latency/error and embedding coverage/version.
- DB query count/slow-query fingerprint, lock wait/deadlock/retry and pool saturation.

### Logs/traces

Log traceId, actor ID when authorized, product/variant/shop IDs, operation, expected/current version, idempotency outcome, safe SQLSTATE category and elapsed time. Do not log access tokens, Idempotency-Key value, raw image, provider credential, legal Supplier data, full free-text search/description or connection strings.

### Health/runbooks

- Liveness does not depend on embedding provider. Readiness distinguishes canonical DB failure from degraded projector/recommendation.
- Runbooks: search projection rebuild, outbox dead-letter replay, SKU/backfill reconciliation, media orphan recovery, inventory movement reconciliation, feature-flag rollback and index/extension failure.
- Alerts use sustained thresholds to avoid noise: projector oldest age, dead-letter growth, search p95/error, stock constraint failures, media cleanup backlog and database lock/deadlock surge.

## 11. Deployment sequence

1. Approve contracts/policies and create a verified backup/restore point. Run preflight/reconciliation on a copy.
2. Apply additive schema/extensions/tables/nullable columns and safe indexes; validate migration head/model sync.
3. Run resumable canonical backfill and constraints validation. Produce counts/checksums/sample report; do not invent/delete invalid data silently.
4. Deploy backend canonical product write/inventory ports dark; route Order inventory through the shared path and monitor.
5. Enable outbox/projector; backfill search documents/terms/facets; compare eligible canonical count and sampled price/status/cards.
6. Deploy public search/detail APIs behind feature flags while old FE remains. Run live HTTP/security/performance smoke on staging.
7. Deploy FE with real services/routes and canary users. Remove runtime mock fallback in the same compatible release.
8. Enable Supplier product writes first for test/canary Suppliers, then public detail/search/autocomplete. Enable recommendation personalization last.
9. Monitor SLO/conflict/lag/stock/media metrics and reconcile data. Expand gradually.
10. Contract legacy status/global SKU/image URL/writers only after all consumers are proven migrated and rollback window closes; use a new reviewed migration.

Rollback switches new UI/API reads/writes off and restores prior compatible reads while retaining additive schema, audit, movement and outbox data. Do not destructive-down migrate live catalog/history. Fix forward after any committed new writes.

## 12. Decisions to confirm at review

| Topic | Baseline in this plan | Impact if changed |
| --- | --- | --- |
| SKU uniqueness | Per shop after uppercase normalization | Global SKU keeps legacy constraint but causes cross-Supplier collisions |
| Draft/public status | Internal Draft/Published intent; outward Inactive/Active/OutOfStock | Persisting only three effective states loses whether restock should republish |
| Gallery | At least 1, max 10, exactly one primary | Optional gallery changes publication checklist/card fallback/media tables |
| Variants | At least 1, max 100 | Larger limit changes body/query/editor/perf budgets |
| Exact stock public | Owner exact; public availability only by default | Exact public stock changes DTO/privacy/competitive policy |
| Region source | Structured Shop region code | Parsing free-text address is unreliable and changes Shop contract/backfill |
| Search engine | PostgreSQL FTS/trigram/read model first | Elasticsearch adds cluster, sync, security, monitoring and consistency scope |
| Search consistency | Eventual target <=5 s + synchronous defensive deny gate | Strong full-index consistency increases write latency/coupling |
| Recommendation | Deterministic fallback mandatory; embedding optional | Making AI mandatory creates an external single point of failure |
| View retention | Raw 90 days proposed | Privacy/legal decision changes cleanup/partition/storage design |
| Media | Public raster JPG/PNG/WebP <=5 MiB/20MP proposed | Other formats require safe decode/render/content-policy review |

Implementation may begin on noncontroversial DTO/schema test scaffolding only after these baselines are accepted or explicitly changed.

## 13. Definition of Done

- [ ] Features #5/#6 match business scope: product/variants/gallery/independent stock, full search/filter/sort, autocomplete and recommendations/fallback.
- [ ] Owner/public authorization and active Shop + verified Supplier public gate are consistent across detail/search/shop/order.
- [ ] Every request DTO field has HTTP validation; service owns business; repository boundary is DTO-only.
- [ ] At least one variant, variant-only ordering, final price, primary image, stock nonnegative, soft-delete/history and publication invariants have service + DB/race tests.
- [ ] Idempotency, optimistic versioning, fixed locks, conditional inventory and movement ledger prevent duplicate/lost/negative writes.
- [ ] SQL injection, XSS/media, cross-tenant, cursor tamper/cache isolation and overposting tests pass.
- [ ] Query counts are bounded; N+1 tests and PostgreSQL benchmark/EXPLAIN artifacts meet approved gates.
- [ ] Search projector is replayable/idempotent/observable and suspension/revoke/unpublish cannot leak through lag.
- [ ] FE uses real API with truthful loading/empty/error/conflict states; runtime mocks/hard-coded products are removed.
- [ ] Supplier and Customer layouts/role flow, mobile/a11y and existing auth/shop/verification routes pass regression.
- [ ] BE build, migrations/model sync, tests; FE build, scoped lint/tests/Playwright; live-backend contract suite all pass with results recorded.
- [ ] Staging backup/restore, reconciliation, canary, metrics/alerts/runbooks and rollback switches are verified before production enablement.
- [ ] Implementation report distinguishes executed proof from proposed limits and records any deferred recommendation/Elasticsearch work.
