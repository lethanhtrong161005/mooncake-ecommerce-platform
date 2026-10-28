# Backlog triển khai, kiểm thử và rollout

Ngày: 2026-10-25. **Đã triển khai BE/FE và migrate database dự án**. P0–P6 đã có source; P7 đã kiểm thử local. Bằng chứng và các bước rollout production chưa thực hiện nằm trong [09_IMPLEMENTATION_REPORT.md](./09_IMPLEMENTATION_REPORT.md). Ma trận bên dưới là tiêu chí thiết kế; không khẳng định mọi biến thể đều có automated test.

## 1. Thứ tự triển khai và deliverable

| Phase | Công việc | Deliverable/điều kiện qua bước |
| --- | --- | --- |
| P0 — Contract và khảo sát | Chốt DTO boundary, status/premium/slug policies, static renderer registry; đọc history/schema trên bản sao và kiểm kê consumers của Admin shop API | API examples + field validation contract, bảng mapping legacy IDs/templates, preflight không còn ambiguity |
| P1 — Data foundation | Expand schema, version, FK/unique/check, revisions, catalog guard, media/idempotency tables; seed ≥5 an toàn | EF migration + SQL review artifact + up/backfill/rollback test trên PostgreSQL; không pending model changes |
| P2 — Persistence DTO | IShopRepository/IShopTransaction và template read/write DTO ports; lock order và conditional update; idempotency/audit cùng transaction | Repository chỉ nhận/trả DTO; entity/DbContext/Npgsql không lộ ra service/controller |
| P3 — Shop business | Owner create/read/update/visibility, media orchestration; Admin suspend/restore; đổi các entry point cũ sang cùng service | Có field validation + service rule + stable errorCode; không còn bypass writer thiếu version |
| P4 — Template/theme | Catalog, exact pinned revisions, renderer/default/token validation, Admin add/publish/availability/premium, minimum guard | ≥5 mẫu selectable, hidden pinned vẫn hoạt động, không nhận CSS/code tùy ý |
| P5 — Public read + integration | Slug profile, appearance, product/category/review paging, owner preview; sửa Admin metrics join/N+1; nối shop locks vào giao dịch mới | Gate active+verified đồng nhất; query count bounded, đồng thời suspend/revoke không lọt giao dịch mới |
| P6 — FE | Thay StoreSetUp mock, create/info/theme forms, 5 renderer, public path, Admin pages, dirty/conflict/errors | API thật, no fake success, route/layout đúng role và reload giữ dữ liệu |
| P7 — E2E và rollout | HTTP contract, concurrency, security, perf, observability, staging smoke, backup/recovery | Checklist DoD hoàn tất trước mở production |

Phụ thuộc: P1→P2→P3/P4→P5; FE renderer có thể làm bằng fixture ghi rõ minh họa sau P0, nhưng chỉ nghiệm thu khi nối API thật. Không deploy public/template editor với schema chưa migrate hoặc chỉ có 4 template local.

## 2. Thay đổi source cần dự kiến

### 2.1. Backend

