# [B2B-ORDER-05] - THIẾT KẾ DỮ LIỆU, XỬ LÝ ĐỒNG THỜI & BẢO MẬT (DATA, SQL, CONCURRENCY & SECURITY)

---

## I. MÔ HÌNH DỮ LIỆU POSTGRESQL (DATABASE SCHEMA DESIGN)

Toàn bộ mô hình dữ liệu của Module 4 được lưu trữ trên PostgreSQL với các ràng buộc khóa ngoại (Foreign Keys), kiểm tra tính toàn vẹn (Check Constraints) và chỉ mục tối ưu hóa truy vấn.

### 1.1. Thực thể Đơn hàng & Dòng đơn hàng (`orders`, `order_items`)

```sql
-- 1. Bảng đơn hàng (orders)
CREATE TABLE IF NOT EXISTS orders (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    customer_id UUID NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    shop_id UUID NOT NULL REFERENCES shops(id) ON DELETE RESTRICT,
    order_number VARCHAR(50) NOT NULL UNIQUE,
    idempotency_key VARCHAR(128),
    idempotency_request_hash VARCHAR(64),
    status VARCHAR(30) NOT NULL DEFAULT 'Pending',
    subtotal NUMERIC(12, 2) NOT NULL CHECK (subtotal >= 0),
    discount_amount NUMERIC(12, 2) NOT NULL DEFAULT 0.00 CHECK (discount_amount >= 0),
    tax_amount NUMERIC(12, 2) NOT NULL DEFAULT 0.00 CHECK (tax_amount >= 0),
    shipping_fee NUMERIC(12, 2) NOT NULL DEFAULT 0.00 CHECK (shipping_fee >= 0),
    packaging_fee NUMERIC(12, 2) NOT NULL DEFAULT 0.00 CHECK (packaging_fee >= 0),
    total_amount NUMERIC(12, 2) NOT NULL CHECK (total_amount >= 0),
    delivery_address TEXT NOT NULL,
    recipient_name VARCHAR(255) NOT NULL,
    recipient_phone VARCHAR(20) NOT NULL,
    delivery_date_expected DATE,
    notes TEXT,
    cancelled_at TIMESTAMPTZ,
    cancelled_by_user_id UUID REFERENCES users(id),
    cancellation_reason TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by UUID,
    updated_by UUID,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE
);

-- Chỉ mục tối ưu truy vấn danh sách đơn hàng
CREATE INDEX IF NOT EXISTS idx_orders_customer_id ON orders(customer_id) WHERE is_deleted = false;
CREATE INDEX IF NOT EXISTS idx_orders_shop_id ON orders(shop_id) WHERE is_deleted = false;
CREATE INDEX IF NOT EXISTS idx_orders_status ON orders(status) WHERE is_deleted = false;
CREATE INDEX IF NOT EXISTS idx_orders_created_at ON orders(created_at DESC);
CREATE UNIQUE INDEX IF NOT EXISTS uq_orders_customer_idempotency 
    ON orders(customer_id, idempotency_key) 
    WHERE idempotency_key IS NOT NULL AND is_deleted = false;

-- 2. Bảng dòng đơn hàng (order_items)
CREATE TABLE IF NOT EXISTS order_items (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    order_id UUID NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    product_id UUID NOT NULL REFERENCES products(id) ON DELETE RESTRICT,
    variant_id UUID NOT NULL REFERENCES product_variants(id) ON DELETE RESTRICT,
    promotion_rule_id UUID REFERENCES promotion_rules(id) ON DELETE SET NULL,
    product_name_snapshot VARCHAR(255) NOT NULL,
    variant_name_snapshot VARCHAR(255) NOT NULL,
    variant_sku_snapshot VARCHAR(100) NOT NULL,
    quantity INT NOT NULL CHECK (quantity > 0),
    unit_price NUMERIC(12, 2) NOT NULL CHECK (unit_price >= 0),
    discount_amount NUMERIC(12, 2) NOT NULL DEFAULT 0.00 CHECK (discount_amount >= 0),
    subtotal NUMERIC(12, 2) NOT NULL CHECK (subtotal >= 0),
    packaging_fee NUMERIC(12, 2) NOT NULL DEFAULT 0.00,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE INDEX IF NOT EXISTS idx_order_items_order_id ON order_items(order_id);
```

