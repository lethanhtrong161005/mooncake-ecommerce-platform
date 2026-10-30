# [M10-F25.6] DỮ LIỆU CSDL, ĐỒNG THỜI & BẢO MẬT: THANH TOÁN & ESCROW

---

## I. LƯỢC ĐỒ CƠ SỞ DỮ LIỆU POSTGRESQL (DATA PERSISTENCE)

### 1.1. Bảng `payments` (Lưu trữ giao dịch thanh toán)

```sql
-- PostgreSQL DDL for table: payments
CREATE TABLE IF NOT EXISTS payments (
    id                   UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id             UUID REFERENCES orders(id) ON DELETE SET NULL,
    contract_id          UUID REFERENCES contracts(id) ON DELETE SET NULL,
    milestone_id         UUID REFERENCES contract_milestones(id) ON DELETE SET NULL,
    customer_id          UUID NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    payment_number       VARCHAR(50) NOT NULL UNIQUE,
    amount               NUMERIC(14, 2) NOT NULL CHECK (amount > 0),
    currency             VARCHAR(10) NOT NULL DEFAULT 'VND',
    provider             VARCHAR(50) DEFAULT 'VNPay',
    idempotency_key      VARCHAR(128),
    transaction_id       VARCHAR(255),
    method               payment_method NOT NULL DEFAULT 'vnpay',
    status               payment_status NOT NULL DEFAULT 'pending',
    evidence_url         VARCHAR(1000),
    refunded_amount      NUMERIC(14, 2) NOT NULL DEFAULT 0.00 CHECK (refunded_amount >= 0),
    refund_reason        TEXT,
    paid_at              TIMESTAMPTZ,
    expires_at           TIMESTAMPTZ,
    created_at           TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by           UUID,
    updated_at           TIMESTAMPTZ,
    updated_by           UUID,
    is_deleted           BOOLEAN NOT NULL DEFAULT FALSE
);

-- Indexes tối ưu hóa truy vấn & đảm bảo tính duy nhất
CREATE UNIQUE INDEX IF NOT EXISTS idx_payments_payment_number ON payments(payment_number);
CREATE INDEX IF NOT EXISTS idx_payments_customer_id ON payments(customer_id);
CREATE INDEX IF NOT EXISTS idx_payments_order_id ON payments(order_id) WHERE order_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_payments_contract_milestone ON payments(contract_id, milestone_id) WHERE contract_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_payments_idempotency_customer ON payments(customer_id, idempotency_key) WHERE idempotency_key IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_payments_status_expires_at ON payments(status, expires_at) WHERE status = 'pending';
```

---

## II. XỬ LÝ ĐỒNG THỜI & CHỐNG THANH TOÁN KÉP (CONCURRENCY CONTROL)

### 2.1. Nguy cơ tiềm ẩn trong môi trường B2B
1. **Double-Click từ người dùng:** Khách hàng bấm liên tiếp 2 lần vào nút "Thanh toán VNPay".
2. **Race condition giữa Return URL và IPN Webhook:** Khách hàng thanh toán xong, trình duyệt được chuyển về `return-callback` cùng thời điểm máy chủ VNPay bắn `ipn` webhook tới WebApi.
3. **Thanh toán lại mốc đã hoàn tất:** Cố tình gọi API thanh toán lại mốc hợp đồng M1 đã được ký quỹ.

### 2.2. Giải pháp kỹ thuật Enterprise
1. **Idempotency Key Pattern:**
   - Mỗi request tạo giao dịch bắt buộc hoặc tự sinh một `Idempotency-Key`.
   - Nếu phát hiện khóa trùng lặp trong thời gian còn hiệu lực và cùng thông số đơn hàng $\rightarrow$ Trả về trực tiếp bản ghi thanh toán hiện hữu mà không tạo mới.
