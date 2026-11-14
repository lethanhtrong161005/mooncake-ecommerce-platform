# Hướng dẫn test thủ công (Step-by-Step) luồng Hoàn tiền B2B (Refund)

Tài liệu này giúp bạn tự kiểm thử toàn bộ các nhánh hoàn tiền B2B giữa **Customer**, **Supplier** và **Admin** trên hệ thống **Nguyệt Phố**.

---

## 1. Tài khoản kiểm thử & Môi trường

### 1.1. Tài khoản có sẵn trong database

> Trên giao diện đăng nhập (`LoginForm`), hệ thống dùng **Số điện thoại** + **Mật khẩu**. Các tài khoản test chuẩn dưới đây đã tồn tại trong `public.users` với mật khẩu mặc định trong seeder là `Password123!`.

| Vai trò | Số điện thoại (đăng nhập UI) | Email | Tên hiển thị / Gian hàng | Mật khẩu |
|---|---|---|---|---|
| **Customer 1 (Chính)** | `0988000101` | `cust.a@test.mooncake.local` | `TEST Customer Alpha Corp` | `Password123!` |
| **Customer 2 (Dự phòng)** | `0988000102` | `cust.b@test.mooncake.local` | `TEST Customer Beta Group` | `Password123!` |
| **Supplier 1 (Chính)** | `0988000103` | `sup.a@test.mooncake.local` | `TEST Supplier Workshop Alpha` — Shop **`TEST-NguyetPho-ShopA`** (`Active`) | `Password123!` |
| **Supplier 2 (Dự phòng)** | `0988000104` | `sup.b@test.mooncake.local` | `TEST Supplier Workshop Beta` — Shop **`TEST-NguyetPho-ShopB`** (`Active`) | `Password123!` |
| **Admin** | `0988000106` | `admin@test.mooncake.local` | `TEST Platform Administrator` | `Password123!` |

> Mẹo: Mở **3 cửa sổ trình duyệt riêng** (hoặc Chrome thường + Chrome Ẩn danh + Firefox/Profile thứ 3) để đăng nhập sẵn 3 vai trò (`Customer`, `Supplier`, `Admin`), thao tác qua lại không cần đăng xuất.

### 1.2. Thẻ test cổng VNPay Sandbox

- **Ngân hàng:** `NCB`
- **Số thẻ:** `9704198526191432198`
- **Tên chủ thẻ:** `NGUYEN VAN A`
- **Ngày phát hành:** `07/15`
- **Mã OTP xác thực:** `123456`

### 1.3. Đường dẫn mặc định

- **Frontend:** `http://localhost:5173`
- **Backend API:** `http://localhost:8089` (Swagger nếu bật: `http://localhost:8089/swagger`)

---

## 2. Kịch bản A (Full UI): Hoàn tiền Direct Bulk Order từ Khiếu nại Giao hàng (PoD Dispute)

Đây là kịch bản **chạy trọn vẹn 100% trên giao diện UI** cho đơn hàng doanh nghiệp đặt trực tiếp.

### Bước 1 — Customer đặt đơn hàng trực tiếp (`Pending`)
1. Đăng nhập tài khoản **Customer** (`0988000101` / `Password123!`).
2. Vào trang sản phẩm thuộc shop **`TEST-NguyetPho-ShopA`** (ví dụ truy cập trực tiếp `/san-pham/d5db5bf9-e06e-41a7-ba8f-1ae3da5b72a1` hoặc `/san-pham/229cb978-6eab-44fc-9639-c3c85c8b11f8`).
3. Chọn số lượng đạt mức sỉ, bấm nút đặt hàng doanh nghiệp để điền thông tin nhận hàng (tên người nhận, SĐT, địa chỉ, ngày giao mong muốn) -> xác nhận tạo đơn.
4. Vào **`/don-hang-cua-toi`** -> ghi nhớ **Mã đơn hàng (`#MC-...`)**, trạng thái lúc này là **`Chờ xác nhận` (`Pending`)**.

### Bước 2 — Supplier xác nhận đơn (`Pending` -> `Confirmed`)
1. Chuyển sang cửa sổ **Supplier** (`0988000103` / `Password123!`).
2. Vào **`/nha-cung-cap/quan-ly-don-hang`**.
3. Tìm đơn vừa tạo -> bấm **`Xác nhận đơn`**.
   - Kết quả: Hệ thống sinh lịch thanh toán VNPay cho đơn hàng, trạng thái chuyển sang **`Đã xác nhận` (`Confirmed`)**.