- [x] Domain Shop/ShopTemplate, ShopTemplateRevision, ShopMediaAsset, ShopIdempotencyRecord, ShopTemplateCatalogState; purpose/action/renderer allowlists được kiểm tra ở DTO/service.
- [x] Request DTO init properties/nested tokens/layout/query DTOs và route/header validation. Không thêm [property: Required] vào positional record.
- [x] Application service business rules và constants/helpers; service không truy cập EF/Npgsql hoặc trả entity.
- [x] Persistence DTO ports/transaction abstraction; một DbContext cho commit shop+audit+idempotency. Tham số scope/query do server tạo, không pass user-supplied SQL/expression.
- [x] Infrastructure mappings/FK/indexes, repository projections và batch read; converter JSONB typed values; giới hạn query/timeout.
- [x] Media dùng **CloudinaryStorageService hiện hữu** cho public images; tách scanner interface chung từ verification adapter khi cần, không chép code quét thành hai implementation lệch nhau. Thêm image decoding/re-encoding adapter có giới hạn pixel, xác minh dependency trước chọn package.
- [x] Media reserve/finalize DTO workflow, owner/quota check lại; job dọn unattached media đề xuất sau 24h, xử lý theo batch. Chỉ xóa asset xác nhận thuộc namespace shop, chưa được shop/template/revision tham chiếu; recheck dưới lock. Provider failure ghi retry, không xóa DB proof trước khi xóa provider thành công.
- [x] ShopsController/ShopTemplatesController/AdminShopTemplatesController; giữ path Admin shop cũ nhưng chuyển request sang action/version/reason và paged read.
- [x] ModelStateResponse factory phân biệt shop/auth/verification, trả field errors đúng module; scope theo endpoint metadata thay vì mọi non-auth là supplier error.
- [x] Transactional-audit endpoint metadata để AdminAuditMiddleware không ghi thêm audit ngoài transaction cho nhóm này.
- [x] AdminPlatformService/GetShops đổi DTO pagination, count projection; AdminPlatformRepository metric join supplier profile ID đúng. Test không nhầm users.id với profiles.id.
- [x] CurrentAccountMiddleware/capabilities tiếp tục quyền DB hiện tại; shop service không tin FE guard. New order/bid gates tham gia cùng Shop lock trong transaction ghi hiện hữu.
- [x] DI/rate policies/config options cần review trong implementation; không thay .env/appsettings chứa credentials như một phần plan.

### 2.2. Frontend

- [x] shopService/shopTemplateService qua apiClient, constants/endpoint/error labels, hooks abort/invalidation/idempotency.
- [x] StoreSetUp không còn success toast khi chưa lưu thật; tách create/information/appearance, bỏ TPL local IDs.
- [x] Duy trì Customer→Supplier verification flow đã chốt; chỉ approved capability mở shop workspace. Shop chưa tạo là empty state riêng, không dùng verification form thay thế.
- [x] Media upload/photo preview, token controls và 5 renderer dùng lại giữa preview/public; clear dirty state chỉ sau committed response.
- [x] Public `/shop/{slug}` và deep-link fallback, product/review/categories reads; không gọi endpoint product chưa triển khai rồi fallback dữ liệu giả.
- [x] Admin table/paging/moderation reason/version; template metadata/revision/premium/visibility và lỗi minimum catalog.
- [x] Cập nhật adminService.setShopStatus callers và App menu/routes tương ứng, không tự bổ sung expectedVersion hiện tại ở client để bypass conflict.

## 3. Test matrix bắt buộc

### 3.1. Request DTO / MVC / HTTP

| ID | Trường hợp | Expected |
| --- | --- | --- |
| V01 | Null/malformed JSON, unknown top-level/nested fields, duplicate case-insensitive keys, body quá lớn/deep | 400/413 đúng envelope, service chưa được gọi |
| V02 | Name whitespace/control/quá dài; slug uppercase/reserved/Unicode/slash/encoded slash | 400 đúng field; không silently tạo slug khác |
| V03 | Guid.Empty, missing templateRevisionId, expectedVersion thiếu/0/âm | 400, không positional-record metadata exception |
| V04 | False bool/0 sortOrder hợp lệ; bool bị bỏ/null sai; enum int ngoài tập | Phân biệt rõ missing và giá trị hợp lệ |
| V05 | Email/phone/public address sai; null clear PUT; client gửi supplierId/verified/reputation/customCss | Lỗi field/overposting; không ghi authority từ request |
| V06 | Tokens sai key/type/null/HEX/range/contrast, fontScale nhiều decimals, layout duplicate/unknown renderer | 400 DTO hoặc service policy đúng field; không lưu CSS lạ |
| V07 | Query invalid sort/page/pageSize/decimal overflow/min>max/cursor bound; ILIKE literals `%` `_` | 400 hoặc literal search đúng, không SQL injection |
| V08 | Toàn bộ request mới qua MVC/HTTP gồm multipart + nested DTO + positional record nếu có | Test validation path thật; không chỉ gọi service trực tiếp |

### 3.2. Business/service

