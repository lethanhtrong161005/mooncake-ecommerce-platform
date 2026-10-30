# MODULE 8: ĐẶC TẢ KỸ THUẬT API & DTO VALIDATION (API CONTRACT SPECIFICATION)
## Tài Liệu Thiết Kế Chi Tiết (API Specification & Contracts)

---

## I. TỔNG QUAN HỆ THỐNG ENDPOINTS

Hệ thống API của Module 8 được xây dựng theo chuẩn RESTful Enterprise, kế thừa `BaseApiController`, trả về định dạng bao gói thống nhất `ApiResponse<T>`, và tuân thủ các chính sách bảo mật định danh Bearer Token (JWT), Rate Limiting và Idempotency.

### 1.1. Bảng Tổng Hợp 12 Endpoints Nghiệp Vụ

| STT | Phương thức | Đường dẫn API | Xác thực / Quyền hạn | Mô tả chức năng |
| :---: | :---: | :--- | :--- | :--- |
| 1 | `POST` | `/api/v1/reviews/transaction` | Bearer Token (`Customer`) | Gửi đánh giá giao dịch 4 tiêu chí cho Đơn hàng/Hợp đồng đã nhận |
| 2 | `GET` | `/api/v1/reviews/transaction/eligible-orders` | Bearer Token (`Customer`) | Lấy danh sách đơn hàng đã Delivered chưa được đánh giá |
| 3 | `POST` | `/api/v1/reviews/transaction/{reviewId}/reply` | Bearer Token (`Supplier`) | Nhà cung cấp gửi phản hồi chính thức (1 lần duy nhất) cho review giao dịch |
| 4 | `GET` | `/api/v1/shops/{slug}/reviews` | Anonymous (Công khai) | Lấy danh sách đánh giá của gian hàng kèm thống kê 4 tiêu chí |
| 5 | `GET` | `/api/v1/products/{productId}/reviews` | Anonymous (Công khai) | Lấy luồng bình luận sản phẩm/biến thể kèm biểu đồ Histogram 1–5 sao |
| 6 | `GET` | `/api/v1/products/{productId}/eligible-order-items` | Bearer Token (`Customer`) | Lấy các OrderItem đã mua đủ điều kiện viết Verified Review |
| 7 | `POST` | `/api/v1/products/{productId}/reviews` | Bearer Token (`Customer`) | Đăng đánh giá sản phẩm/biến thể xác thực mua hàng (Verified Purchase) |
| 8 | `POST` | `/api/v1/products/reviews/{reviewId}/like` | Bearer Token (`Customer`, `Supplier`) | Thả thích / Bỏ thích (Toggle Like) bài review sản phẩm |
| 9 | `POST` | `/api/v1/products/reviews/{reviewId}/reply` | Bearer Token (`Supplier`) | Nhà cung cấp gửi phản hồi cho bài review trên trang sản phẩm |
| 10 | `GET` | `/api/v1/suppliers/{supplierId}/reputation` | Bearer Token / Public | Lấy điểm uy tín, huy hiệu và lịch sử nhật ký biến động điểm (`reputation_logs`) |
| 11 | `PATCH` | `/api/v1/admin/reviews/moderate` | Bearer Token (`Admin`) | Quản trị viên ẩn hoặc hiện đánh giá vi phạm chính sách |
| 12 | `POST` | `/api/v1/admin/suppliers/{supplierId}/reputation/adjustment` | Bearer Token (`Admin`) | Admin áp dụng chế tài phạt điểm hoặc thưởng điểm uy tín |

---

## II. ĐẶC TẢ CHI TIẾT DTO REQUEST & RESPONSE (C# 12 / .NET 8)

### 2.1. Nhóm DTO Đánh Giá Giao Dịch (Feature #21)

