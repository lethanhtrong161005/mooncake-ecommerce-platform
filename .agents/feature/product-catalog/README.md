# Module 3 — Product catalog: features #5 and #6

- Feature key: `product-catalog`.
- Design date: **2026-10-28**. Version: **1.0.0**.
- Status: **Draft / plan only**. This folder does not implement source code or run a database migration.
- #5: Supplier product, variant, gallery, publication and inventory management.
- #6: Marketplace search, multi-select discovery, autocomplete and related/personalized recommendations.

## Reading order

| Document | Purpose |
| --- | --- |
| [01_PRODUCT_VARIANT_MANAGEMENT_DESIGN.md](./01_PRODUCT_VARIANT_MANAGEMENT_DESIGN.md) | Full template I–VI for feature #5, aggregate rules and lifecycle |
| [02_PRODUCT_SEARCH_DISCOVERY_DESIGN.md](./02_PRODUCT_SEARCH_DISCOVERY_DESIGN.md) | Full template I–VI for feature #6, PostgreSQL FTS, autocomplete and recommendations |
| [03_API_DTO_VALIDATION.md](./03_API_DTO_VALIDATION.md) | Endpoint contracts, request/response DTOs, field validation and stable errors |
| [04_DATA_SQL_CONCURRENCY_SECURITY.md](./04_DATA_SQL_CONCURRENCY_SECURITY.md) | Schema, indexes, N+1, race conditions, SQL injection, locks, outbox and migration |
| [05_FRONTEND_INTEGRATION.md](./05_FRONTEND_INTEGRATION.md) | Integration with `mooncake-fe`, Supplier/Customer flows and enterprise UI states |
| [06_IMPLEMENTATION_TEST_ROLLOUT.md](./06_IMPLEMENTATION_TEST_ROLLOUT.md) | Ordered delivery backlog, test matrix, performance gates, rollout and Definition of Done |

## Sources and precedence

1. [Business Module 3, features #5/#6](../../business/mooncake-marketplace.md).
2. [Detail design template](../template/FEATURE_DETAIL_DESIGN_TEMPLATE.md).
3. [Backend rules](../../AGENTS.md) and [frontend rules](../../../../mooncake-fe/.agents/AGENTS.md).
4. Existing Module 2 implementation and contracts in [shop-management](../shop-management/README.md).
5. Existing source under `src/` in both repositories. The plan follows its Clean Architecture and React layered organization; it does not introduce a parallel architecture.

The template contains generic OTP examples. These documents preserve sections I–VI but replace those examples with catalog-specific validation, authorization, concurrency and persistence behavior. Backend identifiers, API messages and logs remain English; the frontend maps stable `errorCode` values to Vietnamese labels.

## Current-state assessment

| Area | Existing baseline | Gap addressed by this plan |
| --- | --- | --- |
| Domain | `Product`, `ProductVariant`, `ProductImage`, `Category`, `ProductStatus`, product `Embedding` | No aggregate version, inventory ledger, media ownership proof, publication invariants or search projection contract |
| Persistence | EF mappings, basic indexes, pgvector, product-name trigram index | Missing product write repository, explicit aggregate FKs/checks, exact primary-image rule, scoped SKU policy, FTS/read model and race handling |
| Public shop | `GET /api/v1/shops/{slug}/products` provides bounded cards | Shop-scoped read only; no global search, product detail or autocomplete; semantics must not diverge from new catalog service |
| Orders | Order creation batch-loads products/variants and performs atomic stock decrement | Supplier stock adjustment and catalog lifecycle are not coordinated through a shared inventory contract |
| Backend API | `ProductResponse` and category DTOs exist | No Product controller/application service/repository for feature #5/#6 |
| Supplier FE | `ProductManage.jsx` is a static demonstration form/table | No API calls, media workflow, variant lifecycle, concurrency conflict or durable state |
| Customer FE | `SearchPage.jsx` and `ProductInformation.jsx` use hard-coded data | No URL-backed filters, autocomplete, product detail, real loading/error/empty states |
| FE service | `productService.js` catches errors and returns `MOCK_PRODUCTS` | Fake success/data masks outages and must be removed from production flows |

## Baseline design decisions

- Product ownership is derived server-side through `User -> verified SupplierProfile -> Shop -> Product`; no request may submit an authoritative `shopId`, `supplierId`, reputation, sold count or rating.
- A product aggregate always has at least one live variant. Public purchasing remains variant-only; a product ID without a variant ID is never orderable.
- Product creation is a draft-first workflow. Supplier publication is explicit; public effective status is `Active`, `Inactive` or derived `OutOfStock` without losing the Supplier's publication intent.
- Initial stock is accepted only while creating a new variant. Later changes use an idempotent inventory-adjustment command and append-only movement record, not an absolute stock overwrite.
- Gallery uses prepared media asset IDs. Public URLs are provider output, never arbitrary client input. One live primary image is enforced by service and a partial unique database index.
- PostgreSQL FTS + `pg_trgm` + a denormalized search read model is the v1 search engine. Elasticsearch is an evidence-based future migration, not a second source of truth in this release.
- Search is eventually consistent through a transactional outbox/projector with a target freshness of five seconds. Owner CRUD reads the canonical write model immediately.
- Search ranking uses allowlisted fixed expressions only. All user values are parameters; no raw sort/filter/FTS text is interpolated into SQL.
- Personalized recommendations fail soft to deterministic related/trending results. Missing embeddings or an unavailable external provider never break product detail/search.
- Existing `/shops/{slug}/products` must reuse the same public eligibility and catalog projection rules or become a compatibility adapter; it must not remain a divergent implementation.
- No SignalR event is required for catalog CRUD/search. FE uses mutation responses, cache invalidation and bounded refetch. Notification/realtime expansion is a separate business requirement.

## Scope guard

In scope: product/variant/gallery CRUD, publish/unpublish, soft delete, inventory adjustment, Supplier list/detail, public detail, categories read, global search/filter/sort, facets, autocomplete, related and personalized recommendations, view events, read-model projection, FE integration, tests and rollout.

Out of scope: promotion authoring (#8), checkout/order UI (#7), RFQ, reviews creation/moderation (#22), category Admin CRUD (#27), billing, warehouse lots, multi-warehouse stock, Elasticsearch operations and generative-AI copywriting. Existing promotion/order/review data may be read for display/ranking but its authoring remains owned by those modules.

