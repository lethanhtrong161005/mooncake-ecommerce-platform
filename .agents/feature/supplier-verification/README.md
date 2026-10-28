# Module 1 — Feature #2: Xác minh & duyệt nhà cung cấp

- Feature key: `supplier-verification`.
- Ngày thiết kế: **2026-10-24**.
- Trạng thái: **Đã triển khai BE/FE, kiểm thử và áp dụng migration vào database dự án ngày 2026-10-24**.
- Phạm vi: gửi hồ sơ, xét duyệt, từ chối, gửi lại, thu hồi, tài liệu riêng tư,
  quyền truy cập, badge Verified, email kết quả, real-time/in-app notification và audit
  xuyên suốt BE/FE.

## Tài liệu và thứ tự đọc

**Chính sách upload mới nhất (2026-10-28):** [11_SIZE_ONLY_DOCUMENT_UPLOAD.md](./11_SIZE_ONLY_DOCUMENT_UPLOAD.md) — nhận mọi loại file, chỉ kiểm tra dung lượng; thay thế các quy định kiểm tra định dạng/nội dung trước đó.

Kết quả thực tế, cấu hình vận hành và các bước còn cần nghiệm thu:
[07_IMPLEMENTATION_REPORT.md](./07_IMPLEMENTATION_REPORT.md).

Quy tắc UI mới nhất: [08_CUSTOMER_TO_SUPPLIER_FLOW.md](./08_CUSTOMER_TO_SUPPLIER_FLOW.md).

Sửa lỗi upload PDF ngày 2026-10-28: [10_PDF_UPLOAD_FIX.md](./10_PDF_UPLOAD_FIX.md).
Chỉ Customer đăng ký trở thành Supplier; giữ layout Customer tới khi hồ sơ được Admin duyệt.

Vận hành upload và scanner: [09_DOCUMENT_UPLOAD_OPERATIONS.md](./09_DOCUMENT_UPLOAD_OPERATIONS.md).

| File | Nội dung |
| --- | --- |
| [01_DETAIL_DESIGN.md](./01_DETAIL_DESIGN.md) | Thiết kế theo đầy đủ sáu phần I–VI của template; nghiệp vụ và flow tổng thể |
| [02_API_CONTRACTS.md](./02_API_CONTRACTS.md) | Endpoint, request/response, quyền, lỗi, version và idempotency |
| [03_DATA_CONCURRENCY_SECURITY.md](./03_DATA_CONCURRENCY_SECURITY.md) | Schema, transaction, N+1, race condition, SQL injection và bảo vệ file |
| [04_FRONTEND_INTEGRATION.md](./04_FRONTEND_INTEGRATION.md) | Tích hợp mooncake-fe, Ant Design, route, trạng thái UI và đồng bộ phiên |
| [05_DELIVERY_TEST_PLAN.md](./05_DELIVERY_TEST_PLAN.md) | Backlog có thứ tự, migration/rollout, test matrix và tiêu chí nghiệm thu |
| [06_REALTIME_NOTIFICATIONS.md](./06_REALTIME_NOTIFICATIONS.md) | SignalR, thông báo bền vững, unread, reconnect/catch-up và scale-out |

## Căn cứ thiết kế

- [Business: Module 1, tính năng #2](../../business/mooncake-marketplace.md).
- [Template thiết kế chi tiết](../template/FEATURE_DETAIL_DESIGN_TEMPLATE.md).
- [Quy tắc BE](../../AGENTS.md),
  [quy tắc FE](../../../../mooncake-fe/.agents/AGENTS.md).
- [Storage-file hiện tại](../storage-file/STORAGE_FILE_PLAN.md): tái sử dụng
  Cloudinary upload service; việc tích hợp tài liệu hồ sơ thuộc feature này.
- [Đăng ký tài khoản](../register-account/REGISTER_ACCOUNT_OVERVIEW.md): chỉ tham khảo
  ranh giới onboarding; contract thực tế phải đối chiếu source hiện tại.

## Quyết định chính để review

1. Giữ `Customer`, `Supplier`, `Admin`; không tạo role `PendingSupplier`.
   Phân biệt `requestedRole` với role đã được cấp và trạng thái xác minh.
2. Hồ sơ gửi đi có revision bất biến; Admin chỉ duyệt revision đang chờ.
3. Mọi quyết định có expected version, transaction, idempotency và audit.
4. DB hiện tại quyết định quyền; JWT cũ không được giữ quyền bán hàng sau thu hồi.
5. Dùng file private có ownership; client gửi `documentId`, không gửi URL tùy ý.
6. Email qua transactional outbox; SMTP lỗi không làm mất quyết định đã commit.
7. FE dùng Ant Design và `ColonyAdminLayout` hiện có; bỏ thông báo thành công giả
   trong phạm vi xác minh Supplier.
8. SignalR chỉ phát sự kiện tối thiểu sau commit; thông báo lưu DB, gửi đúng người
   nhận, dedup và bắt kịp trạng thái qua REST khi mất kết nối.

## Phân biệt yêu cầu và đề xuất

Business bắt buộc duyệt/từ chối, lý do từ chối, thu hồi, badge, email và audit.
Yêu cầu bổ sung của người dùng đưa real-time thông báo vào phạm vi feature này.
Các ngưỡng dung lượng, rate limit, SLA, loại tài liệu bắt buộc và thời gian lưu hồ sơ
trong bộ plan là **đề xuất triển khai**, không phải quy định pháp lý hoặc chức năng
đã có. Owner nghiệp vụ cần chốt các mục được đánh dấu trong phần 1.5 của detail design
trước khi triển khai production.
