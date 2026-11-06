# Kết quả triển khai pagination và filter

Ngày kiểm tra: 05/11/2026. Trạng thái: đã triển khai code BE/FE cho các nhóm P1–P3; chưa nghiệm thu hiệu năng trên dữ liệu PostgreSQL 10.000 bản ghi.

## Backend

- Thêm `PagedQuery`, `PagedResult<T>` và validator chung: `page` 1–1000, `pageSize` 1–100 (audit tối đa 200), `q` tối đa 100 ký tự, ngày hợp lệ, sort theo allowlist. Response trả `items`, `page`, `pageSize`, `totalCount`, `totalPages`, `hasNext`, `hasPrevious`.
- Các danh sách order Customer/Supplier, payments, deliveries, contracts, RFQ, users, audit logs, promotions và AI reports nhận `page/pageSize`; filter và count chạy trong truy vấn database, sắp xếp có `Id` làm tie breaker. Các endpoint cũ không có `page` vẫn giữ response cũ trong giai đoạn chuyển đổi. `/orders/mine/paged` và `/orders/supplier/paged` dùng `page/pageSize`.
- Admin operations tách summary, refund cases, payout requests và delivery disputes thành endpoint riêng. Contract disputes cũng hỗ trợ page. Ledger tài chính order có endpoint phân trang. Review shop/product nhận `page` và trả metadata tổng.
- Các truy vấn order và RFQ dùng batch enrichment theo page để tránh N+1; filter/search được thực hiện trước `Skip/Take` và chỉ dùng LINQ/EF parameterized query. Supplier order query giới hạn theo shop sở hữu.
- Chưa thêm migration/index vì chưa có `EXPLAIN (ANALYZE, BUFFERS)` trên dữ liệu staging gần thực tế. Không chạy migration trên database dùng chung.

## Frontend

- Thêm `usePagedList` (debounce tìm kiếm 400 ms, abort request cũ, đồng bộ hash URL, reset trang khi đổi filter), `FilterBar`, và `useIsMobile`.
- Các màn Customer orders/deliveries/RFQ; Supplier orders, dashboard, deliveries, contracts, RFQ; Admin users/audit và các bảng vận hành dùng API phân trang. Các bảng dùng `pagination.total` và page size 10/20/50/100.
- Bảng cuộn ngang trong vùng bảng ở mobile; bộ lọc xếp gọn và pagination dạng simple cho màn nhỏ tại các màn được kiểm tra.

## Kiểm thử đã chạy

| Kiểm tra | Kết quả |
|---|---|
| `dotnet build` WebApi | Qua, 0 lỗi |
| `dotnet test` trừ `DirectBulkOrderP2DbIntegrationTests` | 437 qua, 24 skip, 0 lỗi |
| Pagination contract tests | 6 qua |
| `npm run build` | Qua |
| ESLint riêng hook/component phân trang mới | Qua |
| Playwright `node scripts/run_paging_e2e.mjs` | 9/9 qua, 12 màn/bảng, dữ liệu mock 250 bản ghi, ảnh/log tại `mooncake-fe/.agents/evidence/PAGING/` |

`npm run lint` toàn FE vẫn lỗi ở nhiều file cũ/đang thay đổi ngoài hook/component phân trang; lượt chạy này ghi nhận 125 errors và 5 warnings. Không xem lint toàn repo là gate đã qua.

Chạy toàn bộ BE suite còn một lỗi ở `DirectBulkOrderP2DbIntegrationTests`: case IPN đến muộn sau hết hạn kỳ vọng `Expired` nhưng thực tế order vẫn `Confirmed` tại dòng 591. Đây là luồng hết hạn order hiện có, không nằm trong logic phân trang; cần xử lý riêng trước khi coi toàn bộ suite sạch.

**Lưu ý dữ liệu test:** `DirectBulkOrderP2DbIntegrationTests` hiện đọc connection string mặc định từ `appsettings.json` và gọi seeder `@test.mooncake.local`. Lượt chạy full suite và lượt chạy riêng test này đã kết nối và ghi dữ liệu test vào database đang cấu hình, không phải database cô lập do test tự tạo. Không chạy thêm test DB và không tự xóa dữ liệu đã seed; cần rà soát dữ liệu test trước khi sử dụng database này cho vận hành. Việc này không phải là benchmark phân trang.

## Giới hạn nghiệm thu

- Chưa có seed 250 bản ghi trên PostgreSQL staging **cô lập** và chưa đo p95 ở 10.000 bản ghi. Bộ Playwright dùng mock 250 bản ghi, không chứng minh SQL production có cùng kết quả/hiệu năng. Trước khi chạy test DB phân trang, cần cấp database test tách biệt và sửa fixture không lấy connection string mặc định.
- Playwright đã chụp 12 màn/bảng ở 4 viewport, nhưng mới kiểm tra toàn bộ các bước pageSize/status/search/empty/reset/reload trên ba danh sách chính. Các bảng còn lại kiểm tra trang 1, trang 2 và responsive; chưa kiểm tra mọi tổ hợp filter/sort. Xem báo cáo FE để biết phạm vi bằng chứng.
- `needsAction` trong query order đang là biểu thức SQL theo các trạng thái chính, chưa tương đương hoàn toàn mọi `allowedActions` tính từ payment, packaging và delivery. Không nên dùng filter này làm nguồn quyết định quyền thao tác; quyền vẫn được tính trong chi tiết order.
- `paymentStatus` của order hiện lọc theo trạng thái payment record; chưa ánh xạ đầy đủ cọc/đủ tiền theo nghĩa vụ thanh toán. Cần hoàn thiện trước khi hiển thị bộ lọc này như “Đã cọc/Đã thanh toán đủ”.
- Chưa dọn endpoint legacy; giữ tương thích một release theo kế hoạch. Chưa commit/push, chưa sửa `.env` hoặc `appsettings.json`.
