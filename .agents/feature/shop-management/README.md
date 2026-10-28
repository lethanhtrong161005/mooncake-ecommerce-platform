# Module 2 — Gian hàng: tính năng #3 và #4

- Feature key: `shop-management`.
- Ngày thiết kế: **2026-10-25**. Phiên bản: **1.0.0**.
- Trạng thái: **Đã triển khai BE + FE và migrate database dự án (2026-10-25)**. Kết quả kiểm thử, benchmark và hướng dẫn chạy nằm trong [09_IMPLEMENTATION_REPORT.md](./09_IMPLEMENTATION_REPORT.md).
- #3: Tạo và quản lý gian hàng, profile công khai, sản phẩm và bộ lọc trong shop.
- #4: Tùy chỉnh giao diện, tối thiểu 5 template, live preview và quản trị template.

## Thứ tự đọc

Bản sửa upload ảnh ngày 2026-10-28: [10_MEDIA_UPLOAD_FIX.md](./10_MEDIA_UPLOAD_FIX.md) — bỏ phụ thuộc ClamAV khi upload logo/banner.

Kết quả thực hiện: [09_IMPLEMENTATION_REPORT.md](./09_IMPLEMENTATION_REPORT.md). SQL triển khai: [07_SHOP_MANAGEMENT_MIGRATION.sql](./07_SHOP_MANAGEMENT_MIGRATION.sql). Bằng chứng SQL/EXPLAIN: [08_QUERY_BENCHMARK.json](./08_QUERY_BENCHMARK.json).

| Tài liệu | Mục đích |
| --- | --- |
| [01_SHOP_MANAGEMENT_DESIGN.md](./01_SHOP_MANAGEMENT_DESIGN.md) | Thiết kế #3 theo đủ I–VI của template, quyền và trạng thái shop |
| [02_STOREFRONT_CUSTOMIZATION_DESIGN.md](./02_STOREFRONT_CUSTOMIZATION_DESIGN.md) | Thiết kế #4 theo đủ I–VI, renderer, token, template revision và preview |
| [03_API_DTO_VALIDATION.md](./03_API_DTO_VALIDATION.md) | Endpoint, validation từng trường, lỗi và DTO qua ranh giới persistence |
| [04_DATA_SQL_CONCURRENCY.md](./04_DATA_SQL_CONCURRENCY.md) | Schema, index, SQL injection, N+1, khóa, retry, migration và bảo mật dữ liệu |
| [05_FRONTEND_INTEGRATION.md](./05_FRONTEND_INTEGRATION.md) | Tích hợp mooncake-fe, UI Customer/Supplier/Admin, trạng thái lỗi và live preview |
| [06_IMPLEMENTATION_TEST_ROLLOUT.md](./06_IMPLEMENTATION_TEST_ROLLOUT.md) | Backlog có thứ tự, test matrix, rollout và tiêu chí nghiệm thu |

## Căn cứ và mức ưu tiên

