# [M10-F25.8] KẾ HOẠCH TRIỂN KHAI, KIỂM THỬ & NGHIỆM THU: THANH TOÁN & ESCROW

---

## I. LỘ TRÌNH TRIỂN KHAI THEO GIAI ĐOẠN (IMPLEMENTATION PHASES)

### Giai đoạn 1: Backend Services, Gateway & Repositories (C# / .NET 8)
1. **Kiểm tra & Bổ sung Entities/Enums:**
   - Xác thực cấu hình `PaymentConfiguration.cs` và ánh xạ Enum `PaymentMethod`, `PaymentStatus` trong PostgreSQL.
   - Bổ sung trường `EvidenceUrl` vào `Payment.cs` để lưu trữ ảnh/file scan UNC.
2. **Cập nhật DTOs & FluentValidation:**
   - Thêm `CreateOrderVnPayUrlRequest.cs`, `CreateContractVnPayUrlRequest.cs`, `CreateBankTransferRequest.cs`, `UploadPaymentEvidenceRequest.cs`.
   - Cài đặt FluentValidation rules cho từng DTO tại tầng Application.
3. **Mở rộng `IPaymentRepository` & `PaymentRepository`:**
   - Bổ sung phương thức `PrepareOrderVnPayPaymentAsync(customerId, orderId)`.
   - Bổ sung phương thức `CreateBankTransferPaymentAsync(...)`.
   - Bổ sung phương thức `AttachEvidenceAsync(paymentId, evidenceUrl, notes)`.
   - Bổ sung phương thức `GetEscrowSummaryAsync()`.
4. **Mở rộng `IPaymentService` & `PaymentService`:**
   - Cập nhật logic `CreateOrderVnPayUrlAsync` (tương tự như hợp đồng).
   - Kiểm tra điều kiện COD: `payments.cod_limit_vnd` và cấm COD cho đơn hàng có `CustomPackaging`.
   - Hoàn thiện xử lý IPN webhook `ProcessVnPayIpnAsync` cho cả Đơn hàng và Hợp đồng.
5. **Cập nhật WebApi `PaymentsController`:**
   - Expose các endpoint: `POST /api/v1/payments/orders/vnpay-url`, `POST /api/v1/payments/bank-transfer`, `GET /api/v1/payments/vnpay/return-callback`, `GET /api/v1/payments/escrow/summary`.

---

### Giai đoạn 2: Frontend Components & Integration (`mooncake-fe`)
1. **API Endpoints & Services:**
   - Cập nhật `src/constants/apiEndpoints.js` với các route `PAYMENTS`.
   - Cập nhật `src/services/paymentService.js` với Mock Fallback Pattern hoàn chỉnh.
2. **Giao diện Modal & Drawer:**
   - Xây dựng `src/components/payment/PaymentCheckoutModal.jsx` (Responsive Bottom Sheet trên mobile, Modal trên desktop).
   - Xây dựng `src/components/payment/VietQrModal.jsx` (Ảnh Napas VietQR, nút sao chép 1-chạm STK/Số tiền/Cú pháp, bộ đếm ngược 48h, nút tải lên UNC).
   - Xây dựng `src/components/payment/PaymentResultView.jsx` (Trang biên lai điện tử sang trọng, hỗ trợ tải/in PDF và quay về Đơn hàng/Hợp đồng).
