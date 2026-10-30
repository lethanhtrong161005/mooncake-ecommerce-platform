# Module 5 — RFQ & Bidding: Features #10, #11, #12, #13

- **Feature Key:** `rfq-bidding`
- **Module Name:** Module 5 – RFQ / Báo giá & Đấu thầu Doanh nghiệp (B2B Quotation & Bid Awarding)
- **Design Date:** **2026-10-29**. **Version:** `1.0.0`
- **Status:** **Implemented**. Backend Clean Architecture và Frontend React đã tích hợp các luồng cốt lõi; xem `09_IMPLEMENTATION_RESULT.md`.
- **Included Features:**
  - **Feature #10 (P0 Critical):** Tạo yêu cầu báo giá (Request for Quotation - RFQ Creation & Lifecycle).
  - **Feature #11 (P1 High):** Mời nhà cung cấp & Gợi ý xưởng phù hợp (RFQ Invitations & Supplier Matching).
  - **Feature #12 (P0 Critical):** Nhà cung cấp gửi báo giá chi tiết (Supplier Bidding & Side-by-Side Comparison).
  - **Feature #13 (P0 Critical):** Chọn báo giá thắng thầu, Sinh dự thảo Hợp đồng & Thanh toán ký quỹ VNPay (Bid Awarding, Contract Generation & VNPay Escrow Deposit).
- **Payment Gateway Config Reference:** Configure `VnPay:TmnCode` and `VnPay:HashSecret` in the ignored Web API `appsettings.json` (never commit credentials).
  - **Sandbox Payment URL:** `https://sandbox.vnpayment.vn/paymentv2/vpcpay.html`

---

## 📖 Danh mục tài liệu thiết kế (Reading Order)

| Thứ tự | Tên tài liệu | Nội dung & Mục đích |
| :--- | :--- | :--- |
| **01** | [01_RFQ_CREATION_LIFECYCLE_DESIGN.md](./01_RFQ_CREATION_LIFECYCLE_DESIGN.md) | Thiết kế chi tiết Tính năng #10: Khởi tạo RFQ nhiều dòng sản phẩm, ngân sách min-max, thời hạn đóng thầu, địa chỉ giao, vòng đời (`Draft -> Open -> Closed / Cancelled`). |
| **02** | [02_SUPPLIER_INVITATION_MATCHING_DESIGN.md](./02_SUPPLIER_INVITATION_MATCHING_DESIGN.md) | Thiết kế chi tiết Tính năng #11: Cơ chế mời tối đa 10 xưởng, thuật toán gợi ý xưởng bánh (danh mục, uy tín >= 80, khu vực), vòng đời lời mời (`Invited -> Viewed -> BidSubmitted / Declined`). |
| **03** | [03_SUPPLIER_BIDDING_MANAGEMENT_DESIGN.md](./03_SUPPLIER_BIDDING_MANAGEMENT_DESIGN.md) | Thiết kế chi tiết Tính năng #12: Xưởng nộp báo giá chi tiết từng dòng (`BidItem`), thời hạn hiệu lực, bảng so sánh trực quan đa chiều cho khách hàng (`BiddingAndQuoteComparison.jsx`). |
| **04** | [04_BID_AWARDING_VNPAY_ESCROW_DESIGN.md](./04_BID_AWARDING_VNPAY_ESCROW_DESIGN.md) | Thiết kế chi tiết Tính năng #13: Chọn báo giá trúng thầu, tự động hủy các bid thua, sinh hợp đồng điện tử (`Contract`), và **tích hợp cổng thanh toán VNPay Sandbox** thanh toán tiền cọc ký quỹ (Deposit Milestone M1). |
| **05** | [05_API_DTO_VALIDATION.md](./05_API_DTO_VALIDATION.md) | Đặc tả hợp đồng API RESTful chuẩn `ApiResponse<T>`, DTO Requests/Responses, DataAnnotations, và bảng mã lỗi chuẩn hóa (`RFQ_CLOSED`, `BID_EXPIRED`, `VNPAY_INVALID_SIGNATURE`...). |
| **06** | [06_DATA_SQL_CONCURRENCY_SECURITY.md](./06_DATA_SQL_CONCURRENCY_SECURITY.md) | Mô hình dữ liệu PostgreSQL, EF Core configurations, khóa dòng `SELECT ... FOR UPDATE` khi Award Bid, bảo mật giá thầu, xác thực chữ ký số HMAC-SHA512 của VNPay. |
| **07** | [07_FRONTEND_INTEGRATION.md](./07_FRONTEND_INTEGRATION.md) | Kiến trúc tích hợp Frontend `mooncake-fe`, kết nối màn hình `BiddingAndQuoteComparison.jsx` và `BiddingPage.jsx`, Service `rfqService.js` và `paymentService.js`, token Luxury Gold. |
| **08** | [08_IMPLEMENTATION_TEST_ROLLOUT.md](./08_IMPLEMENTATION_TEST_ROLLOUT.md) | Lộ trình phát triển 6 giai đoạn (Phases 1-6), Ma trận kiểm thử Unit/Integration/E2E, Kịch bản test VNPay IPN Webhook, Tiêu chí hoàn thành (DoD). |

