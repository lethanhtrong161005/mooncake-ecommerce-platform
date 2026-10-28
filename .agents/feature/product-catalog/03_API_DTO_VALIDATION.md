# API, DTO and validation contract

## 1. Contract conventions

- Base path: `/api/v1`.
- JSON uses camelCase; UTC timestamps use ISO-8601 `Z`; money uses JSON decimal values and PostgreSQL `numeric(12,2)`, never floating point.
- All JSON request records use `JsonUnmappedMemberHandling.Disallow`. Unknown authority/legacy fields fail with 400 rather than being ignored.
- WebApi validates request shape/range/format. Application Service normalizes and applies ownership, eligibility, lifecycle and cross-row business rules. PostgreSQL constraints are final integrity protection.
- Controller obtains actor ID from authenticated claims, then service rechecks the current User/Profile/Shop in the database. Requests never accept authoritative `shopId`, `supplierId`, reputation, rating, sold count, status metrics or public URL.
- Repository ports accept query/command persistence DTOs and return state/projection/result DTOs. EF entities, `DbContext`, `IQueryable`, `NpgsqlConnection` and provider exceptions never cross into Application Service or controllers.
- Mutations that can duplicate side effects require `Idempotency-Key` (ASCII 8–128). The server stores actor + operation + key + canonical request hash + response/resource in the same transaction. Same payload replays; changed payload returns 409.
- Every aggregate edit includes `expectedVersion >= 1`. A stale version returns 409 and the server does not merge silently.
- Error envelopes contain stable English `errorCode`, safe English backend `message`, optional field-error dictionary, path, traceId and UTC time. FE maps known codes to Vietnamese copy.

## 2. Supplier product APIs

| Method/path | Request | Success | Required headers |
| --- | --- | --- | --- |
| `GET /suppliers/me/products` | `OwnerProductQuery` | 200 `CursorPage<ProductOwnerCardResponse>` | Bearer |
| `POST /suppliers/me/products` | `CreateProductRequest` | 201 `ProductOwnerResponse` | Bearer, Idempotency-Key |
| `GET /suppliers/me/products/{productId}` | route UUID | 200 `ProductOwnerResponse` | Bearer |
| `PUT /suppliers/me/products/{productId}` | `UpdateProductCoreRequest` | 200 `ProductOwnerResponse` | Bearer, Idempotency-Key |
| `POST /suppliers/me/products/{productId}/variants` | `CreateProductVariantRequest` | 201 `ProductOwnerResponse` | Bearer, Idempotency-Key |
| `PUT /suppliers/me/products/{productId}/variants/{variantId}` | `UpdateProductVariantRequest` | 200 `ProductOwnerResponse` | Bearer, Idempotency-Key |
| `DELETE /suppliers/me/products/{productId}/variants/{variantId}?expectedVersion=n` | query version | 200 `ProductOwnerResponse` | Bearer, Idempotency-Key |
| `POST /suppliers/me/products/{productId}/variants/{variantId}/inventory-adjustments` | `AdjustVariantInventoryRequest` | 200 `InventoryAdjustmentResponse` | Bearer, Idempotency-Key |
| `PUT /suppliers/me/products/{productId}/gallery` | `ReplaceProductGalleryRequest` | 200 `ProductOwnerResponse` | Bearer, Idempotency-Key |
| `PATCH /suppliers/me/products/{productId}/publication` | `SetProductPublicationRequest` | 200 `ProductOwnerResponse` | Bearer, Idempotency-Key |
| `DELETE /suppliers/me/products/{productId}?expectedVersion=n` | query version | 200 `DeleteProductResponse` | Bearer, Idempotency-Key |
| `POST /suppliers/me/product-media` | multipart `file`, `purpose` | 201 `ProductMediaResponse` | Bearer, Idempotency-Key |

Cross-tenant/nonexistent owner resources use the same 404. A role claim without a currently Verified profile returns 403. A verified Supplier without a shop receives `SHOP_REQUIRED` rather than having a shop created implicitly.

### 2.1 Create product