3. **Tích hợp các trang nghiệp vụ:**
   - Trang Đơn hàng [`OrderAndDesgin.jsx`](file:///Users/admin/Desktop/PRN232/mooncake-fe/src/Pages/Customer/OrderAndDesgin.jsx): Thêm nút "Thanh toán ngay" cho đơn hàng đã xác nhận.
   - Trang Hợp đồng [`ContractsAndMilestone.jsx`](file:///Users/admin/Desktop/PRN232/mooncake-fe/src/Pages/Customer/ContractsAndMilestone.jsx): Tích hợp mở `PaymentCheckoutModal` khi thanh toán mốc M1/M2/M3.
   - Trang Quản trị [`DashBoard.jsx`](file:///Users/admin/Desktop/PRN232/mooncake-fe/src/Pages/Admin/DashBoard.jsx): Tích hợp hiển thị Sổ Ký quỹ Escrow và nút duyệt UNC/Hoàn tiền cho Admin.

---

### Giai đoạn 3: Kiểm thử toàn diện & Đảm bảo tính toàn vẹn (Testing & QA)
1. **Kiểm thử Backend Unit & Verification Tests:**
   - Chạy `dotnet test --no-restore` đạt 100% Passed.
2. **Kiểm thử Frontend Build & Lint:**
   - Chạy `npm run build` trong `mooncake-fe` (0 lỗi, thời gian build < 1s).
   - Chạy `npx eslint` (0 errors, 0 warnings).
3. **Kiểm thử End-to-End Playwright:**
   - Xây dựng và chạy bộ test suite `tests/order-contract-payment.spec.js`.
   - Chạy full regression test suite 24+ test cases qua tất cả các module.

---

## II. MA TRẬN KỊCH BẢN KIỂM THỬ (TEST SPECIFICATIONS)

### 2.1. Backend Tests (xUnit & WebApplicationFactory)

| Test File | Kịch Bản Kiểm Thử | Kết Quả Mong Đợi |
| :--- | :--- | :--- |
| `OrderPaymentVnPayTests.cs` | 1. Khách hàng tạo URL VNPay cho đơn hàng sỉ hợp lệ | Trả về HTTP 200 OK, URL chứa `vnp_TmnCode=BFYI47AF` và chữ ký HMAC-SHA512 |
| `OrderPaymentVnPayTests.cs` | 2. Đơn hàng vượt hạn mức 10.000.000 ₫ chọn thanh toán COD | Trả về HTTP 422 Unprocessable Entity kèm thông điệp vượt hạn mức |
| `OrderPaymentVnPayTests.cs` | 3. Đơn hàng yêu cầu in ấn logo riêng (`CustomPackaging`) chọn COD | Trả về HTTP 422 Unprocessable Entity, bắt buộc thanh toán online |
| `ContractMilestoneEscrowTests.cs` | 4. Khách hàng thanh toán đặt cọc M1 qua VNPay Sandbox | Trả về URL thanh toán, Payment ở trạng thái `Pending` |
| `ContractMilestoneEscrowTests.cs` | 5. Nhận IPN thành công cho mốc M1 | Payment `Completed`, Milestone M1 `Paid`, Contract chuyển `Active` |
| `ContractMilestoneEscrowTests.cs` | 6. Thanh toán mốc M1 đã nạp cọc trước đó | Trả về HTTP 409 Conflict: "Mốc hợp đồng này đã được thanh toán" |
| `PaymentsHttpTests.cs` | 7. Nhận IPN với sai chữ ký Checksum (`vnp_SecureHash`) | Trả về `{"RspCode": "97", "Message": "Invalid Checksum"}` |
| `PaymentsHttpTests.cs` | 8. Admin hoàn tiền giao dịch hợp đồng bị hủy | Payment `Refunded`, ghi nhận `audit_logs` đầy đủ |

### 2.2. Frontend Playwright E2E Tests (`tests/order-contract-payment.spec.js`)

```javascript
test.describe('Module 10: Order & Contract Payment (Feature #25)', () => {
  test('1. Customer checks out Bulk Order with VNPay Sandbox and verifies payment redirect and return receipt', async ({ page }) => {
    // 1. Khách hàng đăng nhập, mở chi tiết đơn hàng đã xác nhận
    // 2. Bấm "Thanh toán ngay", chọn phương thức "Cổng VNPay Sandbox"
    // 3. Kiểm tra liên kết thanh toán VNPay Sandbox được sinh ra
    // 4. Giả lập callback thành công về trang kết quả, hiển thị Biên lai E-Receipt
  });

  test('2. Customer initiates VietQR Bank Transfer for contract milestone M1 deposit, copies transfer memo, and uploads transfer slip', async ({ page }) => {
    // 1. Khách hàng mở trang hợp đồng chờ cọc (PendingDeposit)
    // 2. Chọn "Chuyển khoản Ngân hàng (VietQR)"
    // 3. Kiểm tra mã VietQR hiển thị kèm STK MB Bank và cú pháp PAY {PaymentNumber}
    // 4. Bấm nút sao chép STK & Cú pháp, nhận phản hồi copy thành công
    // 5. Tải lên ảnh Ủy nhiệm chi (UNC) và gửi xác nhận
  });

  test('3. VNPay IPN webhook processing with HMAC-SHA512 verification activates Contract from PendingDeposit to Active', async ({ page }) => {
    // 1. Giả lập Webhook IPN từ máy chủ VNPay Sandbox
    // 2. Backend kiểm tra chữ ký HMAC-SHA512 hợp lệ
    // 3. Hợp đồng chuyển trạng thái sang Active, mốc M1 chuyển sang Paid
    // 4. Tiền được ghi nhận an toàn trong tài khoản Ký quỹ Escrow
  });

  test('4. Admin views Escrow Ledger summary, verifies held balances, and processes refund for a cancelled transaction', async ({ page }) => {
    // 1. Quản trị viên đăng nhập vào Admin Dashboard
    // 2. Chuyển sang tab "Sổ Giám Sát Ký Quỹ Escrow & Milestone"
    // 3. Kiểm tra tổng số dư ký quỹ Escrow và danh sách hợp đồng được bảo chứng
    // 4. Mở modal hoàn tiền cho giao dịch hợp đồng vi phạm, nhập lý do và bấm "Xác nhận hoàn tiền"
    // 5. Trạng thái giao dịch cập nhật thành Refunded kèm log đối soát
  });
});
```

---

## III. CHECKLIST NGHIỆM THU KHÔNG LỖI (ZERO ERROR ACCEPTANCE CRITERIA)

- [ ] **Bảo mật API Key:** Chỉ sử dụng thông tin Sandbox từ `keyvnpay.text`. Tuyệt đối không hardcode bí mật trên frontend.
- [ ] **Môi trường:** Nghiêm cấm môi trường Production của VNPay; 100% giao dịch thực hiện trên Sandbox (`https://sandbox.vnpayment.vn`).
- [ ] **Mobile-First Responsive:** Màn hình thanh toán hoạt động mượt mà trên cả điện thoại di động (iPhone / Android viewport 375px) và desktop.
- [ ] **Quy chuẩn mã nguồn Frontend:** Tuân thủ chuẩn 2-space indentation, không sử dụng ký tự tab (`\t`), ES2024 modern syntax.
- [ ] **Quy chuẩn mã nguồn Backend:** 100% định danh và comment bằng tiếng Anh chuẩn Clean Architecture.
- [ ] **Zero Error Build:** `npm run build` và `dotnet test` đạt kết quả 100% Passed mà không phát sinh bất kỳ warning/lỗi nào.
- [ ] **Tuân thủ Git:** Tuyệt đối không thực hiện commit git.

