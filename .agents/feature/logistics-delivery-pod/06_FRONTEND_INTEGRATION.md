# MODULE 7: TÍCH HỢP GIAO DIỆN FRONTEND (FRONTEND ARCHITECTURE & INTEGRATION)

---

## I. TỔNG QUAN KIẾN TRÚC GIAO DIỆN REACT 19

Frontend của Module 7 tuân thủ kiến trúc phân tầng doanh nghiệp (Enterprise Layered Architecture) và Design System Luxury Gold/Deep Stone của dự án `mooncake-fe`:

```
mooncake-fe/src/
├── constants/
│   └── apiEndpoints.js            # Bổ sung endpoints DELIVERIES & PROOFS
├── services/
│   └── deliveryService.js         # Service tầng mạng qua apiClient + Mock fallback
├── components/
│   └── delivery/
│       ├── SignatureCanvas.jsx    # Bảng vẽ cảm ứng ký số (Touch/Mouse)
│       ├── PoDViewerModal.jsx     # Modal đối soát ảnh PoD, chữ ký & GPS
│       └── CreateDeliveryModal.jsx# Modal chia đợt & tạo chuyến giao mới
└── Pages/
    ├── Customer/
    │   └── DeliveryPoD.jsx        # Nâng cấp trang Khách hàng theo dõi lộ trình & PoD
    ├── Supplier/
    │   └── DeliverManage.jsx      # Nâng cấp trang NCC quản lý chuyến & xếp xe
    └── Delivery/
        └── DriverMobileView.jsx   # Giao diện Responsive Mobile-First cho Tài xế
```

---

## II. ĐẶC TẢ TẦNG HẰNG SỐ & SERVICE (CONSTANTS & NETWORK LAYER)

### 2.1. Cập nhật `src/constants/apiEndpoints.js`

```javascript
  // Logistics & Proof of Delivery (PoD)
  DELIVERIES: {
    LIST: `/${API_VERSION}/deliveries`,
    DETAILS: (id) => `/${API_VERSION}/deliveries/${id}`,
    CREATE: `/${API_VERSION}/deliveries`,
    UPDATE_STATUS: (id) => `/${API_VERSION}/deliveries/${id}/status`,
    UPDATE_CARRIER: (id) => `/${API_VERSION}/deliveries/${id}/carrier-info`,
    BY_CONTRACT: (contractId) => `/${API_VERSION}/deliveries/contract/${contractId}`,
    BY_ORDER: (orderId) => `/${API_VERSION}/deliveries/order/${orderId}`,
    PROOFS: (deliveryId) => `/${API_VERSION}/deliveries/${deliveryId}/proofs`,
    PROOF_DETAILS: (proofId) => `/${API_VERSION}/deliveries/proofs/${proofId}`,
    VERIFY_PROOF: (proofId) => `/${API_VERSION}/deliveries/proofs/${proofId}/verify`,
  },
```

### 2.2. Hiện thực `src/services/deliveryService.js`

```javascript
import apiClient from './apiClient';
import API_ENDPOINTS from '../constants/apiEndpoints';

const shouldFallbackToMock = () =>
  import.meta.env.VITE_ENABLE_MOCK_FALLBACK === 'true';

export const deliveryService = {
  // Lấy danh sách chuyến giao hàng
  async getDeliveries(params = {}) {
    try {
      const response = await apiClient.get(API_ENDPOINTS.DELIVERIES.LIST, { params });
      return response.data?.data ?? response.data;
    } catch (error) {
      if (shouldFallbackToMock()) {
        return getMockDeliveries();
      }
      throw error;
    }
  },

  // Chi tiết chuyến giao hàng
  async getDeliveryById(id) {
    try {
      const response = await apiClient.get(API_ENDPOINTS.DELIVERIES.DETAILS(id));
      return response.data?.data ?? response.data;
    } catch (error) {
      if (shouldFallbackToMock()) {
        return getMockDeliveryDetail(id);
      }
      throw error;
    }
  },

  // Tạo chuyến giao hàng mới (Chia đợt từ Contract hoặc Order)
  async createDelivery(payload) {
    try {
      const response = await apiClient.post(API_ENDPOINTS.DELIVERIES.CREATE, payload);
      return response.data?.data ?? response.data;
    } catch (error) {
      if (shouldFallbackToMock()) {
        return { id: 'mock-deliv-' + Date.now(), ...payload, status: 'Pending' };
      }
      throw error;
    }
  },

  // Cập nhật trạng thái chuyến giao
  async updateStatus(id, payload) {
    try {
      const response = await apiClient.put(API_ENDPOINTS.DELIVERIES.UPDATE_STATUS(id), payload);
      return response.data?.data ?? response.data;
    } catch (error) {
      if (shouldFallbackToMock()) {
        return { id, ...payload };
      }
      throw error;
    }
  },

  // Nộp bằng chứng PoD (Multipart FormData gồm ảnh, chữ ký, GPS)
  async submitDeliveryProof(deliveryId, formData) {
    try {
      const response = await apiClient.post(
        API_ENDPOINTS.DELIVERIES.PROOFS(deliveryId),
        formData,
        {
          headers: { 'Content-Type': 'multipart/form-data' },
        }
      );
      return response.data?.data ?? response.data;
    } catch (error) {
      if (shouldFallbackToMock()) {
        return { id: 'mock-proof-' + Date.now(), deliveryId, isVerified: true };
      }
      throw error;
    }
  },

  // Lấy danh sách bằng chứng của chuyến giao
  async getDeliveryProofs(deliveryId) {
    try {
      const response = await apiClient.get(API_ENDPOINTS.DELIVERIES.PROOFS(deliveryId));
      return response.data?.data ?? response.data;
    } catch (error) {
      if (shouldFallbackToMock()) {
        return getMockProofs(deliveryId);
      }
      throw error;
    }
  },
};

export default deliveryService;
```