| ID | Trường hợp | Expected |
| --- | --- | --- |
| B01 | Customer requestedRole Supplier/pending, Supplier revoked, inactive/deleted account, Admin `/me` | 403, không tạo shop/mở kênh Supplier trái quyền |
| B02 | Verified Supplier tạo lần đầu | Shop Inactive/version1, đúng profile owner, audit/key một lần |
| B03 | Đã có shop kể cả soft-deleted rồi tạo lại | 409; không tạo shop thứ hai/chiếm lại slug |
| B04 | Sửa body cố đổi owner/slug/status bằng info DTO | Reject, không mass assign |
| B05 | Supplier toggle từ Suspended hoặc tự restore | 409/403, suspension không bị bỏ qua |
| B06 | Admin suspend Inactive rồi restore | Trở lại Inactive, không tự Active; reason/audit đủ |
| B07 | Owner bị revoke trong thời gian Suspended, Admin restore | Conflict/forbidden theo policy; không public hóa owner chưa đủ quyền |
| B08 | Update theme | Chỉ thay template revision/tokens/shopVersion, product IDs/count/stock không đổi |
| B09 | Head hidden, shop đang pin cùng tuple; shop khác chọn hidden | Cũ vẫn save/render, mới bị chặn |
| B10 | Head publish revision mới | Shop cũ không đổi; chọn revision catalog stale trả conflict |
| B11 | Premium label true/false | Metadata đổi, không phát sinh phí/entitlement giả |
| B12 | API success nhưng gửi email/push chưa có scope | Không báo “đã gửi thông báo shop” không có implementation |

### 3.3. PostgreSQL integration và race

Dùng PostgreSQL thật (test container/bản test riêng), ít nhất hai DbContext/connection độc lập và barrier đồng thời; không dùng EF InMemory để xác nhận constraints/locks. Không đọc appsettings production trong test fixture; seed/dọn chỉ database test đã xác minh.

| ID | Race/failure | Expected |
| --- | --- | --- |
| D01 | Hai create cùng Supplier/key khác | Một 201, một 409; một shop |
| D02 | Hai Supplier tạo cùng slug | Một 201, một 409 đúng constraint, không 500 |
| D03 | Retry cùng key sau commit/mất response | Cùng response/version, một business audit; đổi payload cùng key 409 |
| D04 | Update info vs theme cùng expectedVersion | Một commit, một conflict, không mất field |
| D05 | Suspend vs owner activate/save | Thứ tự commit xác định, Suspended không bị stale update ghi đè |
| D06 | Revoke/disable vs create/theme/activate | Fresh quyền dưới cùng lock; request chờ không vượt qua revoke đã commit |
| D07 | Hide vs select template; two publishes | Head/revision nhất quán; shop FK revision cùng template; conflict hợp lệ |
| D08 | Đang 6 active, hai Admin ẩn hai mẫu khác nhau | Sau cùng >=5; một hide bị 409. Không assert hai fail khi đang 7 active |
| D09 | Suspend vs create/confirm order hoặc create/award bid | Không ghi giao dịch mới dựa shop đã bị chặn trước lượt commit; fulfillment cũ vẫn theo policy |
| D10 | Cố ghi cross-owner asset/cross-template revision/orphan profile bằng SQL trực tiếp | DB FK/check chặn; không chỉ service check |
| D11 | Inject fail sau shop write trước audit/key | Rollback cả ba; retry không nhân dữ liệu |
| D12 | Deadlock/serialization/lock timeout, connection mất lúc commit | Bounded retry whole transaction, fresh context, cùng key; không replay cloud upload trong lock |
| D13 | Seed/migrate nhiều lần + legacy dữ liệu không sạch | Không duplicate template/Admin, fail preflight có báo cáo; không tự xóa dữ liệu |
| D14 | Template code/slug casing/length và status DB enum | DTO/EF/DB đồng nhất; không lỗi naming casing/thiếu cột |

### 3.4. Public read/security/media