```json
{
  "name": "Bánh nướng thập cẩm Bát Bửu",
  "description": "Sản phẩm bán sỉ cho doanh nghiệp.",
  "categoryId": "2ce3dd3e-4939-4e8b-99b8-a28cc7a9ed9e",
  "basePrice": 85000.00,
  "minOrderQty": 50,
  "maxOrderQty": 5000,
  "unit": "piece",
  "supportsCustomPackaging": true,
  "variants": [
    {
      "name": "Thập cẩm 150g",
      "sku": "TC-150",
      "crustType": "Baked",
      "filling": "Mixed nuts",
      "sizeLabel": "150g",
      "weightGram": 150,
      "isGiftSet": false,
      "priceAdjustment": 0.00,
      "initialStockQty": 1000,
      "imageAssetId": null
    }
  ],
  "images": [
    {
      "mediaAssetId": "c525b193-523b-4a93-83f0-74cd13f5fe7b",
      "sortOrder": 0,
      "isPrimary": true
    }
  ]
}
```

Creation always returns publication intent `Draft`, API status `Inactive`, and version 1. It does not accept `isPublished` to prevent accidental publication before the Supplier reviews the result.

### 2.2 Product core validation

| Field | Request validation | Service/business validation |
| --- | --- | --- |
| `name` | required string, 2–255 chars after trim; reject control chars | NFKC + collapse whitespace; not effectively empty |
| `description` | optional, max 10,000 chars | plain text only; normalize line endings; encode on render |
| `categoryId` | required non-empty UUID | category exists, not deleted and selectable |
| `basePrice` | required decimal `0..9999999999.99`, max 2 fractional digits | plus every live variant adjustment must be `> 0` and within numeric range |
| `minOrderQty` | required integer `1..1,000,000` | must not exceed max |
| `maxOrderQty` | optional integer `1..1,000,000` | null or `>= minOrderQty` |
| `unit` | required enum `piece`, `box`, `set`, `tray` | stored canonical English key; FE localizes label |
| `supportsCustomPackaging` | required boolean, including valid `false` | no implied packaging price/promotion |
| `expectedVersion` | required on updates; long >=1 | must equal locked aggregate version |

`UpdateProductCoreRequest` contains these core fields plus `expectedVersion`. It cannot update variants, images, stock, ownership, publication or metrics.

### 2.3 Variant validation

| Field | Validation/policy |
| --- | --- |
| `name` | required 1–255 chars, normalized plain text |
| `sku` | required 1–100; uppercase canonical; allowed `A-Z`, `0-9`, `.`, `_`, `-`; no whitespace/control; unique normalized value within Shop |
| `crustType` | optional 1–100 plain text; replaces ambiguous legacy use of `Flavor` after migration review |
| `filling` | optional 1–100 plain text |
| `sizeLabel` | optional 1–50 plain text |
| `weightGram` | optional integer 1–100,000 |
| `isGiftSet` | required boolean |
| `priceAdjustment` | required decimal `-9999999999.99..9999999999.99`, 2 decimals; final price must be positive |
| `initialStockQty` | create only, integer 0–10,000,000; never accepted by update DTO |
| `imageAssetId` | optional UUID; Ready product image, same owner, compatible purpose, not expired |
| `expectedVersion` | required on add/update; product aggregate version |

A product may have at most 100 live variants. `UpdateProductVariantRequest` cannot contain `stockQty`; unknown-member rejection catches attempts. Deleting the last live variant returns `PRODUCT_VARIANT_REQUIRED`.

### 2.4 Inventory adjustment

```json
{
  "expectedVersion": 8,
  "delta": -25,
  "reason": "Damaged units removed during cycle count",
  "clientReference": "cycle-count-2026-10-28-batch-03"
}
```

| Field | Validation/policy |
| --- | --- |
| `expectedVersion` | required >=1 |
| `delta` | required nonzero integer `-1,000,000..1,000,000`; committed result cannot be negative |
| `reason` | required 5–500 chars, plain text |
| `clientReference` | optional 1–100 ASCII safe token; unique per owner/variant when provided |

The response includes movementId, productId, variantId, previousStockQty, delta, currentStockQty, inventoryVersion, unchanged/current productVersion and effectiveStatus. It never accepts an absolute stock value. Order traffic increments `inventoryVersion`, not the Product metadata version.