#### Request: `CreateTransactionReviewRequest`
```csharp
namespace Mooncake.EcommercePlatform.Application.DTOs.Reviews.Requests;

using System.ComponentModel.DataAnnotations;

public sealed record CreateTransactionReviewRequest
{
    public Guid? OrderId { get; init; }

    public Guid? ContractId { get; init; }

    [Range(1, 5, ErrorMessage = "Điểm chất lượng bánh phải từ 1 đến 5 sao.")]
    public short QualityRating { get; init; } = 5;

    [Range(1, 5, ErrorMessage = "Điểm đúng hạn giao hàng phải từ 1 đến 5 sao.")]
    public short DeliveryRating { get; init; } = 5;

    [Range(1, 5, ErrorMessage = "Điểm dịch vụ hỗ trợ phải từ 1 đến 5 sao.")]
    public short ServiceRating { get; init; } = 5;

    [Range(1, 5, ErrorMessage = "Điểm đánh giá tổng thể phải từ 1 đến 5 sao.")]
    public short OverallRating { get; init; } = 5;

    [Required(ErrorMessage = "Vui lòng nhập nhận xét chi tiết.")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Nhận xét phải từ 10 đến 2000 ký tự.")]
    public string Comment { get; init; } = string.Empty;

    public bool IsAnonymous { get; init; } = false;

    public List<string>? MediaUrls { get; init; }
}
```

#### Response: `TransactionReviewResponse`
```csharp
namespace Mooncake.EcommercePlatform.Application.DTOs.Reviews.Responses;

public sealed record TransactionReviewResponse
{
    public Guid Id { get; init; }
    public Guid CustomerId { get; init; }
    public string CustomerDisplayName { get; init; } = string.Empty;
    public string? CustomerAvatarUrl { get; init; }
    public bool IsAnonymous { get; init; }
    public Guid SupplierId { get; init; }
    public string SupplierName { get; init; } = string.Empty;
    public Guid? OrderId { get; init; }
    public string? OrderNumber { get; init; }
    public Guid? ContractId { get; init; }
    public string? ContractNumber { get; init; }
    public short OverallRating { get; init; }
    public short QualityRating { get; init; }
    public short DeliveryRating { get; init; }
    public short ServiceRating { get; init; }
    public string Comment { get; init; } = string.Empty;
    public List<string> MediaUrls { get; init; } = new();
    public string? SupplierReply { get; init; }
    public DateTime? SupplierRepliedAt { get; init; }
    public decimal ReputationScoreDelta { get; init; }
    public decimal NewSupplierReputationScore { get; init; }
    public DateTime CreatedAt { get; init; }
}
```

---

### 2.2. Nhóm DTO Luồng Review Sản Phẩm (Feature #22)

#### Request: `CreateProductReviewRequest`
```csharp
namespace Mooncake.EcommercePlatform.Application.DTOs.Reviews.Requests;

using System.ComponentModel.DataAnnotations;

public sealed record CreateProductReviewRequest
{
    [Required(ErrorMessage = "Bắt buộc phải có mã chi tiết đơn hàng (OrderItemId).")]
    public Guid OrderItemId { get; init; }

    public Guid? ProductVariantId { get; init; }

    [Range(1, 5, ErrorMessage = "Số sao đánh giá phải từ 1 đến 5.")]
    public short Rating { get; init; } = 5;

    [StringLength(255, ErrorMessage = "Tiêu đề không quá 255 ký tự.")]
    public string? Title { get; init; }

    [Required(ErrorMessage = "Nội dung nhận xét không được để trống.")]
    [StringLength(3000, MinimumLength = 10, ErrorMessage = "Nhận xét phải từ 10 đến 3000 ký tự.")]
    public string Content { get; init; } = string.Empty;

    public List<string>? MediaUrls { get; init; }
}
```

#### Response: `ProductReviewFeedResponse`
```csharp
namespace Mooncake.EcommercePlatform.Application.DTOs.Reviews.Responses;

public sealed record ProductReviewFeedResponse
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public ProductReviewSummaryDto Summary { get; init; } = new();
    public List<ProductReviewItemDto> Items { get; init; } = new();
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
    public int TotalCount { get; init; }
    public bool HasMore { get; init; }
}

public sealed record ProductReviewSummaryDto
{
    public decimal AverageRating { get; init; }
    public int TotalReviews { get; init; }
    public int VerifiedPurchaseCount { get; init; }
    public int WithMediaCount { get; init; }
    public StarDistributionDto StarDistribution { get; init; } = new();
}

public sealed record StarDistributionDto
{
    public int Star5 { get; init; }
    public int Star4 { get; init; }
    public int Star3 { get; init; }
    public int Star2 { get; init; }
    public int Star1 { get; init; }
}

public sealed record ProductReviewItemDto
{
    public Guid Id { get; init; }
    public string AuthorName { get; init; } = string.Empty;
    public string? AuthorAvatarUrl { get; init; }
    public bool IsVerifiedPurchase { get; init; }
    public Guid? VariantId { get; init; }
    public string? VariantName { get; init; }
    public short Rating { get; init; }
    public string? Title { get; init; }
    public string Content { get; init; } = string.Empty;
    public List<string> MediaUrls { get; init; } = new();
    public int LikeCount { get; init; }
    public bool IsLikedByMe { get; init; }
    public string? SupplierReply { get; init; }
    public DateTime? SupplierRepliedAt { get; init; }
    public DateTime CreatedAt { get; init; }
}
```

