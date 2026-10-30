# [FEATURE-27] THIẾT KẾ QUẢN TRỊ HỆ THỐNG, TRỌNG TÀI TRANH CHẤP & CẤU HÌNH SÀN
## Tài Liệu Thiết Kế Chi Tiết (Detail Design Document - Part 02)

---

## I. TỔNG QUAN TÍNH NĂNG & MỤC TIÊU NGHIỆP VỤ

### 1.1. Thông tin tài liệu
- **Tên tính năng:** Trung Tâm Quản Trị Hệ Thống & Trọng Tài Tranh Chấp Ký Quỹ SafeTrade (Admin Dashboard & Operations)
- **Mã tính năng (Feature Key):** `admin-operations-safetrade` (Feature #27)
- **Phân hệ thuộc về:** Module 11 – Hệ thống & Vận hành
- **Người thiết kế:** Team Kỹ thuật / Solution Architect
- **Phiên bản:** `1.0.0`
- **Trạng thái:** `Approved`

### 1.2. Mục tiêu nghiệp vụ (Business Goals)
1. **Quản trị người dùng & nhà cung cấp toàn diện (User & Shop Governance):**
   - Phê duyệt / từ chối hồ sơ thẩm định pháp lý xưởng sản xuất bánh Trung thu.
   - Khóa (Lock/Suspend) hoặc mở lại (Reactivate) tài khoản người dùng vi phạm chính sách giao dịch.
   - Quản lý trạng thái gian hàng (`Shops`): Tạm đình chỉ khi có dấu hiệu gian lận hoặc chậm trễ giao hàng nghiêm trọng.
2. **Cơ chế trọng tài tranh chấp hợp đồng & Ký quỹ SafeTrade (Dispute Resolution & Escrow Arbitration):**
   - Khi Khách hàng hoặc Nhà cung cấp khiếu nại (VD: Xưởng giao chậm $> 5\text{ ngày}$, bánh không đúng quy cách cam kết, giao thiếu số lượng so với hợp đồng):
   - Admin là bên trung gian công tâm xem xét hồ sơ bằng chứng (Ảnh PoD giao nhận, lịch sử trao đổi, file mockup thiết kế hộp).
   - Đưa ra phán quyết chính thức: Xác định bên có lỗi (`SupplierAtFault`, `CustomerAtFault`, `MutualAgreement`), ra quyết định hoàn tiền (Refund Amount) từ Quỹ Ký Quỹ Escrow, và áp dụng chế tài trừ điểm uy tín (`-20 điểm` đối với hành vi vi phạm nghiêm trọng).
3. **Cấu hình tham số sàn động (`SystemConfig` Management):**
   - Cho phép Quản trị viên thay đổi các quy tắc vận hành mà không cần sửa code hay build lại hệ thống:
     - Hạn mức thanh toán COD tối đa (Mặc định: 10.000.000 ₫).
     - Tỷ lệ đặt cọc hợp đồng bắt buộc (Mốc M1, mặc định: 30%).
     - Tỷ lệ phí hoa hồng nền tảng (Platform Commission Fee, mặc định: 3%).
     - Quy tắc tính điểm uy tín và ngưỡng phong tặng danh hiệu "Đối tác Vàng" (Gold Partner $\ge 80$ điểm).
4. **Kiểm duyệt nội dung (Content Moderation):**
   - Ẩn/hiện các đánh giá sản phẩm hoặc giao dịch có ngôn từ không phù hợp, spam quảng cáo, hoặc hình ảnh không đúng chuẩn mực.

---

## II. FLOW CHART & SEQUENCE DIAGRAM

### 2.1. Sequence Diagram: Luồng Phân Xử Tranh Chấp Hợp Đồng & Giải Ngân / Hoàn Cọc Escrow

```mermaid
sequenceDiagram
  autonumber
  actor Cust as Khách Hàng Doanh Nghiệp
  actor Supp as Nhà Cung Cấp (Xưởng Bánh)
  actor Admin as Quản Trị Viên (Sàn B2B)
  participant FE as Frontend (Admin DashBoard.jsx)
  participant API as WebApi (AdminDisputesController)
  participant DisSvc as DisputeService (Application Layer)
  participant PaySvc as PaymentService (Module 10)
  participant NotifSvc as NotificationService (Feature #26)
  participant DB as Database (PostgreSQL)

  Cust->>API: 1. Gửi đơn khiếu nại tranh chấp Hợp đồng CTR-2026-00012
  API->>DB: 2. Lưu ContractDispute (Status: Pending, EvidenceUrls: ["https://..."])
  API->>DB: 3. Khóa trạng thái Escrow: Disputed (Đóng băng giải ngân)
  API->>NotifSvc: 4. Phát thông báo khẩn cấp tới Admin & Supplier

  Admin->>FE: 5. Mở Tab 'Tranh Chấp & Khiếu Nại', bấm 'Xem chi tiết & Phân xử'
  FE->>API: 6. GET /api/v1/admin/disputes/{id}
  API-->>FE: 7. Trả về chi tiết tranh chấp, ảnh PoD, lịch sử giao dịch và số dư Escrow
  Admin->>FE: 8. Nhập Phán quyết: Lỗi thuộc về Xưởng; Hoàn cọc 100% (120.000.000 ₫); Trừ 20 điểm uy tín
  FE->>API: 9. POST /api/v1/admin/disputes/{id}/resolve (VerdictPayload)

  API->>DisSvc: 10. ResolveDisputeAsync(dto)
  DisSvc->>PaySvc: 11. ExecuteEscrowRefundAsync(contractId, refundAmount, reason)
  PaySvc->>PaySvc: 12. Ghi nhận giao dịch Payment (Type: Refund, Method: VNPay/BankTransfer)
  DisSvc->>DB: 13. Ghi log uy tín: ReputationLog (ScoreDelta = -20, Reason = "Vi phạm tiến độ HĐ")
  DisSvc->>DB: 14. Cập nhật ContractDispute (Status: Resolved, ResolvedBy: Admin.Id)
  DisSvc->>DB: 15. Ghi nhật ký AuditLog (Actor: Admin, Action: "RESOLVE_DISPUTE")

  critical Thông báo kết quả phân xử tức thì
    DisSvc->>NotifSvc: 16. Dispatch Resolution Notifications
    NotifSvc-->>Cust: 17a. Thông báo: "Khiếu nại được chấp thuận. Số tiền 120.000.000 ₫ đã được hoàn trả!"
    NotifSvc-->>Supp: 17b. Thông báo: "Phán quyết tranh chấp: Xưởng bị trừ 20 điểm uy tín do vi phạm hợp đồng."
  end

  DisSvc-->>FE: 18. Trả về HTTP 200 OK
  FE-->>Admin: 19. Thông báo thành công & cập nhật lại bảng Escrow Ledger
```

---

## III. CHI TIẾT TỪNG BƯỚC XỬ LÝ (STEP-BY-STEP LOGIC)

### Bước 1: Tiếp Nhận & Khóa Trạng Thái Giao Dịch Khi Có Tranh Chấp
1. Khi khách hàng nhấn "Yêu cầu sàn can thiệp tranh chấp":
   - Tạo bản ghi `ContractDispute` với `ContractId`, `RaisedByUserId`, `DisputeReason`, `EvidenceUrls`, trạng thái `Pending`.
   - Cập nhật hợp đồng sang trạng thái `Disputed`.
   - **Khóa an toàn dòng tiền (Escrow Freeze):** Toàn bộ các yêu cầu giải ngân mốc thanh toán của hợp đồng này bị vô hiệu hóa ngay lập tức cho đến khi Admin có phán quyết cuối cùng.
2. Phát thông báo khẩn (`priority: urgent`) tới Quản trị viên sàn.

### Bước 2: Thẩm Định Hồ Sơ & Ra Quyết Định Phân Xử (Arbitration Decision)
Quản trị viên sàn kiểm tra các tiêu chí:
- Bằng chứng ảnh giao nhận PoD đối chiếu với thời gian cam kết trong điều khoản hợp đồng.
- Lịch sử đàm phán và thỏa thuận gia hạn (nếu có).
- **Phân loại kết luận (`ResolutionType`):**
  - `SupplierAtFault`: Nhà cung cấp vi phạm nghiêm trọng $\rightarrow$ Hoàn cọc 100% cho Khách hàng, trừ 20 điểm uy tín nhà cung cấp, chuyển hợp đồng sang `Breached`.
  - `CustomerAtFault`: Khách hàng từ chối nhận hàng vô căn cứ $\rightarrow$ Giải ngân tiền cọc cho Nhà cung cấp để bù đắp chi phí sản xuất, không hoàn tiền.
  - `MutualAgreement`: Hai bên đạt thỏa thuận hỗ trợ $\rightarrow$ Hoàn tiền một phần (`partial refund`) theo tỷ lệ thống nhất.

### Bước 3: Thực Thi Quyết Định Hoàn Tiền Ký Quỹ Escrow (Escrow Refund Execution)
1. Gọi phương thức `PaymentService.ExecuteEscrowRefundAsync`:
   - Kiểm tra số dư Escrow hiện hữu của hợp đồng $\ge$ số tiền hoàn trả.
   - Khởi tạo giao dịch hoàn tiền có `PaymentNumber` duy nhất.
   - Cập nhật số dư `held_escrow_amount` của hợp đồng giảm tương ứng.
2. Ghi nhận `AuditLog` ghi lại ID của Admin ra quyết định, địa chỉ IP và lý do phân xử để phục vụ hậu kiểm.

### Bước 4: Quản Lý Đình Chỉ Gian Hàng (Shop Suspension Workflow)
1. Khi phát hiện gian hàng có dấu hiệu vi phạm (tỷ lệ giao chậm $> 30\%$, nhận nhiều khiếu nại, bán hàng kém chất lượng):
2. Admin gọi API `POST /api/v1/admin/shops/{id}/suspend` kèm lý do đình chỉ.
3. Hệ thống:
   - Cập nhật trạng thái Shop `Status = ShopStatus.Suspended`.
   - Toàn bộ sản phẩm của gian hàng tạm thời ẩn khỏi trang tìm kiếm công khai.
   - Gửi thông báo khẩn cấp tới chủ sở hữu gian hàng kèm hướng dẫn khiếu nại/khắc phục.
   - Ghi bản ghi `AuditLog` lưu vết.

### Bước 5: Cấu Hình Tham Số Sàn Động (`SystemConfig`)
1. Quản lý các cấu hình hệ thống lưu dưới dạng JSON trong bảng `system_configs`:
   - `COD_MAX_AMOUNT`: `{"limit": 10000000, "currency": "VND"}`.
   - `CONTRACT_DEFAULT_DEPOSIT_PERCENT`: `{"m1_deposit_percent": 30}`.
   - `PLATFORM_COMMISSION_PERCENT`: `{"rate": 3.0}`.
   - `REPUTATION_TIERS`: `{"new": 20, "trusted": 50, "reputable": 80, "gold_partner": 81}`.
2. Khi Admin cập nhật cấu hình:
   - Cập nhật cơ sở dữ liệu và làm mới Memory Cache của WebApi.
   - Mọi giao dịch mới tạo lập sẽ lập tức áp dụng quy tắc cấu hình mới mà không cần khởi động lại máy chủ.

---

## IV. ĐẶC TẢ KỸ THUẬT API QUẢN TRỊ (ADMIN API SPECIFICATIONS)

### 4.1. Danh mục Endpoints Phân Quyền `[Authorize(Roles = "Admin")]`

| Phương thức | Đường dẫn API | Mục đích nghiệp vụ |
| :--- | :--- | :--- |
| `GET` | `/api/v1/admin/users` | Danh sách người dùng phân trang, lọc theo vai trò (Customer/Supplier/Admin) |
| `PATCH` | `/api/v1/admin/users/{id}/status` | Khóa hoặc mở lại tài khoản người dùng (`active` / `locked`) |
| `GET` | `/api/v1/admin/shops` | Danh sách gian hàng, xem metrics GMV và tỷ lệ hoàn thành |
| `POST` | `/api/v1/admin/shops/{id}/suspend` | Tạm đình chỉ hoạt động gian hàng kèm lý do |
| `POST` | `/api/v1/admin/shops/{id}/reactivate` | Kích hoạt mở lại gian hàng sau khi khắc phục vi phạm |
| `GET` | `/api/v1/admin/disputes` | Danh sách các vụ tranh chấp hợp đồng (Pending / Resolved) |
| `GET` | `/api/v1/admin/disputes/{id}` | Chi tiết vụ tranh chấp, hồ sơ hợp đồng, ảnh PoD và số dư Escrow |
| `POST` | `/api/v1/admin/disputes/{id}/resolve` | Phân xử tranh chấp, quyết định hoàn cọc và áp chế tài điểm uy tín |
| `GET` | `/api/v1/admin/configs` | Lấy toàn bộ tham số cấu hình sàn hiện hành |
| `PUT` | `/api/v1/admin/configs/{key}` | Cập nhật giá trị tham số cấu hình sàn động |
| `GET` | `/api/v1/admin/audit-logs` | Truy vấn nhật ký kiểm toán hành động quản trị viên |

### 4.2. Request Payload: Phân Xử Tranh Chấp Hợp Đồng
```json
{
  "resolutionType": "SupplierAtFault",
  "adminVerdict": "Xưởng giao bánh chậm 7 ngày so với cam kết mốc M2 làm ảnh hưởng sự kiện Trung thu của doanh nghiệp mua. Áp dụng chế tài vi phạm hợp đồng.",
  "refundAmount": 120000000,
  "deductReputationPoints": 20,
  "notes": "Đã đối soát ảnh PoD và biên bản làm việc giữa 2 bên."
}
```

### 4.3. Response Payload: Kết Quả Phân Xử Thành Công (HTTP 200 OK)
```json
{
  "status": 200,
  "message": "Phân xử tranh chấp và hoàn tiền ký quỹ thành công.",
  "data": {
    "disputeId": "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d",
    "contractId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
    "contractNumber": "CTR-2026-00012",
    "status": "Resolved",
    "resolutionType": "SupplierAtFault",
    "refundAmount": 120000000,
    "refundPaymentNumber": "REF-2026-00003",
    "supplierReputationNewScore": 68,
    "resolvedAt": "2026-10-29T10:30:00Z"
  },
  "path": "/api/v1/admin/disputes/a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d/resolve",
  "traceId": "00-84a1e948f2cb-admin-1",
  "time": "2026-10-29T10:30:00Z"
}
```

---

## V. KIẾN TRÚC PHÂN TẦNG VÀ TỔ CHỨC MÃ NGUỒN (LAYER MAPPING)

- **Domain Entities:** `ContractDispute.cs`, `SystemConfig.cs`, `AuditLog.cs`, `ReputationLog.cs`, `Shop.cs`, `User.cs`.
- **Application Layer:**
  - Interfaces: `IAdminOperationsService.cs`, `IDisputeService.cs`, `ISystemConfigService.cs`.
  - DTOs: `ResolveDisputeRequest.cs`, `DisputeDetailResponse.cs`, `UpdateSystemConfigRequest.cs`, `ShopSuspensionRequest.cs`.
- **Infrastructure Layer:**
  - `AdminOperationsRepository.cs`, `DisputeRepository.cs`, `SystemConfigRepository.cs`.
- **WebApi Layer:**
  - `AdminDisputesController.cs`, `AdminShopsController.cs`, `AdminConfigsController.cs`.
  - Middleware: `AdminAuditMiddleware.cs` ghi vết mọi thao tác thay đổi trạng thái.
- **Frontend Layer (`mooncake-fe`):**
  - `src/Pages/Admin/DashBoard.jsx`: Mở rộng Tab Quản Trị Tranh Chấp & Quản Trị Cấu Hình Sàn.
  - `src/components/admin/DisputeResolutionModal.jsx`: Modal phân xử trực quan kèm tính toán số tiền hoàn cọc.
  - `src/components/admin/SystemConfigDrawer.jsx`: Ngăn kéo điều chỉnh hạn mức COD và % đặt cọc.

---

## VI. BẢO MẬT & KIỂM SOÁT HÀNH ĐỘNG QUẢN TRỊ (ADMIN SECURITY & AUDIT)

1. **Kiểm Soát Quyền Truy Cập Nghiêm Ngặt (Strict RBAC):**
   - Toàn bộ các endpoints bắt buộc có claim `Role == "Admin"`. Mọi nỗ lực truy cập từ tài khoản Customer hoặc Supplier đều bị chặn đứng với mã lỗi `403 Forbidden`.
2. **Nhật Ký Kiểm Toán Bất Biến (Immutable Audit Trail):**
   - Mọi hành vi: Phê duyệt hồ sơ, khóa tài khoản, đình chỉ gian hàng, hoàn tiền cọc Escrow, cập nhật cấu hình sàn đều tự động ghi vào bảng `audit_logs` kèm ID quản trị viên, địa chỉ IP, User-Agent và payload JSON trước/sau khi sửa đổi.
3. **Cơ Chế Bảo Vệ Dòng Tiền (Financial Safeguard):**
   - Số tiền hoàn cọc (`refundAmount`) bắt buộc phải $\le$ số tiền Escrow đang thực giữ của hợp đồng đó. Hệ thống tự động từ chối nếu Admin nhập số tiền vượt quá số dư ký quỹ thực tế.