### 2.5 Gallery and media

`ReplaceProductGalleryRequest`:

```json
{
  "expectedVersion": 4,
  "images": [
    { "mediaAssetId": "c525b193-523b-4a93-83f0-74cd13f5fe7b", "sortOrder": 0, "isPrimary": true },
    { "mediaAssetId": "34541203-0490-462c-90ab-f27c3ce9af1f", "sortOrder": 1, "isPrimary": false }
  ]
}
```

- `images`: required 1–10; distinct non-empty media IDs; sortOrder distinct `0..9`; exactly one primary.
- Multipart media purpose is `ProductGallery` or `VariantImage` only.
- Proposed image limits: nonempty to 5 MiB, decoded JPG/PNG/WebP, one frame, dimensions 320–4096 each side, <=20 megapixels. The gateway re-encodes and strips active/metadata payload before public storage.
- These restrictions apply to public product images and do not change the separate size-only policy for private Supplier verification documents.
- Provider URL/public ID/content type/size/dimensions are server output. The client binds only `mediaAssetId`.

### 2.6 Publication and delete

```json
{ "expectedVersion": 5, "isPublished": true }
```

Publishing runs the full completeness/public eligibility check. `false` unpublishes without deleting. Delete requires expectedVersion and Idempotency-Key, performs soft deletion and is safe to replay. The API does not support hard delete.

### 2.7 Owner list query

`GET /suppliers/me/products?q=&statuses=Inactive&categoryIds={uuid}&sort=updatedAtDesc&pageSize=20&cursor=`

- `q`: optional max 100.
- `statuses`: repeated enum, max 3 distinct values.
- `categoryIds`: repeated non-empty UUIDs, max 20 distinct values.
- `sort`: `updatedAtDesc`, `nameAsc`, `stockAsc`; default `updatedAtDesc`.
- `pageSize`: 1–100; cursor max 2048 and bound to owner/query/sort.
- `includeDeleted` is not exposed to Supplier v1. Admin/operations access is a separate authorized contract.

## 3. Public discovery APIs

### 3.1 Search request

Example:

`GET /api/v1/products?q=banh+hat+sen&categoryIds=id1&categoryIds=id2&minPrice=50000&maxPrice=500000&maxMinOrderQty=100&supportsCustomPackaging=true&minReputation=80&regionCodes=VN-HN&inStockOnly=true&sort=relevance&pageSize=24`

| Field | Validation | Semantics |
| --- | --- | --- |
| `q` | optional 0–100 chars; normalized | product/shop/description/variant terms |
| `categoryIds` | repeated UUID, max 20 distinct | OR within group; AND with other groups |
| `minPrice`, `maxPrice` | optional decimal `0..9999999999.99`, 2 decimals | at least one same live variant in range; min <= max |
| `maxMinOrderQty` | optional integer `1..1,000,000` | product min order <= requested threshold |
| `supportsCustomPackaging` | optional bool | exact filter when provided |
| `minReputation` | optional decimal `0..100`, max 2 decimals | current verified Supplier score |
| `regionCodes` | repeated canonical code max 20 | exact structured shop region |
| `inStockOnly` | bool default false | require a matching live variant with stock > 0 |
| `sort` | enum `relevance`, `priceAsc`, `priceDesc`, `reputation`, `rating`, `newest`, `bestSelling` | fixed server branch only |
| `pageSize` | 1–50, default 24 | fetch pageSize + 1 internally |
| `cursor` | optional max 2048 | authenticated token bound to all normalized fields and schema version |

If `q` is empty and `sort=relevance`, the server uses the default discovery ranking. Repeated arrays beyond limits or duplicate/conflicting representations return 400; they are not silently truncated.

### 3.2 Search response

