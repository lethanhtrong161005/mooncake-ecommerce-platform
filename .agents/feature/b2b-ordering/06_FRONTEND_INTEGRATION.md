# [B2B-ORDER-06] - THIẾT KẾ TÍCH HỢP FRONTEND (REACT 19 LAYERED ARCHITECTURE)

---

## I. TỔNG QUAN KIẾN TRÚC FRONTEND (`mooncake-fe`)

Frontend của Module 4 được xây dựng trên nền tảng **React 19**, **Vite**, **Ant Design 6** và **Tailwind CSS**, tuân thủ nghiêm ngặt quy tắc kiến trúc phân tầng (Layered Architecture) và bảng phong cách nhận diện Luxury Gold / Amber.

```text
src/
├── constants/
│   ├── apiEndpoints.js            # Khai báo endpoint ORDERS, PROMOTIONS, PACKAGING
│   └── routes.js                  # ORDER_DESIGN, PACKAGING_LOGO, SUPPLIER_ORDERS
├── services/
│   ├── apiClient.js               # Axios instance tập trung (JWT Bearer, Mock fallback)
│   ├── orderService.js            # Gọi API đặt hàng, danh sách đơn, cập nhật tiến độ
│   ├── promotionService.js        # Bảng giá bậc thang & API tính trước báo giá
│   └── customPackagingService.js  # Upload logo, bản vẽ mockup 3D, duyệt revision
├── hooks/
│   ├── useBulkOrder.js            # Custom hook quản lý form đặt hàng & tính giá realtime
│   └── useCustomPackaging.js      # Custom hook quản lý vòng lặp duyệt bản vẽ thiết kế
├── components/
│   ├── order/
│   │   ├── TieredPricingTable.jsx # Bảng giá bậc thang trực quan
│   │   ├── OrderSummaryCard.jsx   # Thẻ tóm tắt chi phí, chiết khấu, phụ phí
│   │   └── OrderStatusBadge.jsx   # Huy hiệu trạng thái đơn hàng phong cách Luxury
│   └── packaging/
│       ├── LogoUploadBox.jsx      # Hộp tải logo vector kiểm tra định dạng
│       ├── MockupReviewModal.jsx  # Modal xem phối cảnh 3D & duyệt revision
│       └── ColorSchemePicker.jsx  # Bộ chọn màu phối hộp quà sang trọng
└── Pages/
    ├── Customer/
    │   ├── OrderAndDesgin.jsx     # Trang Đặt hàng số lượng lớn & In logo doanh nghiệp
    │   └── PackagingAndLogoPrinting.jsx # Khám phá thư viện quy cách hộp & chất liệu
    └── Supplier/
        └── OrderManage.jsx        # Quản lý đơn hàng xưởng, duyệt đơn, gửi bản vẽ mockup
```

---

## II. ĐẶC TẢ TẦNG DỊCH VỤ (API SERVICE LAYER)

### 2.1. Cập nhật `src/constants/apiEndpoints.js`

```javascript
// Bổ sung vào API_ENDPOINTS hiện hữu
export const API_ENDPOINTS = {
  // ...
  ORDERS: {
    CREATE: `/${API_VERSION}/orders`,
    QUOTE_PREVIEW: `/${API_VERSION}/orders/quote-preview`,
    CUSTOMER_LIST: `/${API_VERSION}/orders/mine`,
    SUPPLIER_LIST: `/${API_VERSION}/orders/supplier`,
    DETAILS: (id) => `/${API_VERSION}/orders/${id}`,
    CANCEL: (id) => `/${API_VERSION}/orders/${id}/cancel`,
    CONFIRM: (id) => `/${API_VERSION}/orders/${id}/confirm`,
    REJECT: (id) => `/${API_VERSION}/orders/${id}/reject`,
    PROCESSING: (id) => `/${API_VERSION}/orders/${id}/processing`,
    SHIP: (id) => `/${API_VERSION}/orders/${id}/ship`,
    CONFIRM_DELIVERY: (id) => `/${API_VERSION}/orders/${id}/delivery/confirm`,
  },
  CUSTOM_PACKAGING: {
    CREATE: (orderId) => `/${API_VERSION}/orders/${orderId}/custom-packaging`,
    GET: (orderId) => `/${API_VERSION}/orders/${orderId}/custom-packaging`,
    SUBMIT_REVISION: (orderId) => `/${API_VERSION}/orders/${orderId}/custom-packaging/revisions`,
    REVIEW_REVISION: (orderId, revisionId) => `/${API_VERSION}/orders/${orderId}/custom-packaging/revisions/${revisionId}/review`,
  },
  SUPPLIER_PROMOTIONS: {
    MINE: `/${API_VERSION}/supplier/promotions`,
    CREATE: `/${API_VERSION}/supplier/promotions`,
    UPDATE: (id) => `/${API_VERSION}/supplier/promotions/${id}`,
    DEACTIVATE: (id) => `/${API_VERSION}/supplier/promotions/${id}`,
  },
};
```

### 2.2. Hiện thực `src/services/customPackagingService.js`

Tuân thủ đúng quy tắc: 2 spaces, không dùng `var`, mock fallback pattern:

