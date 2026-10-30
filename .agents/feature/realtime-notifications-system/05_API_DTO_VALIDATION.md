# [API-SPEC] ĐẶC TẢ KỸ THUẬT API & DTO HỆ THỐNG THÔNG BÁO REAL-TIME
## Tài Liệu Thiết Kế Chi Tiết (Detail Design Document - Part 05)

---

## I. TỔNG QUAN VÀ DANH MỤC ENDPOINTS

Toàn bộ các yêu cầu HTTP đều kế thừa từ `BaseApiController`, yêu cầu xác thực `[Authorize]` bằng JWT Bearer Token, trả về cấu trúc chuẩn `ApiResponse<T>`, và áp dụng chính sách giới hạn tần suất (Rate Limiting) để chống tấn công cạn kiệt tài nguyên máy chủ.

| STT | Phương thức | Đường dẫn API | Mục đích nghiệp vụ | Rate Limit Policy |
| :--- | :--- | :--- | :--- | :--- |
| 1 | `GET` | `/api/v1/notifications` | Lấy danh sách thông báo phân trang (hỗ trợ lọc theo category & trạng thái đã đọc) | `notif-query` (60 req/min) |
| 2 | `GET` | `/api/v1/notifications/unread-count` | Lấy số lượng thông báo chưa đọc phục vụ hiển thị Badge Chuông | `notif-badge` (120 req/min) |
| 3 | `PATCH` | `/api/v1/notifications/{id}/read` | Đánh dấu 1 thông báo cụ thể là đã đọc | `notif-mutate` (60 req/min) |
| 4 | `POST` | `/api/v1/notifications/read-all` | Đánh dấu tất cả thông báo là đã đọc (Kèm Idempotency-Key) | `notif-mutate` (30 req/min) |
| 5 | `DELETE` | `/api/v1/notifications/{id}` | Xóa mềm một thông báo khỏi hộp thư người dùng | `notif-mutate` (30 req/min) |
| 6 | `GET` | `/api/v1/notifications/preferences` | Lấy cấu hình kênh nhận thông báo của người dùng hiện tại | `notif-query` (60 req/min) |
| 7 | `PUT` | `/api/v1/notifications/preferences` | Cập nhật tùy chọn nhận thông báo (In-app, Web Push, Email) | `notif-mutate` (30 req/min) |
| 8 | `POST` | `/api/v1/notifications/test-dispatch` | Endpoint thử nghiệm đẩy thông báo Real-time (Môi trường Dev/Test) | `notif-test` (10 req/min) |

---

## II. ĐẶC TẢ CHI TIẾT TỪNG ENDPOINT

### 2.1. `GET /api/v1/notifications` – Hộp Thư Thông Báo Phân Trang
- **Tham số Query (Query Parameters):**
  - `page`: Số trang (Mặc định: 1, tối thiểu: 1).
  - `pageSize`: Kích thước trang (Mặc định: 20, tối đa: 50).
  - `category`: Nhóm thông báo (`all`, `order`, `contract`, `rfq`, `payment`, `system`).
  - `isRead`: Bộ lọc trạng thái (`null` = tất cả, `true` = đã đọc, `false` = chưa đọc).
- **Phản hồi thành công (HTTP 200 OK):**
```json
{
  "status": 200,
  "message": "Lấy danh sách thông báo thành công.",
  "data": {
    "items": [
      {
        "id": "7b8d4e12-9c3a-4f5d-8e6b-123456789abc",
        "type": "payment_vnpay_success",
        "category": "payment",
        "title": "Thanh toán VNPay Sandbox thành công",
        "content": "Giao dịch PAY-2026-00045 trị giá 120.000.000 ₫ đã hoàn tất qua VNPay Sandbox. Tiền đã được ký quỹ an toàn.",
        "referenceId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
        "referenceType": "contract",
        "actionUrl": "/#/hop-dong-tien-do?id=9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
        "priority": "high",
        "isRead": false,
        "readAt": null,
        "createdAt": "2026-10-29T10:15:30Z"
      }
    ],
    "page": 1,
    "pageSize": 20,
    "totalItems": 15,
    "totalPages": 1,
    "unreadCount": 3
  },
  "path": "/api/v1/notifications",
  "traceId": "00-84a1e948f2cb-1",
  "time": "2026-10-29T10:16:00Z"
}
```

### 2.2. `GET /api/v1/notifications/unread-count` – Số Lượng Chưa Đọc
- **Mục đích:** Cung cấp thông tin siêu nhẹ cho Badge chuông trên Navbar khi người dùng mới vào web.
- **Phản hồi thành công (HTTP 200 OK):**
```json
{
  "status": 200,
  "message": "Thao tác thành công.",
  "data": {
    "unreadCount": 3,
    "urgentCount": 0,
    "latestTimestamp": "2026-10-29T10:15:30Z"
  },
  "path": "/api/v1/notifications/unread-count",
  "traceId": "00-84a1e948f2cb-2",
  "time": "2026-10-29T10:16:01Z"
}
```

