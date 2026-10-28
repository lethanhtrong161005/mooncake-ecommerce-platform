# API, request validation và DTO persistence contract

Trạng thái: plan đề xuất, chưa phải endpoint đã triển khai. Ngày 2026-10-25.

## 1. Quy tắc thống nhất

- Base `/api/v1`; JSON camelCase, enum string allowlist. ApiResponse<T> hiện có gồm status/message/data hoặc errors, errorCode/path/traceId/time UTC.
- Request DTO chịu trách nhiệm required/type/format/length/range/enum/nested structure. Application Service normalize và kiểm tra business/cross-field/state/ownership trong transaction. Repository chỉ persistence, không quyết định ai được tạo/suspend/premium.
- Client không được gửi ownerUserId, supplierId, shopId trong body quản lý `/me`, role, verified badge, reputation, createdBy, version mới, providerPublicId, customCss hoặc raw image URL.
- Route DTO validate Guid khác Empty/slug pattern; query DTO validate cả query không phải body. Reject unknown query keys và unknown JSON fields cho nhóm endpoint shop mới. Body JSON tối đa 32 KiB, depth tối đa 8; reject duplicate property names theo OrdinalIgnoreCase trước deserialize, kể cả nested JSON. Multipart có giới hạn riêng.
- Dùng DTO class/record có init properties với validation attribute. Nếu dùng positional record, attribute validation đặt trên **constructor parameter**, không `[property: Required]`; test qua MVC thực tế, không chỉ Validator.TryValidateObject.
- `bool?`, `long?`, `int?` + Required khi cần phân biệt field bị bỏ với false/0 hợp lệ. Reject integer không xác định trong enum; `Required Guid` không thay thế check Guid.Empty. Không dựa vào EF annotation để trả lỗi đầu vào.
- Business normalize: trim, Unicode NFKC cho text; description/reason cho phép newline/tab nhưng cấm control khác. Slug chỉ nhận ASCII canonical lower-case, không tự chuyển một slug sai thành định danh khác. API/log message tiếng Anh constants; FE map errorCode/field sang tiếng Việt.

## 2. Endpoint matrix

| Method/path | Request → Response data DTO | Permission |
| --- | --- | --- |
| GET `/shops/me` | — → OwnerShopResponse | Supplier verified |
| POST `/shops/me` | CreateShopRequest → OwnerShopResponse | Supplier verified, chưa có shop |
| PUT `/shops/me` | UpdateShopRequest → OwnerShopResponse | Chủ shop verified, không suspended |
| PATCH `/shops/me/visibility` | SetMyShopVisibilityRequest → ShopCommandResponse | Như trên |
| POST `/shops/me/media` | ShopMediaUploadRequest multipart → ShopMediaResponse | Supplier verified, purpose Logo/Banner |
| GET `/shops/me/appearance` | — → ShopAppearanceResponse | Chủ shop verified |
| GET `/shops/me/preview` | — → OwnerShopPreviewResponse, tối đa 12 product cards | Chủ shop verified, owner-only; cho preview cả Inactive |
| PUT `/shops/me/appearance` | UpdateShopAppearanceRequest → ShopAppearanceResponse | Chủ shop verified, không suspended |
| GET `/shop-templates` | TemplateCatalogQuery → CursorPage<TemplateSummaryResponse> | Supplier verified |
| GET `/shops/{slug}` | ShopSlugRoute → PublicShopResponse | Public gate |
| GET `/shops/{slug}/products` | ShopProductQuery → CursorPage<ProductCardResponse> | Public gate |
| GET `/shops/{slug}/categories` | ShopCategoryQuery → CursorPage<CategoryOptionResponse> | Public gate |
| GET `/shops/{slug}/reviews` | ShopReviewQuery → CursorPage<PublicShopReviewResponse> | Public gate |
| GET `/admin/shops` | AdminShopQuery → Page<AdminShopSummaryResponse> | Admin |
| GET `/admin/shops/{shopId}` | ShopIdRoute → AdminShopDetailResponse | Admin |
| PATCH `/admin/shops/{shopId}/status` | AdminShopModerationRequest → ShopCommandResponse | Admin |
| GET `/admin/shop-templates` | AdminTemplateQuery → Page<AdminTemplateSummaryResponse> | Admin |
| GET `/admin/shop-templates/{templateId}` | TemplateIdRoute → AdminTemplateDetailResponse | Admin |
| POST `/admin/shop-templates` | CreateShopTemplateRequest → AdminTemplateDetailResponse | Admin |
| PUT `/admin/shop-templates/{templateId}` | PublishTemplateRevisionRequest → AdminTemplateDetailResponse | Admin |
| PATCH `/admin/shop-templates/{templateId}/availability` | SetTemplateAvailabilityRequest → TemplateCommandResponse | Admin |
| PATCH `/admin/shop-templates/{templateId}/premium` | SetTemplatePremiumRequest → TemplateCommandResponse | Admin |
| POST `/admin/shop-templates/media` | TemplateMediaUploadRequest → ShopMediaResponse | Admin, TemplatePreview purpose |

