# [M10-F25.7] TÍCH HỢP FRONTEND: REACT 19, TAILWIND & VNPAY SANDBOX

---

## I. KIẾN TRÚC PHÂN TẦNG FRONTEND (`mooncake-fe`)

```text
src/
├── constants/
│   └── apiEndpoints.js            # Khai báo PAYMENTS endpoints tập trung
├── services/
│   └── paymentService.js          # Gọi API qua apiClient, tích hợp Mock Fallback Pattern
├── components/
│   └── payment/
│       ├── PaymentCheckoutModal.jsx # Modal/Drawer thanh toán chọn phương thức (VNPay, VietQR, COD)
│       ├── VietQrModal.jsx          # Modal hiển thị mã VietQR kèm nút sao chép 1-chạm & upload UNC
│       ├── PaymentResultView.jsx    # Màn hình kết quả & biên lai thanh toán (E-Receipt printable)
│       └── PaymentHistoryTable.jsx  # Bảng lịch sử giao dịch & lọc trạng thái
└── Pages/
    ├── Customer/
    │   ├── OrderAndDesgin.jsx       # Tích hợp nút thanh toán đơn hàng trực tiếp
    │   └── ContractsAndMilestone.jsx # Tích hợp thanh toán mốc hợp đồng & bảo chứng Escrow
    └── Admin/
        └── DashBoard.jsx            # Tích hợp Sổ giám sát Ký quỹ Escrow & duyệt UNC/Hoàn tiền
```

---

## II. THIẾT KẾ SERVICE VÀ MOCK FALLBACK PATTERN

### 2.1. `src/services/paymentService.js`

```javascript
import apiClient from './apiClient';
import { API_ENDPOINTS } from '../constants/apiEndpoints';

const unwrap = (response) => response?.data ?? response;

export const paymentService = {
  // 1. Tạo liên kết VNPay Sandbox cho Đơn hàng
  async createOrderVnPayUrl(orderId, returnUrl) {
    try {
      const res = await apiClient.post(API_ENDPOINTS.PAYMENTS.ORDER_VNPAY_URL, {
        orderId,
        returnUrl: returnUrl || `${window.location.origin}/#/thanh-toan/ket-qua`,
      });
      return unwrap(res);
    } catch (error) {
      if (import.meta.env.VITE_ENABLE_MOCK_FALLBACK === 'true') {
        return {
          paymentId: `mock-pay-${Date.now()}`,
          paymentNumber: `PAY-MOCK-${Date.now()}`,
          amount: 15000000,
          paymentUrl: 'https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?mock=true',
        };
      }
      throw error;
    }
  },

  // 2. Tạo liên kết VNPay Sandbox cho Mốc Hợp đồng
  async createContractVnPayUrl(contractId, milestoneId, returnUrl) {
    try {
      const res = await apiClient.post(API_ENDPOINTS.PAYMENTS.CONTRACT_VNPAY_URL, {
        contractId,
        milestoneId,
        returnUrl: returnUrl || `${window.location.origin}/#/thanh-toan/ket-qua`,
      });
      return unwrap(res);
    } catch (error) {
      if (import.meta.env.VITE_ENABLE_MOCK_FALLBACK === 'true') {
        return {
          paymentId: `mock-contract-pay-${Date.now()}`,
          paymentNumber: `PAY-MOCK-CTR-${Date.now()}`,
          amount: 45000000,
          paymentUrl: 'https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?mock=true',
        };
      }
      throw error;
    }
  },

  // 3. Khởi tạo chuyển khoản VietQR
  async createBankTransfer(payload) {
    try {
      const res = await apiClient.post(API_ENDPOINTS.PAYMENTS.BANK_TRANSFER, payload);
      return unwrap(res);
    } catch (error) {
      if (import.meta.env.VITE_ENABLE_MOCK_FALLBACK === 'true') {
        const pNum = `PAY-${new Date().toISOString().slice(0, 10).replace(/-/g, '')}-MK${Math.floor(Math.random() * 10000)}`;
        return {
          paymentId: `mock-bank-${Date.now()}`,
          paymentNumber: pNum,
          amount: payload.amount || 25000000,
          bankName: 'Ngân hàng Quân Đội (MB Bank)',
          bankBin: '970422',
          accountNumber: '0988668899',
          accountName: 'CONG TY CP NGUYET PHO B2B',
          transferMemo: `PAY ${pNum}`,
          qrCodeUrl: `https://img.vietqr.io/image/MB-0988668899-compact2.png?amount=25000000&addInfo=PAY%20${pNum}&accountName=CONG%20TY%20CP%20NGUYET%20PHO%20B2B`,
          expiresAt: new Date(Date.now() + 172800000).toISOString(),
        };
      }
      throw error;
    }
  },

  // 4. Xác thực kết quả VNPay Return Callback
  async verifyReturnCallback(queryString) {
    try {
      const res = await apiClient.get(`${API_ENDPOINTS.PAYMENTS.VNPAY_RETURN}?${queryString}`);
      return unwrap(res);
    } catch (error) {
      if (import.meta.env.VITE_ENABLE_MOCK_FALLBACK === 'true') {
        return {
          paymentNumber: 'PAY-20261029-A78F21B3',
          amount: 45000000,
          isSuccess: true,
          responseCode: '00',
          bankCode: 'NCB',
          transactionNo: '14567890',
          paidAt: new Date().toISOString(),
        };
      }
      throw error;
    }
  },

  // 5. Tải lên chứng từ UNC
  async uploadEvidence(paymentId, evidenceUrl, notes) {
    return unwrap(await apiClient.post(API_ENDPOINTS.PAYMENTS.UPLOAD_EVIDENCE(paymentId), {
      evidenceUrl,
      notes,
    }));
  },

  // 6. Lấy danh sách giao dịch của tôi
  async listMine() {
    try {
      return unwrap(await apiClient.get(API_ENDPOINTS.PAYMENTS.LIST));
    } catch (error) {
      if (import.meta.env.VITE_ENABLE_MOCK_FALLBACK === 'true') {
        return [
          {
            id: 'pay-001',
            paymentNumber: 'PAY-20261029-C81B92D1',
            amount: 45000000,
            currency: 'VND',
            method: 'VNPay',
            status: 'Completed',
            paidAt: new Date().toISOString(),
          },
        ];
      }
      throw error;
    }
  },

  // 7. Lấy báo cáo tổng hợp Escrow (Admin)
  async getEscrowSummary() {
    try {
      return unwrap(await apiClient.get(API_ENDPOINTS.PAYMENTS.ESCROW_SUMMARY));
    } catch (error) {
      if (import.meta.env.VITE_ENABLE_MOCK_FALLBACK === 'true') {
        return {
          totalEscrowBalance: 1285000000,
          totalActiveContracts: 24,
          pendingDisbursementAmount: 340000000,
          disputedEscrowAmount: 45000000,
          escrowLedgerItems: [],
        };
      }
      throw error;
    }
  },

  // 8. Admin xác nhận thanh toán chuyển khoản
  async confirm(paymentId, reason) {
    return unwrap(await apiClient.post(API_ENDPOINTS.PAYMENTS.CONFIRM(paymentId), { reason }));
  },

  // 9. Admin hoàn tiền
  async refund(paymentId, reason) {
    return unwrap(await apiClient.post(API_ENDPOINTS.PAYMENTS.REFUND(paymentId), { reason }));
  },
};

