# [RFQ-BIDDING-06] - THIẾT KẾ DỮ LIỆU, XỬ LÝ ĐỒNG THỜI & BẢO MẬT (DATA, SQL, CONCURRENCY & VNPAY SECURITY)

---

## I. MÔ HÌNH DỮ LIỆU POSTGRESQL (DATABASE SCHEMA DESIGN)

Toàn bộ mô hình dữ liệu của Module 5 được lưu trữ trên PostgreSQL với các quan hệ ràng buộc chặt chẽ, hỗ trợ kiểm soát tính toàn vẹn và giao dịch ACID.

### 1.1. Thực thể Yêu cầu báo giá & Mặt hàng thầu (`request_for_quotations`, `rfq_items`)

```sql
CREATE TABLE IF NOT EXISTS request_for_quotations (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    customer_id UUID NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    rfq_number VARCHAR(50) NOT NULL UNIQUE,
    title VARCHAR(255) NOT NULL,
    description TEXT,
    delivery_address TEXT NOT NULL,
    delivery_date_required DATE NOT NULL,
    budget_range_min NUMERIC(12, 2) CHECK (budget_range_min IS NULL OR budget_range_min >= 0),
    budget_range_max NUMERIC(12, 2) CHECK (budget_range_max IS NULL OR budget_range_max >= budget_range_min),
    bid_deadline TIMESTAMPTZ NOT NULL,
    status VARCHAR(30) NOT NULL DEFAULT 'Draft', -- 'Draft', 'Open', 'InNegotiation', 'Awarded', 'Closed', 'Cancelled'
    awarded_bid_id UUID, -- Sẽ bổ sung FK tới bảng bids
    awarded_at TIMESTAMPTZ,
    selection_note TEXT,
    cancellation_reason TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by UUID,
    updated_by UUID,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE INDEX IF NOT EXISTS idx_rfq_customer_id ON request_for_quotations(customer_id) WHERE is_deleted = false;
CREATE INDEX IF NOT EXISTS idx_rfq_status ON request_for_quotations(status) WHERE is_deleted = false;
CREATE INDEX IF NOT EXISTS idx_rfq_deadline ON request_for_quotations(bid_deadline) WHERE status = 'Open' AND is_deleted = false;

CREATE TABLE IF NOT EXISTS rfq_items (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    rfq_id UUID NOT NULL REFERENCES request_for_quotations(id) ON DELETE CASCADE,
    product_name VARCHAR(255) NOT NULL,
    description TEXT,
    quantity INT NOT NULL CHECK (quantity > 0),
    unit VARCHAR(50) NOT NULL,
    target_price NUMERIC(12, 2) CHECK (target_price IS NULL OR target_price >= 0),
    specifications TEXT,
    packaging_requirements TEXT,
    needs_custom_packaging BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by UUID,
    updated_by UUID,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE INDEX IF NOT EXISTS idx_rfq_items_rfq_id ON rfq_items(rfq_id);
```

### 1.2. Thực thể Lời mời tham gia thầu (`rfq_invitations`)

```sql
CREATE TABLE IF NOT EXISTS rfq_invitations (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    rfq_id UUID NOT NULL REFERENCES request_for_quotations(id) ON DELETE CASCADE,
    supplier_id UUID NOT NULL REFERENCES supplier_profiles(id) ON DELETE RESTRICT,
    status VARCHAR(30) NOT NULL DEFAULT 'Invited', -- 'Invited', 'Viewed', 'BidSubmitted', 'Declined'
    decline_reason TEXT,
    invited_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    responded_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT uq_rfq_supplier_invitation UNIQUE (rfq_id, supplier_id)
);

CREATE INDEX IF NOT EXISTS idx_invitations_rfq_id ON rfq_invitations(rfq_id);
CREATE INDEX IF NOT EXISTS idx_invitations_supplier_id ON rfq_invitations(supplier_id);
```

### 1.3. Thực thể Hồ sơ Báo giá & Chi tiết (`bids`, `bid_items`)

```sql
CREATE TABLE IF NOT EXISTS bids (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    rfq_id UUID NOT NULL REFERENCES request_for_quotations(id) ON DELETE RESTRICT,
    supplier_id UUID NOT NULL REFERENCES supplier_profiles(id) ON DELETE RESTRICT,
    bid_number VARCHAR(50) NOT NULL UNIQUE,
    total_price NUMERIC(12, 2) NOT NULL CHECK (total_price >= 0),
    estimated_delivery_days INT NOT NULL CHECK (estimated_delivery_days > 0),
    validity_days INT NOT NULL DEFAULT 30 CHECK (validity_days > 0),
    notes TEXT,
    status VARCHAR(30) NOT NULL DEFAULT 'Submitted', -- 'Submitted', 'Accepted', 'Cancelled'
    submitted_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    accepted_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by UUID,
    updated_by UUID,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE INDEX IF NOT EXISTS idx_bids_rfq_id ON bids(rfq_id) WHERE is_deleted = false;
CREATE INDEX IF NOT EXISTS idx_bids_supplier_id ON bids(supplier_id) WHERE is_deleted = false;
CREATE INDEX IF NOT EXISTS idx_bids_status ON bids(status) WHERE is_deleted = false;

CREATE TABLE IF NOT EXISTS bid_items (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    bid_id UUID NOT NULL REFERENCES bids(id) ON DELETE CASCADE,
    rfq_item_id UUID NOT NULL REFERENCES rfq_items(id) ON DELETE RESTRICT,
    unit_price NUMERIC(12, 2) NOT NULL CHECK (unit_price > 0),
    quantity INT NOT NULL CHECK (quantity > 0),
    subtotal NUMERIC(12, 2) NOT NULL CHECK (subtotal >= 0),
    notes TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE INDEX IF NOT EXISTS idx_bid_items_bid_id ON bid_items(bid_id);
```

