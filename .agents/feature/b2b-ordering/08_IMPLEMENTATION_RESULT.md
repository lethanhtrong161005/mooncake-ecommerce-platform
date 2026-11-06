# B2B Ordering — Direct Bulk Order Enterprise Implementation Result

## 1. Phạm vi đã hiện thực hoàn chỉnh (Enterprise Direct Bulk Order)

Dựa theo tài liệu phân tích kiến trúc doanh nghiệp `DIRECT_BULK_ORDER_ENTERPRISE_ANALYSIS.md`, hệ thống đã hoàn thiện toàn diện các thành phần:

1. **Khắc phục triệt để lỗ hổng P0:**
   - Xóa bỏ hoàn toàn truy vấn fallback 100 đơn toàn sàn trong `OrderRepository.GetSupplierOrdersAsync`. Đảm bảo cô lập tenant tuyệt đối.
   - Kiểm tra chặt chẽ `IsShopOwnedByUserAsync` ngăn chặn Supplier này can thiệp đơn hàng của Supplier khác (`404 TenantForbidden`).
   - Tạo hồ sơ bao bì `CustomPackaging` trong cùng transaction với `Order` (không nuốt lỗi).
   - Loại bỏ VietQR khỏi đơn hàng trực tiếp; loại bỏ tự động chuyển trạng thái `Processing` tại callback VNPay phía client.

2. **Chính sách SLA & Quy chuẩn nền tảng (Pha 1):**
   - Supplier phản hồi đơn: 12 giờ (`ConfirmationDeadline`).
   - Customer phản hồi đề xuất ngày giao: 12 giờ (`DeliveryProposalDeadline`).
   - Customer thanh toán cọc (Deposit): 24 giờ (`DepositDeadline`).
   - Customer xem xét PoD và xác nhận nhận hàng: 48 giờ (`PodReviewDeadline`).
   - Giữ kho chờ mockup bao bì tùy biến: 5 ngày làm việc (`ReservationExpiresAt`).
   - Giới hạn trần thanh toán VNPay mỗi đợt: 100.000.000 VND.
   - Chuẩn hóa mã lỗi RFC 7807: `PRICE_CHANGED`, `INSUFFICIENT_STOCK`, `SHOP_UNAVAILABLE`, `ORDER_EXPIRED`, `STATE_CONFLICT`, `PAYMENT_REQUIRED`, `PACKAGING_APPROVAL_REQUIRED` kèm `traceId`.

3. **Lõi giao dịch Backend (Pha 2):**
   - Khóa bi quan PostgreSQL (`SELECT ... FOR UPDATE`), sắp xếp variant theo ID tăng dần chống Deadlock khi trừ tồn kho.
   - Bảng `inventory_reservations` và sổ cái giữ kho chống hoàn/giữ kho lặp.
   - Bảng `order_payment_schedule` & `payment_parts` chia nhỏ lượt thanh toán với trần 100M VND/part, gắn `part_id` trên `payments`.
   - Sổ cái tài chính mô phỏng (`Held`, `Eligible`, `PaidOutSimulated`, `Disputed`, `Refunded`) qua bảng `order_financial_ledger_entries`.
   - Quản lý tranh chấp giao nhận (`order_delivery_disputes`), yêu cầu hoàn tiền (`order_refund_cases`), yêu cầu giải ngân (`supplier_payout_requests`), và cấu hình tài khoản thụ hưởng (`supplier_payout_accounts`).

4. **API & Giao nhận PoD (Pha 3):**
   - Quy định v1 chỉ 1 chuyến giao duy nhất trên đơn (từ chối chuyến giao thứ 2 với mã `409 SINGLE_DELIVERY_LIMIT`).
   - Luồng nộp ảnh Proof-of-Delivery (`POST /api/v1/deliveries/{id}/proofs`) đưa đơn vào trạng thái `AwaitingCustomerConfirmation`.
   - Khách xác nhận nhận hàng (`POST /api/v1/orders/{id}/delivery/confirm-receipt`) mở khóa tiền lưu ký sang `Eligible`.
   - Khách báo cáo tranh chấp (`POST /api/v1/orders/{id}/delivery/dispute`) khóa tiền vào `Disputed`.
   - Quản trị viên xử lý tranh chấp (`POST /api/v1/admin/orders/delivery-disputes/{id}/resolve`) theo 3 hướng: `ReleasePayout`, `PartialRefund`, `FullRefund`.

5. **Giao diện người dùng Frontend (Pha 4):**
   - `OrderAndDesgin.jsx`: Đặt hàng đa dòng variant cùng xưởng, debounce preview báo giá, Modal cam kết điều khoản pháp lý.
   - `MyOrders.jsx`: SLA đếm ngược, Drawer thanh toán VNPay theo đợt trần 100M, Modal xem ảnh PoD xác nhận đơn/khiếu nại, Modal duyệt mockup bao bì & phí phụ thu.
   - `OrderManage.jsx`: Các thẻ số dư tài chính (`Held`, `Eligible`, `PaidOutSimulated`, `Disputed`, `Refunded`), Drawer chính sách cọc & phí giao của Shop, Drawer số tài khoản ngân hàng thụ hưởng, Modal yêu cầu giải ngân số dư Eligible.
   - `TradingOperating.jsx`: Tab Đơn Hàng Doanh Nghiệp (Direct Bulk Orders) cho Admin: theo dõi ký quỹ sàn, duyệt khiếu nại giao nhận, xử lý hoàn tiền, duyệt giải ngân Sandbox.

