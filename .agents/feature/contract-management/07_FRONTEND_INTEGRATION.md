# [CONTRACT-07] - THIẾT KẾ TÍCH HỢP FRONTEND (REACT 19 LAYERED ARCHITECTURE)

---

## I. TỔNG QUAN KIẾN TRÚC FRONTEND (`mooncake-fe`)

Frontend Module 6 được xây dựng trên nền tảng **React 19**, **Vite**, **Ant Design 6** và **Tailwind CSS**, tuân thủ nguyên tắc phân tầng và thẩm mỹ Luxury Gold / Deep Stone của thương hiệu Nguyệt Phố B2B.

```text
src/
├── constants/
│   ├── apiEndpoints.js            # Khai báo endpoint CONTRACTS, PAYMENTS, ADMIN
│   └── routes.js                  # HOP_DONG_TIEN_DO, NHA_CUNG_CAP_QUAN_LY_HOP_DONG
├── services/
│   ├── apiClient.js               # Axios instance tập trung với Bearer Token
│   ├── contractService.js         # Gọi API ký OTP, cập nhật mốc, xin gia hạn, nghiệm thu
│   └── paymentService.js          # Gọi API tạo checkout URL VNPay Sandbox
├── hooks/
│   ├── useContractDetails.js      # Hook tải chi tiết hợp đồng, danh sách mốc và reload
│   └── useMilestoneProgress.js    # Hook quản lý upload bằng chứng và timeline tiến độ
├── components/
│   └── contracts/
│       ├── OtpSignatureModal.jsx  # Modal nhập OTP 6 số ký hợp đồng điện tử
│       ├── MilestoneTimeline.jsx  # Biểu đồ tiến độ trực quan M1 -> M2 -> M3
│       ├── EvidenceUploadModal.jsx# Modal xưởng tải ảnh sản xuất / phiếu giao hàng
│       ├── ExtensionModal.jsx     # Modal nộp và duyệt đơn xin gia hạn tiến độ
│       └── DisputeModal.jsx       # Modal nộp đơn khiếu nại tranh chấp cọc Escrow
└── Pages/
    ├── Customer/
    │   └── ContractsAndMilestone.jsx # Màn hình Khách hàng theo dõi HĐ, ký OTP, trả cọc VNPay
    └── Supplier/
        └── ContractManage.jsx        # Màn hình Xưởng quản lý hợp đồng, ký số, nộp tiến độ
```

---

## II. ĐẶC TẢ TẦNG DỊCH VỤ (`contractService.js`)

```javascript
import apiClient from './apiClient';
import { API_ENDPOINTS } from '../constants/apiEndpoints';

const unwrap = (response) => response?.data ?? response;

export const contractService = {
  async listMine() {
    return unwrap(await apiClient.get(API_ENDPOINTS.CONTRACTS.LIST));
  },

  async get(contractId) {
    return unwrap(await apiClient.get(API_ENDPOINTS.CONTRACTS.DETAILS(contractId)));
  },

  async submitForSignature(contractId) {
    return unwrap(await apiClient.post(API_ENDPOINTS.CONTRACTS.SUBMIT_FOR_SIGNATURE(contractId), {}));
  },

  async requestSignatureOtp(contractId) {
    return unwrap(await apiClient.post(API_ENDPOINTS.CONTRACTS.SIGNATURE_OTP(contractId), {}));
  },

  async sign(contractId, otpCode, confirmTerms) {
    return unwrap(await apiClient.post(API_ENDPOINTS.CONTRACTS.SIGN(contractId), {
      otpCode,
      confirmTerms,
    }));
  },

  async submitMilestoneProgress(contractId, milestoneId, progressNotes, evidenceUrls) {
    return unwrap(await apiClient.post(
      `${API_ENDPOINTS.CONTRACTS.DETAILS(contractId)}/milestones/${milestoneId}/submit-progress`,
      { progressNotes, evidenceUrls }
    ));
  },

  async verifyMilestone(contractId, milestoneId, isApproved, rejectionReason = '') {
    return unwrap(await apiClient.post(
      `${API_ENDPOINTS.CONTRACTS.DETAILS(contractId)}/milestones/${milestoneId}/verify`,
      { isApproved, rejectionReason }
    ));
  },

  async requestExtension(contractId, proposedDueDate, reason) {
    return unwrap(await apiClient.post(
      `${API_ENDPOINTS.CONTRACTS.DETAILS(contractId)}/extension-request`,
      { proposedDueDate, reason }
    ));
  },

  async respondExtension(contractId, isApproved, responseNote = '') {
    return unwrap(await apiClient.post(
      `${API_ENDPOINTS.CONTRACTS.DETAILS(contractId)}/extension-request/respond`,
      { isApproved, responseNote }
    ));
  },

  async raiseDispute(contractId, disputeReason, evidenceUrls) {
    return unwrap(await apiClient.post(
      `${API_ENDPOINTS.CONTRACTS.DETAILS(contractId)}/dispute`,
      { disputeReason, evidenceUrls }
    ));
  },

  async requestFinalAcceptance(contractId) {
    return unwrap(await apiClient.post(
      `${API_ENDPOINTS.CONTRACTS.DETAILS(contractId)}/request-final-acceptance`,
      {}
    ));
  },

  async confirmFinalAcceptance(contractId, confirmedQualityAndQuantity, acceptanceNotes = '') {
    return unwrap(await apiClient.post(
      `${API_ENDPOINTS.CONTRACTS.DETAILS(contractId)}/confirm-final-acceptance`,
      { confirmedQualityAndQuantity, acceptanceNotes }
    ));
  },
};

export default contractService;
```

