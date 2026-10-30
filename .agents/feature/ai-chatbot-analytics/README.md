# MODULE 9: AI CHATBOT & SALES ANALYTICS (TÍNH NĂNG #23 & #24)
## HỆ THỐNG TRỢ LÝ ẢO TƯ VẤN QUÀ TẶNG B2B, VĂN HÓA TRUNG THU & ĐỘNG CƠ PHÂN TÍCH KINH DOANH THÔNG MINH

---

## 1. TỔNG QUAN TÀI LIỆU & PHẠM VI HỆ THỐNG

| Thuộc tính | Chi tiết |
| :--- | :--- |
| **Mã Module** | `MODULE-09-AI` |
| **Tính Năng Nghiệp Vụ** | **Tính năng #23:** AI Chatbot – Tư vấn quà tặng B2B & Lịch sử bánh Trung Thu (RAG)<br/>**Tính năng #24:** AI Phân tích dữ liệu bán hàng & Khuyến nghị kinh doanh (Analytics Intelligence) |
| **Độ Ưu Tiên (Priority)** | 🟡 P2 Medium (Chiến lược gia tăng tỷ lệ chuyển đổi đơn hàng & thấu cảm khách hàng B2B) |
| **Đối Tượng Tác Động (Actors)** | **Khách hàng Doanh nghiệp (Customer):** Được tư vấn set quà theo ngân sách, khẩu vị, số lượng, giải thích văn hóa.<br/>**Nhà cung cấp (Supplier):** Nhận báo cáo phân tích doanh thu gian hàng, biến thể bán chạy, cảnh báo tồn kho.<br/>**Quản trị viên (Admin):** Nhận báo cáo tổng quan toàn sàn (GMV, tỷ lệ chốt thầu RFQ, nhà cung cấp dẫn đầu), đề xuất chiến lược vận hành. |
| **Nền Tảng AI Cốt Lõi** | **Google Gemini Flash API** (`gemini-flash-latest` / `gemini-1.5-flash`), Tích hợp RAG (Retrieval-Augmented Generation) với Live Catalog DB. |
| **Cổng Thanh Toán Liên Kết** | **VNPay Sandbox** (Theo cấu hình [`keyvnpay.text`](file:///Users/admin/Desktop/PRN232/keyvnpay.text) & `appsettings.json`) khi chuyển từ tư vấn sang đặt cọc đơn hàng. |
| **API Key AI** | Google Gemini Key được cấu hình tại [`gemini-key.text`](file:///Users/admin/Desktop/PRN232/gemini-key.text) và quản trị tập trung tại Backend `appsettings.json`. |
| **Trạng Thái** | **Thiết kế chi tiết (Detail Design Approved)** |

---

## 2. KIẾN TRÚC TỔNG THỂ HỆ THỐNG (ARCHITECTURE OVERVIEW)

```mermaid
flowchart TD
  subgraph Frontend["Frontend (mooncake-fe / React 19)"]
    Widget["Floating AI Chat Widget<br/>(Lucian Gold Pulse)"]
    ChatDrawer["AI Chat Drawer & Modal<br/>(Responsive Desktop & Mobile)"]
    AdminDash["Admin AI Analytics Panel<br/>(Toàn sàn GMV & Top Supplier)"]
    SupplierDash["Supplier AI Advisor Panel<br/>(Doanh thu & Cảnh báo tồn kho)"]
  end

  subgraph Backend["Backend API (.NET 8 Clean Architecture)"]
    AiController["AiChatController &<br/>AiAnalyticsController"]
    AiService["AiChatService &<br/>AiAnalyticsService"]
    RagEngine["RAG Retrieval Engine<br/>(Live Catalog + Cultural KB)"]
    GeminiGateway["Google Gemini Gateway<br/>(HttpClient + Json Parsing)"]
    Repo["AiConversationRepository &<br/>AiAnalyticsReportRepository"]
  end

  subgraph Database["Database (PostgreSQL)"]
    T_AIC["ai_conversations"]
    T_AIM["ai_messages"]
    T_AAR["ai_analytics_reports"]
    T_PROD["products & product_variants"]
    T_ORD["orders & contracts"]
  end

  subgraph External["External AI Provider"]
    GeminiApi["Google Gemini API<br/>(gemini-flash-latest)"]
  end

  Widget --> ChatDrawer
  ChatDrawer -->|POST /api/v1/ai/chat/messages| AiController
  AdminDash -->|POST /api/v1/ai/analytics/platform| AiController
  SupplierDash -->|POST /api/v1/ai/analytics/shop| AiController

  AiController --> AiService
  AiService --> RagEngine
  RagEngine -->|Query Top Matching Catalog| T_PROD
  AiService -->|Aggregate Metrics| T_ORD
  AiService --> GeminiGateway
  GeminiGateway -->|HTTPS Prompt + Context| GeminiApi
  GeminiApi -->>|Natural Language Response| GeminiGateway
  AiService --> Repo
  Repo --> T_AIC & T_AIM & T_AAR
```

---

## 3. CÁC ĐẶC TẢ CHI TIẾT TRONG THƯ MỤC THIẾT KẾ

Thư mục thiết kế chi tiết này được tổ chức thành 8 tài liệu theo đúng chuẩn Enterprise Template:

| STT | Tập Tin Tài Liệu | Nội Dung Trọng Tâm |
| :---: | :--- | :--- |
| **01** | [`01_AI_CHATBOT_ADVISOR_DESIGN.md`](file:///Users/admin/Desktop/PRN232/mooncake-ecommerce-platform/.agents/feature/ai-chatbot-analytics/01_AI_CHATBOT_ADVISOR_DESIGN.md) | **Tính năng #23 (Phần 1):** Kiến trúc RAG tư vấn quà tặng B2B, Prompt Engineering chống ảo giác, trích xuất nhu cầu (ngân sách, số lượng, khẩu vị) và gợi ý sản phẩm trực tiếp từ DB. |
| **02** | [`02_MOONCAKE_CULTURAL_KNOWLEDGE_BASE_DESIGN.md`](file:///Users/admin/Desktop/PRN232/mooncake-ecommerce-platform/.agents/feature/ai-chatbot-analytics/02_MOONCAKE_CULTURAL_KNOWLEDGE_BASE_DESIGN.md) | **Tính năng #23 (Phần 2):** Cẩm nang tri thức văn hóa bánh Trung thu Việt Nam & Á Đông, phong tục tặng quà doanh nghiệp (Set 2, 4, 6, 8 bánh), nghệ thuật thưởng trà. |
| **03** | [`03_SALES_ANALYTICS_AI_INSIGHTS_DESIGN.md`](file:///Users/admin/Desktop/PRN232/mooncake-ecommerce-platform/.agents/feature/ai-chatbot-analytics/03_SALES_ANALYTICS_AI_INSIGHTS_DESIGN.md) | **Tính năng #24:** Động cơ phân tích dữ liệu bán hàng cho Admin (toàn sàn) và Supplier (gian hàng), sinh báo cáo tóm tắt ngôn ngữ tự nhiên, cảnh báo rủi ro & xuất file PDF/In ấn. |
| **04** | [`04_MOBILE_WORKFLOW_RESPONSIVE_WEB_DESIGN.md`](file:///Users/admin/Desktop/PRN232/mooncake-ecommerce-platform/.agents/feature/ai-chatbot-analytics/04_MOBILE_WORKFLOW_RESPONSIVE_WEB_DESIGN.md) | **Thiết kế Mobile & Web Responsive:** Quy trình trải nghiệm trên Mobile, giao diện Drawer trượt dưới (Bottom-sheet), chống lỗi bàn phím ảo, hiển thị thẻ sản phẩm vuốt chạm ngang. |
| **05** | [`05_API_DTO_VALIDATION.md`](file:///Users/admin/Desktop/PRN232/mooncake-ecommerce-platform/.agents/feature/ai-chatbot-analytics/05_API_DTO_VALIDATION.md) | **Đặc tả API & DTOs:** Hợp đồng API chuẩn `ApiResponse<T>`, các DTO Request/Response, quy tắc kiểm tra FluentValidation, cơ chế phòng chống Prompt Injection & Rate Limiting (20 req/phút). |
| **06** | [`06_DATA_SQL_CONCURRENCY_SECURITY.md`](file:///Users/admin/Desktop/PRN232/mooncake-ecommerce-platform/.agents/feature/ai-chatbot-analytics/06_DATA_SQL_CONCURRENCY_SECURITY.md) | **Cơ sở dữ liệu, Bảo mật & Hiệu năng:** Mapping EF Core các bảng `ai_conversations`, `ai_messages`, `ai_analytics_reports`, bảo mật khóa API Gemini, quản trị hạn mức token (Token Budgeting). |
| **07** | [`07_FRONTEND_INTEGRATION.md`](file:///Users/admin/Desktop/PRN232/mooncake-ecommerce-platform/.agents/feature/ai-chatbot-analytics/07_FRONTEND_INTEGRATION.md) | **Tích hợp Giao diện Frontend:** Thiết kế Widget nổi, Drawer hội thoại, các Action Chips nhanh, kết nối `aiService.js` với Mock Fallback Pattern, hiển thị Markdown & thẻ sản phẩm tương tác. |
| **08** | [`08_IMPLEMENTATION_TEST_ROLLOUT.md`](file:///Users/admin/Desktop/PRN232/mooncake-ecommerce-platform/.agents/feature/ai-chatbot-analytics/08_IMPLEMENTATION_TEST_ROLLOUT.md) | **Lộ trình Triển khai & Kế hoạch Kiểm thử:** Ma trận Unit Test Backend xUnit, kịch bản E2E Playwright, kế hoạch giám sát chi phí token API Gemini và tiêu chuẩn bàn giao Zero-Error. |

---

## 4. MA TRẬN PHÂN QUYỀN TRUY CẬP (ACCESS CONTROL MATRIX)

| Vai trò người dùng | Phiên Chatbot Tư Vấn | Đặt câu hỏi Văn Hóa | Phân tích Doanh Thu Toàn Sàn | Phân tích Doanh Thu Gian Hàng | Xuất Báo Cáo PDF |
| :--- | :---: | :---: | :---: | :---: | :---: |
| **Khách vãng lai (Guest)** | ✅ (Giới hạn 5 lượt/phiên) | ✅ | ❌ | ❌ | ❌ |
| **Khách hàng B2B (Customer)** | ✅ (Không giới hạn) | ✅ | ❌ | ❌ | ❌ |
| **Nhà cung cấp (Supplier)** | ✅ | ✅ | ❌ | ✅ (Chỉ gian hàng của mình) | ✅ |
| **Quản trị viên (Admin)** | ✅ | ✅ | ✅ (Toàn sàn) | ✅ (Mọi gian hàng) | ✅ |

---

## 5. CAM KẾT TUÂN THỦ QUY CHUẨN KỸ THUẬT
1. **Bảo mật tuyệt đối:** Tuyệt đối không commit git, không để lộ Gemini API Key ra Client. Mọi request ra Gemini đều chạy qua Backend Gateway với cơ chế Timeout 30s và Error Fallback.
2. **Quy chuẩn mã nguồn:** Thụt lề 2 spaces cho toàn bộ các file (`.js`, `.jsx`, `.css`, `.json`, `.md`), Backend 100% tiếng Anh, Frontend tiếng Việt giao diện cao cấp Lucian Gold.
3. **Mock Fallback:** Đảm bảo `aiService.js` có bộ dữ liệu mock thông minh mô phỏng câu trả lời của AI ngay cả khi không có kết nối internet hoặc backend chưa bật.

