# Kết quả triển khai shop-management

Ngày: 2026-10-25. Phạm vi: Module 2, tính năng #3 quản lý gian hàng và #4 tùy chỉnh giao diện. Source BE/FE đã triển khai; database dự án đã cập nhật schema. Không commit, push hoặc bình luận GitHub.

## 1. Các luồng đã nối API

| Người dùng | Điểm vào FE | Hành vi |
| --- | --- | --- |
| Customer | Trang chủ → đăng ký nhà cung cấp | Giữ nguyên quy trình xác minh trong Customer layout; chưa được duyệt không mở workspace shop |
| Supplier đã duyệt | `/#nha-cung-cap/thiet-lap-gian-hang` | Tạo một shop ở trạng thái Inactive; sửa nhận diện, thông tin công khai; chủ động bật/ẩn |
| Supplier đã duyệt | `/#nha-cung-cap/giao-dien-gian-hang` | Chọn một trong ≥5 mẫu từ API, chỉnh màu/bo góc/cỡ chữ, preview local, lưu version/revision |
| Khách truy cập | `/shop/{slug}` | Thông tin công khai, danh mục, sản phẩm, bộ lọc và đánh giá; reload/deep link hoạt động |
| Admin | `/#admin/gian-hang` | Phân trang, lọc, xem chi tiết, đình chỉ/khôi phục bằng reason + expectedVersion |
| Admin | `/#admin/mau-gian-hang` | Tạo mẫu, phát hành revision, hiện/ẩn, đổi nhãn Premium và upload ảnh mẫu |
| Admin | Tab Gian hàng trong `/#admin/dieu-khien` | Dùng lại panel API, đã bỏ shop mock và thao tác trạng thái giả |

Năm renderer dùng chung giữa preview và public: heritage, royal-gift, minimal-grid, artisan-story, modern-catalog. Mỗi renderer có cấu trúc khác nhau. Premium là nhãn phân loại; không có thu phí giả. Module sản phẩm hiện có được đọc qua API; không thêm product CRUD hoặc checkout giả vào scope này.

Save lỗi giữ bản nháp; xung đột không tự lấy version mới để ghi đè. Có xác nhận khi rời trang đang sửa, kể cả điều hướng SPA; tải lại mới bỏ bản nháp theo quyết định người dùng. Các trang shop được tải theo route. Màu chỉ áp dụng trong renderer shop, không đổi theme Admin/Customer. Header Customer và menu drawer Supplier/Admin thích ứng màn hình nhỏ; có test chống tràn ngang ở viewport 390px cho public shop, form Supplier và preview.

## 2. Cấu trúc source

- Application: `DTOs/Shops/{Requests,Persistence,Responses}`, `IShopRepository`, `IShopService`, `ShopService*`, `Shop*Helper`, `ShopConstants`.
- Infrastructure: `ShopRepository*`, `ShopFeatureConfiguration`, `ShopLockHelper`, `ShopTradingLockHelper`, `ShopMediaGateway`, `ShopCursorProtector`.
- WebApi: `ShopsController`, `ShopTemplatesController`, `AdminShopsController`, `AdminShopTemplatesController`, `ShopRequestGuard`, `ShopMediaCleanupWorker`.
- FE: `services/shopService.js`, `hooks/useShopResource.js`, `components/shop/*`, Supplier shop pages, Admin shop/template pages, `PublicShopPage.jsx`, `utils/shopRoutes.js`.

Repository shop nhận/trả DTO; EF entity, DbContext và SQL được giữ ở Infrastructure. Application quyết định quyền, trạng thái, version, revision, quota, contrast và quy tắc media bên trong transaction. Những đoạn xử lý dùng lại nằm trong các file Helper.

Đã xóa các method shop cũ trong AdminPlatformService/Repository/interfaces và các wrapper FE status-only. HTTP Admin shop dùng chung path với hợp đồng action/version/reason mới. Admin metrics join shop.supplier_id với supplier_profiles.id.

## 3. Validation và SQL/concurrency

