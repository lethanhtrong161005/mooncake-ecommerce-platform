# Module 4 — B2B Ordering: Features #7, #8, #9

- **Feature Key:** `b2b-ordering`
- **Module Name:** Module 4 – Đặt hàng B2B (B2B Ordering & Enterprise Customization)
- **Design Date:** **2026-10-29**. **Version:** `1.0.0`
- **Status:** **Implemented / Ready for migration review** (2026-10-29). Backend, frontend integration và unit tests đã hoàn tất; migration cần được áp dụng theo quy trình triển khai của môi trường đích.
- **Included Features:**
  - **Feature #7 (P0 Critical):** Đặt hàng số lượng lớn (Bulk Order & Lifecycle Management).
  - **Feature #8 (P0 Critical):** Khuyến mãi / Chiết khấu bậc thang theo số lượng (Tiered Pricing & Volume Promotions).
  - **Feature #9 (P1 High):** Đóng gói & In ấn logo thương hiệu doanh nghiệp (Custom Packaging & Multi-round Design Revisions).

---

## 📖 Danh mục tài liệu thiết kế (Reading Order)

| Thứ tự | Tên tài liệu | Nội dung & Mục đích |
| :--- | :--- | :--- |
| **01** | [01_BULK_ORDER_MANAGEMENT_DESIGN.md](./01_BULK_ORDER_MANAGEMENT_DESIGN.md) | Thiết kế chi tiết Tính năng #7: Đặt hàng theo biến thể (Variant-only), kiểm tra tồn kho nguyên tử, quản lý vòng đời đơn hàng (`Pending -> Confirmed -> Processing -> Shipped -> Delivered`), Idempotency. |
| **02** | [02_VOLUME_PROMOTION_TIERED_PRICING_DESIGN.md](./02_VOLUME_PROMOTION_TIERED_PRICING_DESIGN.md) | Thiết kế chi tiết Tính năng #8: Thiết lập bảng giá bậc thang (Tiered Pricing), công cụ tính toán chiết khấu tối ưu (Best-Discount Engine), kiểm tra hạn hiệu lực và scope áp dụng. |
| **03** | [03_CUSTOM_PACKAGING_BRANDING_DESIGN.md](./03_CUSTOM_PACKAGING_BRANDING_DESIGN.md) | Thiết kế chi tiết Tính năng #9: Luồng tùy biến bao bì doanh nghiệp, tải logo vector, duyệt thiết kế mockup 3D qua nhiều vòng (Revisions), tính phí thiết kế và tích hợp hợp đồng. |
| **04** | [04_API_DTO_VALIDATION.md](./04_API_DTO_VALIDATION.md) | Đặc tả hợp đồng API RESTful, định dạng Request/Response JSON chuẩn `ApiResponse<T>`, FluentValidation rules, Header `Idempotency-Key` và bảng mã lỗi chuẩn hóa. |
| **05** | [05_DATA_SQL_CONCURRENCY_SECURITY.md](./05_DATA_SQL_CONCURRENCY_SECURITY.md) | Mô hình dữ liệu PostgreSQL, EF Core configurations, xử lý tranh chấp tồn kho (Pessimistic/Optimistic Locking), Indexes, Audit Logs, chống giả mạo giá (Anti-tampering). |
| **06** | [06_FRONTEND_INTEGRATION.md](./06_FRONTEND_INTEGRATION.md) | Kiến trúc tích hợp Frontend `mooncake-fe`, kết nối UI Customer (`OrderAndDesgin.jsx`, `PackagingAndLogoPrinting.jsx`) và Supplier (`OrderManage.jsx`), Services, Custom Hooks, Design System Lucian Luxury. |
| **07** | [07_IMPLEMENTATION_TEST_ROLLOUT.md](./07_IMPLEMENTATION_TEST_ROLLOUT.md) | Kế hoạch hiện thực hóa từng pha (Phases 1-6), Ma trận kiểm thử Unit/Integration/E2E, Kịch bản tải cao điểm mùa Trung Thu, Tiêu chí bàn giao (Definition of Done) và Kế hoạch Rollback. |
| **08** | [08_IMPLEMENTATION_RESULT.md](./08_IMPLEMENTATION_RESULT.md) | Báo cáo hiện thực, danh sách endpoint, migration và kết quả kiểm thử thực tế. |