Tất cả mutation cần `Idempotency-Key` là UUID khác Empty, một key/lần thao tác logic; same key/same payload trả committed result, khác payload trả 409. Create lần đầu HTTP 201; replay giữ cùng status/body, có idempotentReplay=true. Update HTTP 200. Key scope actor + operation + resource (create shop dùng profile ID, create template dùng namespace resource cố định); TTL đề xuất 24h. Kiểm tra authority mới nhất trước replay, expectedVersion sau replay; revoked actor không dùng replay để lấy quyền quản lý đã mất.

## 3. Request DTO #3: validation từng trường

### 3.1. CreateShopRequest

| Field | DTO validation | Service validation/normalization |
| --- | --- | --- |
| name | Required, max 255 | Sau trim/NFKC 2–255, không toàn whitespace/control, plain text |
| slug | Required, length 3–80, `^[a-z0-9]+(?:-[a-z0-9]+)*$` | Reserved names: me, admin, api, shop, shops, templates, products, reviews, categories, support, auth; unique global kể cả tombstone |
| description | Optional, max 2000 | Trim/newlines cho phép; không render HTML |
| templateId | Required Guid?, non-empty | Head selectable, revision thuộc đúng head |
| templateRevisionId | Required Guid?, non-empty | Phải là current revision tại thời điểm commit |
| logoAssetId | Optional non-empty Guid nếu có | Asset Ready/Logo, owner actor, chưa xóa; null là không có logo |
| bannerAssetId | Optional non-empty Guid nếu có | Asset Ready/Banner cùng owner |
| publicContactEmail | Optional EmailAddress, max 255 | Empty → null; không tự lấy email user |
| publicContactPhone | Optional max 20 | Normalize + E.164 pattern `^\+[1-9][0-9]{7,14}$` hoặc chuỗi rỗng→null; FE giúp đổi số VN sang +84 |
| publicAddress | Optional max 500 | Plain text; chỉ công khai giá trị chủ shop nhập vào field này |

Không có expectedVersion cho POST create; cổng một shop được đảm bảo bằng lock + unique constraint. Bật Active không yêu cầu ảnh bắt buộc ở v1, nhưng name/slug/revision phải hợp lệ.

### 3.2. UpdateShopRequest / SetMyShopVisibilityRequest

Update có name, description, logoAssetId, bannerAssetId và ba publicContact fields như create, thêm **expectedVersion: Required long?, >=1**. PUT là full replacement của các field được sửa; null của field optional là xóa giá trị. Không có slug/template/status trong DTO; theme và visibility dùng endpoint riêng. Field required bỏ qua trả 400.

Visibility gồm expectedVersion và **status: Required ShopStatus?**, chỉ Active/Inactive. Suspended không thuộc domain command này; service kiểm tra transition hợp lệ và full shop snapshot trước publish.

### 3.3. AdminShopModerationRequest

| Field | DTO validation | Service |
| --- | --- | --- |
| expectedVersion | Required long?, >=1 | Conditional update đúng version |
| action | Required nullable enum Suspend/Restore | Suspend chỉ từ Active/Inactive; Restore chỉ từ Suspended |
| reason | Required, 10–1000 | Sau trim/NFKC vẫn >=10; giữ lý do trong audit/moderation history, không trả public |

Restore không nhận target status từ client: dùng preSuspensionStatus đã lưu; owner phải active/verified hiện tại. Không cho Admin mạo danh Supplier bằng `/shops/me`. Admin self-target moderation bị chặn nếu actor trùng owner.

### 3.4. Media multipart DTO

| Field/header | Validate |
| --- | --- |
| file | Required IFormFile, một file, bytes thực >0 và <=5 MiB; request limit 6 MiB bao gồm multipart overhead |
| purpose | Required enum Logo/Banner; Admin endpoint chỉ TemplatePreview |
| fileName | Basename, <=255, cấm path/control; không tin MIME/extension đơn lẻ |
| Idempotency-Key | Required UUID non-empty; fingerprint gồm bytes SHA256, purpose và basename |