```json
{
  "status": 200,
  "message": "Products retrieved successfully.",
  "data": {
    "items": [
      {
        "productId": "6cc0fb96-b43f-40a4-ae14-7b954cf127aa",
        "name": "Bánh nướng thập cẩm Bát Bửu",
        "shop": { "shopId": "...", "slug": "xuong-banh-a", "name": "Xưởng bánh A" },
        "category": { "id": "...", "name": "Bánh nướng" },
        "primaryImageUrl": "https://provider.example/image.webp",
        "minPrice": 85000.00,
        "maxPrice": 110000.00,
        "minOrderQty": 50,
        "unit": "piece",
        "supportsCustomPackaging": true,
        "effectiveStatus": "Active",
        "reputationScore": 92.50,
        "averageRating": 4.80,
        "reviewCount": 125
      }
    ],
    "facets": {
      "categories": [{ "value": "...", "label": "Bánh nướng", "count": 42 }],
      "regions": [{ "value": "VN-HN", "label": "Hà Nội", "count": 18 }],
      "price": { "min": 50000.00, "max": 1500000.00 },
      "customPackagingCount": 61,
      "inStockCount": 90
    },
    "appliedSort": "relevance",
    "hasMore": true,
    "nextCursor": "protected-token",
    "projectionAsOfUtc": "2026-10-28T10:00:00Z"
  },
  "path": "/api/v1/products",
  "traceId": "trace-id",
  "time": "2026-10-28T10:00:00Z"
}
```

Cards never return exact private stock, internal search rank, embedding, legal address, moderation reason or provider public ID.

### 3.3 Autocomplete

`GET /products/autocomplete?q=hat+se&types=Product&types=Shop&limit=8`

- q required after trim, 2–100 chars.
- types optional distinct enum `Product`, `Shop`, `Category`, max 3; default all.
- limit 1–10, default 8.
- response: suggestionId/targetId, type, label, optional secondaryLabel and target path. No HTML-highlight string; FE highlights safely from plain text.
- No cursor/total count. Suggestions enforce public eligibility and cannot reveal hidden shops/products.

### 3.4 Product detail, related, recommendations and views

- `GET /products/{id}` returns public core, ordered gallery, all live variants with computed price/availability, shop summary and approved review aggregates. A variant SKU is returned only if approved for public B2B display; no exact stock is required.
- `GET /products/{id}/related?limit=12`: limit 1–20; excludes seed product; public candidates only.
- `GET /products/recommendations?limit=20&cursor=`: optional bearer; authenticated history if available, deterministic fallback otherwise; response indicates `strategy` as `Personalized`, `Related`, `Trending` or `New` without exposing model internals.
- `POST /products/{id}/views`: body `{ "sessionId": "uuid", "source": "Search" }`; session ID optional for authenticated users, required valid UUID for anonymous; source enum `Search`, `Shop`, `Related`, `Direct`; returns 202 in the standard envelope. Burst dedupe means accepted does not guarantee a new row.

### 3.5 Category and region metadata

- `GET /categories?parentId=&depth=2`: depth 1–3, bounded tree/list; only non-deleted selectable categories.
- `GET /regions`: versioned canonical code/label list used by shop setup and search. The server validates region codes from this same source.

## 4. Response DTOs

Owner responses and public responses are deliberately separate.

- `ProductOwnerResponse`: productId/shopId/version, core fields, publicationIntent, effectiveStatus, variants including exact stock/SKU/version metadata, gallery asset IDs/URLs, completeness issues, allowedActions, timestamps, idempotentReplay.
- `ProductOwnerCardResponse`: bounded list projection; no full description/gallery/variant collection.
- `ProductPublicDetailResponse`: public core/shop/category, gallery URLs, live variant public fields/calculated prices/availability, packaging flag, review summary; no owner-only asset IDs or inventory movements.
- `ProductSearchCardResponse`: minimal search card as shown above.
- `ProductSearchResponse`: cursor page + facets + projection timestamp.
- `ProductAutocompleteResponse`: max ten plain-text suggestions.

## 5. Stable error catalogue