### 2.3. `PATCH /api/v1/notifications/{id}/read` – Đánh Dấu Đã Đọc 1 Tin
- **Header:** `Authorization: Bearer <Token>`
- **URL Param:** `id` (GUID định danh thông báo).
- **Phản hồi thành công (HTTP 200 OK):**
```json
{
  "status": 200,
  "message": "Đã đánh dấu thông báo là đã đọc.",
  "data": {
    "id": "7b8d4e12-9c3a-4f5d-8e6b-123456789abc",
    "isRead": true,
    "readAt": "2026-10-29T10:16:05Z",
    "remainingUnreadCount": 2
  },
  "path": "/api/v1/notifications/7b8d4e12-9c3a-4f5d-8e6b-123456789abc/read",
  "traceId": "00-84a1e948f2cb-3",
  "time": "2026-10-29T10:16:05Z"
}
```

### 2.4. `POST /api/v1/notifications/read-all` – Đánh Dấu Đã Đọc Tất Cả
- **Header:** `Idempotency-Key: <UUID>` (Bắt buộc để chống gửi kép)
- **Request Body:**
```json
{
  "category": "all",
  "readThroughTimestamp": "2026-10-29T10:15:30Z"
}
```
- **Phản hồi thành công (HTTP 200 OK):**
```json
{
  "status": 200,
  "message": "Toàn bộ thông báo đã được đánh dấu đã đọc.",
  "data": {
    "updatedCount": 3,
    "unreadCount": 0
  },
  "path": "/api/v1/notifications/read-all",
  "traceId": "00-84a1e948f2cb-4",
  "time": "2026-10-29T10:16:10Z"
}
```

### 2.5. `GET` & `PUT /api/v1/notifications/preferences` – Cài Đặt Nhận Tin
- **Request Body (PUT):**
```json
{
  "enableInApp": true,
  "enableWebPush": true,
  "enableEmailDigest": false,
  "channels": {
    "order": { "inApp": true, "webPush": true, "email": true },
    "contract": { "inApp": true, "webPush": true, "email": true },
    "rfq": { "inApp": true, "webPush": false, "email": true },
    "payment": { "inApp": true, "webPush": true, "email": true },
    "system": { "inApp": true, "webPush": true, "email": false }
  }
}
```

---

## III. ĐỊNH NGHĨA DTO TRONG BACKEND C# (.NET 8)

```csharp
namespace Mooncake.EcommercePlatform.Application.DTOs.Notifications;

using System.ComponentModel.DataAnnotations;

public sealed record NotificationQueryRequest(
    [Range(1, int.MaxValue)] int Page = 1,
    [Range(1, 50)] int PageSize = 20,
    string? Category = null,
    bool? IsRead = null);

public sealed record NotificationItemDto(
    Guid Id,
    string Type,
    string Category,
    string Title,
    string Content,
    Guid? ReferenceId,
    string? ReferenceType,
    string? ActionUrl,
    string Priority,
    bool IsRead,
    DateTime? ReadAt,
    DateTime CreatedAt);

public sealed record NotificationInboxResponse(
    IReadOnlyList<NotificationItemDto> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages,
    int UnreadCount);

public sealed record MarkAllReadRequest(
    string? Category = "all",
    DateTime? ReadThroughTimestamp = null);

public sealed record NotificationPreferencesDto(
    bool EnableInApp,
    bool EnableWebPush,
    bool EnableEmailDigest,
    Dictionary<string, ChannelConfigDto> Channels);

public sealed record ChannelConfigDto(
    bool InApp,
    bool WebPush,
    bool Email);
```

---

## IV. BẢNG MÃ LỖI & THÔNG ĐIỆP PHẢN HỒI (ERROR CODES)

| HTTP Status | Ngữ cảnh nghiệp vụ phát sinh | Thông điệp hiển thị cho người dùng |
| :--- | :--- | :--- |
| `400 Bad Request` | Tham số query `pageSize > 50` hoặc `page < 1` | `"Kích thước trang hoặc số trang không hợp lệ."` |
| `401 Unauthorized` | Không có hoặc hết hạn JWT Bearer Token | `"Phiên làm việc đã kết thúc. Vui lòng đăng nhập lại."` |
| `403 Forbidden` | Cố gắng đọc thông báo thuộc về tài khoản người dùng khác | `"Bạn không có quyền truy cập thông báo này."` |
| `404 Not Found` | Thông báo không tồn tại hoặc đã bị xóa | `"Thông báo yêu cầu không tồn tại trên hệ thống."` |
| `409 Conflict` | Thiếu hoặc trùng lặp `Idempotency-Key` khi thực hiện `read-all` | `"Yêu cầu xử lý đang được thực thi. Vui lòng không nhấn liên tục."` |
| `429 Too Many Requests` | Vượt ngưỡng Rate Limit cho phép | `"Bạn thao tác quá nhanh. Vui lòng chờ vài giây trước khi thử lại."` |