---

## III. THIẾT KẾ GIAO DIỆN & TRẢI NGHIỆM NGƯỜI DÙNG (UI/UX)

### 3.1. Màn hình Khách hàng (`ContractsAndMilestone.jsx`)

1. **Bảng tổng quan Hợp đồng:**
   - Cột Mã hợp đồng (`HD-2026-TEST01`), Tiêu đề gói thầu, Tổng giá trị (format VNĐ nổi bật).
   - Tag trạng thái thời gian thực:
     - `Draft`: Bản nháp.
     - `PendingSignature`: Chờ ký số hai bên.
     - `PendingDeposit`: **Cần nộp cọc M1** (gắn cờ cảnh báo màu Amber).
     - `Active`: Đang sản xuất & giao nhận (màu xanh lục bảo).
     - `CompletedOnTime`: Hoàn thành đúng hạn (Huy hiệu vàng).
     - `Breached` / `Disputed`: Vi phạm / Đang giải quyết tranh chấp (màu đỏ).
2. **Ký số bằng OTP (`OtpSignatureModal`):**
   - Hộp thoại sang trọng hiển thị tóm tắt điều khoản pháp lý.
   - Ô nhập mã OTP 6 số tự động format font monospace to rõ.
   - Nút bấm lớn màu xanh Navy: **"Xác nhận ký số hợp đồng điện tử"**.
3. **Thanh toán cọc VNPay Sandbox:**
   - Nút **"Thanh toán cọc M1"** mở modal xác nhận số tiền cọc (30%).
   - Tự động gọi API `paymentService.createVnPayUrl` và chuyển hướng mượt sang cổng VNPay.
4. **Biểu đồ mốc tiến độ (Milestone Steps):**
   - Thể hiện rõ các mốc M1 (Cọc), M2 (Giao hàng 50%), M3 (Nghiệm thu dứt điểm).
   - Nút xem ảnh bằng chứng xưởng đã nộp và nút **"Nghiệm thu mốc"**.

### 3.2. Màn hình Nhà cung cấp (`ContractManage.jsx`)

1. **Dashboard Hợp đồng Xưởng:**
   - Danh sách các hợp đồng xưởng đã trúng thầu.
   - Nút "Ký số hợp đồng" đại diện pháp nhân.
2. **Cập nhật Bằng chứng Tiến độ (`EvidenceUploadModal`):**
   - Cho phép xưởng kéo thả ảnh chụp thực tế xưởng sản xuất, biên bản giao hàng.
   - Nhập ghi chú chi tiết số lượng bánh đã ra lò.
3. **Xử lý Gia hạn & Hoàn tất:**
   - Nút "Xin gia hạn" mở popup chọn ngày đề xuất mới kèm ô nhập lý do sự cố bất khả kháng.
   - Khi hoàn thành 100% lô hàng: Nút **"Yêu cầu nghiệm thu tổng thể"** sáng màu vàng kim kích hoạt luồng thanh quyết toán cuối cùng.

---

## IV. BẢO MẬT GIAO DIỆN & CƠ CHẾ MOCK FALLBACK

1. **Ngăn chặn Double-Submit khi Ký OTP:**
   - Nút "Xác nhận ký" lập tức hiển thị loading spinner và disable input để ngăn việc gửi nhiều request cùng lúc.
2. **Cơ chế Fallback mượt mà:**
   - Khi cờ `VITE_ENABLE_MOCK_FALLBACK=true`, hệ thống cung cấp dữ liệu mock đầy đủ của hợp đồng `HD-2026-TEST01` giúp giao diện chạy độc lập khi backend đang bảo trì hoặc chưa có kết nối database.

