# Plan: Phân trang `page/pageSize` + Filter cho các flow "get all"

## 1. Hiện trạng (đã khảo sát)

| Nhóm | Hiện trạng |
| :--- | :--- |
| **Đã chuẩn `page/pageSize/totalCount`** | `AdminShops`, `AdminShopTemplates`, `Suppliers/applications` & `history`, `Products/Search`, `VerificationNotifications`, `Notifications inbox` |
| **Có phân trang nhưng thiếu total/filter** | `Admin/audit-logs` (trả mảng trần), `Reviews/shops/{slug}` (`pageNumber`), `ProductReviews feed`, `Shop public` (cursor) |
| **Phân trang cursor (`limit`)** – lệch chuẩn | `Orders/mine/paged`, `Orders/supplier/paged` |
| **Trả TOÀN BỘ, FE tự cắt bằng AntD** | `Orders/mine`, `Orders/supplier`, `Payments/mine`, `Deliveries` (mine), `Contracts/mine`, `Rfqs/mine`, `Rfqs/supplier`, `SupplierPromotions`, `Users` (GetAll), `Admin/disputes`, `Admin/orders/operations` (4 danh sách lồng), `AiAnalytics/reports` |
| **Không cần phân trang** (dữ liệu nhỏ/bounded) | `system-config`, `categories`, `regions`, `contract-deposit-rules`, `packaging-templates`, `deliveries/{id}/proofs`, `rfqs/{id}/bids`, `top-suppliers` (Top N) |

Vấn đề: FE `MyOrders`, `OrderManage`, `TradingOperating`, `DeliverManage`, `ContractManage`, `SupplierDashboard`, `DeliveryPoD` đều `pagination={{pageSize: N}}` phía client → tải hết dữ liệu, chậm khi data lớn, filter chỉ trên trang hiện tại.

## 2. Contract chuẩn dùng chung

**Request** (`PagedQuery` base, validate ở service):
```
page      int  >=1, <=1000, default 1
pageSize  int  1..100, default 10   (audit-logs: max 200). Chỉ dùng page/pageSize, KHÔNG dùng cursor/limit
q         string? (<=100, tìm theo text)
sort      string? whitelist theo từng endpoint (vd createdAtDesc, totalAmountAsc)
from/to   DateTime? (lọc theo createdAt, UTC)
```
**Response** (`PagedResult<T>`, tương thích `ShopPageResponse` / `VerificationPage` đang có để FE cũ không vỡ):
```
{ items, page, pageSize, totalCount, totalPages, hasNext, hasPrevious }
```
Quy tắc: sort whitelist (không nhận field tự do), tie-break theo `Id` để ổn định, `AsNoTracking`, count + page chạy 1 round-trip nếu được, trả `400 VALIDATION_ERROR` khi tham số sai, **giữ endpoint cũ** (không `page` ⇒ vẫn hành xử cũ trong 1 release, đánh dấu `Obsolete`) để không phá FE/test hiện có.

## 3. Ma trận endpoint cần làm & filter