### 1.2. Thực thể Chiết khấu bậc thang (`promotion_rules`)

```sql
CREATE TABLE IF NOT EXISTS promotion_rules (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    shop_id UUID NOT NULL REFERENCES shops(id) ON DELETE CASCADE,
    product_id UUID REFERENCES products(id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    min_qty INT NOT NULL CHECK (min_qty >= 1),
    max_qty INT CHECK (max_qty IS NULL OR max_qty >= min_qty),
    discount_type VARCHAR(30) NOT NULL, -- 'Percentage', 'FixedAmount', 'FixedPrice'
    discount_value NUMERIC(12, 2) NOT NULL CHECK (discount_value > 0),
    start_date TIMESTAMPTZ,
    end_date TIMESTAMPTZ,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by UUID,
    updated_by UUID,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE INDEX IF NOT EXISTS idx_pr_shop_id ON promotion_rules(shop_id) WHERE is_deleted = false;
CREATE INDEX IF NOT EXISTS idx_pr_product_id ON promotion_rules(product_id) WHERE product_id IS NOT NULL AND is_deleted = false;
CREATE INDEX IF NOT EXISTS idx_pr_active_window ON promotion_rules(is_active, start_date, end_date) WHERE is_deleted = false;
```

### 1.3. Thực thể Tùy biến đóng gói & Duyệt thiết kế (`custom_packagings`, `custom_packaging_revisions`)

```sql
CREATE TABLE IF NOT EXISTS custom_packagings (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    order_id UUID NOT NULL UNIQUE REFERENCES orders(id) ON DELETE CASCADE,
    company_name VARCHAR(255) NOT NULL,
    company_logo_url TEXT NOT NULL,
    box_design_url TEXT,
    packaging_type VARCHAR(100) NOT NULL,
    color_scheme JSONB,
    special_message TEXT,
    notes TEXT,
    status VARCHAR(30) NOT NULL DEFAULT 'Pending', -- 'Pending', 'DesignReview', 'Approved', 'InProduction', 'Completed', 'Rejected'
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by UUID,
    updated_by UUID,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE INDEX IF NOT EXISTS idx_cpkg_order_id ON custom_packagings(order_id);
CREATE INDEX IF NOT EXISTS idx_cpkg_status ON custom_packagings(status);

CREATE TABLE IF NOT EXISTS custom_packaging_revisions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    custom_packaging_id UUID NOT NULL REFERENCES custom_packagings(id) ON DELETE CASCADE,
    revision_number INT NOT NULL,
    mockup_url TEXT NOT NULL,
    supplier_notes TEXT,
    customer_notes TEXT,
    design_fee NUMERIC(12, 2) NOT NULL DEFAULT 0.00,
    status VARCHAR(30) NOT NULL DEFAULT 'DesignReview',
    submitted_by_user_id UUID REFERENCES users(id) ON DELETE SET NULL,
    submitted_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    reviewed_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT uq_packaging_revision UNIQUE (custom_packaging_id, revision_number)
);
```

---

## II. XỬ LÝ TRANH CHẤP ĐỒNG THỜI (CONCURRENCY CONTROL)

Trong mùa cao điểm Trung Thu, nhiều doanh nghiệp có thể cùng lúc đặt mua biến thể bánh bán chạy nhất. Cần ngăn chặn tuyệt đối tình trạng bán vượt quá tồn kho (Overselling).

### 2.1. Giải pháp Khóa dòng tồn kho nguyên tử (Pessimistic Row Lock & Atomic Update)

