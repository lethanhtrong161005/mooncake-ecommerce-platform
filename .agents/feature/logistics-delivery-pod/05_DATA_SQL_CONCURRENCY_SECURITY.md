# MODULE 7: THIẾT KẾ CƠ SỞ DỮ LIỆU, ĐỒNG THỜI & BẢO MẬT (DATA, SQL, CONCURRENCY & SECURITY)

---

## I. THIẾT KẾ SCHEMA CƠ SỞ DỮ LIỆU (POSTGRESQL DDL)

Cơ sở dữ liệu PostgreSQL sử dụng 2 bảng chính: `deliveries` và `delivery_proofs`, được liên kết với `contracts`, `orders` và bảng quản lý người dùng `users`.

### 1.1. PostgreSQL DDL: Bảng `deliveries`

```sql
-- Tạo các kiểu Enum nếu chưa tồn tại
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'delivery_type') THEN
        CREATE TYPE delivery_type AS ENUM ('platform_managed', 'self_arranged');
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'delivery_status') THEN
        CREATE TYPE delivery_status AS ENUM ('pending', 'confirmed', 'picked_up', 'in_transit', 'delivered', 'failed', 'returned');
    END IF;
END $$;

-- Bảng Chuyến giao hàng
CREATE TABLE IF NOT EXISTS deliveries (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    contract_id UUID NULL REFERENCES contracts(id) ON DELETE SET NULL,
    order_id UUID NULL REFERENCES orders(id) ON DELETE SET NULL,
    delivery_number VARCHAR(50) NOT NULL UNIQUE,
    scheduled_date DATE NULL,
    actual_delivery_date DATE NULL,
    from_address VARCHAR(500) NULL,
    to_address VARCHAR(500) NULL,
    recipient_name VARCHAR(150) NULL,
    recipient_phone VARCHAR(30) NULL,
    tracking_code VARCHAR(100) NULL,
    notes TEXT NULL,
    delivery_type delivery_type NOT NULL DEFAULT 'self_arranged',
    status delivery_status NOT NULL DEFAULT 'pending',
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NULL,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMPTZ NULL,

    -- Ràng buộc: Mỗi chuyến giao bắt buộc phải thuộc một Hợp đồng hoặc Đơn hàng
    CONSTRAINT chk_delivery_source CHECK (contract_id IS NOT NULL OR order_id IS NOT NULL)
);

-- Chỉ mục tối ưu hóa hiệu năng truy vấn
CREATE INDEX IF NOT EXISTS idx_deliveries_contract_id ON deliveries(contract_id) WHERE is_deleted = FALSE;
CREATE INDEX IF NOT EXISTS idx_deliveries_order_id ON deliveries(order_id) WHERE is_deleted = FALSE;
CREATE INDEX IF NOT EXISTS idx_deliveries_status ON deliveries(status);
CREATE INDEX IF NOT EXISTS idx_deliveries_tracking_code ON deliveries(tracking_code);
CREATE INDEX IF NOT EXISTS idx_deliveries_scheduled_date ON deliveries(scheduled_date);
```

### 1.2. PostgreSQL DDL: Bảng `delivery_proofs`

```sql
-- Tạo Enum proof_type nếu chưa tồn tại
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'proof_type') THEN
        CREATE TYPE proof_type AS ENUM ('pickup', 'in_transit', 'delivery', 'failed_attempt');
    END IF;
END $$;

-- Bảng Bằng chứng giao nhận PoD
CREATE TABLE IF NOT EXISTS delivery_proofs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    delivery_id UUID NOT NULL REFERENCES deliveries(id) ON DELETE CASCADE,
    proof_type proof_type NOT NULL DEFAULT 'delivery',
    photo_url VARCHAR(1000) NULL,
    signature_url VARCHAR(1000) NULL,
    taken_by VARCHAR(150) NULL,
    taken_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    location_lat DECIMAL(9, 6) NULL,
    location_lng DECIMAL(9, 6) NULL,
    notes TEXT NULL,
    tamper_proof_hash VARCHAR(64) NULL, -- Chuỗi SHA-256 bảo vệ tính toàn vẹn
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NULL,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMPTZ NULL,

    -- Ràng buộc tọa độ GPS hợp lệ theo chuẩn WGS-84
    CONSTRAINT chk_proof_lat CHECK (location_lat IS NULL OR (location_lat >= -90.0 AND location_lat <= 90.0)),
    CONSTRAINT chk_proof_lng CHECK (location_lng IS NULL OR (location_lng >= -180.0 AND location_lng <= 180.0))
);

-- Chỉ mục truy vấn bằng chứng
CREATE INDEX IF NOT EXISTS idx_delivery_proofs_delivery_id ON delivery_proofs(delivery_id) WHERE is_deleted = FALSE;
CREATE INDEX IF NOT EXISTS idx_delivery_proofs_proof_type ON delivery_proofs(proof_type);
CREATE INDEX IF NOT EXISTS idx_delivery_proofs_taken_at ON delivery_proofs(taken_at);
```

