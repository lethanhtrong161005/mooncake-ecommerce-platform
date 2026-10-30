# TÍNH NĂNG #23 & #24: TÍCH HỢP GIAO DIỆN REACT 19 & LUXURY DESIGN SYSTEM
## TÀI LIỆU THIẾT KẾ CHI TIẾT (FRONTEND INTEGRATION SPECIFICATION)

---

## 1. CẤU TRÚC TỆP TIN FRONTEND ĐƯỢC TỔ CHỨC MỚI

Mọi thành phần giao diện mới được đặt đúng tầng trách nhiệm theo kiến trúc chuẩn:

```text
mooncake-fe/src/
├── constants/
│   └── apiEndpoints.js            # Khai báo các endpoints AI tập trung
├── services/
│   └── aiService.js               # Service gọi API qua apiClient kèm Mock Fallback
├── components/
│   └── ai/
│       ├── AiChatWidget.jsx       # Nút nổi Chatbot góc dưới phải (Lucian Pulse)
│       ├── AiChatDrawer.jsx       # Cửa sổ chat Drawer / Bottom-sheet sang trọng
│       ├── AiProductCard.jsx      # Thẻ sản phẩm gợi ý tương tác trong khung chat
│       ├── AiAnalyticsPanel.jsx   # Khối phân tích AI cho Admin & Supplier Dashboard
│       └── AiRiskBadge.jsx        # Huy hiệu mức độ nghiêm trọng rủi ro (High/Medium/Low)
└── Pages/
    ├── Admin/
    │   └── DashBoard.jsx          # Tích hợp tab "Phân Tích AI" cho Ban Quản Trị
    └── Supplier/
        └── ShopInformationPage.jsx # Tích hợp tab "Trợ Lý Bán Hàng AI" cho Chủ Xưởng
```

---

## 2. ĐẶC TẢ TẦNG SERVICE (`aiService.js`) VỚI MOCK FALLBACK PATTERN

Tập tin `src/services/aiService.js` sử dụng `apiClient.js` và tự động kích hoạt Mock Fallback khi biến môi trường `VITE_ENABLE_MOCK_FALLBACK=true` hoặc máy chủ Backend chưa bật:

```javascript
import apiClient from './apiClient';
import { API_ENDPOINTS } from '../constants/apiEndpoints';

const isMock = import.meta.env.VITE_ENABLE_MOCK_FALLBACK === 'true';

export const aiService = {
  // 1. Khởi tạo / tiếp tục phiên chat
  async startSession(payload = {}) {
    try {
      const res = await apiClient.post(API_ENDPOINTS.AI.CHAT_SESSIONS, payload);
      return res.data?.data || res.data;
    } catch (error) {
      if (isMock || !error.response) {
        return {
          conversationId: 'mock-conv-001',
          sessionToken: payload.existingSessionToken || `mock-token-${Date.now()}`,
          conversationType: payload.conversationType || 'General',
          createdAt: new Date().toISOString(),
        };
      }
      throw error;
    }
  },

  // 2. Gửi tin nhắn và nhận câu trả lời RAG từ Gemini
  async sendMessage(payload) {
    try {
      const res = await apiClient.post(API_ENDPOINTS.AI.CHAT_MESSAGES, payload);
      return res.data?.data || res.data;
    } catch (error) {
      if (isMock || !error.response) {
        return mockAiAdvisorReply(payload.content);
      }
      throw error;
    }
  },

  // 3. Lấy lịch sử hội thoại
  async getChatHistory(sessionToken) {
    try {
      const res = await apiClient.get(API_ENDPOINTS.AI.CHAT_HISTORY(sessionToken));
      return res.data?.data || res.data;
    } catch (error) {
      if (isMock || !error.response) {
        return mockChatHistory;
      }
      throw error;
    }
  },

  // 4. Sinh báo cáo phân tích bán hàng thông minh
  async generateAnalyticsReport(payload) {
    try {
      const res = await apiClient.post(API_ENDPOINTS.AI.ANALYTICS_GENERATE, payload);
      return res.data?.data || res.data;
    } catch (error) {
      if (isMock || !error.response) {
        return mockAnalyticsReport(payload.reportType);
      }
      throw error;
    }
  },
};

export default aiService;
```