| HTTP | errorCode | Condition |
| --- | --- | --- |
| 400 | `PRODUCT_REQUEST_INVALID` | malformed body/query, unknown field, invalid range/enum |
| 400 | `PRODUCT_PRICE_INVALID` | final variant price <=0/overflow |
| 400 | `PRODUCT_MEDIA_INVALID` | invalid image/purpose/dimensions |
| 401 | `AUTHENTICATION_REQUIRED` | missing/invalid token on owner API |
| 403 | `SUPPLIER_NOT_ELIGIBLE` | actor not currently verified/active Supplier |
| 403 | `PRODUCT_ACTION_FORBIDDEN` | action not allowed in current capability/state |
| 404 | `SHOP_REQUIRED` | eligible Supplier has no shop for catalog ownership |
| 404 | `PRODUCT_NOT_FOUND` | unavailable/nonexistent/cross-owner product |
| 404 | `PRODUCT_VARIANT_NOT_FOUND` | unavailable/nonexistent/cross-product variant |
| 409 | `PRODUCT_VERSION_CONFLICT` | expectedVersion stale |
| 409 | `PRODUCT_IDEMPOTENCY_CONFLICT` | same key with different canonical request |
| 409 | `PRODUCT_SKU_CONFLICT` | normalized SKU already used in owner shop |
| 409 | `PRODUCT_VARIANT_REQUIRED` | delete would leave no live variant |
| 409 | `PRODUCT_GALLERY_INVALID` | primary/count/order/binding invariant violated |
| 409 | `PRODUCT_PUBLICATION_INCOMPLETE` | publish completeness/public gate failed |
| 409 | `PRODUCT_INVENTORY_INSUFFICIENT` | atomic delta would make stock negative |
| 409 | `PRODUCT_MEDIA_ALREADY_BOUND` | asset lease/binding conflict |
| 413 | `PRODUCT_MEDIA_TOO_LARGE` | upload bytes/pixels exceed limit |
| 415 | `PRODUCT_MEDIA_TYPE_UNSUPPORTED` | public image cannot be decoded as supported format |
| 429 | `PRODUCT_RATE_LIMITED` | endpoint policy exceeded |
| 503 | `PRODUCT_MEDIA_STORAGE_UNAVAILABLE` | public image provider unavailable |
| 503 | `PRODUCT_SEARCH_UNAVAILABLE` | bounded search infrastructure failure; no mock data returned |

Database/provider details are logged safely with trace ID but not returned. PostgreSQL 23505/23503/check violations are translated to the most specific stable conflict/validation code.

All product ID route templates use the ASP.NET `:guid` constraint so static paths such as `/products/autocomplete` and `/products/recommendations` cannot be captured as product IDs.

## 6. Proposed limits and rate policies

These are reviewable technical defaults, not production facts:

| Policy | Proposed limit |
| --- | --- |
| Product reads/search | 120 requests/minute per actor/IP; burst 30 |
| Autocomplete | 60/minute; burst 10; FE debounce 300 ms |
| Product mutations | 30/minute per actor |
| Inventory adjustments | 60/minute per actor plus idempotency |
| Media upload | 20/hour per actor; concurrent upload cap 3 |
| View events | 120/minute per actor/session with server dedupe |
| JSON body | 256 KiB; multipart image up to 5 MiB |
| Variants/images | max 100 variants, 10 gallery images |

`Retry-After` is returned for 429. FE does not automatically retry mutations without reusing the same Idempotency-Key.

## 7. Persistence DTO boundary examples

Application-owned examples:

- `ProductActorScopeDto(UserId, ProductId?, VariantId?, ExpectedVersion?, CatalogWriteGuard)`
- `ProductCapabilityStateDto(UserId, UserActive, SupplierProfileId, VerificationStatus, ShopId, ShopStatus)`
- `ProductAggregateStateDto(Product, Variants, Images, MediaProofs)` where every item is a persistence DTO, not a Domain/EF object
- `CreateProductWriteDto`, `UpdateProductCoreWriteDto`, `VariantWriteDto`, `InventoryMovementWriteDto`, `GalleryBindingWriteDto`
- `ProductSearchPersistenceQueryDto(NormalizedText, CategoryIds, RangeFilters, SortKey, CursorPosition, Limit)`
- `ProductSearchProjectionDto`, `ProductSearchFacetDto`, `ProductSearchDocumentWriteDto`
- `ProductPersistResultDto(ProductId, Version, Created, IdempotentReplay)`

The repository owns entity mapping and transaction mechanics. The service owns policy and constructs these DTOs. The service never passes a user-provided expression, column name, table name, raw SQL fragment or unbounded collection into Infrastructure.