### Bước 3 — Customer thanh toán VNPay (`Held` trong sổ cái sàn)
1. Quay lại cửa sổ **Customer** tại **`/don-hang-cua-toi`**, tải lại trang.
2. Tại đơn vừa xác nhận, bấm **`Lịch trình thanh toán VNPay`**.
3. Ở từng lượt thanh toán (`Lượt 1/1` hoặc các lượt <= 100 triệu/lượt), bấm **`Thanh toán VNPay ngay`**.
4. Nhập thông tin thẻ test NCB ở Mục 1.2 trên cổng VNPay Sandbox -> nhập OTP `123456` -> hoàn tất thanh toán.
5. Khi quay về trang kết quả thanh toán, kiểm tra lại **`/don-hang-cua-toi`**:
   - Nếu đơn không có mockup bao bì chờ duyệt và đã thanh toán đủ lượt đầu/cọc: tiền đã được ghi nhận vào trạng thái **Lưu ký (`Held`)**.

### Bước 4 — Supplier sản xuất, xuất kho & nộp ảnh PoD (`Confirmed` -> `Processing` -> `Shipped`)
1. Quay lại cửa sổ **Supplier** tại **`/nha-cung-cap/quan-ly-don-hang`**, tải lại trang.
2. Bấm **`Bắt đầu sản xuất`** -> đơn sang **`Đang sản xuất` (`Processing`)**.
3. Sau khi thanh toán đủ 100% giá trị đơn, bấm **`Xuất kho giao`** -> đơn sang **`Đã xuất kho` (`Shipped`)**.
4. Bấm **`Tải ảnh PoD`** -> nhập/giữ nguyên URL ảnh mẫu, tên người ký nhận, ghi chú -> xác nhận gửi biên bản PoD.

### Bước 5 — Customer khiếu nại hàng giao lỗi để khóa ký quỹ
1. Quay lại cửa sổ **Customer** tại **`/don-hang-cua-toi`**, tải lại trang.
2. Tại đơn **`Đã xuất kho` (`Shipped`)**, bấm nút **`Kiểm tra ảnh PoD & Xác nhận nhận hàng`**.
3. Ở chân modal kiểm tra ảnh PoD, bấm nút đỏ **`Báo cáo vấn đề giao hàng`**.
4. Chọn lý do (ví dụ: *Bánh bị vỡ / móp méo trong quá trình vận chuyển*), nhập mô tả chi tiết tối thiểu 10 ký tự -> bấm **`Gửi Khiếu Nại & Khóa Ký Quỹ`**.
   - Kết quả: Khoản tiền `Held` của đơn chuyển sang trạng thái **`Disputed` (đóng băng giải ngân)**.

### Bước 6 — Admin phân xử & duyệt Hoàn tiền
1. Chuyển sang cửa sổ **Admin** (`0988000106` / `Password123!`).
2. Vào **`/admin/van-hanh-giao-dich`**.
3. Chọn tab **`📦 Đơn Hàng Doanh Nghiệp (Direct Bulk Orders)`** -> chọn sub-tab **`🚨 Khiếu Nại Giao Hàng PoD`**.
4. Tìm đơn vừa khiếu nại -> bấm **`Phán Quyết`**.
5. Trong modal giải quyết tranh chấp:
   - Chọn **`Chấp thuận Hoàn Tiền 100%`** (`FullRefund`) hoặc **`Hoàn tiền một phần (Hòa giải)`** (`PartialRefund` kèm số tiền hoàn).
   - Nhập nội dung *Biên Bản & Căn Cứ Phán Quyết* -> xác nhận.
6. **Kiểm tra kết quả:**
   - Số liệu **`Đã Hoàn Tiền (Refunded Sandbox)`** ở đầu tab tăng đúng bằng số tiền đã hoàn.
   - Chuyển sang tab/modal chi tiết tài chính đơn hàng (hoặc phía Customer/Supplier): trạng thái sổ cái ghi nhận bút toán hoàn tiền thành công.

---

## 3. Kịch bản B: Hoàn tiền qua Hồ sơ Yêu cầu Hoàn tiền (`OrderRefundCase`)

Dùng cho trường hợp đơn hàng **đã thanh toán** sau đó **bị hủy thỏa thuận** (`Cancelled` / `CancellationRequested`) hoặc **Customer tạo yêu cầu hoàn tiền (`refund-requests`)** để Admin duyệt tại sub-tab **`💸 Hồ Sơ Yêu Cầu Hoàn Tiền`**.

> Lưu ý UI hiện tại: Nút `Yêu cầu hủy đơn` đã có ở phía Customer (`MyOrders.jsx`), nhưng nút duyệt yêu cầu hủy ở phía Supplier (`OrderManage.jsx`) và nút gửi claim `refund-requests` độc lập ở Customer hiện cần gọi thêm qua API/Swagger cho 2 bước trung gian.

### Bước 1 — Chuẩn bị đơn đã thanh toán
Thực hiện **Bước 1 -> Bước 3** của Kịch bản A (hoặc Bước 1 -> Bước 5 nếu đơn đang ở `DeliveryDisputed`).

