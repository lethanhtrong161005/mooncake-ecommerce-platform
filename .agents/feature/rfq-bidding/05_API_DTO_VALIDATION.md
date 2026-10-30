# [RFQ-BIDDING-05] - ĐẶC TẢ HỢP ĐỒNG API, DTO & QUY TẮC VALIDATION (MODULE 5)

---

## I. TỔNG QUAN HỢP ĐỒNG API (API ENDPOINT MATRIX)

Toàn bộ các API của Module 5 được chuẩn hóa theo tiền tố `/api/v1` và bọc dữ liệu trong cấu trúc chuẩn `ApiResponse<T>`.

### 1.1. Nhóm API Quản lý Yêu cầu báo giá (Feature #10: RFQ Management)
| Phương thức | Đường dẫn (URL) | Phân quyền (Auth) | Mô tả nghiệp vụ |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/v1/rfqs` | `Customer` | Khởi tạo yêu cầu báo giá mới (Trạng thái `Draft`) |
| `GET` | `/api/v1/rfqs/mine` | `Customer` | Lấy danh sách RFQ do khách hàng hiện tại tạo |
| `GET` | `/api/v1/rfqs/{rfqId}` | `Customer, Supplier` | Xem chi tiết RFQ (Xưởng xem nếu được mời hoặc công khai) |
| `POST` | `/api/v1/rfqs/{rfqId}/publish` | `Customer` | Phát hành RFQ sang trạng thái `Open` |
| `POST` | `/api/v1/rfqs/{rfqId}/cancel` | `Customer` | Hủy gói thầu RFQ (kèm lý do) |

### 1.2. Nhóm API Mời nhà cung cấp (Feature #11: Invitations & Matching)
| Phương thức | Đường dẫn (URL) | Phân quyền (Auth) | Mô tả nghiệp vụ |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/v1/rfqs/{rfqId}/suggested-suppliers` | `Customer` | Lấy danh sách xưởng bánh gợi ý phù hợp (Top 10) |
| `POST` | `/api/v1/rfqs/{rfqId}/invitations` | `Customer` | Gửi lời mời thầu tới tối đa 10 xưởng |
| `POST` | `/api/v1/rfqs/{rfqId}/decline` | `Supplier` | Xưởng từ chối tham gia chào thầu kèm lý do |

### 1.3. Nhóm API Báo giá & So sánh thầu (Feature #12: Bidding)
| Phương thức | Đường dẫn (URL) | Phân quyền (Auth) | Mô tả nghiệp vụ |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/v1/rfqs/{rfqId}/bids` | `Supplier` | Nộp hồ sơ báo giá hoặc cập nhật báo giá trước deadline |
| `GET` | `/api/v1/rfqs/{rfqId}/bids` | `Customer` | Lấy danh sách toàn bộ báo giá để so sánh Side-by-Side |
| `POST` | `/api/v1/rfqs/{rfqId}/bids/{bidId}/cancel` | `Customer` | Khách chủ động hủy/loại bỏ 1 báo giá không phù hợp |

### 1.4. Nhóm API Chọn thầu & Ký quỹ VNPay (Feature #13: Awarding & VNPay Escrow)
| Phương thức | Đường dẫn (URL) | Phân quyền (Auth) | Mô tả nghiệp vụ |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/v1/rfqs/{rfqId}/bids/{bidId}/accept` | `Customer` | Chấp thuận 1 báo giá, hủy các bid khác & sinh Hợp đồng |
| `POST` | `/api/v1/payments/vnpay/create-url` | `Customer` | Sinh URL thanh toán VNPay Sandbox cho mốc cọc M1 |
| `GET` | `/api/v1/payments/vnpay/ipn` | `Public (VNPay Webhook)` | Nhận kết quả thanh toán ngầm từ VNPay và kích hoạt Contract |
| `GET` | `/api/v1/payments/vnpay/return` | `Customer (Browser)` | Nhận redirect trình duyệt từ VNPay sau khi thanh toán |

---

## II. ĐẶC TẢ DTO REQUEST VÀ RULES VALIDATION

### 2.1. Request DTOs: Yêu cầu báo giá (RFQ)