---

## II. XỬ LÝ TRANH CHẤP ĐỒNG THỜI (CONCURRENCY CONTROL)

### 2.1. Ngăn chặn xung đột khi Trao thầu (Award Bid Concurrency)
- **Vấn đề:** Khách hàng mở 2 tab trình duyệt hoặc double-click chọn 2 báo giá khác nhau trong cùng 1 tích tắc.
- **Giải pháp:** Sử dụng khóa dòng bi quan (**Pessimistic Row Locking**) ngay khi bắt đầu xử lý chọn thầu:
```sql
SELECT * FROM request_for_quotations 
WHERE id = @rfqId 
FOR UPDATE;
```
Lệnh này giữ khóa độc quyền trên bản ghi RFQ cho tới khi Transaction Commit. Nếu request thứ hai chạy đồng thời, nó buộc phải chờ request thứ nhất xong; khi request thứ hai đọc được `rfq.Status == 'Awarded'`, nó lập tức bị chặn với lỗi `HttpException(409, "A different bid has already been awarded for this RFQ.")`.

### 2.2. Tranh chấp giữa Nộp Báo Giá và Hạn chót Đóng Thầu (`RfqDeadlineJob`)
- Khi đến giờ `BidDeadline`, Background Service `RfqDeadlineJob` thực thi:
```sql
UPDATE request_for_quotations
SET status = 'Closed', updated_at = now()
WHERE status = 'Open' AND bid_deadline <= now() AND is_deleted = false;
```
- Khi Supplier gọi `SubmitBidAsync`, máy chủ kiểm tra `clock.UtcNow < rfq.BidDeadline` bên trong Transaction. Nếu xưởng cố tình nộp chậm 1 giây sau deadline, lệnh nộp bị từ chối ngay lập tức.

---

## III. BẢO MẬT & TÍCH HỢP CHỮ KÝ SỐ VNPAY (SECURITY & VNPAY HMAC-SHA512)

### 3.1. Thuật toán tạo và xác thực chữ ký số VNPay (HMAC-SHA512)

Chuẩn tích hợp VNPay 2.1.0 yêu cầu mọi tham số gửi đi và nhận về phải được sắp xếp theo bảng chữ cái A-Z và băm với Secret Key:

```csharp
public static class VnPaySecurityHelper
{
    public static string CreateSecureHash(IDictionary<string, string> parameters, string secretKey)
    {
        var sortedParams = new SortedDictionary<string, string>(parameters, StringComparer.Ordinal);
        var rawData = new StringBuilder();
        foreach (var (key, value) in sortedParams)
        {
            if (string.IsNullOrEmpty(value) || key.Equals("vnp_SecureHash", StringComparison.OrdinalIgnoreCase) 
                || key.Equals("vnp_SecureHashType", StringComparison.OrdinalIgnoreCase))
                continue;

            if (rawData.Length > 0) rawData.Append('&');
            rawData.Append(WebUtility.UrlEncode(key)).Append('=').Append(WebUtility.UrlEncode(value));
        }

        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secretKey));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawData.ToString()));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public static bool VerifySecureHash(IDictionary<string, string> parameters, string secureHash, string secretKey)
    {
        var calculatedHash = CreateSecureHash(parameters, secretKey);
        return string.Equals(calculatedHash, secureHash, StringComparison.OrdinalIgnoreCase);
    }
}
```

### 3.2. Cấu hình bảo mật từ appsettings cục bộ
Hệ thống sử dụng các thông số bí mật được lưu an toàn trong `appsettings.json` bị Git ignore:
- `vnp_TmnCode`: `<VNPAY_TMN_CODE>`
- `vnp_HashSecret`: `<VNPAY_HASH_SECRET>`
- Mọi giao dịch chuyển sang VNPay đều phải nhân số tiền với 100: `vnp_Amount = amount * 100`.

### 3.3. Xử lý IPN Idempotent (Chống kích hoạt trùng lặp)
Khi nhận Webhook `GET /api/v1/payments/vnpay/ipn`:
1. Kiểm tra mã giao dịch `vnp_TxnRef`:
   - Nếu bản ghi `Payment` tương ứng đã ở trạng thái `Paid` -> Phản hồi ngay `{ "RspCode": "02", "Message": "Order already confirmed" }`.
   - Điều này triệt tiêu hoàn toàn khả năng một giao dịch bị xử lý hai lần khi cổng thanh toán gửi lại webhook thử lại (Retry).