- DTO: required, giới hạn độ dài/số, enum, email, điện thoại quốc tế, slug, nested tokens/layout. Boolean nullable phân biệt false hợp lệ với thiếu trường.
- Trước model binding: giới hạn JSON 32 KiB/depth 8, chặn duplicate keys không phân biệt hoa thường, unknown JSON properties, query thừa/lặp và null token/layout members. Mutations yêu cầu UUID Idempotency-Key.
- Service: authority từ DB, một shop trong toàn bộ vòng đời Supplier, slug cố định/reserved, trạng thái suspended không được chủ shop bỏ qua, restore giữ trạng thái trước đình chỉ.
- SQL qua LINQ hoặc interpolated parameters; sort/action/renderer đi qua allowlist. Không ghép SQL từ q, slug, reason hoặc cursor.
- Khóa User → SupplierProfile → Shop → catalog guard khi cần → template heads theo ID → media theo ID. Cùng transaction ghi shop/template + audit + idempotent response.
- DB có unique/FK/check/version constraints; FK ghép chặn gán ảnh khác owner hoặc revision khác template. Retry deadlock/serialization có giới hạn; lock timeout trả SHOP_BUSY.
- Hide catalog giữ tối thiểu 5 mẫu, không ẩn mẫu mặc định. Shop pin revision cũ, kể cả khi head đã phát hành revision mới/ẩn.
- Order create/confirm và RFQ create/award có kiểm tra Active dưới khóa Shop trong transaction hiện hữu. Các transition fulfillment cũ giữ chính sách riêng của module đó.
- Public profile/products/categories/reviews gate lại shop Active + tài khoản active + Supplier Verified trong cùng read snapshot.
- Cursor DataProtection tách purpose, ràng buộc loại danh sách/shop/filter, hạn 15 phút. Public DTO không trả giấy tờ pháp lý, liên hệ riêng hoặc lý do moderation.

Không thêm RLS/grant policy cho các module khác. Quyền truy cập tính năng được thực thi ở API/service và các FK của schema; chưa xác nhận RLS bằng runtime role production.

## 4. Ảnh và cleanup

Ảnh public dùng Cloudinary hiện hữu. Scanner chung được tách thành IFileSafetyScanner/ClamAvScanHelper, tiếp tục phục vụ verification. Gateway shop chỉ nhận JPG/PNG/WebP một frame, tối đa 5 MiB, giới hạn dimension/pixel; SkiaSharp decode rồi re-encode để bỏ metadata. Không nhận SVG, HTML hoặc URL ảnh tự khai báo.

Reserve/finalize là các transaction ngắn; quét và upload diễn ra ngoài lock. Finalize kiểm tra lại quyền, trạng thái shop và upload lease. Có quota 20 lượt/giờ, tối đa 2 upload đang xử lý, lưu audit attempt/Ready. Retry cùng file/key không tạo Ready mới nếu đã hoàn tất.

Worker chạy mỗi phút, batch 20, chọn ảnh không gắn sau 24 giờ; recheck reference dưới lock rồi chuyển Deleting. Xóa provider ngoài transaction và chỉ tombstone DB sau thành công. Lỗi provider giữ public ID/proof để retry sau lease 5 phút. Retry upload không làm mất proof của provider object cũ. Idempotency hết hạn được dọn batch 100.

Đã smoke thực tế: tạo PNG 128×128 → ClamAV → re-encode → public Cloudinary upload → tải ảnh kiểm tra dimension → xóa ảnh thử thành công. Không tạo hồ sơ/shop hoặc gửi email cho tài khoản thật để thử luồng.

## 5. Migration và database

Đã áp dụng:

1. `20261025023429_ShopManagement`: shop version/owner/contact/suspension; revisions, media, catalog guard, idempotency; backfill/preflight và seed 5 mẫu.
2. `20261025031020_ShopPublicProductFeedIndex`: index phục vụ feed sản phẩm sau benchmark.

Preflight database dự án trước migration: 0 shop, 0 template, không orphan/slug sai/trạng thái legacy cần hòa giải. Bản sao hai bảng trước thay đổi nằm trong `.tools/backups/` được gitignore; không chứa trong tài liệu chia sẻ này. Không chỉnh .env/appsettings credentials.

SQL review artifact: [07_SHOP_MANAGEMENT_MIGRATION.sql](./07_SHOP_MANAGEMENT_MIGRATION.sql), idempotent từ migration supplier-verification. EF xác nhận không còn pending model changes. Kiểm thử migration dùng PostgreSQL thật dựng schema trước verification, sau đó migrate qua verification/shop/index.

Khi rollout vào môi trường khác: backup/rehearsal trước, migrate trước khi chạy binary mới, rồi deploy FE cùng API contract. Với bảng products lớn đang có traffic, chuẩn bị index trong cửa sổ rollout phù hợp; script hiện tại tạo index trong transaction migration. Tránh down schema shop đang có dữ liệu; ưu tiên forward fix và binary tương thích.

## 6. Bằng chứng kiểm thử

| Kiểm tra | Kết quả |
| --- | --- |
| .NET build / test project | 82/82 pass, không skip PostgreSQL tests |
| FE browser suite với LIVE_API_ORIGIN | 23/23 pass, gồm verification, shop và live API |
| FE production build | Pass; các trang shop có chunk riêng |
| ESLint các file shop mới/sửa trực tiếp | Pass |
| HTTP contract đang chạy localhost:8089 | Swagger các route shop, CORS, 401 endpoint riêng, 404 public unavailable: pass |
| ClamAV + Cloudinary thực tế | Pass, ảnh thử đã xóa |
| EF pending-model check | Không có thay đổi model chưa migrate |