---

### 2.3. Nhóm DTO Động Cơ Điểm Uy Tín (Feature #22b)

#### Response: `SupplierReputationResponse`
```csharp
namespace Mooncake.EcommercePlatform.Application.DTOs.Reviews.Responses;

public sealed record SupplierReputationResponse
{
    public Guid SupplierId { get; init; }
    public string CompanyName { get; init; } = string.Empty;
    public decimal ReputationScore { get; init; }
    public string BadgeTier { get; init; } = string.Empty; // New, Reliable, Reputable, GoldPartner
    public string BadgeTitle { get; init; } = string.Empty; // Mới, Tin cậy, Uy tín, Đối tác Vàng
    public string BadgeColor { get; init; } = string.Empty; // stone, blue, purple, amber
    public SupplierStatsDto Stats { get; init; } = new();
    public List<ReputationLogDto> RecentLogs { get; init; } = new();
}

public sealed record SupplierStatsDto
{
    public int TotalContracts { get; init; }
    public int ContractsOnTime { get; init; }
    public int ContractsLate { get; init; }
    public int ContractsBreached { get; init; }
    public decimal? AverageRating { get; init; }
    public int PositiveReviewsCount { get; init; }
    public int NegativeReviewsCount { get; init; }
}

public sealed record ReputationLogDto
{
    public Guid Id { get; init; }
    public string EventType { get; init; } = string.Empty;
    public decimal ScoreDelta { get; init; }
    public string? Reason { get; init; }
    public Guid? ReferenceId { get; init; }
    public string? ReferenceType { get; init; }
    public DateTime CreatedAt { get; init; }
}
```

---

## III. BẢNG MÃ LỖI & THÔNG ĐIỆP PHẢN HỒI (ERROR CODES & LOCALIZATION)

| HTTP Status | Ngữ cảnh nghiệp vụ | Mã lỗi (`errorCode`) | Message hiển thị cho người dùng |
| :--- | :--- | :--- | :--- |
| `400 Bad Request` | Điểm sao không nằm trong khoảng 1..5 | `INVALID_RATING_RANGE` | `"Điểm đánh giá phải từ 1 đến 5 sao."` |
| `400 Bad Request` | Nhà cung cấp đã gửi phản hồi trước đó | `ALREADY_REPLIED` | `"Nhà cung cấp chỉ được gửi phản hồi chính thức 1 lần duy nhất."` |
| `400 Bad Request` | Đơn hàng chưa chuyển sang trạng thái Delivered | `ORDER_NOT_DELIVERED` | `"Đơn hàng chưa hoàn thành giao nhận, không thể gửi đánh giá."` |
| `403 Forbidden` | Khách hàng không sở hữu đơn hàng | `FORBIDDEN_ORDER_ACCESS` | `"Quý khách không có quyền đánh giá đơn hàng của doanh nghiệp khác."` |
| `404 Not Found` | Không tìm thấy sản phẩm hoặc đơn hàng | `RESOURCE_NOT_FOUND` | `"Dữ liệu yêu cầu không tồn tại trên hệ thống."` |
| `409 Conflict` | Đã gửi đánh giá cho đơn hàng/item này rồi | `DUPLICATE_REVIEW` | `"Giao dịch này đã được quý khách gửi đánh giá trước đó."` |
| `429 Too Many Requests` | Thao tác thả Like hoặc gửi đánh giá dồn dập | `RATE_LIMIT_EXCEEDED` | `"Thao tác quá nhanh. Vui lòng thử lại sau vài giây."` |