---

## 🎯 Căn cứ nghiệp vụ & Nguồn tham chiếu

1. **Tài liệu nghiệp vụ:** [mooncake-marketplace.md](../../business/mooncake-marketplace.md) - Module 5 (Features #10, #11, #12, #13).
2. **Template thiết kế chuẩn:** [FEATURE_DETAIL_DESIGN_TEMPLATE.md](../template/FEATURE_DETAIL_DESIGN_TEMPLATE.md).
3. **Cấu hình VNPay Sandbox:** [keyvnpay.text](../../../keyvnpay.text).
4. **Quy chuẩn mã nguồn:**
   - **Backend rules:** [AGENTS.md](../../AGENTS.md) (Clean Architecture, C# 12, .NET 8, English Only trong source code, Standard `ApiResponse<T>`, Static Helpers).
   - **Frontend rules:** [AGENTS.md](../../../../.agents/AGENTS.md) (React 19, 2 spaces indentation, ES2024, Không dùng `var`, Layered architecture, `apiClient` Axios tập trung, Mock fallback pattern, Theme Luxury Gold).

---

## ⚖️ Các nguyên tắc thiết kế bất biến (Core Invariants)

1. **Tính độc lập & Công bằng trong đấu thầu:** Trong suốt thời gian mở thầu (`Status = Open`), các nhà cung cấp gửi báo giá độc lập, tuyệt đối không nhìn thấy giá chào thầu của các đối thủ khác (Sealed Bidding).
2. **Quy tắc thời hạn đóng thầu (Deadline Integrity):** Khi thời gian hiện tại vượt qua `BidDeadline`, cổng nhận báo giá tự động đóng; hệ thống không cho phép nộp thêm báo giá mới hoặc sửa đổi báo giá đã gửi. Background job `RfqDeadlineJob` tự động cập nhật trạng thái `Open -> Closed`.
3. **Quy tắc trúng thầu duy nhất (Single Award Invariant):** Một yêu cầu báo giá chỉ có duy nhất 1 báo giá được chấp thuận (`Status = Accepted`). Khi khách hàng chọn thầu:
   - Bid được chọn chuyển sang `Accepted`.
   - Toàn bộ các Bid còn lại của RFQ đó tự động chuyển sang `Cancelled`.
   - RFQ chuyển sang `Awarded` và không thể hủy bỏ.
4. **Liên kết Ký kết Hợp đồng & Ký quỹ VNPay:**
   - Ngay khi bid được chọn, hệ thống tự động sinh bản thảo Hợp đồng điện tử (`Contract`) với trạng thái `Draft` và tạo mốc thanh toán đặt cọc bắt buộc (`Milestone M1 = Deposit`).
   - Khách hàng thực hiện thanh toán ký quỹ qua cổng thanh toán VNPay. Chỉ khi VNPay phản hồi thanh toán thành công (IPN URL / Return URL) với chữ ký số hợp lệ (`vnp_SecureHash`), mốc cọc mới chuyển sang `Paid` và Hợp đồng kích hoạt sang `Active` để xưởng bắt đầu sản xuất.