### Bước 2 — Hủy đơn đã thanh toán (nếu test nhánh hủy hoàn tiền)
1. Trên UI **Customer** (`/don-hang-cua-toi`), bấm **`Yêu cầu hủy đơn`** -> nhập lý do >= 5 ký tự -> gửi.
2. Lấy `orderId` và `requestId` (từ tab Network hoặc API `GET /api/v1/orders/{orderId}`), dùng token **Supplier** (`0988000103`) gọi:
   ```http
   POST /api/v1/orders/{orderId}/cancellation-requests/{requestId}/respond
   Content-Type: application/json
   Authorization: Bearer <SUPPLIER_TOKEN>

   {
     "approve": true,
     "retainedFeeAmount": 0,
     "responseNotes": "Xưởng đồng ý hủy và hoàn cọc cho khách hàng"
   }
   ```
   - Đơn chuyển sang **`Cancelled`**, tồn kho được giải phóng.

### Bước 3 — Customer gửi yêu cầu hoàn tiền (`refund-requests`)
Dùng token **Customer** (`0988000101`) gọi API:
```http
POST /api/v1/orders/{orderId}/refund-requests
Content-Type: application/json
Authorization: Bearer <CUSTOMER_TOKEN>

{
  "reason": "Đơn hàng đã hủy sau khi thanh toán VNPay, đề nghị hoàn lại tiền cọc"
}
```
- Hệ thống tạo 1 bản ghi **`OrderRefundCase`** với trạng thái **`PendingAdminReview`**.

### Bước 4 — Admin duyệt hoàn tiền trên UI (`/admin/van-hanh-giao-dich`)
1. Đăng nhập **Admin** (`0988000106` / `Password123!`) -> vào **`/admin/van-hanh-giao-dich`**.
2. Chọn tab **`📦 Đơn Hàng Doanh Nghiệp (Direct Bulk Orders)`** -> sub-tab **`💸 Hồ Sơ Yêu Cầu Hoàn Tiền`**.
3. Tìm hồ sơ trạng thái **`Chờ Admin Duyệt`** -> bấm **`Xử Lý Hoàn`**.
4. Chọn **Chấp thuận hoàn tiền**, nhập số tiền duyệt, mã tham chiếu và ghi chú -> xác nhận.
   - Kết quả: Trạng thái hồ sơ chuyển thành **`Đã Hoàn Tiền (Sandbox)`**, sổ cái ghi giảm `Held`/`Disputed` và tăng `Refunded`.

---

## 4. Kịch bản C: Kiểm thử Refund ở Hợp đồng RFQ & Tab Escrow `/admin`

### 4.1. Luồng phân xử tranh chấp hợp đồng SafeTrade (hoạt động trên UI)
1. **Customer** và **Supplier** hoàn tất luồng RFQ -> Hợp đồng -> Ký số OTP -> Customer thanh toán cọc M1 qua VNPay (`Contract.Status = Active`).
2. **Customer** mở khiếu nại tranh chấp hợp đồng -> Admin đăng nhập **`/admin`**.
3. Vào tab **`Trọng tài tranh chấp SafeTrade`** -> bấm **`Phân xử`** (`DisputeResolutionModal`).
4. Chọn hình thức xử lý (`FullRefund` / `PartialRefund` / `SupplierAtFault`), nhập số tiền hoàn và biên bản phán quyết -> Xác nhận.

### 4.2. Lưu ý lỗi `409 STATE_CONFLICT` tại nút `Hoàn tiền` trong tab `Sổ giám sát ký quỹ escrow & milestone` (`/admin`)
- Hiện tại, nếu bấm nút **`Hoàn tiền`** trực tiếp trong bảng **Sổ giám sát ký quỹ escrow** ở trang **`/admin`**, Frontend (`DashBoard.jsx`) đang truyền nhầm **`contractId`** vào endpoint `POST /api/v1/payments/{paymentId}/refund`, khiến backend không tìm thấy bản ghi trong bảng `payments` và trả về `409 STATE_CONFLICT: "This payment cannot be refunded."`.
- **Cách test đúng endpoint `POST /api/v1/payments/{paymentId}/refund` trong lúc chờ fix bug UI:**
  1. Lấy **`payment.id` thật** thuộc hợp đồng có `status = Completed` (ví dụ qua API `GET /api/v1/payments/mine` của Customer/Supplier hoặc kiểm tra bảng `payments`).
  2. Gọi API với token **Admin** (`0988000106`):
     ```http
     POST /api/v1/payments/{REAL_PAYMENT_ID}/refund
     Content-Type: application/json
     Authorization: Bearer <ADMIN_TOKEN>

     {
       "reason": "Hoàn tiền ký quỹ hợp đồng theo yêu cầu Admin"
     }
     ```
  - Nếu `payment.Status == Completed` và là cọc M1 của hợp đồng `Active`: `Payment.Status` chuyển `Refunded`, milestone M1 quay về `AwaitingPayment` và hợp đồng quay về `PendingDeposit`.
  - Nếu gọi lại lần 2 trên cùng `paymentId` đó (lúc này `Status == Refunded`): backend trả `409 STATE_CONFLICT: "This payment cannot be refunded."` (đây mới là case `409` hợp lệ).
