# Frontend integration plan (`mooncake-fe`)

## 1. Current frontend gaps

- `src/Pages/Supplier/ProductManage.jsx` is a demonstration screen with local/static product and variant data.
- `src/Pages/SearchPage.jsx` is entirely hard-coded and does not apply its controls to a backend query.
- `src/Pages/Products/ProductInformation.jsx` contains one hard-coded product, variants, prices and reviews.
- `src/services/productService.js` catches every API failure and returns `MOCK_PRODUCTS`, hiding authentication, validation, outage and empty-state behavior.
- `API_ENDPOINTS.PRODUCTS` names paths but there is no matching complete backend controller today.
- `App.jsx` uses hash page keys. Supplier and Customer layouts already exist and must be reused rather than introducing a second router/layout system without need.

The implementation removes production mock fallback. Test fixtures remain in Playwright/mock transport only and are never imported by runtime services.

## 2. Role and route flow

### 2.1 Access rules

- Customer, Supplier and anonymous visitor may browse/search public products in the Customer layout.
- A Customer in Supplier application Pending/Rejected state remains in Customer layout and cannot enter product management.
- Only a currently approved/verified Supplier capability may open the Supplier product workspace. FE guard improves navigation, but backend remains authoritative.
- A verified Supplier without a shop is sent to the existing shop setup empty state with a clear CTA. Product creation does not implicitly create or activate a shop.
- A Supplier with an Inactive shop may create/edit Draft products but cannot publish until the shop is Active. A suspended/revoked state becomes read-only/blocked according to backend `allowedActions`.

### 2.2 Proposed hash routes compatible with current App

| Route | Layout | Screen |
| --- | --- | --- |
| `#/tim-kiem?...` | Customer | Global search/discovery |
| `#/san-pham/{productId}` | Customer | Public product detail |
| `#/nha-cung-cap/quan-ly-san-pham` | Supplier | Owner product list |
| `#/nha-cung-cap/quan-ly-san-pham/tao-moi` | Supplier | Create product editor |
| `#/nha-cung-cap/quan-ly-san-pham/{productId}` | Supplier | Owner product editor/detail |

The App route parser must preserve existing `/auth/google/callback`, `/shop/{slug}`, Customer→Supplier onboarding and browser back/forward behavior. Deep links reload correctly through Vite/hosting fallback.

## 3. Source organization

Follow existing React layered folders and Ant Design v6 conventions:

```text
src/
  constants/
    apiEndpoints.js                 # product/category/region paths
    product.js                      # limits/status/sort/error labels
  services/
    productService.js               # public and owner product calls via apiClient
  hooks/
    useSupplierProducts.js
    useProductEditor.js
    useProductSearch.js
    useProductAutocomplete.js
    useProductDetail.js
    useProductRecommendations.js
  components/product/
    ProductCard.jsx
    ProductSearchBar.jsx
    ProductFilterPanel.jsx
    ProductGallery.jsx
    VariantSelector.jsx
    ProductCoreForm.jsx
    ProductVariantTable.jsx
    ProductVariantDrawer.jsx
    ProductInventoryDrawer.jsx
    ProductMediaUpload.jsx
    ProductPublicationChecklist.jsx
    ProductStatusTag.jsx
    RecommendationRail.jsx
  Pages/
    Supplier/
      ProductManage.jsx
      ProductEditorPage.jsx
    Products/
      ProductInformation.jsx
    SearchPage.jsx
```

Do not put fetch logic inside presentational components. Do not call `fetch` directly; all network access uses the existing `apiClient` so auth refresh/error envelope behavior remains consistent.

## 4. Service and hook contracts

### 4.1 `productService`

Proposed methods:

- `listMine(query, { signal })`
- `getMine(productId, { signal })`
- `createProduct(payload, idempotencyKey)`
- `updateCore(productId, payload, idempotencyKey)`
- `createVariant(productId, payload, idempotencyKey)`
- `updateVariant(productId, variantId, payload, idempotencyKey)`
- `deleteVariant(productId, variantId, expectedVersion, idempotencyKey)`
- `adjustInventory(productId, variantId, payload, idempotencyKey)`
- `replaceGallery(productId, payload, idempotencyKey)`
- `setPublication(productId, payload, idempotencyKey)`
- `deleteProduct(productId, expectedVersion, idempotencyKey)`
- `uploadProductMedia(file, purpose, idempotencyKey, { onProgress, signal })`
- `search(query, { signal })`, `autocomplete(query, { signal })`
- `getPublicDetail(productId, { signal })`
- `getRelated(productId, limit, { signal })`
- `getRecommendations(query, { signal })`
- `recordView(productId, source, sessionId, { signal })`
- `listCategories({ signal })`, `listRegions({ signal })`

The service returns backend `data` consistently or throws the normalized API error. It does not return mocks, empty arrays or success on catch. Cancellation is not shown as an error toast.

### 4.2 Hooks and state ownership