6. **Kiểm thử E2E Browser tự động (Pha 5):**
   - Chạy 10/10 kịch bản E1–E10 bằng Playwright trên trình duyệt Google Chrome thực tế trên Windows.
   - Toàn bộ 27 ảnh chụp màn hình và nhật ký API JSON được lưu tại `mooncake-fe/.agents/evidence/E2E/` và tổng hợp tại `mooncake-fe/.agents/evidence/REPORT.md`.

---

## 2. Danh mục Endpoints Hoàn Chỉnh

| Method | Endpoint | Quyền hạn | Mô tả |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/v1/orders/preview` | Customer / Anonymous | Báo giá thời gian thực có hiệu lực 15 phút, tính thuế/phí/cọc |
| `POST` | `/api/v1/orders` | Customer | Đặt hàng doanh nghiệp (Idempotent Key, giữ tồn kho) |
| `GET` | `/api/v1/orders/{id}/payment-summary` | Customer/Supplier/Admin | Chi tiết lịch thanh toán, các đợt 100M và điều kiện mở xưởng |
| `POST` | `/api/v1/orders/{id}/payment-parts/{partId}/vnpay-url` | Customer | Tạo URL cổng thanh toán VNPay cho từng đợt |
| `POST` | `/api/v1/orders/{id}/propose-delivery-date` | Supplier | Đề xuất ngày giao hàng mới khi cần thêm thời gian |
| `POST` | `/api/v1/orders/{id}/respond-delivery-proposal` | Customer | Khách chấp thuận hoặc từ chối ngày giao đề xuất |
| `POST` | `/api/v1/orders/{id}/cancellation-request` | Customer | Khách gửi yêu cầu hủy thỏa thuận |
| `POST` | `/api/v1/orders/{id}/respond-cancellation` | Supplier | NCC chấp thuận hủy đơn và giải phóng kho |
| `POST` | `/api/v1/orders/{id}/delivery/confirm-receipt` | Customer | Khách duyệt ảnh PoD, nhận hàng, mở khóa tiền lưu ký sang Eligible |
| `POST` | `/api/v1/orders/{id}/delivery/dispute` | Customer | Khách khiếu nại chất lượng/số lượng khi giao nhận |
| `POST` | `/api/v1/orders/{id}/refund-requests` | Customer | Khách yêu cầu hoàn tiền cho đơn hủy/hết hạn |
| `POST` | `/api/v1/orders/{id}/payout-requests` | Supplier | NCC gửi yêu cầu giải ngân số dư Eligible |
| `GET` | `/api/v1/suppliers/me/order-policy` | Supplier | Lấy chính sách cọc và phí vận chuyển theo khu vực |
| `PUT` | `/api/v1/suppliers/me/order-policy` | Supplier | Cập nhật chính sách cọc và phí vận chuyển |
| `GET` | `/api/v1/suppliers/me/payout-account` | Supplier | Lấy thông tin tài khoản ngân hàng thụ hưởng |
| `PUT` | `/api/v1/suppliers/me/payout-account` | Supplier | Cập nhật thông tin tài khoản ngân hàng thụ hưởng |
| `GET` | `/api/v1/suppliers/me/financial-overview` | Supplier | Lấy thống kê số dư Held, Eligible, PaidOutSimulated, Disputed |
| `GET` | `/api/v1/admin/orders/operations` | Admin | Bảng điều hành giao dịch doanh nghiệp toàn nền tảng |
| `POST` | `/api/v1/admin/orders/delivery-disputes/{id}/resolve` | Admin | Phân xử khiếu nại giao nhận (ReleasePayout, PartialRefund, FullRefund) |
| `POST` | `/api/v1/admin/orders/refund-cases/{id}/process` | Admin | Phê duyệt hoàn tiền lưu ký cho khách hàng |
| `POST` | `/api/v1/admin/orders/payout-requests/{id}/process` | Admin | Phê duyệt giải ngân mô phỏng Sandbox cho NCC |

---

## 3. Quản Trị Vận Hành & Bảo Mật

- **Mô phỏng Sandbox:** Quy trình giải ngân và hoàn tiền được mô phỏng trong Database an toàn (`PaidOutSimulated`, `Refunded`). Tính năng chuyển khoản liên ngân hàng thực tế được giữ ở chế độ vô hiệu hóa sau Feature Flag `PayoutLiveTransfersEnabled = false`.
- **Cam kết kiểm soát phiên bản:** Tuyệt đối không tự ý chạy `git commit` hay `git push` ngoài phạm vi yêu cầu của người dùng.