```csharp
namespace Mooncake.EcommercePlatform.Application.DTOs.Rfqs.Requests;

using System.ComponentModel.DataAnnotations;

public sealed record CreateRfqRequest
{
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 200 characters.")]
    public string Title { get; init; } = string.Empty;

    [MaxLength(5000, ErrorMessage = "Description cannot exceed 5000 characters.")]
    public string? Description { get; init; }

    [Required(ErrorMessage = "Delivery address is required.")]
    [StringLength(1000, MinimumLength = 10, ErrorMessage = "Delivery address must be between 10 and 1000 characters.")]
    public string DeliveryAddress { get; init; } = string.Empty;

    [Required(ErrorMessage = "Delivery date is required.")]
    public DateOnly DeliveryDateRequired { get; init; }

    [Range(0, 100_000_000_000, ErrorMessage = "BudgetRangeMin must be non-negative.")]
    public decimal? BudgetRangeMin { get; init; }

    [Range(0, 100_000_000_000, ErrorMessage = "BudgetRangeMax must be non-negative.")]
    public decimal? BudgetRangeMax { get; init; }

    [Required(ErrorMessage = "Bid deadline is required.")]
    public DateTime BidDeadline { get; init; }

    [Required(ErrorMessage = "Items list is required.")]
    [MinLength(1, ErrorMessage = "At least one item must be requested.")]
    [MaxLength(100, ErrorMessage = "Cannot request more than 100 items per RFQ.")]
    public List<CreateRfqItemRequest> Items { get; init; } = [];
}

public sealed record CreateRfqItemRequest
{
    [Required(ErrorMessage = "Product name is required.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Product name must be between 2 and 200 characters.")]
    public string ProductName { get; init; } = string.Empty;

    [MaxLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
    public string? Description { get; init; }

    [Range(1, 1_000_000, ErrorMessage = "Quantity must be between 1 and 1,000,000.")]
    public int Quantity { get; init; }

    [Required(ErrorMessage = "Unit is required (e.g. Hộp, Chiếc, Set).")]
    [MaxLength(50, ErrorMessage = "Unit cannot exceed 50 characters.")]
    public string Unit { get; init; } = string.Empty;

    [Range(0, 1_000_000_000, ErrorMessage = "Target price must be non-negative.")]
    public decimal? TargetPrice { get; init; }

    [MaxLength(4000, ErrorMessage = "Specifications cannot exceed 4000 characters.")]
    public string? Specifications { get; init; }

    [MaxLength(1000, ErrorMessage = "Packaging requirements cannot exceed 1000 characters.")]
    public string? PackagingRequirements { get; init; }

    public bool NeedsCustomPackaging { get; init; } = false;
}

public sealed record CancelRfqRequest
{
    [Required(ErrorMessage = "Cancellation reason is required.")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "Reason must be between 5 and 500 characters.")]
    public string Reason { get; init; } = string.Empty;
}
```

### 2.2. Request DTOs: Lời mời & Nộp Báo giá (Invitations & Bids)

```csharp
namespace Mooncake.EcommercePlatform.Application.DTOs.Rfqs.Requests;

using System.ComponentModel.DataAnnotations;

public sealed record InviteSuppliersRequest
{
    [Required(ErrorMessage = "SupplierUserIds list is required.")]
    [MinLength(1, ErrorMessage = "At least one supplier must be invited.")]
    [MaxLength(10, ErrorMessage = "Cannot invite more than 10 suppliers per RFQ.")]
    public List<Guid> SupplierUserIds { get; init; } = [];
}

public sealed record DeclineInvitationRequest
{
    [Required(ErrorMessage = "Decline reason is required.")]
    [StringLength(1000, MinimumLength = 3, ErrorMessage = "Reason must be between 3 and 1000 characters.")]
    public string Reason { get; init; } = string.Empty;
}

public sealed record SubmitBidRequest
{
    [Required(ErrorMessage = "Items quotation is required.")]
    [MinLength(1, ErrorMessage = "At least one item must be quoted.")]
    [MaxLength(100, ErrorMessage = "Cannot quote more than 100 items.")]
    public List<SubmitBidItemRequest> Items { get; init; } = [];

    [Range(1, 365, ErrorMessage = "Estimated delivery days must be between 1 and 365.")]
    public int EstimatedDeliveryDays { get; init; }

    [Range(1, 365, ErrorMessage = "Validity days must be between 1 and 365.")]
    public int ValidityDays { get; init; } = 30;

    [MaxLength(4000, ErrorMessage = "Notes cannot exceed 4000 characters.")]
    public string? Notes { get; init; }
}

public sealed record SubmitBidItemRequest
{
    [Required(ErrorMessage = "RfqItemId is required.")]
    public Guid RfqItemId { get; init; }

    [Range(0.01, 1_000_000_000, ErrorMessage = "UnitPrice must be greater than 0.")]
    public decimal UnitPrice { get; init; }

    [MaxLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters.")]
    public string? Notes { get; init; }
}

public sealed record AwardBidRequest
{
    [MaxLength(2000, ErrorMessage = "Selection note cannot exceed 2000 characters.")]
    public string? SelectionNote { get; init; }
}
```

### 2.3. Request DTOs: Thanh toán ký quỹ VNPay