BE gồm kiểm thử HTTP Kestrel + service + PostgreSQL thật, không chỉ mock service: tạo Inactive → bật Active → public → Admin suspend; CRUD template/revision; unknown/duplicate/malformed/null JSON, query thừa/lặp, multipart thiếu file, version cũ. Các test khác bao phủ concurrent create cùng owner/cùng slug, info-vs-theme, write đang chờ khi account bị disable, public gating, template minimum dưới hai Admin, pinned hidden revision, key mismatch, rollback khi audit lỗi, foreign-owner FK, cleanup ảnh còn reference, variant predicates, query count, cursor tamper/scope/expiry và ảnh/scanner lỗi.

Browser transport fixtures xác nhận UI và payload/idempotency; chúng không thay thế HTTP/PostgreSQL tests. Bộ test cũ xác nhận Customer onboarding, realtime xác minh và preview PDF/ảnh vẫn hoạt động. Shop tests kiểm tra create, 5 renderer, save/reload, draft conflict, role guard, public URL/filter/404, moderation, minimum catalog, dirty navigation và mobile.

## 7. Benchmark và truy vấn

[08_QUERY_BENCHMARK.json](./08_QUERY_BENCHMARK.json) chứa SQL parameterized và EXPLAIN ANALYZE BUFFERS. [Bản trước tối ưu](./08_QUERY_BENCHMARK_BEFORE.json) giữ để đối chiếu.

Dataset test: 10.000 shop, 100.000 sản phẩm, 300.000 variant, 100.000 ảnh; shop nóng có 50.000 sản phẩm. Môi trường Windows host, Docker PostgreSQL 17/pgvector, loopback, warm cache, 20 lời gọi Application service tuần tự mỗi kịch bản.

| Kịch bản | p95 trước (ms) | p95 sau (ms) | Reader commands sau |
| --- | ---: | ---: | ---: |
| Public profile | 12.52 | 8.23 | 2 |
| Owner preview | 525.82 | 131.30 | 3 |
| Sản phẩm mới nhất | 597.42 | 104.89 | 2 |
| Sản phẩm lọc giá/nhân | 384.64 | 212.67 | 2 |
| Admin page 10 | 6.78 | 6.82 | 3 |
| Admin page 100 | 7.95 | 7.35 | 3 |

Admin gồm một query authority + count + projection rows; tăng pageSize không tăng số query. Feed newest lấy trang trước khi tính giá/ảnh của từng sản phẩm; không aggregate toàn bộ shop trước LIMIT. Preview dùng cùng truy vấn với giới hạn 12 card. Price sort vẫn aggregate trên các variant đáp ứng cùng bộ lọc; chưa cần price-summary read model theo phép đo này.

Đây là phép đo local trên data giả, chưa phải load test HTTP nhiều người dùng hoặc xác nhận SLO production. Chưa thực hiện canary/production deploy, recovery drill, RLS runtime-role test hoặc xây dashboard telemetry riêng.

## 8. Chạy lại

Backend:

```powershell
dotnet build Mooncake.EcommercePlatform.sln
$env:TEST_PG_CONNECTION_STRING = '<disposable loopback PostgreSQL with pgvector>'
dotnet test tests/Mooncake.EcommercePlatform.Verification.Tests
dotnet run --project tests/Mooncake.EcommercePlatform.Shop.Benchmarks
```

Fixture bắt buộc loopback, tạo database tên ngẫu nhiên và drop khi hoàn tất; không đọc database dự án để seed dữ liệu test. Benchmark tạo lại artifact trong folder này.

Frontend (thư mục mooncake-fe):

```powershell
npm run build
$env:LIVE_API_ORIGIN = 'http://localhost:8089'
npx playwright test
```

API local: `dotnet run --project src/Mooncake.EcommercePlatform.WebApi`. Khi hosting FE, cấu hình SPA fallback về index.html cho `/shop/*` và giữ route `/auth/google/callback`. FE hiện dùng hash route cho workspace; public shop dùng pathname thật.

## 9. Kết quả chốt

Đã chốt: **82/82 BE tests, 23/23 browser tests, FE build và scoped lint đều pass**. API local đã chạy lại với binary mới trên localhost:8089. PostgreSQL container dành riêng cho test đã dừng sau khi fixture dọn database tạm; ClamAV phục vụ upload vẫn hoạt động. Không commit, push hoặc bình luận GitHub.

[Ảnh chụp public shop trên mobile](./artifacts/public-shop-mobile.png) dùng dữ liệu fixture trong browser test.