- [ ] Shop Inactive/Suspended, owner revoked/disabled/deleted: mọi profile/products/reviews/categories endpoint đều 404; không chỉ hide UI.
- [ ] Public DTO không có legal documents, owner private email/phone, moderation reason, raw custom_css/provider secret, unpublished template defaults.
- [ ] Product filter dùng cùng variant: fixture A rẻ nhưng filling khác, B đúng filling nhưng giá ngoài range phải không match sai.
- [ ] Public product cards không kéo full image/variant collection, no duplicate product trên page; cursor tie-break stable, token bị sửa/khác filters/shop/expired bị chặn.
- [ ] Review private/anonymous không lộ display-name/email ngoài policy; aggregate chỉ tính review hợp lệ public, null rating không thành 0★.
- [ ] Upload sai signature, fake extension, SVG/HTML, quá bytes/pixels, scanner down/infected, provider lỗi, asset khác owner/purpose; không bind Ready sai.
- [ ] Media finalize sau revoke/timeout, retry same upload key, orphan cleanup không xóa ảnh còn được revision/shop tham chiếu.
- [ ] SQL injection payload ở q/slug/sort/cursor/reason không thay SQL semantics; XSS/CSS url()/JS/HTML không thực thi.
- [ ] DB grants/RLS nếu áp dụng được test bằng đúng runtime role và connection pooling reuse giữa hai user, không test bằng superuser rồi kết luận RLS đúng.

### 3.5. FE/browser E2E

- [ ] Customer đăng ký Supplier theo Customer layout; approved mới có workspace; reload auth không lộ menu Supplier trước quyết định.
- [ ] Verified chưa có shop tạo Inactive → chỉnh thông tin → Active → mở `/shop/slug` và reload thấy dữ liệu thật.
- [ ] ≥5 mẫu có renderer khác nhau; ColorPicker/Slider chỉ render local, không ghi DB khi chưa Save.
- [ ] Owner preview Inactive xem được sản phẩm của chính shop; Anonymous không gọi được preview endpoint.
- [ ] Theme save/reload/public nhất quán; đổi mẫu giữ số product/variant/tồn kho; hidden pinned không auto đổi.
- [ ] Two-tab conflict, suspend/revoke khi đang edit, catalog hidden/revision stale: giữ draft, hiển thị lỗi, không success giả.
- [ ] Admin list/moderation/template metadata chạy API; version/reason bắt buộc; reason không mất sau conflict.
- [ ] Product filters reset cursor, debounce/abort không render response cũ; 404/empty/error tách biệt.
- [ ] Mobile, keyboard, labels/contrast, URL deep-link/back/forward và `/auth/google/callback` không regression.
- [ ] Mock transport browser tests cộng live HTTP test trên test DB; không dùng mocked 200 làm bằng chứng whole integration đã pass.

## 4. Performance và observability

- Test data đề xuất 10k shops, 100k products, nhiều variants/images/reviews; các shop nóng và skew count lớn. Seed riêng môi trường perf, không bơm vào DB dự án.
- EF DbCommandInterceptor đếm queries theo request; pageSize 10/50/100 vẫn query count bounded theo 04. Lưu SQL parameterized + EXPLAIN (ANALYZE, BUFFERS) artifact trên test dữ liệu, không bật sensitive-data logging.
- SLO dự kiến cần đo: p95 profile <=300ms, product/admin page <=500ms trên môi trường benchmark được ghi rõ; không bao gồm external media upload. Không coi đây là kết quả đã đạt.
- Upload budget: tối đa 5 MiB, scanner ~15s và provider theo options hiện có (tối đa 60s đề xuất); FE timeout 90s + hủy khi rời trang. Quota ngăn concurrency vô hạn; image decode có timeout/pixel bounds.
- Metrics: mutation conflicts, idempotent replays/mismatch, lock wait/retry/deadlock, query counts/latency, media Failed/orphan cleanup backlog, public unavailable, renderer fallback. Log traceId/actor/resource/operation/version nhưng không token/file/secret/full legal details.
- Current Serilog DB Debug có thể tạo nhiều log; review telemetry level khi release, không thay config như một mẹo fix SQL. Log middleware không ghi query access_token hoặc body upload.

## 5. Rollout / compatibility / rollback