---

## 🎯 Căn cứ nghiệp vụ & Nguồn tham chiếu

1. **Tài liệu nghiệp vụ:** [mooncake-marketplace.md](../../business/mooncake-marketplace.md) - Module 4 (Features #7, #8, #9).
2. **Template thiết kế chuẩn:** [FEATURE_DETAIL_DESIGN_TEMPLATE.md](../template/FEATURE_DETAIL_DESIGN_TEMPLATE.md).
3. **Quy chuẩn mã nguồn:**
   - **Backend rules:** [AGENTS.md](../../AGENTS.md) (Clean Architecture, C# 12, .NET 8, English Only trong source code, Standard `ApiResponse<T>`, Static Helpers, Không hardcode magic strings).
   - **Frontend rules:** [AGENTS.md](../../../../.agents/AGENTS.md) (React 19, 2 spaces indentation, ES2024, Không dùng `var`, Layered architecture, `apiClient` Axios tập trung, Mock fallback pattern, Theme Luxury Gold/Amber).
4. **Hiện trạng codebase:**
   - Đã có thực thể Domain: `Order`, `OrderItem`, `PromotionRule`, `CustomPackaging`, `CustomPackagingRevision`.
   - Đã có sơ khởi: `IOrderService`, `IPromotionService`, `OrdersController`, `SupplierPromotionsController`.
   - Cần bổ sung & hoàn thiện: Luồng duyệt thiết kế bao bì `CustomPackagingService`, API tính trước chiết khấu giỏ hàng (Pricing Quote / Preview API), kiểm tra tồn kho đồng thời nguyên tử, kết nối Frontend thực tế thay vì mock data độc lập.

---

## ⚖️ Các nguyên tắc thiết kế bất biến (Core Invariants)

1. **Quy tắc bắt buộc chọn Variant:** Khách hàng không thể đặt hàng ở cấp độ sản phẩm cha (`Product`); bắt buộc phải chỉ định `VariantId` hợp lệ (quy định loại vỏ, nhân, trọng lượng). Đơn giá xuất phát từ: `Variant.BasePrice + Variant.PriceAdjustment`.
2. **Bảo toàn tính toàn vẹn giá (Price Integrity):** Mọi giá bán, tỷ lệ chiết khấu, phụ phí đóng gói đều được máy chủ tính toán lại dựa trên cơ sở dữ liệu tại thời điểm đặt hàng; Frontend chỉ gửi số lượng và các thuộc tính lựa chọn, tuyệt đối không tin cậy giá tiền gửi lên từ Client.
3. **Chống trùng lặp giao dịch (Idempotency):** Mọi yêu cầu tạo đơn đặt hàng `POST /orders` bắt buộc phải kèm Header `Idempotency-Key` (UUIDv4). Nếu nhận cùng Key và Hash trùng khớp, hệ thống trả lại kết quả đơn đã tạo trước đó mà không trừ tồn kho lần hai.
4. **Trừ tồn kho nguyên tử & Chống Race Condition:** Khi đặt đơn, hệ thống khóa dòng (`SELECT ... FOR UPDATE`) các bản ghi `ProductVariant` tương ứng trong Transaction, đảm bảo không xảy ra hiện tượng âm kho (Overselling) trong các đợt cao điểm đặt hàng số lượng lớn.
5. **Vòng đời duyệt thiết kế bao bì (Packaging Review Loop):** Bản vẽ mockup bao bì chỉ được đưa vào sản xuất (`InProduction`) sau khi cả Supplier đã upload mockup và Customer ấn phê duyệt (`Approved`). Mọi thay đổi đều được ghi lại thành các bản ghi `CustomPackagingRevision` có đánh số thứ tự tăng dần.