Service kiểm tra JPEG/PNG/WebP được cấu hình cho ảnh public; decode/re-encode loại metadata, signature/mime đúng, tối đa 20 megapixels, kích thước đề xuất Logo 128–2048 mỗi cạnh, Banner width 640–4096/height 200–2048, TemplatePreview width 320–2048/height 180–2048. Cấm SVG/GIF/HTML/PDF cho branding. Reuse public image upload của CloudinaryStorageService, không reuse legal document IDs/private-file URLs.

Scan/metadata decode/pixel bounds thuộc media business service qua adapter, không đưa IFormFile xuống repository. Transaction đặt chỗ upload, gọi scanner/storage ngoài transaction, finalize Ready chỉ sau recheck quyền/ownership; trả mediaAssetId. Không tự attach vào shop hay tăng Shop.Version khi upload chưa được save.

## 4. Request DTO #4

### 4.1. UpdateShopAppearanceRequest

| Field | DTO validation | Service |
| --- | --- | --- |
| expectedVersion | Required long?, >=1 | Shop version chung cho info/visibility/theme |
| templateId / templateRevisionId | Required Guid?, non-empty | Revision/head cùng nhau, latest khi chọn mới; hidden pinned exception theo tài liệu 02 |
| overrides | Required ThemeOverridesRequest object, có thể `{}` | Merge với pinned defaults, contrast và renderer capability checks |

ThemeOverridesRequest có đúng 7 optional init properties: primaryColor, backgroundColor, textColor, surfaceColor, buttonTextColor (`^#[0-9a-fA-F]{6}$`, max 7); borderRadiusPx integer 0–24; fontScale decimal 0.85–1.25, tối đa 2 decimals. Null value không hợp lệ nếu key xuất hiện; property omission mới là dùng default. Parser/validator phải phân biệt presence/null, không silently bỏ null. Canonicalize HEX uppercase trước hash/save.

### 4.2. CreateShopTemplateRequest / PublishTemplateRevisionRequest

| Field | DTO validation | Service |
| --- | --- | --- |
| code | Create only; Required 3–50, kebab-case ASCII | Unique canonical global; immutable về sau |
| name | Required 2–100 | Trim/NFKC, plain text |
| description | Optional max 1000 | Plain text |
| previewAssetId | Optional Guid non-empty | Ready TemplatePreview asset do Admin upload |
| sortOrder | Required int?, 0–10000 | Sort tie-breaker code/id |
| rendererKey | Required enum 5 key ở tài liệu 02 | Server registry + renderer capabilities |
| schemaVersion | Required int?, bằng 1 | BE/FE đã deploy hỗ trợ |
| defaults | Required ThemeDefaultsRequest | Cả 7 token đều bắt buộc; cùng range/contrast như overrides |
| layout | Required TemplateLayoutRequest | Typed, không nhận JSON string/HTML |
| expectedVersion | Publish only, Required long?, >=1 | Template head conditional update |

TemplateLayoutRequest: `sectionOrder` Required array 2–5 enum Hero/Story/Products/Reviews/Contact, không phần tử lặp, bắt buộc Products và Contact; `productColumnsDesktop` Required int? thuộc {2,3,4}. Cross-field và compatibility với renderer đặt ở service. Renderer có cấu trúc riêng, layout DTO chỉ cấu hình sections trong khả năng hỗ trợ. Không có object con tự tham chiếu.

Tạo template trả IsActive=false, IsPremium=false; publish mới không thay hai flags. `SetTemplateAvailabilityRequest`: expectedVersion + required bool? isActive. `SetTemplatePremiumRequest`: expectedVersion + required bool? isPremium. False là giá trị hợp lệ. Hide có global catalog guard, không giảm selectable <5 hoặc ẩn default template. Không hard-delete template/revision qua API v1.

## 5. Route/query DTOs

| Nhóm | Các trường và giới hạn |
| --- | --- |
| ShopSlugRoute | Pattern/length như create; canonical; reserved words không đi public query |
| ShopIdRoute/TemplateIdRoute | Guid khác Empty, không bind owner từ route |
| ShopProductQuery | categoryId Guid? non-empty; q optional <=100; minPrice/maxPrice decimal? 0…999999999999.99, <=2 decimals; flavor/filling optional <=100; weightGram int? 1–10000; inStockOnly bool default false; sort enum newest/priceAsc/priceDesc default newest; pageSize 1–50 default 20; cursor optional <=2048 |
| ShopCategoryQuery | q optional <=100; pageSize 1–100 default 20; cursor <=2048; sort name/id cố định |
| ShopReviewQuery | rating short? 1–5; pageSize 1–50 default 20; cursor <=2048; newest/id cố định |
| TemplateCatalogQuery | pageSize 1–50, cursor <=2048; sort sortOrder/code/id cố định; q <=100 |
| AdminShopQuery | page 1–1000, pageSize 1–100 default 20; status enum?; q <=100; sort createdAtDesc/nameAsc; filter ownerProfileId optional non-empty Guid |
| AdminTemplateQuery | page/pageSize như trên; isActive/isPremium bool?; q <=100; sort sortOrderAsc/updatedAtDesc |