- Hooks own request lifecycle, AbortController, normalized query key, loading/error/empty distinction and cache invalidation.
- Idempotency keys are generated once per logical submit and retained through timeout/retry until a committed response or explicit edit changes the payload.
- Editor holds `serverSnapshot`, `draft`, `dirty`, `saving`, `uploading` and conflict state. A success response replaces the snapshot/version; a failed response keeps the draft.
- Search hook debounces text 300 ms, immediately applies discrete filters, aborts stale requests and discards responses whose query key no longer matches.
- Search filter state is serialized in stable URL order. Defaults are omitted. Changing filter/sort resets cursor/results.
- Detail hook treats 404 after navigation as “product is no longer available,” not a generic crash.
- View event is fire-and-forget after a real detail render and never blocks product content.

## 5. Supplier product management UX

### 5.1 Product list

Enterprise list behavior:

- Header with title, concise help, “Add product” primary CTA and shop-status warning if publication is unavailable.
- Search, status/category filters and sort. Desktop uses Ant Design Table; mobile uses accessible cards/list with the same actions.
- Columns: primary thumbnail, name/category, effective status, variant count, stock summary, price range, packaging flag, updated time and actions.
- Status uses text + color/icon: Draft/Inactive, Active, Out of stock. Never color alone.
- Server cursor pagination/load more; no client filtering of one loaded page.
- Row actions: edit, preview public if published, publish/unpublish, delete. Destructive actions require Modal confirmation and explain history retention.
- Loading skeleton/table, first-use empty state, filtered empty state, retryable error and permission/shop-required state are separate.

### 5.2 Product editor

Use one full-page editor under Supplier layout rather than an oversized modal. Sections/steps:

1. Basic information: name, category, description, base price, min/max quantity, unit, packaging support.
2. Variants: Ant Design editable table/drawer with SKU, crust, filling, size/weight, gift-set flag, adjustment/final price and availability. At least one required.
3. Gallery: drag/reorder with keyboard controls/fallback buttons, one primary image, upload progress/retry/remove, max count/size hints.
4. Review and publication: completeness checklist, effective status explanation, save Draft and Publish actions.

Create submits the complete initial aggregate after media has Ready asset IDs. Edit uses section commands from the API; it does not send owner/status/metrics. Variant stock after creation is never edited in the normal variant form.

### 5.3 Inventory adjustment

- Open a dedicated Drawer from a variant row.
- Show exact current stock and last refreshed time, signed adjustment control or Increase/Decrease mode, required reason and optional reference.
- Preview resulting stock client-side but trust server result.
- Submit atomic delta with one idempotency key. A conflict/insufficient response refreshes current stock and preserves the entered reason/delta for review.
- Do not expose a free editable stock cell that sends an absolute replacement.

### 5.4 Concurrency and capability changes

On `PRODUCT_VERSION_CONFLICT`, show a non-destructive modal:

- “Reload latest” (fetch server, warn that unsaved fields can be copied/exported), or
- “Continue reviewing my draft” (no retry until user reconciles).

Do not silently replace expectedVersion with the latest value and resubmit. If Supplier is revoked/shop suspended while editing, switch to a blocked/read-only state based on 403/409; preserve local draft in memory and explain the next action. Browser unload with dirty form uses a standard leave warning.

## 6. Search/discovery UX

### 6.1 Search header and autocomplete

- Ant Design AutoComplete/Input.Search with clear button, 300 ms debounce and minimum two characters for suggestions.
- Suggestions grouped/labeled Product, Shop, Category with keyboard arrows, Enter and Escape support. Plain text only; highlight via React nodes, never backend HTML.
- Enter submits the current query even if autocomplete fails/rate-limits.
- Search history is local only if approved; it is not part of this feature baseline.

### 6.2 Filters and sort

- Desktop: sticky left FilterPanel. Mobile/tablet: Filter button with Drawer and selected count.
- Category and region are multi-select with searchable option lists; price uses two InputNumber fields (slider may supplement but cannot be the only precise input); max minimum-order quantity, packaging, reputation and in-stock controls.
- Selected filter chips appear above results with individual remove and “Clear all.”
- Results summarize query and active filters. Sort Select exposes relevance, price, reputation, rating, newest and best-selling with Vietnamese labels mapped to server keys.
- Do not show discount sort because feature #8 data is outside this contract.
- Facet counts/disabled zero-count options use backend response. FE does not infer counts from the current page.

### 6.3 Results

- `ProductCard` displays provider image/fallback, product/shop, verified/reputation indicator based on real response, price range, minimum order, unit, rating evidence, packaging and availability.
- “View details” opens `#/san-pham/{id}`. Ordering CTA may carry product context to Module 4 only after a variant is selected on detail; search cards do not pretend product-level ordering is valid.
- Use responsive CSS grid and image aspect ratio to avoid layout shift. Ant Design Skeleton matches card size.
- Empty state offers clear filters/change query; error state shows retry with trace ID copy option; 503 never displays mock products.
- Use cursor “Load more” or next-page control. Back from detail restores cached results, filters and scroll where practical; changing query invalidates the cursor chain.

