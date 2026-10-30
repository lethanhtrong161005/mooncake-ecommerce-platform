# [API-SPEC] ĐẶC TẢ KỸ THUẬT API, DTO & VALIDATION CHO TOÀN BỘ PHÂN HỆ VẬN HÀNH
## Tài Liệu Thiết Kế Chi Tiết (Detail Design Document - Part 05)

---

## I. TỔNG QUAN VÀ DANH MỤC ENDPOINTS

Toàn bộ API đều tuân thủ kiến trúc RESTful, kế thừa `BaseApiController`, yêu cầu xác thực JWT Bearer Token, kiểm soát phân quyền dựa trên vai trò (`Role-Based Access Control`), và trả về định dạng bọc chuẩn `ApiResponse<T>`.

| STT | Phương thức | Đường dẫn API | Mục đích nghiệp vụ | Quyền truy cập |
| :--- | :--- | :--- | :--- | :--- |
| **Nhóm 1: Thông Báo Real-time (Feature #26)** | | | | |
| 1 | `GET` | `/api/v1/notifications` | Lấy danh sách hộp thư thông báo phân trang | All Authenticated |
| 2 | `GET` | `/api/v1/notifications/unread-count` | Lấy số lượng thông báo chưa đọc cho Badge chuông | All Authenticated |
| 3 | `PATCH` | `/api/v1/notifications/{id}/read` | Đánh dấu 1 thông báo cụ thể là đã đọc | All Authenticated |
| 4 | `POST` | `/api/v1/notifications/read-all` | Đánh dấu tất cả thông báo là đã đọc (Idempotency) | All Authenticated |
| 5 | `GET/PUT`| `/api/v1/notifications/preferences` | Xem / Cập nhật cấu hình kênh nhận tin | All Authenticated |
| **Nhóm 2: Quản Trị Hệ Thống & Trọng Tài (Feature #27)** | | | | |
| 6 | `GET` | `/api/v1/admin/users` | Danh sách người dùng sàn phân trang & bộ lọc vai trò | `Admin` |
| 7 | `PATCH` | `/api/v1/admin/users/{id}/status` | Khóa hoặc mở lại tài khoản người dùng | `Admin` |
| 8 | `GET` | `/api/v1/admin/shops` | Danh sách gian hàng, theo dõi GMV và trạng thái | `Admin` |
| 9 | `POST` | `/api/v1/admin/shops/{id}/suspend` | Tạm đình chỉ hoạt động gian hàng kèm lý do | `Admin` |
| 10 | `POST` | `/api/v1/admin/shops/{id}/reactivate` | Kích hoạt mở lại gian hàng | `Admin` |
| 11 | `GET` | `/api/v1/admin/disputes` | Danh sách các vụ tranh chấp hợp đồng B2B | `Admin` |
| 12 | `GET` | `/api/v1/admin/disputes/{id}` | Chi tiết vụ tranh chấp, bằng chứng ảnh PoD, số dư cọc | `Admin` |
| 13 | `POST` | `/api/v1/admin/disputes/{id}/resolve` | Phân xử tranh chấp, quyết định hoàn cọc và trừ uy tín | `Admin` |
| 14 | `GET/PUT`| `/api/v1/admin/configs` | Xem và cập nhật tham số cấu hình sàn động | `Admin` |
| **Nhóm 3: Báo Cáo & Thống Kê Đa Chiều (Feature #28)** | | | | |
| 15 | `GET` | `/api/v1/reports/dashboard-metrics` | Các chỉ số tổng quan GMV, Đơn hàng, Hợp đồng B2B | `Admin` |
| 16 | `GET` | `/api/v1/reports/conversion-funnel` | Dữ liệu 5 tầng phễu chuyển đổi RFQ $\rightarrow$ Hợp đồng | `Admin` |
| 17 | `GET` | `/api/v1/reports/top-suppliers` | Bảng xếp hạng Top Nhà cung cấp xuất sắc | `Admin`, `Supplier` |
| 18 | `POST` | `/api/v1/reports/ai-summary` | Gọi Gemini AI sinh nhận định chiến lược thị trường | `Admin`, `Supplier` |
| 19 | `GET` | `/api/v1/reports/export` | Xuất dữ liệu thống kê ra file CSV / Excel / PDF | `Admin` |

---

## II. ĐẶC TẢ CHI TIẾT REQUEST & RESPONSE PAYLOADS

### 2.1. Phân Xử Tranh Chấp & Hoàn Tiền Ký Quỹ SafeTrade
- **Endpoint:** `POST /api/v1/admin/disputes/{id}/resolve`
- **Request Body:**
```json
{
  "resolutionType": "SupplierAtFault",
  "adminVerdict": "Xưởng giao bánh chậm 7 ngày so với tiến độ cam kết mốc M2 làm hỏng kế hoạch tặng quà của doanh nghiệp mua. Áp dụng chế tài vi phạm hợp đồng.",
  "refundAmount": 120000000,
  "deductReputationPoints": 20,
  "notes": "Đã kiểm tra bằng chứng ảnh PoD và biên bản làm việc giữa 2 bên."
}
```
- **Response Body (HTTP 200 OK):**
```json
{
  "status": 200,
  "message": "Phân xử tranh chấp và xử lý hoàn tiền ký quỹ thành công.",
  "data": {
    "disputeId": "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d",
    "contractId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
    "contractNumber": "CTR-2026-00012",
    "status": "Resolved",
    "resolutionType": "SupplierAtFault",
    "refundAmount": 120000000,
    "refundPaymentNumber": "REF-2026-00003",
    "supplierNewReputationScore": 68,
    "resolvedAt": "2026-10-29T10:40:00Z"
  },
  "path": "/api/v1/admin/disputes/a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d/resolve",
  "traceId": "00-84a1e948f2cb-admin-1",
  "time": "2026-10-29T10:40:00Z"
}
```

### 2.2. Nhận Định Chiến Lược Gemini AI Cho Báo Cáo
- **Endpoint:** `POST /api/v1/reports/ai-summary`
- **Request Body:**
```json
{
  "timeframe": "this_month",
  "totalGmv": 18450000000,
  "rfqCount": 150,
  "contractCount": 128,
  "conversionRate": 85.3
}
```
- **Response Body (HTTP 200 OK):**
```json
{
  "status": 200,
  "message": "Sinh nhận định phân tích AI thành công.",
  "data": {
    "model": "gemini-flash-latest",
    "summaryMarkdown": "### 🌕 Báo Cáo Chiến Lược Thị Trường Bánh Trung Thu B2B (Tháng 10/2026)\n\n**1. Đánh giá tăng trưởng GMV:**\nTổng giá trị giao dịch đạt **18.45 Tỷ VNĐ**, tăng trưởng **+18.4%** so với tháng trước nhờ nhu cầu đặt quà tặng doanh nghiệp tăng vọt trước Tết Đoàn Viên.\n\n**2. Hiệu quả phễu RFQ:**\nTỷ lệ chuyển đổi từ báo giá sang hợp đồng ký số đạt mức kỷ lục **85.3%**, cho thấy chất lượng hồ sơ chào thầu của các xưởng bánh liên tục được nâng cao.\n\n**3. Khuyến nghị vận hành:**\nCần tiếp tục duy trì chính sách cọc **30% mốc M1** qua cổng VNPay Sandbox để bảo vệ dòng tiền ký quỹ Escrow an toàn tuyệt đối.",
    "generatedAt": "2026-10-29T10:45:00Z"
  },
  "path": "/api/v1/reports/ai-summary",
  "traceId": "00-84a1e948f2cb-ai-1",
  "time": "2026-10-29T10:45:00Z"
}
```

---

## III. ĐỊNH NGHĨA DTO TRONG BACKEND C# (.NET 8)

```csharp
namespace Mooncake.EcommercePlatform.Application.DTOs.SystemOperations;

using System.ComponentModel.DataAnnotations;

// DTOs cho Quản Trị Tranh Chấp & SafeTrade
public sealed record ResolveDisputeRequest(
    [Required] string ResolutionType,
    [Required, StringLength(1000)] string AdminVerdict,
    [Range(0, 5000000000)] decimal RefundAmount,
    [Range(0, 50)] int DeductReputationPoints = 0,
    string? Notes = null);

public sealed record DisputeDetailResponse(
    Guid Id,
    Guid ContractId,
    string ContractNumber,
    string CustomerName,
    string SupplierName,
    string DisputeReason,
    IReadOnlyList<string> EvidenceUrls,
    string Status,
    string? ResolutionType,
    string? AdminVerdict,
    decimal RefundAmount,
    decimal HeldEscrowAmount,
    DateTime RaisedAt,
    DateTime? ResolvedAt);

// DTOs cho Cấu Hình Sàn Động
public sealed record UpdateSystemConfigRequest(
    [Required] string ValueJson,
    string? Description = null);

// DTOs cho Báo Cáo Thống Kê Đa Chiều
public sealed record DashboardMetricsDto(
    decimal TotalGmv,
    decimal PlatformRevenue,
    int PendingSupplierApprovals,
    int ActiveContractsCount,
    int PendingDisputesCount,
    decimal TotalEscrowBalance,
    double GrowthRatePercentage);

public sealed record ConversionFunnelStageDto(
    string StageName,
    int Count,
    double ConversionRatePercentage);

public sealed record ConversionFunnelReportDto(
    string Timeframe,
    IReadOnlyList<ConversionFunnelStageDto> Stages,
    double OverallSuccessRate);
```

---

## IV. BẢNG MÃ LỖI & THÔNG ĐIỆP PHẢN HỒI (HTTP ERROR CODES)

| HTTP Status | Ngữ cảnh nghiệp vụ | Thông điệp phản hồi tiếng Việt |
| :--- | :--- | :--- |
| `400 Bad Request` | Số tiền hoàn cọc vượt quá số dư ký quỹ Escrow thực tế | `"Số tiền hoàn trả không được vượt quá số dư ký quỹ đang giữ của hợp đồng."` |
| `401 Unauthorized` | Không có token hoặc token đã hết hạn | `"Phiên làm việc đã hết hạn. Vui lòng đăng nhập lại."` |
| `403 Forbidden` | Người dùng không mang vai trò `Admin` | `"Bạn không có quyền truy cập vào chức năng quản trị hệ thống."` |
| `404 Not Found` | Vụ tranh chấp hoặc gian hàng không tồn tại | `"Không tìm thấy dữ liệu yêu cầu trên hệ thống."` |
| `409 Conflict` | Vụ tranh chấp đã được phân xử trước đó | `"Vụ tranh chấp này đã được phân xử và đóng hồ sơ."` |
| `429 Too Many Requests` | Thao tác xuất báo cáo quá dồn dập | `"Bạn thao tác xuất file quá nhanh. Vui lòng chờ 30 giây."` |
| `500 Server Error` | Lỗi phát sinh ngoài ý muốn | `"Hệ thống đang bận. Vui lòng thử lại sau giây lát."` |