---

## III. NÂNG CẤP VÀ XÂY DỰNG MÀN HÌNH GIAO DIỆN (UI SCREENS)

### 3.1. Trang Khách hàng: `src/Pages/Customer/DeliveryPoD.jsx`
- **Mục tiêu:** Cho phép doanh nghiệp mua hàng theo dõi tiến độ giao các đợt bánh, xem ảnh chụp thực tế tại điểm giao và kiểm tra chữ ký của thủ kho trước khi ký biên bản nghiệm thu.
- **Tính năng nổi bật:**
  - Bộ lọc trạng thái trực quan: `Tất cả`, `Đang vận chuyển`, `Đã bàn giao`, `Có vấn đề`.
  - Timeline lộ trình thời gian thực: Xuất kho xưởng ➔ Đang vận chuyển ➔ Đến điểm giao ➔ Bàn giao thành công.
  - Nút "Xem Bằng Chứng PoD" mở modal hiển thị ảnh chụp độ nét cao, chữ ký số và tọa độ GPS đối soát.
  - Nút hành động nhanh: "Nghiệm thu đợt hàng" chuyển tiếp trực tiếp sang màn hình Hợp đồng & Tiến độ (`/#hop-dong-tien-do`).

### 3.2. Trang Nhà cung cấp: `src/Pages/Supplier/DeliverManage.jsx`
- **Mục tiêu:** Bảng điều khiển trung tâm giúp Xưởng bánh quản lý đội xe nội bộ (`SelfArranged`) và đơn vị chuyển phát (`PlatformManaged`).
- **Tính năng nổi bật:**
  - Nút "Tạo chuyến giao hàng mới": Hỗ trợ chia nhỏ lô hàng theo hợp đồng (Split Shipment) với ngày dự kiến và thông tin xe.
  - Cập nhật nhanh thông tin vận đơn: Sửa mã vận đơn, SĐT tài xế, biển số xe tải chuyên dụng.
  - Đổi trạng thái một chạm: `Bốc hàng tại xưởng` ➔ `Xe lăn bánh (In-Transit)` ➔ `Xem bằng chứng bàn giao`.
  - Cảnh báo các chuyến trễ hạn so với ngày dự kiến `ScheduledDate`.

### 3.3. Trang Tài xế Hiện trường: `src/Pages/Delivery/DriverMobileView.jsx`
- **Mục tiêu:** Màn hình Web Mobile-first chuyên dụng dành cho tài xế/shipper mở trên smartphone tại điểm giao hàng.
- **Thiết kế Responsive Mobile:**
  - Viewport full màn hình điện thoại (375px - 430px).
  - Tích hợp thành phần `SignatureCanvas.jsx`: Cho phép thủ kho dùng ngón tay ký tên mượt mà trên màn hình cảm ứng, có nút "Xóa ký lại".
  - Chụp ảnh trực tiếp từ camera sau điện thoại thông qua HTML5 `<input type="file" accept="image/*" capture="environment" />`.
  - Nút tự động lấy tọa độ GPS từ trình duyệt bằng `navigator.geolocation`.
  - Lưu trữ ngoại tuyến khi mất sóng 4G và tự động đẩy lên máy chủ khi có mạng trở lại.

---

## IV. BẢO ĐẢM TÍNH TƯƠNG THÍCH VÀ ĐIỀU HƯỚNG (NAVIGATION & ROUTING)

- Cập nhật `src/constants/shopRoutes.js`:
  - Tuyến khách hàng: `#giao-hang-pod` ➔ `DeliveryPoD.jsx`.
  - Tuyến xưởng bánh: `#van-chuyen-pod` ➔ `DeliverManage.jsx`.
  - Tuyến tài xế di động: `#driver-delivery-view` ➔ `DriverMobileView.jsx`.
- Bảo toàn tương thích ngược với luồng hợp đồng: Nút "Xem đơn giao hàng" trong `ContractsAndMilestone.jsx` tự động điều hướng sang `#giao-hang-pod?contractId=...`.