2. **Database Row-Level Locking (`FOR UPDATE`):**
   ```csharp
   // Khi xử lý ConfirmVnPayPaymentAsync hoặc ProcessVnPayIpnAsync
   var payment = await _dbContext.Payments
       .FromSqlInterpolated($"SELECT * FROM payments WHERE payment_number = {paymentNumber} FOR UPDATE")
       .FirstOrDefaultAsync(cancellationToken);

   if (payment == null || payment.Status == PaymentStatus.Completed)
   {
       // Đã xử lý xong trước đó bởi tiến trình đối ứng -> Bỏ qua và trả về kết quả thành công an toàn
       return payment;
   }
   ```
3. **Chuyển dịch trạng thái bất biến (State Machine Integrity):**
   - Chỉ cho phép chuyển trạng thái `Pending` $\rightarrow$ `Completed` hoặc `Failed`.
   - Tuyệt đối không cho phép chuyển trạng thái từ `Completed` ngược về `Pending` hay `Completed` $\rightarrow$ `Completed` lần 2.

---

## III. BẢO MẬT GIAO DỊCH & CHỮ KÝ SỐ VNPAY SANDBOX

### 3.1. Thuật toán ký số HMAC-SHA512
Cổng thanh toán VNPay yêu cầu mọi giao dịch tạo mới và callback trả về phải được xác thực bằng chuỗi băm bảo mật HMAC-SHA512 với khóa bí mật `vnp_HashSecret` (`WBGRFFJWAHOAFNIEYICELPMQORJJHSQL`).

```csharp
private string CreateSecureHash(IReadOnlyDictionary<string, string> parameters)
{
    // 1. Lọc bỏ các tham số null, rỗng, vnp_SecureHash, vnp_SecureHashType
    // 2. Sắp xếp toàn bộ tham số theo thứ tự alphabet của key (Ordinal)
    // 3. Nối thành chuỗi dạng url-encoded: key1=value1&key2=value2
    var rawData = string.Join("&", parameters
        .Where(item => !string.IsNullOrWhiteSpace(item.Value) &&
                       !string.Equals(item.Key, "vnp_SecureHash", StringComparison.OrdinalIgnoreCase) &&
                       !string.Equals(item.Key, "vnp_SecureHashType", StringComparison.OrdinalIgnoreCase))
        .OrderBy(item => item.Key, StringComparer.Ordinal)
        .Select(item => $"{WebUtility.UrlEncode(item.Key)}={WebUtility.UrlEncode(item.Value)}"));

    // 4. Băm HMAC-SHA512 với vnp_HashSecret
    using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(_settings.HashSecret));
    var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawData));
    return Convert.ToHexString(hashBytes).ToLowerInvariant();
}
```

### 3.2. Chống tấn công Timing Attack
Khi kiểm tra chữ ký do VNPay gửi về, sử dụng hàm `CryptographicOperations.FixedTimeEquals` để so sánh byte mảng thay vì toán tử `==` nhằm loại bỏ nguy cơ rò rỉ độ dài chuỗi qua thời gian phản hồi:

```csharp
public bool IsValidSignature(IReadOnlyDictionary<string, string> parameters)
{
    if (!parameters.TryGetValue("vnp_SecureHash", out var secureHash) || string.IsNullOrWhiteSpace(secureHash))
        return false;

    var expectedHash = CreateSecureHash(parameters);
    return CryptographicOperations.FixedTimeEquals(
        Encoding.UTF8.GetBytes(expectedHash),
        Encoding.UTF8.GetBytes(secureHash.ToLowerInvariant())
    );
}
```

### 3.3. Nhật ký kiểm toán tài chính (Financial Audit Logging)
Mọi thao tác can thiệp tài chính của Quản trị viên (Admin Confirm chuyển khoản, Admin Refund hoàn tiền) bắt buộc được ghi vết tự động vào bảng `audit_logs`:
- `ActorUserId`: ID của Admin thực hiện
- `Action`: `"PaymentConfirmed"` hoặc `"PaymentRefunded"`
- `EntityName`: `"Payment"`
- `EntityId`: `payment.Id`
- `MetadataJson`: Lưu số tiền, mã thanh toán, lý do đối soát và địa chỉ IP thực hiện.