## 7. Public product detail UX

- CustomerHeader and Customer layout remain, including when the viewer also has Supplier role.
- Breadcrumb: search/category/shop labels from API; links preserve safe navigation.
- Gallery: primary image, thumbnails, lazy loading, keyboard navigation and alt text derived from product/variant names.
- Product core: real shop/reputation/rating, category, description, packaging, min/max order and unit.
- Variant selector is mandatory before an order/RFQ action. Each option shows name/attributes, calculated price and availability; unavailable variants are disabled but readable.
- Quantity control uses product min/max and selected variant availability policy. Server revalidates stock/price during order creation; detail does not promise reservation.
- Related/recommendation rail has deterministic empty/failure behavior and does not block main detail.
- Review display may consume existing approved aggregates/reads, but creating/replying/moderating reviews remains Module 8.
- Product 404/unavailable has a friendly Customer page with “Back to search”; it does not leak suspended/revoked/deleted reason.

## 8. Client-side validation mapping

FE mirrors obvious shape constraints for fast feedback but backend is authoritative:

- trim required fields; counter/max lengths;
- decimal currency with 2 digits, min/max relationship;
- minimum/maximum order relationship;
- SKU uppercase/allowlist and duplicate detection within current draft;
- at least one variant, gallery count 1–10 and exactly one primary;
- final price preview >0 for all variants;
- media byte/type/dimensions hint before upload, with backend error mapping;
- inventory delta nonzero and reason length.

Ant Design Form `name` paths map server field errors, including array paths such as `variants[1].sku`. Unknown/global errors appear in Alert at form top with trace ID. No generic success toast fires before committed response.

## 9. Error and status mapping

| Backend condition | FE behavior |
| --- | --- |
| 400 field errors | Mark exact Form.Item, scroll/focus first error, retain draft |
| `PRODUCT_SKU_CONFLICT` | Focus SKU field/variant row; suggest changing SKU, never auto-suffix without confirmation |
| `PRODUCT_VERSION_CONFLICT` | Reconciliation modal; no silent resubmit |
| `PRODUCT_PUBLICATION_INCOMPLETE` | Show returned completeness issues/checklist |
| `PRODUCT_INVENTORY_INSUFFICIENT` | Refresh stock, retain adjustment input, explain conflict |
| 401 | Existing auth refresh/login behavior; no mock data |
| 403 capability/revoke | Block owner actions and route according to current account capability flow |
| `SHOP_REQUIRED` | Shop setup CTA under Supplier layout |
| Public 404 | Product unavailable Customer state |
| 413/415 media | Per-file error and retry/remove; other completed uploads remain |
| 429 | Respect Retry-After; disable/re-enable action with countdown if useful |
| 503 search/media | Explicit retryable outage, trace ID; no fake cards/success |

Use stable constants in `src/constants/product.js`; do not scatter string comparisons/messages across pages.

## 10. Accessibility, responsive and performance

- Every control has visible label/accessible name; required/errors are announced; modal/drawer focus is trapped/restored.
- Autocomplete, gallery reorder, variant select and filter drawer are keyboard-operable. Status and stock do not rely on color alone.
- Respect Ant Design tokens and existing theme; no new ad-hoc design system. Test 390 px mobile through desktop without horizontal page overflow.
- Use semantic heading order, alt text and WCAG AA contrast. Price/number formatting uses `vi-VN` but API numbers stay numeric.
- Lazy-load product/editor routes and images; no full gallery/variant data on list/search cards. Memoize only measured hotspots.
- Abort requests on route/query changes; never update unmounted/stale screen. Upload concurrency <=3.

## 11. Frontend test plan

### Unit/component

- Query serialize/parse canonical order, defaults omitted, repeated filters and reset cursor.
- Currency/status/unit/error formatting.
- Product form validation including false/zero edge values, SKU normalization, final price and gallery primary.
- Hook abort/stale response, idempotency-key reuse, success version replacement and conflict retention.
- Autocomplete keyboard/a11y and safe plain-text highlight.

### Playwright browser

- Approved Supplier: empty list -> upload -> create Draft -> edit variants/gallery -> publish -> reload sees persisted API data.
- Customer/pending/revoked account cannot enter owner routes; verified no-shop receives setup CTA.
- Two tabs edit same product: one succeeds, one gets conflict and keeps draft.
- Inventory adjustment races mocked at transport plus live HTTP/PostgreSQL integration evidence on backend.
- Search query/filter multi-select/sort updates URL and API, aborts old response, empty/error/429/503 remain truthful.
- Autocomplete selection opens correct product/shop/category route; Enter search works when suggestion API fails.
- Product detail requires variant; unavailable variant/order action behavior; 404 after unpublish.
- Mobile filter Drawer, product editor/table cards, no horizontal overflow, keyboard focus and back/forward restoration.

Runtime FE tests use controlled API fixtures only as test doubles. At least one live-backend contract suite on a disposable database proves the UI contracts match the implemented API; a mocked 200 alone is not end-to-end evidence.