Cross-field: minPrice <= maxPrice; price predicates áp cùng variant; cursor chữ ký/expiry/purpose/shop/filter/sort đúng. Unknown sort/enum/query key trả 400, không ghép vào SQL. Category lookup chỉ category đang có sản phẩm public trong shop, vẫn public-gate; trả suggestions phân trang, không cần tải toàn bộ taxonomy.

## 6. Ví dụ payload/response

```json
{
  "name": "Bánh Trung Thu An Nhiên",
  "slug": "banh-an-nhien",
  "description": "Bánh trung thu cho doanh nghiệp.",
  "templateId": "11111111-1111-4111-8111-111111111111",
  "templateRevisionId": "22222222-2222-4222-8222-222222222222",
  "logoAssetId": null,
  "bannerAssetId": null,
  "publicContactEmail": "sales@example.test",
  "publicContactPhone": "+84901234567",
  "publicAddress": "TP. Hồ Chí Minh"
}
```

Ví dụ appearance: `{"expectedVersion":3,"templateId":"11111111-1111-4111-8111-111111111111","templateRevisionId":"22222222-2222-4222-8222-222222222222","overrides":{"primaryColor":"#8C1524","buttonTextColor":"#FFFFFF","borderRadiusPx":12}}`.

```json
{
  "status": 201,
  "message": "Shop created successfully.",
  "data": {
    "shopId": "33333333-3333-4333-8333-333333333333",
    "version": 1,
    "name": "Bánh Trung Thu An Nhiên",
    "slug": "banh-an-nhien",
    "status": "Inactive",
    "allowedActions": ["EditInformation", "EditAppearance", "Activate"],
    "idempotentReplay": false
  },
  "path": "/api/v1/shops/me",
  "traceId": "example-trace",
  "time": "2026-10-25T02:00:00Z"
}
```

OwnerShopResponse thực tế còn có contact/media fields, template selection và suspension metadata theo quyền. PublicShopResponse dùng DTO riêng: shopId/slug/name/description/public contact/logo/banner, verified badge/reputation/aggregate review stats, appearance typed. Không serialize OwnerShopResponse rồi xóa một vài property.

Field error đề xuất giữ ApiResponse.errors string[] và bổ sung optional `validationErrors: [{field, code, message}]` để Form.setFields; backward-compatible cho consumer đọc errors. Business error cũng có field khi phù hợp (slug/templateId), không lộ constraint/table name. Cần review mở rộng ApiResponse trước implementation; không đổi cấu trúc errors hiện hữu.

Hiện `ConfigureInvalidModelStateResponse` dùng SupplierVerification error cho mọi non-auth request. Implementation phải phân loại theo endpoint metadata/nhóm route và trả SHOP_INVALID_REQUEST/THEME_TOKENS_INVALID cho shop, giữ nguyên auth/verification contracts; nếu không, DTO shop đúng vẫn bị gắn mã lỗi module khác. Đưa nested field path vào validationErrors, không bỏ mọi ModelState errors rồi trả message chung.

## 7. Mã lỗi / rate limits

