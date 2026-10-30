# Module 6 — Contract Management: Features #15, #16, #17, #18

- **Feature Key:** `contract-management`
- **Module Name:** Module 6 – Hợp đồng & Tiến độ Thực hiện (B2B E-Contract, Milestone Escrow & Dispute Resolution)
- **Design Date:** **2026-10-29**. **Version:** `1.0.0`
- **Status:** **Draft / Plan Only**. Chuẩn bị cho triển khai Backend Clean Architecture và Frontend React 19 Layered.
- **Included Features:**
  - **Feature #15 (P0 Critical):** Tạo & Ký kết hợp đồng điện tử (E-Contract Creation, PDF Generation & OTP Digital Signature).
  - **Feature #16 (P1 High):** Theo dõi tiến độ & Milestone hợp đồng (Milestone Tracking, Deposit M1 via VNPay Escrow & Evidence Verification).
  - **Feature #17 (P1 High):** Xử lý vi phạm hợp đồng & Tranh chấp (Overdue Detection, Extension Requests, Breach Penalty Terms & Admin Dispute Resolution).
  - **Feature #18 (P1 High):** Hoàn thành hợp đồng & Chốt kết quả (Contract Completion, Acceptance Protocol, Milestone M3 Final Payout & Reputation Settlement).
- **Payment Gateway Config Reference:** [keyvnpay.text](../../../keyvnpay.text) & `appsettings.json`:
  - **Terminal ID (vnp_TmnCode):** `BFYI47AF`
  - **Secret Key (vnp_HashSecret):** `WBGRFFJWAHOAFNIEYICELPMQORJJHSQL`
  - **Payment URL (vnp_Url):** `https://sandbox.vnpayment.vn/paymentv2/vpcpay.html`

---

## 📖 Danh mục tài liệu thiết kế (Reading Order)

| Thứ tự | Tên tài liệu | Nội dung & Mục đích |
| :--- | :--- | :--- |
| **01** | [01_ECONTRACT_CREATION_SIGNATURE_DESIGN.md](./01_ECONTRACT_CREATION_SIGNATURE_DESIGN.md) | Thiết kế chi tiết Tính năng #15: Tự động khởi tạo hợp đồng kinh tế từ Bid thắng thầu, sinh bản thảo điều khoản pháp lý & chế tài, chữ ký số hai bên xác thực qua mã OTP email/SMS, lưu giữ dấu vân tay SHA-256 (`SignedDocumentSha256`) và xuất PDF. |
| **02** | [02_MILESTONE_TRACKING_PROGRESS_DESIGN.md](./02_MILESTONE_TRACKING_PROGRESS_DESIGN.md) | Thiết kế chi tiết Tính năng #16: Quản lý các mốc tiến độ (M1 cọc 30% kích hoạt hợp đồng qua VNPay Sandbox, M2 sản xuất/giao hàng 50%, M3 nghiệm thu dứt điểm); cập nhật bằng chứng hiện trường và hệ thống cảnh báo tự động T-7, T-3, T-1 ngày. |
| **03** | [03_CONTRACT_BREACH_DISPUTE_RESOLUTION_DESIGN.md](./03_CONTRACT_BREACH_DISPUTE_RESOLUTION_DESIGN.md) | Thiết kế chi tiết Tính năng #17: Tự động gắn cờ `Overdue`, quy trình xin gia hạn deadline kèm lý do bất khả kháng, phân loại vi phạm (`Late` vs `Breach`), phạt hợp đồng theo ngày trễ, cơ chế khiếu nại tranh chấp (`Disputed`) và Admin phân xử bảo vệ tiền cọc ký quỹ. |
| **04** | [04_CONTRACT_COMPLETION_ACCEPTANCE_DESIGN.md](./04_CONTRACT_COMPLETION_ACCEPTANCE_DESIGN.md) | Thiết kế chi tiết Tính năng #18: Yêu cầu nghiệm thu tổng thể từ xưởng, biên bản bàn giao điện tử, phân loại kết quả (`completed_on_time` vs `completed_late`), quyết toán giải ngân mốc thanh toán cuối cùng, tính điểm uy tín và kích hoạt luồng đánh giá. |
| **05** | [05_API_DTO_VALIDATION.md](./05_API_DTO_VALIDATION.md) | Đặc tả toàn bộ API RESTful endpoints, DTO Requests/Responses 100% tiếng Anh, DataAnnotations, kiểm soát phân quyền (`Customer`, `Supplier`, `Admin`), và bảng mã lỗi chuẩn hóa (`CONTRACT_NOT_SIGNABLE`, `MILESTONE_ALREADY_PAID`...). |
| **06** | [06_DATA_SQL_CONCURRENCY_SECURITY.md](./06_DATA_SQL_CONCURRENCY_SECURITY.md) | Mô hình dữ liệu quan hệ PostgreSQL / EF Core entities, khóa bi quan `SELECT ... FOR UPDATE` khi ký OTP và xác nhận mốc tiền cọc, bảo mật chống tráo tài liệu hợp đồng bằng SHA-256, và nhật ký kiểm toán `WorkflowEvents`. |
| **07** | [07_FRONTEND_INTEGRATION.md](./07_FRONTEND_INTEGRATION.md) | Kiến trúc tích hợp Frontend `mooncake-fe`, kết nối màn hình `ContractsAndMilestone.jsx` (Khách hàng) và `ContractManage.jsx` (Nhà cung cấp), dịch vụ `contractService.js` và `paymentService.js`, token Luxury Gold / Deep Stone. |
| **08** | [08_IMPLEMENTATION_TEST_ROLLOUT.md](./08_IMPLEMENTATION_TEST_ROLLOUT.md) | Lộ trình triển khai 6 giai đoạn (Phases 1-6), Ma trận kiểm thử Unit/Integration/E2E, Kịch bản test thẻ VNPay Sandbox, Tiêu chí hoàn thành (DoD) & chiến lược Rollback. |