---

## 3. THIẾT KẾ CÁC LUXURY UI COMPONENTS

### 3.1. `AiChatWidget.jsx` (Nút nổi tương tác thông minh)
- **Vị trí:** Neo cố định ở góc dưới bên phải màn hình (`bottom: 28px; right: 28px; z-index: 1000`).
- **Hiệu ứng thị giác (Luxury Aesthetics):**
  - Gradient màu đỏ rượu vang Lucian và vàng kim (`from-amber-800 to-stone-900`).
  - Viền phát sáng nhịp đập vàng kim huyền ảo (`shadow-lg shadow-amber-900/30 ring-2 ring-amber-400/50 animate-pulse`).
  - Badge đếm số lượng gợi ý quà tặng hoặc trạng thái sẵn sàng ("AI Trực Tuyến").

### 3.2. `AiChatDrawer.jsx` (Giao diện hội thoại sang trọng)
- **Header:** Logo Nguyệt Phố B2B, dòng trạng thái *"Trợ lý Quà tặng & Tri thức Mùa Trăng (Powered by Google Gemini Flash)"*, nút xóa lịch sử và nút thu nhỏ.
- **Dòng tin nhắn (Message Stream):**
  - Phân biệt rõ rệt bong bóng tin nhắn của Khách hàng (`bg-amber-800 text-white rounded-tr-none`) và của Trợ lý AI (`bg-white border border-stone-200 text-stone-800 rounded-tl-none shadow-xs`).
  - Render Markdown chuẩn mực (đoạn văn, in đậm thông số quan trọng, gạch đầu dòng rõ ràng).
  - Khối sản phẩm gợi ý `AiProductCard` xuất hiện nhịp nhàng dưới câu trả lời của AI.
- **Bong bóng gợi ý nhanh (Follow-up Action Chips):**
  - Thanh chip ngang: *"Set quà dưới 500k"*, *"Bánh chay ít đường"*, *"Ý nghĩa set 4 bánh Tứ Quý"*, *"Tạo RFQ cho 300 hộp"*.
  - Người dùng bấm 1 chạm là tự động gửi tin nhắn mà không cần gõ phím.

### 3.3. `AiAnalyticsPanel.jsx` (Bảng phân tích kinh doanh thời gian thực)
- **Vị trí tích hợp:** Tab mới mang tên **"Phân Tích AI & Xu Hướng Thị Trường"** nằm ngay trong màn hình Admin Dashboard ([`DashBoard.jsx`](file:///Users/admin/Desktop/PRN232/mooncake-fe/src/Pages/Admin/DashBoard.jsx)) và màn hình Quản trị Gian hàng của Supplier.
- **Khối Tóm tắt Điều hành (Executive Summary Card):**
  - Nền vàng nhạt quý tộc (`bg-gradient-to-r from-amber-50 to-stone-50 border border-amber-200`).
  - Huy hiệu AI lấp lánh (`ThunderboltFilled className="text-amber-600"`).
- **Khối Cảnh báo Rủi ro & Kiến nghị Chiến lược (Risk & Action Matrix):**
  - Render dạng Grid 2 cột: Cột trái cảnh báo trễ hạn giao PoD / thiếu nguyên vật liệu; Cột phải đề xuất hành động tăng trưởng doanh số thầu RFQ.
- **Nút "Xuất Báo Cáo PDF / In Ấn":** Kích hoạt hộp thoại in ấn trình duyệt với chuẩn layout A4 không viền menu.

---

## 4. QUY CHUẨN MÃ NGUỒN FRONTEND
1. **Thụt lề:** Bắt buộc 2 spaces cho toàn bộ các file (`.js`, `.jsx`, `.css`, `.json`).
2. **Dấu nháy:** Nháy đơn `'...'` cho chuỗi JavaScript; Nháy kép `"..."` cho thuộc tính JSX (`className="..."`).
3. **Biến:** Dùng `const` theo mặc định, chỉ dùng `let` khi reassign, tuyệt đối cấm dùng `var`.
4. **Build test:** Đảm bảo `npm run build` kết thúc với **0 error**, không có lỗi type/lint nào trước khi chuyển giao.