```csharp
namespace Mooncake.EcommercePlatform.Application.DTOs.Payments.Requests;

using System.ComponentModel.DataAnnotations;

public sealed record CreateVnPayUrlRequest
{
    [Required(ErrorMessage = "ContractId is required.")]
    public Guid ContractId { get; init; }

    [Required(ErrorMessage = "MilestoneId is required.")]
    public Guid MilestoneId { get; init; }

    [Required(ErrorMessage = "ReturnUrl is required.")]
    [Url(ErrorMessage = "ReturnUrl must be a valid URL.")]
    public string ReturnUrl { get; init; } = string.Empty;
}
```

---

## III. ĐẶC TẢ DTO RESPONSE CHUẨN HÓA

### 3.1. Phản hồi Yêu cầu báo giá (`RfqResponse`)

```csharp
namespace Mooncake.EcommercePlatform.Application.DTOs.Rfqs.Responses;

using Mooncake.EcommercePlatform.Domain.Enums;

public sealed record RfqResponse(
    Guid Id,
    string RfqNumber,
    string? Title,
    string? Description,
    string? DeliveryAddress,
    DateOnly? DeliveryDateRequired,
    decimal? BudgetRangeMin,
    decimal? BudgetRangeMax,
    DateTime? BidDeadline,
    RfqStatus Status,
    IReadOnlyList<RfqItemResponse> Items,
    int InvitationCount,
    int BidCount);

public sealed record RfqItemResponse(
    Guid Id,
    string? ProductName,
    string? Description,
    int Quantity,
    string? Unit,
    decimal? TargetPrice,
    string? Specifications,
    string? PackagingRequirements,
    bool NeedsCustomPackaging);
```

### 3.2. Phản hồi Hồ sơ Báo giá (`BidResponse`)

```csharp
namespace Mooncake.EcommercePlatform.Application.DTOs.Rfqs.Responses;

using Mooncake.EcommercePlatform.Domain.Enums;

public sealed record BidResponse(
    Guid Id,
    string BidNumber,
    Guid SupplierId,
    string? SupplierName,
    decimal? ReputationScore,
    string? Badge,
    decimal? TotalPrice,
    int? EstimatedDeliveryDays,
    int ValidityDays,
    DateTime SubmittedAt,
    bool IsExpired,
    BidStatus Status,
    IReadOnlyList<BidItemResponse> Items);

public sealed record BidItemResponse(
    Guid RfqItemId,
    string? ProductName,
    int Quantity,
    string? Unit,
    decimal UnitPrice,
    decimal Subtotal,
    string? Notes);
```

### 3.3. Phản hồi URL thanh toán VNPay (`VnPayUrlResponse`)

```csharp
namespace Mooncake.EcommercePlatform.Application.DTOs.Payments.Responses;

public sealed record VnPayUrlResponse(
    Guid PaymentId,
    string PaymentNumber,
    decimal Amount,
    string PaymentUrl);
```

---

## IV. BẢNG MÃ LỖI CHUẨN HÓA (STABLE ERROR CODES)

| HTTP Status | Error Code (Máy đọc) | Ý nghĩa nghiệp vụ |
| :--- | :--- | :--- |
| `400 Bad Request` | `RFQ_INVALID_BUDGET` | Khoảng ngân sách không hợp lệ (`BudgetMin > BudgetMax`) |
| `400 Bad Request` | `RFQ_INVALID_DEADLINE` | Hạn chót nộp báo giá phải ở tương lai và trước ngày cần giao hàng |
| `400 Bad Request` | `BID_INCOMPLETE_ITEMS` | Báo giá không chào giá đủ 100% các mặt hàng trong RFQ |
| `400 Bad Request` | `INVITATION_LIMIT_EXCEEDED` | Tổng số lượng lời mời vượt quá giới hạn 10 xưởng |
| `404 Not Found` | `RFQ_NOT_FOUND` | Gói thầu RFQ không tồn tại hoặc không thuộc quyền sở hữu |
| `404 Not Found` | `BID_NOT_FOUND` | Hồ sơ báo giá không tồn tại hoặc không thuộc RFQ này |
| `409 Conflict` | `RFQ_CLOSED` | Gói thầu đã hết hạn deadline hoặc đã đóng |
| `409 Conflict` | `BID_EXPIRED` | Báo giá đã quá thời hạn hiệu lực cam kết (`ValidityDays`) |
| `409 Conflict` | `RFQ_ALREADY_AWARDED` | Gói thầu đã được trao cho một nhà cung cấp khác |
| `422 Unprocessable`| `VNPAY_INVALID_SIGNATURE` | Chữ ký số mã băm HMAC-SHA512 từ cổng thanh toán không khớp |
| `422 Unprocessable`| `PAYMENT_ALREADY_CONFIRMED` | Giao dịch đã được thanh toán và kích hoạt trước đó |