---

## 🎯 Căn cứ nghiệp vụ & Nguồn tham chiếu

1. **Tài liệu nghiệp vụ:** [mooncake-marketplace.md](../../business/mooncake-marketplace.md) - Module 6 (Features #15, #16, #17, #18).
2. **Template thiết kế chuẩn:** [FEATURE_DETAIL_DESIGN_TEMPLATE.md](../template/FEATURE_DETAIL_DESIGN_TEMPLATE.md).
3. **Cấu hình VNPay Sandbox:** [keyvnpay.text](../../../keyvnpay.text) & `src/Mooncake.EcommercePlatform.WebApi/appsettings.json`.
4. **Quy chuẩn mã nguồn:**
   - **Backend rules:** [AGENTS.md](../../AGENTS.md) (Clean Architecture, C# 12, .NET 8, English Only trong source code, Standard `ApiResponse<T>`, Static Helpers, PostgreSQL Concurrency Locking).
   - **Frontend rules:** [AGENTS.md](../../../../.agents/AGENTS.md) (React 19, 2 spaces indentation, ES2024, Không dùng `var`, Layered architecture, `apiClient` Axios tập trung, Mock fallback pattern, Theme Luxury Gold).

---

## ⚖️ Các nguyên tắc thiết kế bất biến (Core Invariants)

1. **Bất biến kích hoạt hợp đồng bằng cọc Escrow (Deposit Escrow Invariant):**
   - Hợp đồng sau khi được cả hai bên ký điện tử sẽ chuyển sang trạng thái `PendingDeposit`.
   - **Hợp đồng KHÔNG THỂ chuyển sang `Active` nếu mốc cọc Milestone M1 chưa được thanh toán thành công.**
   - Nhà cung cấp chỉ bắt đầu chuẩn bị nguyên vật liệu và sản xuất bánh khi trạng thái hợp đồng chính thức là `Active`.
2. **Bất biến chữ ký số điện tử (Digital Signature Integrity):**
   - Mỗi bên (Khách hàng & Nhà cung cấp) khi ký hợp đồng bắt buộc phải qua xác thực hai lớp (2FA via OTP).
   - Hệ thống ghi lại địa chỉ IP, User-Agent trình duyệt, thời điểm ký UTC, và mã băm SHA-256 bất biến của nội dung điều khoản (`SignedDocumentSha256`). Mọi sự thay đổi dù là 1 ký tự sau khi ký đều làm mất hiệu lực của chữ ký số.
3. **Bất biến nghiệm thu và thanh toán theo từng mốc (Milestone-based Escrow Release):**
   - Giá trị hợp đồng được giữ an toàn trên hệ thống trung gian ký quỹ.
   - Khoản thanh toán của từng mốc chỉ được giải ngân cho Nhà cung cấp khi Khách hàng kiểm tra bằng chứng giao nhận / sản xuất và bấm nút "Xác nhận hoàn thành mốc".
4. **Bất biến chế tài vi phạm & uy tín nhà cung cấp (Breach & Reputation Enforcement):**
   - Mốc tiến độ quá hạn `due_date` tự động chuyển trạng thái `Overdue`.
   - Nếu Nhà cung cấp không xin gia hạn hoặc vi phạm nghiêm trọng (`Breached`), hệ thống tự động trừ **-20 điểm uy tín** trong `reputation_logs` và cho phép Khách hàng kích hoạt tranh chấp (`Disputed`) để đóng băng tiền cọc.
   - Khi hoàn thành đúng hạn toàn bộ hợp đồng, xưởng được cộng thưởng **+10 điểm uy tín** và nâng hạng huy hiệu đối tác.