---

## II. CHIẾN LƯỢC ĐỒNG THỜI & TRANSACTION (CONCURRENCY CONTROL)

### 2.1. Vấn đề Xung đột Trạng thái (Race Conditions)
Trong nghiệp vụ logistics hiện trường:
- Tài xế có thể bấm nộp PoD xác nhận thành công cùng thời điểm một sự kiện Webhook của hãng vận chuyển gửi tín hiệu báo sự cố.
- Hai nhân viên cùng kho có thể cố gắng xác nhận một chuyến hàng đồng thời.

### 2.2. Giải pháp Khóa Bi quan (Pessimistic Locking `FOR UPDATE`)
Khi cập nhật trạng thái chuyến giao hàng từ `InTransit` sang `Delivered`:

```csharp
// Sử dụng EF Core với Raw SQL hoặc Execution Strategy khóa bi quan dòng dữ liệu
await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

var delivery = await _dbContext.Deliveries
    .FromSqlInterpolated($"SELECT * FROM deliveries WHERE id = {deliveryId} FOR UPDATE")
    .FirstOrDefaultAsync(cancellationToken);

if (delivery == null)
    throw new NotFoundException("Chuyến giao hàng không tồn tại.");

if (delivery.Status == DeliveryStatus.Delivered)
    throw new ConflictException("Chuyến giao hàng đã hoàn tất trước đó.");

// Cập nhật trạng thái an toàn
delivery.Status = DeliveryStatus.Delivered;
delivery.ActualDeliveryDate = DateOnly.FromDateTime(DateTime.UtcNow);
delivery.UpdatedAt = DateTime.UtcNow;

await _dbContext.SaveChangesAsync(cancellationToken);
await transaction.CommitAsync(cancellationToken);
```

---

## III. BẢO MẬT & TÍNH TOÀN VẸN BẰNG CHỨNG (DATA SECURITY & TAMPER EVIDENCE)

### 3.1. Thuật toán Mã băm Bất biến (SHA-256 Tamper-Proof Hash)
Để đảm bảo ảnh chụp kiện bánh và chữ ký số của thủ kho có giá trị chứng cứ pháp lý khi xảy ra tranh chấp hợp đồng (Feature #17):

```
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                        QUY TRÌNH TẠO MÃ BĂM BẢO CHỨNG PHÁP LÝ                          │
└────────────────────────────────────────────────────────────────────────────────────────┘
  File Ảnh Chụp (Bytes)  ─────────► SHA-256 ─────────► PhotoHash (32 bytes hex)
                                                           │
  Metadata Chuỗi:                                          ▼
  [DeliveryId] | [ProofType] | [Lat] | [Lng] | [TakenAt] | [PhotoHash] | [SignatureHash]
                                                           │
                                                           ▼
                                                Mã băm SHA-256 tổng thể
                                                           │
                                                           ▼
                                         Lưu vào DB: tamper_proof_hash (64 chars)
```

Khi Tòa trọng tài hoặc Admin mở phiên giải quyết tranh chấp:
1. Hệ thống đọc lại file ảnh từ Cloud Storage và tính lại SHA-256.
2. Ghép với metadata trong DB và so sánh với `tamper_proof_hash`.
3. Nếu khớp 100% ➔ Bằng chứng nguyên bản, không bị can thiệp.

### 3.2. Chiến lược Lưu trữ File An toàn (Secure Storage Naming)
1. Không sử dụng tên file gốc của người dùng (`IMG_001.jpg`) để tránh tấn công Path Traversal.
2. Tên file được sinh ngẫu nhiên theo cấu trúc:
   `/pod/{yyyy}/{MM}/{deliveryId}_{proofType}_{uuid}.{ext}`.
3. Cấp quyền truy cập Read-only thông qua Presigned URL có thời hạn hoặc CDN bảo mật.

### 3.3. Cô lập Dữ liệu Đa người thuê (Tenant Isolation)
- Supplier chỉ có quyền đọc/ghi trên các bản ghi `deliveries` thuộc các Hợp đồng mà Shop của họ là bên bán (`Contract.SupplierId == currentSupplierId`).
- Customer chỉ xem được các chuyến giao thuộc hợp đồng/đơn hàng mà họ là bên mua (`Contract.CustomerId == currentCustomerId`).
- Admin có quyền xem và can thiệp toàn quyền phục vụ kiểm định chất lượng và giải quyết tranh chấp.

