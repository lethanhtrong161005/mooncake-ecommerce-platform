# [RFQ-BIDDING-07] - THIẾT KẾ TÍCH HỢP FRONTEND (REACT 19 LAYERED ARCHITECTURE)

---

## I. TỔNG QUAN KIẾN TRÚC FRONTEND (`mooncake-fe`)

Frontend của Module 5 được xây dựng trên nền tảng **React 19**, **Vite**, **Ant Design 6** và **Tailwind CSS**, tuân thủ nghiêm ngặt quy tắc kiến trúc phân tầng (Layered Architecture) và phong cách nhận diện Luxury Gold / Amber.

```text
src/
├── constants/
│   ├── apiEndpoints.js            # Khai báo endpoint RFQ, BIDS, VNPAY
│   └── routes.js                  # RFQ_BIDDING, SUPPLIER_BIDDING, CONTRACTS
├── services/
│   ├── apiClient.js               # Axios client tập trung
│   ├── rfqService.js              # Gọi API tạo RFQ, mời xưởng, nộp bid, chọn thầu
│   └── paymentService.js          # Gọi API sinh URL thanh toán VNPay
├── hooks/
│   ├── useRfqManagement.js        # Hook quản lý danh sách RFQ & trạng thái
│   └── useBidComparison.js       # Hook tính toán ma trận so sánh thầu Side-by-Side
├── components/
│   └── rfq/
│       ├── CreateRfqModal.jsx     # Modal tạo gói thầu nhiều dòng sản phẩm
│       ├── SupplierInviteModal.jsx# Modal chọn xưởng từ danh sách gợi ý thông minh
│       ├── BidComparisonTable.jsx # Bảng so sánh đa chiều Side-by-Side
│       └── SubmitBidDrawer.jsx    # Drawer xưởng nộp báo giá chi tiết từng dòng
└── Pages/
    ├── Customer/
    │   └── BiddingAndQuoteComparison.jsx # Màn hình Đấu thầu & So sánh báo giá
    └── Supplier/
        └── BiddingPage.jsx        # Màn hình Xưởng nhận lời mời & Nộp báo giá
```

---

## II. ĐẶC TẢ TẦNG DỊCH VỤ (API SERVICE LAYER)

### 2.1. Cập nhật `src/constants/apiEndpoints.js`

```javascript
export const API_ENDPOINTS = {
  // ...
  RFQ: {
    CREATE: `/${API_VERSION}/rfqs`,
    LIST: `/${API_VERSION}/rfqs/mine`,
    DETAILS: (id) => `/${API_VERSION}/rfqs/${id}`,
    PUBLISH: (id) => `/${API_VERSION}/rfqs/${id}/publish`,
    CANCEL: (id) => `/${API_VERSION}/rfqs/${id}/cancel`,
    SUGGESTED_SUPPLIERS: (id) => `/${API_VERSION}/rfqs/${id}/suggested-suppliers`,
    INVITATIONS: (id) => `/${API_VERSION}/rfqs/${id}/invitations`,
    DECLINE: (id) => `/${API_VERSION}/rfqs/${id}/decline`,
    BIDS: (id) => `/${API_VERSION}/rfqs/${id}/bids`,
    CANCEL_BID: (rfqId, bidId) => `/${API_VERSION}/rfqs/${rfqId}/bids/${bidId}/cancel`,
    ACCEPT_BID: (rfqId, bidId) => `/${API_VERSION}/rfqs/${rfqId}/bids/${bidId}/accept`,
  },
  PAYMENTS: {
    CREATE: `/${API_VERSION}/payments`,
    LIST: `/${API_VERSION}/payments/mine`,
    CONFIRM: (id) => `/${API_VERSION}/payments/${id}/confirm`,
    REFUND: (id) => `/${API_VERSION}/payments/${id}/refund`,
    VNPAY_CREATE_URL: `/${API_VERSION}/payments/vnpay/create-url`,
  },
};
```

### 2.2. Nâng cấp `src/services/paymentService.js` tích hợp VNPay

