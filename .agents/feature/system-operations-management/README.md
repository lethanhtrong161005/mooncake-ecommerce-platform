# Module 11: Hệ Thống & Vận Hành Toàn Sàn B2B (Features #26, #27, #28)
## Enterprise Architectural Design Plan

---

## 1. Tổng Quan & Mục Tiêu Nghiệp Vụ (Executive Summary)

Tài liệu này đặc tả toàn bộ kiến trúc giải pháp, thiết kế kỹ thuật phân tầng và kế hoạch triển khai cho **Module 11: Hệ Thống & Vận Hành** của Sàn Giao Dịch Bánh Trung Thu Doanh Nghiệp B2B (**Nguyệt Phố**), bao gồm bộ 3 tính năng cốt lõi:
- **Tính năng #26: Thông báo real-time (Real-time Notifications & Notification Center):** Đẩy thông báo tức thì qua SignalR WebSocket, đồng bộ số đếm chưa đọc (Unread badge), ngăn kéo thông báo đa kênh phong cách Hoàng Gia và tùy biến kênh nhận tin.
- **Tính năng #27: Quản trị hệ thống (Admin Dashboard & Operations):** Quản lý người dùng và duyệt/khóa tài khoản, quản lý đình chỉ/mở lại gian hàng, xử lý tranh chấp hợp đồng (Dispute handling) & trọng tài ký quỹ Escrow SafeTrade, cấu hình hạn mức và chính sách sàn (`SystemConfig`), kiểm duyệt nội dung (Content moderation).
- **Tính năng #28: Báo cáo & Thống kê đa chiều (Multi-Dimensional Reporting & Analytics):** Báo cáo GMV doanh thu toàn sàn, phễu chuyển đổi RFQ $\rightarrow$ Bid $\rightarrow$ Contract, bảng xếp hạng Top Nhà cung cấp & Sản phẩm, xuất file báo cáo CSV/Excel/PDF, và tích hợp Gemini AI sinh phân tích ngôn ngữ tự nhiên.