Phương pháp kết hợp giữa Khóa dòng có sắp xếp ID (chống Deadlock) và cập nhật có điều kiện:

```sql
-- Bước 1: Khóa dòng các biến thể được đặt mua theo thứ tự tăng dần của ID (Ngăn chặn Deadlock hoàn toàn)
SELECT id, product_id, sku, stock_qty 
FROM product_variants 
WHERE id = ANY(@variantIds) AND is_deleted = false
ORDER BY id ASC
FOR UPDATE;

-- Bước 2: Cập nhật trừ tồn kho an toàn
UPDATE product_variants
SET stock_qty = stock_qty - @quantity,
    updated_at = now()
WHERE id = @variantId AND stock_qty >= @quantity;
```

Nếu số dòng bị ảnh hưởng (`rows_affected`) trả về 0, Transaction lập tức được Rollback và ném lỗi `HttpException(409, "Insufficient stock for variant.")`.

### 2.2. Khóa trạng thái đơn hàng (Optimistic Status Transition)

Để tránh trường hợp Supplier duyệt đơn (`Confirm`) đồng thời với thời điểm Customer bấm hủy đơn (`Cancel`), các câu lệnh chuyển đổi trạng thái luôn mang theo điều kiện trạng thái hiện tại:

```csharp
public async Task<bool> TransitionOrderAsync(Guid orderId, OrderStatus fromStatus, OrderStatus toStatus, Guid actorId, CancellationToken ct)
{
    var rowsAffected = await _dbContext.Database.ExecuteSqlInterpolatedAsync($@"
        UPDATE orders 
        SET status = {toStatus.ToString()}, 
            updated_at = now(), 
            updated_by = {actorId}
        WHERE id = {orderId} AND status = {fromStatus.ToString()} AND is_deleted = false", ct);

    return rowsAffected > 0;
}
```

Nếu `rowsAffected == 0`, điều đó chứng tỏ đơn hàng đã bị thay đổi bởi luồng khác; hệ thống từ chối cập nhật và thông báo người dùng tải lại trang.

---

## III. BẢO MẬT & TOÀN VẸN DỮ LIỆU (SECURITY & DATA INTEGRITY)

### 3.1. Chống thao túng giá (Price Tampering Protection)
- Frontend tuyệt đối **không gửi** các trường `unitPrice`, `discountAmount`, `subtotal`, `totalAmount`.
- Backend truy vấn trực tiếp giá gốc của sản phẩm (`Product.BasePrice + ProductVariant.PriceAdjustment`) từ Database trong Transaction.
- Thuật toán chiết khấu chỉ áp dụng các rule đang hợp lệ tại thời điểm `DateTimeProvider.UtcNow`.

### 3.2. An toàn tệp tải lên (File & Vector Logo Upload Security)
- Logo công ty và bản vẽ phối cảnh (Mockup) chỉ được tải lên qua `StorageService` với:
  - Kiểm tra MIME Type nghiêm ngặt: `image/png`, `image/jpeg`, `image/svg+xml`, `application/pdf`.
  - Giới hạn dung lượng: Tối đa 5MB.
  - Tệp SVG được làm sạch (Sanitize) bằng thư viện phân tích XML an toàn để gỡ bỏ toàn bộ thẻ `<script>`, `onload`, `javascript:` nhằm triệt tiêu lỗ hổng Stored XSS.

### 3.3. Quyền hạn sở hữu dòng dữ liệu (Row-Level Authorization)
- Khách hàng chỉ truy vấn được các đơn hàng có `customer_id == CurrentUser.Id`.
- Nhà cung cấp chỉ truy vấn được các đơn hàng có `shop_id` thuộc quyền sở hữu của tài khoản Supplier đó.
- Admin có thể đọc toàn bộ đơn để phục vụ giám sát và giải quyết khiếu nại.