```javascript
import apiClient from './apiClient';
import { API_ENDPOINTS } from '../constants/apiEndpoints';

const unwrap = (response) => response?.data ?? response;

export const customPackagingService = {
  async createPackaging(orderId, request) {
    return unwrap(await apiClient.post(API_ENDPOINTS.CUSTOM_PACKAGING.CREATE(orderId), request));
  },

  async getPackaging(orderId) {
    return unwrap(await apiClient.get(API_ENDPOINTS.CUSTOM_PACKAGING.GET(orderId)));
  },

  async submitRevision(orderId, request) {
    return unwrap(await apiClient.post(API_ENDPOINTS.CUSTOM_PACKAGING.SUBMIT_REVISION(orderId), request));
  },

  async reviewRevision(orderId, revisionId, action, notes) {
    return unwrap(await apiClient.post(
      API_ENDPOINTS.CUSTOM_PACKAGING.REVIEW_REVISION(orderId, revisionId),
      { action, notes }
    ));
  },
};

export default customPackagingService;
```

---

## III. THIẾT KẾ GIAO DIỆN & TRẢI NGHIỆM NGƯỜI DÙNG (UI/UX)

### 3.1. Màn hình Đặt hàng Doanh nghiệp (`OrderAndDesgin.jsx`)

1. **Bộ chọn Biến thể & Số lượng:**
   - Người dùng bắt buộc chọn quy cách bánh (Ví dụ: `Bánh nướng Thập Cẩm Bát Bửu 2 Trứng 200g`).
   - Ô nhập số lượng tích hợp cơ chế **Debounce 300ms** tự động gọi `POST /orders/quote-preview` để cập nhật số tiền chiết khấu bậc thang.
2. **Khu vực Tùy biến Bao bì In Logo:**
   - Công tắc kích hoạt: "Yêu cầu in logo & đóng gói quà tặng doanh nghiệp".
   - Tải file logo vector với xem trước ảnh tức thời.
   - Chọn loại hộp: Hộp Sơn Mài Thượng Hạng (4 bánh), Hộp VIP Dạ Nguyệt (6 bánh + Trà), Hộp Giấy Mỹ Thuật Mộc.
   - Nhập lời chúc in ép kim lên nắp hộp (hỗ trợ đếm ký tự 0/200).
3. **Thẻ Tóm tắt Chi phí (Luxury Order Summary):**
   - Đơn giá gốc: `110.000 ₫`
   - Số lượng: `350 hộp`
   - Tổng giá trị thô: `38.500.000 ₫`
   - Chiết khấu bậc thang (Bậc 100-499: -10%): `-3.850.000 ₫` (Màu xanh Emerald)
   - Phụ phí gia công hộp sơn mài: `+5.250.000 ₫`
   - **Tổng thanh toán:** `39.900.000 ₫` (Màu vàng Luxury Gold rực rỡ, chữ số to đậm).

### 3.2. Màn hình Quản trị Đơn hàng Xưởng Bánh (`OrderManage.jsx`)

1. **Bảng Danh sách Đơn hàng:**
   - Cột thông tin: Mã đơn (`MC-2026...`), Doanh nghiệp đặt hàng, Tên sản phẩm & Biến thể, Số lượng, Tổng tiền, Trạng thái đơn, Trạng thái bao bì.
   - Nút hành động theo trạng thái:
     - `Pending`: Nút "Xác nhận đơn" (Xanh lục) & "Từ chối" (Đỏ cam).
     - `Confirmed`: Nút "Bắt đầu sản xuất" (Xanh dương).
     - `Processing`: Nút "Giao hàng" (Tím).
2. **Modal Duyệt & Tải Mockup Bao bì (`MockupReviewModal`):**
   - Hiển thị logo công ty của khách hàng và lời chúc yêu cầu.
   - Nút tải lên file phối cảnh 3D (PNG/PDF).
   - Ô nhập phí khuôn in/thiết kế (nếu có).
   - Lịch sử các vòng chỉnh sửa (Revision Timeline): Rev 1 (Khách góp ý) -> Rev 2 (Xưởng cập nhật) -> Rev 3 (Khách đã duyệt).

---

## IV. BẢO MẬT & XỬ LÝ LỖI TRÊN GIAO DIỆN (ERROR HANDLING)

1. **Hiển thị thông điệp thân thiện:**
   - Ánh xạ mã lỗi máy tính từ backend sang tiếng Việt chuẩn mực:
     - `ORDER_INSUFFICIENT_STOCK` -> *"Số lượng bánh trong xưởng tạm thời không đủ đáp ứng. Vui lòng giảm số lượng hoặc liên hệ trực tiếp xưởng."*
     - `ORDER_BELOW_MIN_QTY` -> *"Số lượng tối thiểu cho đơn hàng doanh nghiệp là {min} hộp."*
2. **Khóa nút bấm ngăn chặn gửi lặp (Spam Prevention):**
   - Toàn bộ các nút submit hành động quan trọng đều được gắn cờ `loading={isSubmitting}` và vô hiệu hóa (`disabled`) trong suốt quá trình chờ backend phản hồi.
3. **Cơ chế Fallback mượt mà:**
   - Khi cờ `VITE_ENABLE_MOCK_FALLBACK=true`, nếu backend đang bảo trì, hệ thống tự động sinh dữ liệu mock chuẩn để nhà phát triển tiếp tục hoàn thiện giao diện mà không bị crash ứng dụng.