1. Yêu cầu hiện tại: validation hình dạng dữ liệu ở request DTO; business ở Application Service; đi/đến repository bằng DTO. Quy tắc này áp dụng cho phần shop mới và các entry point shop cũ được chuyển đổi.
2. [Business Module 2, #3/#4](../../business/mooncake-marketplace.md), [template I–VI](../template/FEATURE_DETAIL_DESIGN_TEMPLATE.md).
3. [Quy tắc BE](../../AGENTS.md), [quy tắc FE](../../../../mooncake-fe/.agents/AGENTS.md).
4. [Luồng Customer → Supplier đã chốt](../supplier-verification/08_CUSTOMER_TO_SUPPLIER_FLOW.md): giữ layout Customer tới khi Admin duyệt. Không tạo lối đăng ký thẳng Supplier trong feature shop.
5. [Storage-file](../storage-file/STORAGE_FILE_PLAN.md), [vận hành scanner](../supplier-verification/09_DOCUMENT_UPLOAD_OPERATIONS.md).
6. [PostgreSQL skill trong repo](../../skills/supabase-postgres-best-practices/SKILL.md): các reference constraints, FK indexes, lock ordering, short transactions, N+1, pagination, privileges, EXPLAIN.

Template có ví dụ auth/OTP và message tiếng Việt. Bộ thiết kế dùng cấu trúc I–VI, thay ví dụ OTP bằng nghiệp vụ shop; message/log/API identifier dùng tiếng Anh theo AGENTS, FE dịch nhãn/lỗi sang tiếng Việt. Quy tắc DTO riêng của yêu cầu này cụ thể hơn quy tắc repository trả entity trong AGENTS; phần shop không đưa EF entity/IQueryable/DbContext qua Application boundary.

## Hiện trạng trước triển khai (giữ để đối chiếu thiết kế)

| Thành phần | Đã có | Khoảng trống/rủi ro cần giải quyết |
| --- | --- | --- |
| Shop / ShopTemplate | Entity, EF mapping, unique supplier/slug; JSONB colors/layout | Chưa có version shop/template, premium, revision bất biến, FK rõ ràng trong mapping |
| `SaveShopRequest`, `ShopResponse` | DTO sơ bộ | Chưa có Supplier CRUD controller/service; DTO nhận URL ảnh tùy ý; thiếu concurrency và theme contract |
| Admin shops | GET list, PATCH status trong AdminPlatformController | List không phân trang; service gọi count từng shop gây N+1; mutation thiếu expectedVersion/reason/audit riêng |
| Identity | `shops.supplier_id` được Order/RFQ/Promotion dùng như `supplier_profiles.id` | Admin metrics join `profile.UserId` là không thống nhất, phải sửa về `profile.Id`; không đoán dữ liệu thực tế trước preflight |
| FE StoreSetUp | Có UI mẫu, 4 template TPL-01…04 và success toast | Không gọi shop API, chưa đủ ≥5, không lưu thật, không public shop route |
| Bootstrap | Seed một template Classic | Cần seed bổ sung an toàn, không chạy lại bootstrap tạo Admin cho mục đích seed |
| Eligibility | Middleware hiện tại + User → SupplierProfile lock | Tái sử dụng để chống race với revoke/disable; JWT/FE không là nguồn quyết định quyền |

Source hiện có được đọc ở Domain/Entities/Shop.cs, ShopTemplate.cs; Infrastructure/Persistence/Configurations/Shop*.cs; Application/Services/Implementations/AdminPlatformService.cs; Infrastructure/Repositories/{AdminPlatform,Order,Rfq,Promotion}Repository.cs; mooncake-fe/src/Pages/Supplier/StoreSetUp.jsx và services/adminService.js. Không kết nối DB thực tế để khảo sát trong lần lập plan này.

## Các quyết định dùng làm baseline đề xuất

- Một Supplier đã verified có tối đa một shop trong toàn bộ vòng đời, kể cả soft delete. Shop mới bắt đầu Inactive; chủ shop chủ động bật Active sau khi đủ dữ liệu.
- `supplierId` nội bộ là supplier profile ID, `ownerUserId` là User ID. Client không được chỉ định hai trường này khi tạo/sửa.
- Slug ASCII canonical, duy nhất toàn cục và bất biến sau tạo trong phiên bản đầu. Không thêm hard delete hay đổi slug trong scope này.
- Admin suspension ưu tiên hơn mọi toggle của Supplier; khôi phục về trạng thái trước suspension, không tự bật một shop vốn Inactive.
- Shop đã lưu pin template revision; ẩn template chặn chọn mới, không phá giao diện các shop đang dùng. Premium là nhãn, chưa có paywall/subscription.
- Không cho CSS/HTML/JS tùy ý. Override là typed tokens allowlist; vẫn lưu cột `custom_colors` JSONB tương thích business.
- Không tự thêm SignalR shop events, billing hay product CRUD; dùng cơ chế xác minh hiện có và fetch lại dữ liệu có kiểm soát. Public shop có read APIs riêng cho sản phẩm/review hiện có.

Các giới hạn độ dài, file, rate limit, quota, slug policy, token range và SLO trong plan là **đề xuất kỹ thuật cần chốt khi review**, không phải quy định pháp lý. Chưa có quyền sản xuất/billing Premium để suy diễn thêm.