| # | Endpoint | Filter đề xuất | Sort |
| :-- | :--- | :--- | :--- |
| 1 | `GET orders/mine`, `orders/supplier` (chuyển `/paged` sang `page/pageSize`, giữ cursor làm tuỳ chọn) | `status[]`, `q` (mã đơn, tên người nhận), `from/to`, `hasPackaging`, `paymentStatus` (cọc/đủ), `needsAction` (theo `allowedActions`), `shopId` (customer) | createdAt, totalAmount, deadline gần nhất |
| 2 | `GET payments/mine` | `status`, `method`, `orderId`, `from/to`, `q` (mã GD) | createdAt, amount |
| 3 | `GET deliveries` (mine) | `status`, `orderId/contractId`, `scheduledFrom/To`, `q` | scheduledDate, createdAt |
| 4 | `GET contracts/mine` | `status`, `from/to`, `q` | createdAt, totalAmount |
| 5 | `GET rfqs/mine`, `rfqs/supplier` | `status`, `categoryId`, `from/to`, `q` | createdAt, deadline |
| 6 | `GET admin/orders/operations` → tách 4 endpoint: `delivery-disputes`, `refund-cases`, `payout-requests`, `summary` (số liệu Held/Disputed…) | `status`, `shopId`, `orderNumber`, `from/to`, `minAmount/maxAmount` | createdAt, amount |
| 7 | `GET admin/disputes` | `status`, `q`, `from/to` | createdAt |
| 8 | `GET users` (admin) | `role`, `isActive`, `q` (tên/email/phone) | createdAt, fullName |
| 9 | `GET admin/audit-logs` | `action`, `actorUserId`, `entityName`, `from/to`; trả `PagedResult` (thêm total) | createdAt |
| 10 | `GET supplier-promotions` | `isActive`, `productId`, `q` | startAt |
| 11 | `GET ai-analytics/reports` | `type`, `from/to` | createdAt |
| 12 | `reviews/shops/{slug}`, `products/{id}/reviews` | đồng bộ tên `page/pageSize`, thêm `totalCount` nếu thiếu | giữ nguyên |
| 13 | `orders/{id}/financial-summary` ledger entries (nếu dài) | `bucket`, `entryType` | createdAt |

Ưu tiên: **P1** (1,2,6,7) → **P2** (3,4,5,8,9) → **P3** (10,11,12,13).

## 4. Các Pha thực hiện

- **Pha A – Nền tảng BE:** `PagedQuery`, `PagedResult<T>`, helper `ApplyPaging/ApplySort` (IQueryable, whitelist), validator dùng chung, mapping lỗi chuẩn; unit test helper.
- **Pha B – Repository/Service/Controller (theo thứ tự ưu tiên P1→P3):** thêm query object + `IQueryable` filter ở repository (đẩy xuống SQL, tránh N+1; dùng lại batch-load hiện có của `DirectBulkOrderService`), index DB bổ sung nếu EXPLAIN cần (vd `(shop_id,status,created_at)`, `(customer_id,created_at)`, `(status,created_at)`) qua migration **mới, không tự chạy**.
- **Pha C – FE:** hook dùng chung `usePagedList({fetcher, initialQuery})` (debounce `q` 400ms, hủy request cũ bằng `AbortController`, reset page=1 khi đổi filter, đồng bộ query lên URL để back/refresh giữ trạng thái) + component `FilterBar` (Status/Date/Search). Chuyển `MyOrders`, `OrderManage`, `TradingOperating` (4 bảng), `DeliverManage`, `DeliveryPoD`, `ContractManage`, `SupplierDashboard`, trang Users/Audit sang **server-side pagination** (`pagination.total`, `onChange(page,size)`, `showSizeChanger [10,20,50,100]`).
- **Pha D – Kiểm thử & bằng chứng:** tạo tài khoản/seed test (`@test.mooncake.local`, ≥ 250 đơn/payments/disputes để kiểm tra nhiều trang) → chạy API test (page đầu/giữa/cuối, pageSize vượt max → 400, sort/filter kết hợp, trang rỗng, đổi dữ liệu giữa 2 request) + benchmark (p95 < 300ms ở 10k bản ghi) + **Playwright chụp màn hình** từng bảng (đổi trang, đổi pageSize, lọc status, search, reset) lưu `mooncake-fe/.agents/evidence/PAGING/` và `REPORT_PAGING.md`.
- **Pha E – Dọn dẹp:** bỏ endpoint không phân trang cũ sau khi FE chuyển xong, cập nhật Swagger + tài liệu `08_IMPLEMENTATION_RESULT.md`.