export default paymentService;
```

---

## III. THIẾT KẾ GIAO DIỆN COMPONENT CHI TIẾT

### 3.1. `PaymentCheckoutModal.jsx` (Modal Chọn Phương Thức Thanh Toán)
- Hỗ trợ cả 2 chế độ:
  - **Desktop:** Ant Design `Modal` bề rộng `520px`, bo tròn thanh lịch `rounded-2xl`, viền `border-amber-200`.
  - **Mobile:** Tự động co giãn thành `Drawer` kéo từ đáy màn hình (`placement="bottom"`), thân thiện với thao tác ngón cái.
- 3 lựa chọn thẻ radio có icon minh họa:
  1. **VNPay Sandbox:** Biểu tượng thẻ ngân hàng & QR, có huy hiệu `"⚡ Tức thời - Đề xuất"`.
  2. **Chuyển khoản VietQR:** Biểu tượng ngân hàng, hiển thị mã tài khoản ký quỹ SafeTrade Escrow.
  3. **Thanh toán khi nhận hàng (COD):** Hiển thị cảnh báo giới hạn 10tr và tự động disable nếu đơn hàng vượt hạn mức hoặc có in logo thương hiệu.

### 3.2. `VietQrModal.jsx` (Modal Chi Tiết Chuyển Khoản & 1-Chạm Sao Chép)
- **Hiển thị mã QR trung tâm:** Ảnh VietQR rõ nét, kích thước 220x220px kèm logo MB Bank.
- **Thanh thông tin chuyển khoản:**
  - Ngân hàng thụ hưởng: **MB Bank (Ngân hàng Quân Đội)**
  - Tên tài khoản: **CONG TY CP NGUYET PHO B2B**
  - Số tài khoản: `0988668899` (kèm nút copy 📋)
  - Số tiền: Định dạng tiền tệ VNĐ (kèm nút copy 📋)
  - Nội dung chuyển khoản: `PAY {PaymentNumber}` (kèm nút copy 📋)
- **Bộ đếm thời gian hiệu lực:** Đếm ngược thời gian hết hạn giao dịch (mặc định 48 giờ).
- **Khu vực đính kèm UNC:** Hỗ trợ kéo thả ảnh biên lai hoặc chọn file chụp màn hình, kích hoạt nút "Tôi đã chuyển khoản xong".

### 3.3. `PaymentResultView.jsx` (Màn Hình Biên Lai Sau Thanh Toán)
- Hiển thị kết quả thanh toán từ VNPay Sandbox (Return URL) hoặc xác nhận chuyển khoản.
- Nếu thành công:
  - Header xanh ngọc / hổ phách sang trọng với dấu tích xanh `CheckCircleFilled`.
  - Bảng chi tiết hóa đơn: Số tiền, Mã giao dịch sàn, Mã giao dịch VNPay, Ngân hàng thanh toán, Thời điểm.
  - Thẻ thông báo trạng thái tiền: *"Số tiền của quý khách đã được bảo chứng an toàn tại tài khoản Ký quỹ Escrow Sàn Nguyệt Phố."*
  - Nút bấm: "Xem tiến độ hợp đồng" / "Xem danh sách đơn hàng" và "In biên lai / Tải PDF".
- Nếu thất bại/hủy:
  - Hiển thị icon cảnh báo `CloseCircleFilled`, nêu rõ nguyên nhân (Khách hàng hủy giao dịch, Thẻ chưa kích hoạt thanh toán online...) và nút "Thử thanh toán lại".