| HTTP | errorCode tiêu biểu | FE xử lý |
| --- | --- | --- |
| 400 | SHOP_INVALID_REQUEST, THEME_TOKENS_INVALID, THEME_CONTRAST_INVALID, SHOP_CURSOR_INVALID | Gắn lỗi field, không retry |
| 401 | SESSION_EXPIRED | Refresh single-flight hoặc đăng nhập lại |
| 403 | SHOP_OPERATION_FORBIDDEN, SUPPLIER_NOT_VERIFIED | Refresh /auth/me, giữ draft, về Customer flow khi cần |
| 404 | SHOP_NOT_FOUND, SHOP_UNAVAILABLE, SHOP_MEDIA_NOT_FOUND | Empty state owner hoặc unavailable public; không lộ owner khác |
| 409 | SHOP_ALREADY_EXISTS, SHOP_SLUG_IN_USE, SHOP_VERSION_CONFLICT, SHOP_SUSPENDED, SHOP_STATE_CONFLICT | Giữ draft/tải lại; không báo success |
| 409 | TEMPLATE_NOT_SELECTABLE, TEMPLATE_REVISION_CHANGED, TEMPLATE_VERSION_CONFLICT, TEMPLATE_MINIMUM_CATALOG, TEMPLATE_DEFAULT_REQUIRED | Load catalog/detail mới, không auto đổi mẫu |
| 409 | SHOP_IDEMPOTENCY_PAYLOAD_MISMATCH, SHOP_MEDIA_NOT_READY | Tách attempt khi đổi payload hoặc tải lại |
| 413/415 | SHOP_MEDIA_TOO_LARGE, SHOP_MEDIA_UNSUPPORTED | Giải thích file, không retry tự động |
| 429 | SHOP_RATE_LIMITED | Tôn trọng Retry-After |
| 503 | DOCUMENT_SCANNER_UNAVAILABLE, SHOP_MEDIA_STORAGE_UNAVAILABLE, SHOP_BUSY | Retry có giới hạn cùng key, không giả thành công |
| 500 | INTERNAL_ERROR | TraceId cho support, không lộ stack/SQL |

Ngưỡng đề xuất: read authenticated 120/phút/user, public 120/phút/IP với proxy trusted đúng; write 30/phút/user; create 5/giờ/user; media 20/giờ/user và tối đa 2 upload đang xử lý. Mutation quotas quan trọng lưu DB dưới User lock, read limiter node-local chỉ là lớp phụ; scale-out cần gateway/distributed limiting. Không hard-code ngưỡng vào UI như chính sách pháp lý.

## 8. Ranh giới DTO khi truy cập database

```text
HTTP Request DTO → Controller → Service (business)
                                ↓ Application persistence DTOs
                  IShopRepository / IShopTransaction
                                ↓ Infrastructure mapping
                     EF entities / parameterized SQL
                                ↑ DTO snapshots/results
Service → Response DTO → ApiResponse<T>
```

Các nhóm DTO mới (không dùng HTTP DTO như entity update):

| DTO | Dữ liệu và trách nhiệm |
| --- | --- |
| ShopActorDto | UserId/role context server tạo; role từ middleware vẫn phải re-read dưới lock |
| ShopLockScopeDto | ActorUserId, owner profile/shop/template identifiers đã resolve server-side; operation kind; không nhận raw SQL |
| ShopContextQueryDto / ShopBusinessStateDto | Fresh UserStatusDto, SupplierEligibilityDto, ShopStateDto, TemplateStateDto, media ownership states |
| ShopWriteDto | Shop ID, expectedVersion, approved scalar changes, template revision/tokens đã normalize; không entity navigation |
| ShopMediaReserveDto / ShopMediaFinalizeDto | Owner/purpose/operation/hash/status/provider metadata đã kiểm chứng ở service |
| TemplateWriteDto / TemplateRevisionWriteDto | Normalized head metadata và immutable typed layout/defaults |
| ShopAuditWriteDto | Actor/action/resource/time/old-new state tóm tắt, reason; service tạo nội dung nghiệp vụ |
| ShopIdempotencyWriteDto | Scope/key/canonical hash/committed response/status/expiry |
| ShopPersistResultDto | Affected row count/new version/conflict category; service map sang business response |
| PublicShopQueryDto / ProductPageQueryDto | Typed allowlisted filters, decoded cursor, bounded limits |
| ShopReadDto / ShopProductCardReadDto / ShopAdminPageDto | Projection đúng fields, không entity/IQueryable/DbSet |

Transaction contract đề xuất: `InTransactionAsync<TResponse>(ShopLockScopeDto, Func<IShopTransaction, CancellationToken, Task<TResponse>>, CancellationToken)`. `IShopTransaction` chỉ nhận/trả DTO qua `ReadContextAsync`, `WriteShopAsync`, `WriteTemplateAsync`, `AppendAuditAsync`, `StoreIdempotencyAsync`. Concrete implementation giữ transaction/change tracker bên trong Infrastructure; callback service thực hiện business và throw để rollback. Một transaction/DbContext cho mutation+audit+idempotency; không service tự SaveChanges trước callback.

Không mở repository generic public `Update(Entity)`, không cho service tham chiếu Npgsql/EF hoặc tự tạo SQL; không đưa JSON/string SQL trong DTO. Legacy Admin shop entry points phải delegate contract này để không tồn tại đường vòng bỏ lock/version. Refactor các repository module khác sang DTO toàn bộ nằm ngoài scope, nhưng shop endpoints mới/cũ được chuyển đổi không được tiếp tục truyền entity.