## 5. Rủi ro & lưu ý
- Đổi shape response (mảng → object) là **breaking** → giữ endpoint cũ/`?page` opt-in trong giai đoạn chuyển.
- `needsAction` & SLA-deadline filter phải tính ở SQL (không lọc sau khi tải) để `totalCount` đúng.
- Count trên bảng lớn tốn chi phí → có thể `totalCount` tuỳ chọn (`includeTotal=false`) cho audit-logs.
- Không đụng `appsettings.json`/`.env`; **không `git commit/push`** nếu bạn chưa yêu cầu.

## 6. Quyết định đã chốt
1. **Order lists:** chỉ `page/pageSize`, **bỏ hẳn cursor** (BE `/paged` đổi sang page/pageSize; FE không dùng cursor).
2. `pageSize` mặc định **10**, tối đa **100** (audit-logs 200). FE `pageSizeOptions: [10, 20, 50, 100]`.
3. **Xuất CSV:** ngoài phạm vi.
4. Phạm vi: **P1 → P3** (toàn bộ).

## 7. Test tự động + chụp màn hình (bắt buộc)
Script Playwright (Chrome thật) `mooncake-fe/scripts/run_paging_e2e.mjs`, ảnh lưu `mooncake-fe/.agents/evidence/PAGING/<màn hình>/`, log API JSON kèm theo, tổng hợp `REPORT_PAGING.md`.
- **Seed:** tạo tài khoản `@test.mooncake.local` + ≥ 250 bản ghi (orders, payments, deliveries, disputes, audit…) để có nhiều trang.
- **Mỗi bảng (≈ 12 màn hình) chụp các bước:** (1) trang 1 mặc định pageSize=10 → (2) sang trang 2 → (3) trang cuối → (4) đổi pageSize 20/50/100 → (5) lọc status → (6) tìm kiếm `q` → (7) kết hợp filter + sort → (8) kết quả rỗng (empty state) → (9) reset filter → (10) reload giữ nguyên query trên URL.
- **API test:** pageSize vượt max → 400; page vượt tổng → items rỗng; sort ngoài whitelist → 400; `totalCount` khớp DB; không trùng/sót bản ghi khi duyệt qua các trang; p95 < 300ms ở 10k bản ghi.

## 8. Test UI Responsive (dễ nhìn)
Chụp mỗi màn hình ở 4 viewport: **Mobile 375×812**, **Mobile lớn 430×932**, **Tablet 768×1024**, **Desktop 1440×900** (+ 1920×1080 cho bảng admin), ảnh lưu `evidence/PAGING/responsive/<màn hình>_<viewport>.png`.
Tiêu chí đạt (kiểm tra tự động + mắt thường trên ảnh):
- Không tràn ngang trang (`document.scrollWidth <= innerWidth`), bảng cuộn ngang trong container (`scroll={{x}}`), cột quan trọng (mã, trạng thái, hành động) cố định/ưu tiên.
- FilterBar xếp dọc/gập thành Drawer hoặc `Collapse` ở mobile; nút chạm ≥ 44px; ô search full-width.
- Thanh phân trang gọn: mobile dùng `simple` pagination + `showSizeChanger` dạng Select nhỏ; không bị che/cắt.
- Chữ ≥ 14px, tương phản đủ, trạng thái dùng Tag màu + chữ (không chỉ màu), loading skeleton và empty state rõ ràng.
- Ghép ảnh thành lưới so sánh 4 viewport trong `REPORT_PAGING.md` để xem nhanh.

## 9. Quy tắc làm việc (Full-access)
- Toàn quyền chỉnh sửa code, chạy lệnh, build, migration thử trên DB test, cài package, mở/đóng server, chạy trình duyệt tự động… **không hỏi xác nhận trong quá trình làm**; chỉ báo **kết quả cuối cùng**.
- **Ngoại lệ duy nhất: KHÔNG `git commit` / `git push`** (hoặc bất kỳ thao tác đẩy lên GitHub) khi bạn chưa yêu cầu rõ.
- Vẫn giữ: không sửa `.env` / `appsettings.json`; code + comment tiếng Anh, UI tiếng Việt.