Hệ thống tuân thủ nghiêm ngặt chuẩn kiến trúc **Clean Architecture (.NET 8 C# 12)** cho Backend WebApi và **Enterprise Layered Architecture (React 19 / Vite / Ant Design v6 / Tailwind Luxury Gold UI)** cho Frontend `mooncake-fe`.

```
================================================================================
  B2B LUXURY MOONCAKE MARKETPLACE - MODULE 11 ARCHITECTURAL SCOPE
  Features Covered:
    - Feature #26: Real-Time Multi-Role Push Notifications & Notification Center
    - Feature #27: Admin Operations (Users, Shops, Disputes, SafeTrade, Configs)
    - Feature #28: Multi-Dimensional Analytics (GMV, Funnel, Export & Gemini AI)
================================================================================
```

---

## 2. Thông Tin Cấu Hình & Khóa Tích Hợp (Security & Integration Keys)

### 2.1. Cổng Thanh Toán VNPay Sandbox (Kiểm Toán & Trọng Tài Hoàn Cọc Escrow)
Theo cấu hình chuẩn hóa tại [keyvnpay.text](file:///Users/admin/Desktop/PRN232/keyvnpay.text), hệ thống **nghiêm ngặt chỉ sử dụng môi trường VNPay Sandbox**:
- **Terminal ID (vnp_TmnCode):** `BFYI47AF`
- **Secret Hash Key (vnp_HashSecret):** `WBGRFFJWAHOAFNIEYICELPMQORJJHSQL`
- **Payment Gateway Endpoint (vnp_Url):** `https://sandbox.vnpayment.vn/paymentv2/vpcpay.html`
- **Vai trò trong Module 11:** Phục vụ đối soát dòng tiền ký quỹ Escrow trên Admin Dashboard, kiểm tra tính toàn vẹn chữ ký HMAC-SHA512 của các giao dịch, và thực thi quyết định hoàn tiền (Refund) khi Admin phân xử tranh chấp hợp đồng.

### 2.2. Trí Tuệ Nhân Tạo Google Gemini AI (Tính Năng #28 - Natural Language Insights)
Theo cấu hình API tại [gemini-key.text](file:///Users/admin/Desktop/PRN232/gemini-key.text):
- **Model:** `gemini-flash-latest`
- **API Key:** `YOUR_GEMINI_API_KEY`
- **Vai trò trong Module 11:** Tự động phân tích các chỉ số GMV, tỷ lệ hoàn thành hợp đồng, tỷ lệ thắng thầu RFQ để tạo bản tóm tắt chiến lược thị trường (Executive Summary), cảnh báo sớm các dấu hiệu gian lận và đề xuất tối ưu hóa chính sách sàn.

---

## 3. Bản Đồ Tích Hợp Giữa Các Tính Năng (System Integration Map)

```mermaid
flowchart TD
  classDef notif fill:#FEF3C7,stroke:#D97706,stroke-width:2px,color:#78350F;
  classDef admin fill:#EFF6FF,stroke:#2563EB,stroke-width:2px,color:#1E3A8A;
  classDef report fill:#ECFDF5,stroke:#059669,stroke-width:2px,color:#064E3B;
  classDef db fill:#F3F4F6,stroke:#4B5563,stroke-width:2px,color:#111827;
  classDef ext fill:#FDF2F8,stroke:#DB2777,stroke-width:2px,color:#831843;

  subgraph F26["Feature #26: Real-time Notifications"]
    SignalRHub["SignalR Hub\n/api/v1/hubs/notifications"]:::notif
    NotifBell["Notification Bell & Drawer\nUnread Counter & Chime"]:::notif
    NotifPref["Notification Preferences\nIn-App / Push / Email"]:::notif
  end

  subgraph F27["Feature #27: Admin Operations"]
    AdminDash["Admin Dashboard\nMulti-tab Control Center"]:::admin
    UserShopMgmt["User & Shop Control\nApprove / Suspend / Unlock"]:::admin
    DisputeSafeTrade["Dispute Resolution\nSafeTrade Escrow Arbitration"]:::admin
    SystemConfig["System Configuration\nCOD Limits & Deposit %"]:::admin
  end

  subgraph F28["Feature #28: Multi-Dimensional Analytics"]
    PlatformGMV["GMV & Revenue Analytics\nTimeframe Filtering"]:::report
    FunnelChart["RFQ Conversion Funnel\nRFQ -> Bid -> Contract"]:::report
    ExportEngine["Export Engine\nCSV / Excel / PDF"]:::report
    AiSummary["Gemini AI Natural Language\nMarket & Executive Digest"]:::report
  end

  subgraph CoreStorage["Database & Gateways"]
    PostgresDB[("PostgreSQL\nNotifications, Disputes,\nConfigs & Audit Logs")]:::db
    VNPayGateway["VNPay Sandbox Gateway\nEscrow & Refund Auditing"]:::ext
    GeminiAPI["Google Gemini AI\nflash-latest API"]:::ext
  end

  %% Linkages
  AdminDash --> UserShopMgmt
  AdminDash --> DisputeSafeTrade
  AdminDash --> SystemConfig
  AdminDash --> PlatformGMV
  AdminDash --> FunnelChart
  PlatformGMV --> ExportEngine
  PlatformGMV --> AiSummary

  DisputeSafeTrade -->|Trigger Dispute Alert| SignalRHub
  DisputeSafeTrade -->|Execute Refund| VNPayGateway
  UserShopMgmt -->|Trigger Status Alert| SignalRHub
  AiSummary <-->|Analytics Prompt & Digest| GeminiAPI

  SignalRHub --> NotifBell
  NotifBell --> NotifPref

  UserShopMgmt --> PostgresDB
  DisputeSafeTrade --> PostgresDB
  SystemConfig --> PostgresDB
  SignalRHub --> PostgresDB
```

---

## 4. Sequence Diagram: Luồng Xử Lý Tranh Chấp Hợp Đồng, Hoàn Tiền Ký Quỹ & Phát Thông Báo

```mermaid
sequenceDiagram
  autonumber
  actor Customer as Khách Hàng Doanh Nghiệp
  actor Admin as Quản Trị Viên Sàn
  actor Supplier as Nhà Cung Cấp (Xưởng Bánh)
  participant FE_Admin as Frontend Admin Dashboard
  participant API as Backend (DisputesController)
  participant DisSvc as DisputeService (Feature #27)
  participant PaySvc as PaymentService (Feature #25)
  participant NotifSvc as NotificationService (Feature #26)
  participant Hub as NotificationHub (SignalR)
  participant DB as Database (PostgreSQL)

  Customer->>API: 1. Khởi tạo khiếu nại tranh chấp Hợp đồng CTR-2026-00012 (Lỗi giao chậm 7 ngày)
  API->>DB: 2. Lưu bản ghi ContractDispute (Status: Pending) & đóng băng mốc Escrow
  API->>NotifSvc: 3. Phát thông báo khẩn cấp tới Admin & Supplier
  NotifSvc->>Hub: 4. Push SignalR: "Hợp đồng CTR-2026-00012 phát sinh tranh chấp"
  Hub-->>FE_Admin: 5. Hiển thị cảnh báo đỏ trên Tab Tranh Chấp & Chuông báo rung

  Admin->>FE_Admin: 6. Mở chi tiết Tranh chấp, kiểm tra chứng từ & ảnh PoD
  FE_Admin->>API: 7. POST /api/v1/admin/disputes/{id}/resolve (Verdict: SupplierAtFault, Refund: 120.000.000 ₫)
  API->>DisSvc: 8. Phân xử tranh chấp & kích hoạt chế tài
  DisSvc->>PaySvc: 9. ExecuteEscrowRefundAsync(contractId, refundAmount, reason)
  PaySvc->>PaySvc: 10. Hoàn cọc Escrow về tài khoản bên mua
  DisSvc->>DB: 11. Trừ 20 điểm uy tín xưởng bánh (ReputationLog: -20đ Breach)
  
  critical Phát thông báo phân xử thời gian thực tới 2 bên
    DisSvc->>NotifSvc: 12. Dispatch Resolution Events
    NotifSvc->>Hub: 13a. SendToUser(Customer.Id, "Nhận hoàn tiền cọc 120M thành công")
    NotifSvc->>Hub: 13b. SendToUser(Supplier.Id, "Kết luận tranh chấp: Trừ 20đ uy tín do vi phạm tiến độ")
    Hub-->>Customer: 14a. Toast xanh + Âm thanh chuông báo
    Hub-->>Supplier: 14b. Cảnh báo vi phạm hợp đồng
  end

  DisSvc-->>FE_Admin: 15. Cập nhật Dashboard: Vụ việc Đã giải quyết, Số dư Escrow cân bằng
```

---

## 5. Danh Mục Bộ Tài Liệu Thiết Kế Chi Tiết (8 Chuyên Đề)

Bộ tài liệu thiết kế chi tiết cho **Module 11 (Features #26, #27, #28)** được cấu trúc chuẩn hóa tại thư mục `mooncake-ecommerce-platform/.agents/feature/system-operations-management/`:

| STT | Mã tài liệu | Tên chuyên đề thiết kế | Trọng tâm nghiệp vụ |
| :--- | :--- | :--- | :--- |
| 1 | [01_REALTIME_NOTIFICATIONS_SIGNALR_DESIGN.md](file:///Users/admin/Desktop/PRN232/mooncake-ecommerce-platform/.agents/feature/system-operations-management/01_REALTIME_NOTIFICATIONS_SIGNALR_DESIGN.md) | Kiến Trúc SignalR & WebSocket Real-time (Feature #26) | Kết nối WebSocket hai chiều, định danh người dùng ổn định, phân phối nhóm theo vai trò, đồng bộ số đếm unread đa tab, phục hồi kết nối Exponential Backoff. |
| 2 | [02_ADMIN_OPERATIONS_DASHBOARD_DESIGN.md](file:///Users/admin/Desktop/PRN232/mooncake-ecommerce-platform/.agents/feature/system-operations-management/02_ADMIN_OPERATIONS_DASHBOARD_DESIGN.md) | Quản Trị Hệ Thống, Trọng Tài & Cấu Hình (Feature #27) | Quản lý người dùng/gian hàng, xử lý tranh chấp hợp đồng (SafeTrade Arbitration), áp dụng chế tài uy tín, cấu hình sàn (`SystemConfig`), kiểm duyệt nội dung. |
| 3 | [03_MULTIDIMENSIONAL_REPORTING_ANALYTICS_DESIGN.md](file:///Users/admin/Desktop/PRN232/mooncake-ecommerce-platform/.agents/feature/system-operations-management/03_MULTIDIMENSIONAL_REPORTING_ANALYTICS_DESIGN.md) | Báo Cáo & Thống Kê Đa Chiều (Feature #28) | Phân tích GMV đa khung thời gian, phễu chuyển đổi RFQ $\rightarrow$ Bid $\rightarrow$ Contract, bảng xếp hạng NCC, xuất báo cáo CSV/Excel/PDF, và Gemini AI Natural Language Insights. |
| 4 | [04_MOBILE_WORKFLOW_RESPONSIVE_WEB_DESIGN.md](file:///Users/admin/Desktop/PRN232/mooncake-ecommerce-platform/.agents/feature/system-operations-management/04_MOBILE_WORKFLOW_RESPONSIVE_WEB_DESIGN.md) | Trải Nghiệm Mobile-First Trên Nền Tảng Web | Thiết kế giao diện quản trị và trung tâm thông báo tương thích di động, Bottom Sheet cảm ứng, thao tác 1-chạm xử lý tranh chấp, biểu đồ trực quan co giãn. |
| 5 | [05_API_DTO_VALIDATION.md](file:///Users/admin/Desktop/PRN232/mooncake-ecommerce-platform/.agents/feature/system-operations-management/05_API_DTO_VALIDATION.md) | Đặc Tả Kỹ Thuật API, DTO & Validation | Đặc tả toàn bộ RESTful Endpoints cho Notifications, Admin Operations, Disputes, System Configs, Reports & Export, bảng mã lỗi và thông điệp tiếng Việt. |
| 6 | [06_DATA_SQL_CONCURRENCY_SECURITY.md](file:///Users/admin/Desktop/PRN232/mooncake-ecommerce-platform/.agents/feature/system-operations-management/06_DATA_SQL_CONCURRENCY_SECURITY.md) | Cơ Sở Dữ Liệu PostgreSQL & Kiểm Soát An Ninh | Lược đồ bảng `notifications`, `contract_disputes`, `system_configs`, `audit_logs`, tối ưu hóa chỉ mục, RBAC `[Authorize(Roles = "Admin")]`, lưu vết kiểm toán. |
| 7 | [07_FRONTEND_INTEGRATION.md](file:///Users/admin/Desktop/PRN232/mooncake-ecommerce-platform/.agents/feature/system-operations-management/07_FRONTEND_INTEGRATION.md) | Hiện Thực Frontend React 19 (`mooncake-fe`) | Tích hợp `@microsoft/signalr`, Component `NotificationBell`, Drawer, Tab Tranh Chấp, Biểu đồ thống kê, Mock Fallback Pattern và phong cách Luxury Amber UI. |
| 8 | [08_IMPLEMENTATION_TEST_ROLLOUT.md](file:///Users/admin/Desktop/PRN232/mooncake-ecommerce-platform/.agents/feature/system-operations-management/08_IMPLEMENTATION_TEST_ROLLOUT.md) | Kế Hoạch Triển Khai & Ma Trận Kiểm Thử | Ma trận Unit Test xUnit Backend, kịch bản Playwright E2E cho cả 3 tính năng trên Desktop & Mobile, và quy trình kiểm thử bàn giao không lỗi. |

---

## 6. Cam Kết & Tiêu Chuẩn Thực Thi (Compliance & Guardrails)
- **Zero Git Commit Rule:** Tuyệt đối không thực thi `git commit` hay `git push` trong bất kỳ tình huống nào.
- **VNPay Sandbox Exclusivity:** Chỉ kết nối và xử lý trên cổng VNPay Sandbox (`https://sandbox.vnpayment.vn/paymentv2/vpcpay.html`).
- **Indentation & Formatting:** Bắt buộc 2 spaces cho toàn bộ các file `.js`, `.jsx`, `.css`, `.json`, `.md`. Không dùng ký tự Tab.
- **Clean Architecture & Enterprise UI:** Backend 100% tiếng Anh chuẩn mực; Frontend sử dụng tiếng Việt trang trọng, tôn vinh nghệ thuật và văn hóa biếu tặng Trung thu doanh nghiệp B2B cao cấp.