```javascript
import apiClient from './apiClient';
import { API_ENDPOINTS } from '../constants/apiEndpoints';

const unwrap = (response) => response?.data ?? response;

export const paymentService = {
  async createVnPayUrl(contractId, milestoneId, returnUrl) {
    return unwrap(await apiClient.post(API_ENDPOINTS.PAYMENTS.VNPAY_CREATE_URL, {
      contractId,
      milestoneId,
      returnUrl,
    }));
  },

  async listMine() {
    return unwrap(await apiClient.get(API_ENDPOINTS.PAYMENTS.LIST));
  },
};

export default paymentService;
```

---

## III. THIẾT KẾ GIAO DIỆN & TRẢI NGHIỆM NGƯỜI DÙNG (UI/UX)

### 3.1. Màn hình Khách hàng (`BiddingAndQuoteComparison.jsx`)

1. **Quản lý Danh sách Gói thầu RFQ:**
   - Bảng tổng quan các gói thầu: Mã RFQ (`RFQ-2026...`), Tiêu đề, Số lượng mặt hàng, Khoảng ngân sách, Hạn nộp thầu (`BidDeadline` kèm đồng hồ đếm ngược), Trạng thái (`Draft`, `Open`, `Closed`, `Awarded`).
   - Nút hành động: "Tạo RFQ mới", "Mời xưởng bánh", "Xem & So sánh báo giá".
2. **Bảng so sánh Báo giá Đa chiều (Side-by-Side Comparison):**
   - Đặt các xưởng cạnh nhau theo cột:
     - Dòng 1: Tên xưởng & Huy hiệu đối tác (Ví dụ: Đối Tác Vàng).
     - Dòng 2: Điểm uy tín (`98/100 ★★★★★`).
     - Dòng 3: Tổng giá trị báo giá (Tô đậm màu Luxury Gold, tự động gắn nhãn "Giá tốt nhất" cho xưởng chào thấp nhất).
     - Dòng 4: Thời gian giao hàng cam kết (Ví dụ: `7 ngày`).
     - Dòng 5: Chi tiết đơn giá từng loại bánh (So sánh ma trận giữa các xưởng).
     - Dòng 6: Cam kết quà tặng kèm & in logo.
     - Dòng 7: Nút hành động: **"Chấp thuận báo giá"** (Xanh lục sang trọng).
3. **Modal Thanh toán Ký quỹ VNPay:**
   - Sau khi chọn thầu và ký hợp đồng: Hiển thị popup thanh toán tiền cọc Milestone M1 (30%).
   - Nút lớn mang logo VNPay: **"Thanh toán qua cổng VNPAY-QR / Thẻ ATM & Quốc Tế"**.
   - Bấm vào sẽ mở tab hoặc chuyển hướng mượt mà sang `https://sandbox.vnpayment.vn/paymentv2/vpcpay.html`.

### 3.2. Màn hình Nhà cung cấp (`BiddingPage.jsx`)

1. **Danh sách Gói thầu được mời:**
   - Thẻ hiển thị gói thầu: Tên doanh nghiệp mời, Tiêu đề, Số lượng yêu cầu, Thời hạn còn lại, Nút "Từ chối" và "Nộp hồ sơ báo giá".
2. **Drawer Nộp Báo giá Chi tiết (`SubmitBidDrawer`):**
   - Danh sách các mặt hàng của RFQ kèm số lượng yêu cầu.
   - Ô nhập đơn giá cho từng mặt hàng (`InputNumber` tự động định dạng tiền tệ VNĐ).
   - Tự động cộng tổng giá trị `TotalPrice` theo thời gian thực.
   - Nhập số ngày giao hàng cam kết (`EstimatedDeliveryDays`) và thời hạn hiệu lực báo giá (`ValidityDays`).
   - Ô nhập cam kết phụ (Miễn phí in logo, tặng trà, xe lạnh giao tận nơi).

---

## IV. BẢO MẬT & XỬ LÝ LỖI TRÊN GIAO DIỆN (ERROR HANDLING)

1. **Ngăn chặn Double Click khi Chọn thầu:**
   - Nút "Chấp thuận báo giá" lập tức hiển thị `loading={isAwarding}` và khóa toàn bộ các nút khác để tránh gửi trùng request.
2. **Cơ chế Fallback mượt mà:**
   - Khi cờ `VITE_ENABLE_MOCK_FALLBACK=true`, hệ thống cung cấp mock data phong phú để demo luồng chọn thầu và so sánh báo giá mà không phụ thuộc backend.