1. Review chính sách và contracts, tạo migration artifact với preflight/report; chạy trên bản sao DB đang dùng các migration gần nhất, không assume schema baseline từ mcake.sql.
2. Backup, thử phục hồi; expand schema và backfill, validate FK/check/index; seed 5 + guard/default an toàn. Không deploy binary cần cột mới trước migration — bài học notification_sequence.
3. Deploy FE renderer registry trước hoặc cùng schema version mà BE cho publish; BE expose API sau khi minimum catalog/revisions đã hợp lệ.
4. Shop Admin API cũ đổi unpaged list/status-only body sang paged/action+version+reason; cập nhật tất cả consumer cùng release. Legacy payload thiếu version trả 400, không tự lấy version hiện tại để “tương thích” làm mất concurrency guard.
5. Chuyển hết old writers về DTO transaction path. Endpoint metadata chặn generic audit ngoài transaction; ModelState mappings đúng feature. Không để song song controller cũ ghi thẳng DB.
6. Staging smoke với hai Supplier, hai Admin, một Customer/Anonymous; chạy test race/public gating bằng DB thật và test upload bằng dịch vụ test. Không tự gửi thư/thực hiện moderation lên shop thật để nghiệm thu.
7. Canary rồi mở rộng, theo dõi conflict/SQL/runtime renderer errors; disable write feature nếu cần, vẫn giữ đọc pinned revisions tương thích.
8. Rollback không xóa revisions hoặc data mới đang có referential links. Giữ schema additive và forward fix; không down migration destructive trên live để quay lại UI mock.

## 6. Chính sách cần xác nhận khi review

| Chủ đề | Baseline đề xuất trong plan | Tác động nếu đổi |
| --- | --- | --- |
| Visibility lúc tạo | Inactive, Supplier tự bật | Nếu muốn Active mặc định cần đổi test/public publish rules |
| Slug | Immutable v1, reserved/tombstone không tái dùng | Đổi slug cần alias/redirect/SEO và race design bổ sung |
| Premium | Nhãn, không billing/entitlement | Paywall là scope mới có quyền mua/gia hạn |
| Template hidden | Pin existing revision, chặn chọn mới; luôn >=5 selectable | Nếu emergency-disable ảnh hưởng shop đang dùng cần workflow fallback/audit riêng |
| Admin restore | Về preSuspensionStatus; owner verified | Khác chính sách phải thống nhất service/DB/UI |
| Branding | Public logo/banner, 5 MiB/20MP, proof qua mediaAssetId | Private branding cần cấp quyền đọc/render khác |
| Contact | Opt-in riêng, không lấy legal/user private tự động | Thay đổi cần privacy review và DTO version |
| Limits/SLO | Giá trị đề xuất ở 03/04/06 | Chỉnh theo tải thực tế, test trước rollout |

Những mục này không chặn việc đọc/review plan; chưa được xem là approval deploy hoặc kết quả implementation.

## 7. Definition of Done

- [ ] Đủ #3/#4 theo business, ≥5 template, flow Customer→Supplier không regression.
- [ ] HTTP DTO validation + service business + DB constraints cùng bảo vệ; lỗi đúng ApiResponse/field/errorCode.
- [ ] Shop persistence boundary input/output chỉ DTO; service/controller không dùng EF entity/DbContext/IQueryable.
- [ ] SQL injection/N+1/lost update/deadlock/idempotency/suspend-revoke races có test thật, không chỉ mô tả.
- [ ] Admin old writers và metrics join sai đã được chuyển/sửa; audit nguyên tử một lần cho một logical command.
- [ ] Migration/backfill/reconciliation có artifact và kết quả test trên bản sao, không mất product/order/history.
- [ ] BE build/tests/model sync; FE build/lint phần sửa/E2E; toàn repo baseline failures được ghi riêng, không claim pass nếu chưa pass.
- [ ] Public data/contact/theme an toàn, rendering/preview consistent, media scan/storage vận hành có hướng dẫn.
- [ ] Báo cáo triển khai mới ghi rõ kiểm tra đã chạy, giới hạn chưa kiểm chứng, cấu hình cần thiết; không commit/push nếu chưa có yêu cầu.
