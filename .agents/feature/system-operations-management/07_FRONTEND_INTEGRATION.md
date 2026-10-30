# [FRONTEND-REACT] TÍCH HỢP TẦNG GIAO DIỆN REACT 19, ADMIN DASHBOARD & MOCK FALLBACK
## Tài Liệu Thiết Kế Chi Tiết (Detail Design Document - Part 07)

---

## I. TỔNG QUAN VÀ MỤC TIÊU TRIỂN KHAI FRONTEND

Tầng giao diện người dùng của **Module 11 (Features #26, #27, #28)** được tích hợp đồng bộ trong ứng dụng `mooncake-fe` (React 19, Vite, Ant Design v6, Tailwind CSS). Toàn bộ mã nguồn tuân thủ nghiêm ngặt các quy định tại `.agents/AGENTS.md`:
- **Định dạng:** 2 spaces indentation cho tất cả các file (`.js`, `.jsx`, `.css`, `.json`). Tuyệt đối không dùng ký tự Tab.
- **Dấu nháy:** Nháy đơn `'...'` cho file JS/JSX, nháy kép `"..."` cho thuộc tính JSX (`className="..."`).
- **Phong cách Luxury:** Tone màu chủ đạo Hoàng Gia Á Đông (Dark Stone `#1C1917`, Amber Gold `#D97706` / `#F59E0B`, Cream Silk `#FEF3C7`).
- **Mock Fallback Pattern:** Cung cấp dữ liệu mẫu dự phòng khi cờ `VITE_ENABLE_MOCK_FALLBACK=true`, đảm bảo giao diện hoạt động độc lập và kiểm thử tự động hóa E2E không phụ thuộc mạng.

---

## II. BỔ SUNG ENDPOINTS & SERVICES

### 2.1. Cập Nhật `src/constants/apiEndpoints.js`
```javascript
// src/constants/apiEndpoints.js
export const NOTIFICATION_ENDPOINTS = {
  LIST: '/api/v1/notifications',
  UNREAD_COUNT: '/api/v1/notifications/unread-count',
  MARK_READ: (id) => `/api/v1/notifications/${id}/read`,
  MARK_ALL_READ: '/api/v1/notifications/read-all',
  PREFERENCES: '/api/v1/notifications/preferences',
  HUB_URL: '/api/v1/hubs/notifications',
};

export const ADMIN_ENDPOINTS = {
  USERS: '/api/v1/admin/users',
  USER_STATUS: (id) => `/api/v1/admin/users/${id}/status`,
  SHOPS: '/api/v1/admin/shops',
  SUSPEND_SHOP: (id) => `/api/v1/admin/shops/${id}/suspend`,
  REACTIVATE_SHOP: (id) => `/api/v1/admin/shops/${id}/reactivate`,
  DISPUTES: '/api/v1/admin/disputes',
  DISPUTE_DETAIL: (id) => `/api/v1/admin/disputes/${id}`,
  RESOLVE_DISPUTE: (id) => `/api/v1/admin/disputes/${id}/resolve`,
  CONFIGS: '/api/v1/admin/configs',
};

export const REPORT_ENDPOINTS = {
  DASHBOARD_METRICS: '/api/v1/reports/dashboard-metrics',
  CONVERSION_FUNNEL: '/api/v1/reports/conversion-funnel',
  TOP_SUPPLIERS: '/api/v1/reports/top-suppliers',
  AI_SUMMARY: '/api/v1/reports/ai-summary',
  EXPORT: '/api/v1/reports/export',
};
```

### 2.2. Xây Dựng `src/services/adminService.js` – Quản Trị Tranh Chấp & Cấu Hình Sàn
```javascript
import apiClient from './apiClient.js';
import { ADMIN_ENDPOINTS } from '../constants/apiEndpoints.js';

const MOCK_DISPUTES = [
  {
    id: 'disp-001',
    contractId: 'ctr-001',
    contractNumber: 'CTR-2026-00012',
    customerName: 'Doanh Nghiệp Hoa Mai',
    supplierName: 'Xưởng Bánh Tràng An Nghệ Nhân',
    disputeReason: 'Xưởng giao bánh chậm 7 ngày so với cam kết mốc M2, ảnh PoD xác nhận trễ hạn.',
    evidenceUrls: ['https://storage.nguyetpho.vn/evidence/pod_late_01.jpg'],
    status: 'Pending',
    heldEscrowAmount: 120000000,
    raisedAt: '2026-10-29T08:30:00Z',
  },
  {
    id: 'disp-002',
    contractId: 'ctr-002',
    contractNumber: 'CTR-2026-00018',
    customerName: 'Tập Đoàn Công Nghệ FPT',
    supplierName: 'Bảo Phương Gia Truyền',
    disputeReason: 'Bao bì in logo công ty bị sai màu nhận diện thương hiệu.',
    evidenceUrls: ['https://storage.nguyetpho.vn/evidence/mockup_diff_02.jpg'],
    status: 'Resolved',
    resolutionType: 'MutualAgreement',
    adminVerdict: 'Xưởng đồng ý in lại 200 vỏ hộp và hỗ trợ giảm 15 triệu đồng.',
    refundAmount: 15000000,
    heldEscrowAmount: 250000000,
    raisedAt: '2026-10-28T14:15:00Z',
    resolvedAt: '2026-10-29T09:00:00Z',
  },
];

export const adminService = {
  async getDisputes(params = {}) {
    try {
      const res = await apiClient.get(ADMIN_ENDPOINTS.DISPUTES, { params });
      return res.data;
    } catch (err) {
      if (import.meta.env.VITE_ENABLE_MOCK_FALLBACK === 'true') {
        return { status: 200, data: { items: MOCK_DISPUTES, totalItems: MOCK_DISPUTES.length } };
      }
      throw err;
    }
  },

  async resolveDispute(id, payload) {
    try {
      const res = await apiClient.post(ADMIN_ENDPOINTS.RESOLVE_DISPUTE(id), payload);
      return res.data;
    } catch (err) {
      if (import.meta.env.VITE_ENABLE_MOCK_FALLBACK === 'true') {
        const item = MOCK_DISPUTES.find((d) => d.id === id);
        if (item) {
          item.status = 'Resolved';
          item.resolutionType = payload.resolutionType;
          item.adminVerdict = payload.adminVerdict;
          item.refundAmount = payload.refundAmount;
          item.resolvedAt = new Date().toISOString();
        }
        return { status: 200, message: 'Phân xử tranh chấp thành công.' };
      }
      throw err;
    }
  },

  async getSystemConfigs() {
    try {
      const res = await apiClient.get(ADMIN_ENDPOINTS.CONFIGS);
      return res.data;
    } catch (err) {
      if (import.meta.env.VITE_ENABLE_MOCK_FALLBACK === 'true') {
        return {
          status: 200,
          data: {
            COD_MAX_AMOUNT: 10000000,
            CONTRACT_DEFAULT_DEPOSIT_PERCENT: 30,
            PLATFORM_COMMISSION_PERCENT: 3.0,
          },
        };
      }
      throw err;
    }
  },
};
```

---

## III. NÂNG CẤP ADMIN DASHBOARD (`src/Pages/Admin/DashBoard.jsx`)

Mở rộng giao diện `DashBoard.jsx` với bộ tabs vận hành toàn diện:
1. **Tab 1: 📊 Tổng Quan & Phễu Chuyển Đổi RFQ (`conversion-funnel`):** Hiển thị 5 bước chuyển đổi kèm tỷ lệ thành công của các hồ sơ chào thầu.
2. **Tab 2: ⚖️ Trọng Tài Tranh Chấp SafeTrade (`disputes`):** Bảng quản lý các vụ khiếu nại đang chờ xử lý, nút mở `DisputeResolutionModal`.
3. **Tab 3: 🛡️ Sổ Giám Sát Ký Quỹ Escrow & Hoàn Cọc:** Kế thừa bảng Escrow Ledger và modal hoàn tiền cọc Admin.
4. **Tab 4: ⚙️ Cấu Hình Sàn Động (`system-configs`):** Điều chỉnh nhanh hạn mức COD và % đặt cọc mốc M1.
5. **Tab 5: ✨ Báo Cáo AI Analytics (Gemini AI Insights):** Nhúng `AiAnalyticsPanel` kết nối Gemini API.

```jsx
// Cấu trúc Tab Tranh Chấp & Phân Xử trong DashBoard.jsx
<Tabs
  activeKey={activeTab}
  onChange={setActiveTab}
  items={[
    {
      key: 'campaign',
      label: '📈 Doanh Số & Top Nhà Cung Cấp',
      children: renderCampaignTab(),
    },
    {
      key: 'funnel',
      label: '📊 Phễu Chuyển Đổi RFQ & Báo Cáo',
      children: renderFunnelReportTab(),
    },
    {
      key: 'disputes',
      label: (
        <span>
          ⚖️ Tranh Chấp SafeTrade{' '}
          {pendingDisputesCount > 0 && <Badge count={pendingDisputesCount} size="small" />}
        </span>
      ),
      children: renderDisputesTab(),
    },
    {
      key: 'escrow',
      label: '🛡️ Sổ Giám Sát Ký Quỹ Escrow',
      children: renderEscrowTab(),
    },
    {
      key: 'configs',
      label: '⚙️ Cấu Hình Sàn Động',
      children: renderConfigsTab(),
    },
    {
      key: 'ai-insights',
      label: '✨ Báo Cáo AI Analytics',
      children: <AiAnalyticsPanel role="Admin" />,
    },
  ]}
/>
```

---

## IV. BỘ MODAL QUẢN TRỊ TRỌNG TÀI (`DisputeResolutionModal.jsx`)

Component `src/components/admin/DisputeResolutionModal.jsx`:
- Hiển thị tóm tắt vụ việc, tên Khách hàng, tên Nhà cung cấp, số dư Ký quỹ Escrow đang giữ.
- Khu vực xem trước ảnh chứng từ PoD bàn giao.
- Form chọn kết luận phán quyết:
  - Radio lựa chọn: *Lỗi do Nhà cung cấp / Lỗi do Khách hàng / Thỏa thuận hòa giải*.
  - Ô nhập lời phán quyết chính thức của Admin (`AdminVerdict`).
  - Ô nhập số tiền hoàn cọc (`RefundAmount`), có cảnh báo nếu vượt quá số dư ký quỹ.
  - Ô chọn trừ điểm uy tín (`-20 điểm`).
- Nút *"Xác Nhận Phán Quyết & Hoàn Tiền"* phong cách đỏ quý phái (Luxury Crimson Danger).

---

## V. TÍCH HỢP TRUNG TÂM THÔNG BÁO HOÀNG GIA TRÊN NAVBAR CHUNG

Component `NotificationBell.jsx` được tích hợp vào thanh điều hướng chính (`ColonyAdminLayout.jsx` và Top Navigation của Customer / Supplier):
- Hiển thị icon `BellOutlined` với `Badge` viền vàng hổ phách.
- Khi có thông báo mới đẩy qua SignalR, tự động kích hoạt `playLuxuryChime()` và mở Drawer xem chi tiết.
- Trên màn hình di động, Drawer tự động trượt từ đáy màn hình lên thành Bottom Sheet.

