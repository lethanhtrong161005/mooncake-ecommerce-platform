# [B2B-ORDER-04] - ĐẶC TẢ HỢP ĐỒNG API, DTO & QUY TẮC VALIDATION (MODULE 4)

---

## I. TỔNG QUAN HỢP ĐỒNG API (API ENDPOINT MATRIX)

Toàn bộ các API của Module 4 được chuẩn hóa theo tiền tố `/api/v1` và bọc dữ liệu trong cấu trúc chuẩn `ApiResponse<T>`.

### 1.1. Nhóm API Đơn hàng số lượng lớn (Feature #7: Bulk Order)
| Phương thức | Đường dẫn (URL) | Phân quyền (Auth) | Mô tả nghiệp vụ |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/v1/orders` | `Customer` | Đặt hàng số lượng lớn (Yêu cầu `Idempotency-Key` header) |
| `GET` | `/api/v1/orders/mine` | `Customer` | Lấy danh sách đơn hàng của khách hàng hiện tại |
| `GET` | `/api/v1/orders/supplier` | `Supplier` | Lấy danh sách đơn hàng xưởng nhận được |
| `GET` | `/api/v1/orders/{orderId}` | `Customer, Supplier, Admin` | Xem chi tiết đơn hàng (có kiểm tra quyền sở hữu) |
| `POST` | `/api/v1/orders/{orderId}/cancel` | `Customer` | Khách hàng hủy đơn khi đang `Pending` (hoàn tồn kho) |
| `POST` | `/api/v1/orders/{orderId}/confirm` | `Supplier` | Xưởng xác nhận nhận đơn (`Pending -> Confirmed`) |
| `POST` | `/api/v1/orders/{orderId}/reject` | `Supplier` | Xưởng từ chối đơn kèm lý do (hoàn tồn kho) |
| `POST` | `/api/v1/orders/{orderId}/processing` | `Supplier` | Bắt đầu sản xuất (`Confirmed -> Processing`) |
| `POST` | `/api/v1/orders/{orderId}/ship` | `Supplier` | Bàn giao đơn vị vận chuyển (`Processing -> Shipped`) |
| `POST` | `/api/v1/orders/{orderId}/delivery/confirm` | `Customer` | Khách xác nhận đã nhận hàng (`Shipped -> Delivered`) |

### 1.2. Nhóm API Chiết khấu bậc thang (Feature #8: Volume Promotions)
| Phương thức | Đường dẫn (URL) | Phân quyền (Auth) | Mô tả nghiệp vụ |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/v1/orders/quote-preview` | `Anonymous / Customer` | Tính trước chiết khấu giỏ hàng theo số lượng |
| `GET` | `/api/v1/supplier/promotions` | `Supplier` | Danh sách các quy tắc chiết khấu của xưởng |
| `POST` | `/api/v1/supplier/promotions` | `Supplier` | Tạo quy tắc chiết khấu bậc thang mới |
| `PUT` | `/api/v1/supplier/promotions/{id}` | `Supplier` | Cập nhật quy tắc chiết khấu bậc thang |
| `DELETE` | `/api/v1/supplier/promotions/{id}` | `Supplier` | Vô hiệu hóa quy tắc chiết khấu |

### 1.3. Nhóm API Tùy biến đóng gói in logo (Feature #9: Custom Packaging)
| Phương thức | Đường dẫn (URL) | Phân quyền (Auth) | Mô tả nghiệp vụ |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/v1/orders/{orderId}/custom-packaging` | `Customer` | Đăng ký thông số đóng gói in logo cho đơn hàng |
| `GET` | `/api/v1/orders/{orderId}/custom-packaging` | `Customer, Supplier` | Lấy chi tiết bao bì và lịch sử các bản vẽ thiết kế |
| `POST` | `/api/v1/orders/{orderId}/custom-packaging/revisions` | `Supplier` | Xưởng gửi bản vẽ mockup phối cảnh kèm phụ phí |
| `POST` | `/api/v1/orders/{orderId}/custom-packaging/revisions/{revisionId}/review` | `Customer` | Khách duyệt hoặc yêu cầu chỉnh sửa bản vẽ |

---

## II. ĐẶC TẢ DTO REQUEST VÀ RULES VALIDATION

### 2.1. Request DTOs: Đặt hàng B2B

```csharp
namespace Mooncake.EcommercePlatform.Application.DTOs.Orders.Requests;

using System.ComponentModel.DataAnnotations;

public sealed record CreateOrderRequest
{
    [Required(ErrorMessage = "ShopId is required.")]
    public Guid ShopId { get; init; }

    [Required(ErrorMessage = "Items list is required.")]
    [MinLength(1, ErrorMessage = "At least one item must be ordered.")]
    [MaxLength(100, ErrorMessage = "A bulk order cannot exceed 100 distinct items.")]
    public List<CreateOrderItemRequest> Items { get; init; } = [];

    [Required(ErrorMessage = "Delivery address is required.")]
    [StringLength(500, MinimumLength = 10, ErrorMessage = "Delivery address must be between 10 and 500 characters.")]
    public string DeliveryAddress { get; init; } = string.Empty;

    [Required(ErrorMessage = "Recipient name is required.")]
    [StringLength(255, MinimumLength = 2, ErrorMessage = "Recipient name must be between 2 and 255 characters.")]
    public string RecipientName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Recipient phone is required.")]
    [RegularExpression(@"^(0[3|5|7|8|9])+([0-9]{8})\b$", ErrorMessage = "Recipient phone must be a valid 10-digit Vietnamese phone number.")]
    public string RecipientPhone { get; init; } = string.Empty;

    [MaxLength(2000, ErrorMessage = "Notes cannot exceed 2000 characters.")]
    public string? Notes { get; init; }

    public DateOnly? DeliveryDateExpected { get; init; }
}

public sealed record CreateOrderItemRequest
{
    [Required(ErrorMessage = "ProductId is required.")]
    public Guid ProductId { get; init; }

    [Required(ErrorMessage = "VariantId is required.")]
    public Guid VariantId { get; init; }

    [Range(1, 1_000_000, ErrorMessage = "Quantity must be between 1 and 1,000,000.")]
    public int Quantity { get; init; }
}

public sealed record CancelOrderRequest
{
    [Required(ErrorMessage = "Cancellation reason is required.")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "Reason must be between 5 and 500 characters.")]
    public string Reason { get; init; } = string.Empty;
}
```

### 2.2. Request DTOs: Chiết khấu bậc thang & Báo giá

```csharp
namespace Mooncake.EcommercePlatform.Application.DTOs.Promotions.Requests;

using System.ComponentModel.DataAnnotations;
using Mooncake.EcommercePlatform.Domain.Enums;

public sealed record SavePromotionRequest
{
    public Guid? ProductId { get; init; }

    [Required(ErrorMessage = "Promotion name is required.")]
    [StringLength(255, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 255 characters.")]
    public string Name { get; init; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    public string? Description { get; init; }

    [Range(1, 1_000_000, ErrorMessage = "MinQty must be at least 1.")]
    public int? MinQty { get; init; }

    [Range(1, 1_000_000, ErrorMessage = "MaxQty must be at least 1.")]
    public int? MaxQty { get; init; }

    [Required(ErrorMessage = "DiscountType is required.")]
    public DiscountType DiscountType { get; init; }

    [Range(0.01, 1_000_000_000, ErrorMessage = "DiscountValue must be greater than 0.")]
    public decimal DiscountValue { get; init; }

    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed record OrderQuotePreviewRequest
{
    [Required(ErrorMessage = "ShopId is required.")]
    public Guid ShopId { get; init; }

    [Required(ErrorMessage = "Items list cannot be empty.")]
    public List<CreateOrderItemRequest> Items { get; init; } = [];
}
```

### 2.3. Request DTOs: Tùy biến bao bì & Đánh giá bản vẽ

```csharp
namespace Mooncake.EcommercePlatform.Application.DTOs.Packaging.Requests;

using System.ComponentModel.DataAnnotations;

public sealed record CreateCustomPackagingRequest
{
    [Required(ErrorMessage = "Company name is required.")]
    [StringLength(255, MinimumLength = 2, ErrorMessage = "Company name must be between 2 and 255 characters.")]
    public string CompanyName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Company logo URL is required.")]
    [Url(ErrorMessage = "Company logo URL must be a valid URL.")]
    public string CompanyLogoUrl { get; init; } = string.Empty;

    [Required(ErrorMessage = "Packaging type is required.")]
    [StringLength(100, ErrorMessage = "Packaging type cannot exceed 100 characters.")]
    public string PackagingType { get; init; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "Color scheme JSON cannot exceed 1000 characters.")]
    public string? ColorScheme { get; init; }

    [MaxLength(500, ErrorMessage = "Special message cannot exceed 500 characters.")]
    public string? SpecialMessage { get; init; }

    [MaxLength(2000, ErrorMessage = "Notes cannot exceed 2000 characters.")]
    public string? Notes { get; init; }
}

public sealed record SubmitPackagingRevisionRequest
{
    [Required(ErrorMessage = "Mockup URL is required.")]
    [Url(ErrorMessage = "Mockup URL must be a valid URL.")]
    public string MockupUrl { get; init; } = string.Empty;

    [MaxLength(2000, ErrorMessage = "Supplier notes cannot exceed 2000 characters.")]
    public string? SupplierNotes { get; init; }

    [Range(0, 100_000_000, ErrorMessage = "Design fee must be non-negative.")]
    public decimal DesignFee { get; init; } = 0;
}

public sealed record ReviewPackagingRevisionRequest
{
    [Required(ErrorMessage = "Action is required.")]
    [RegularExpression(@"^(Approve|RequestRevision)$", ErrorMessage = "Action must be either 'Approve' or 'RequestRevision'.")]
    public string Action { get; init; } = string.Empty;

    [MaxLength(2000, ErrorMessage = "Review notes cannot exceed 2000 characters.")]
    public string? Notes { get; init; }
}
```

---

## III. ĐẶC TẢ DTO RESPONSE CHUẨN HÓA

### 3.1. Phản hồi Đơn hàng (`OrderResponse`)

```csharp
namespace Mooncake.EcommercePlatform.Application.DTOs.Orders.Responses;

using Mooncake.EcommercePlatform.Domain.Enums;

public sealed record OrderResponse(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    Guid ShopId,
    OrderStatus Status,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal ShippingFee,
    decimal PackagingFee,
    decimal TotalAmount,
    string? DeliveryAddress,
    string? RecipientName,
    string? RecipientPhone,
    DateOnly? DeliveryDateExpected,
    string? Notes,
    DateTime CreatedAtUtc,
    IReadOnlyList<OrderItemResponse> Items,
    CustomPackagingSummaryResponse? CustomPackaging);

public sealed record OrderItemResponse(
    Guid ProductId,
    Guid VariantId,
    string ProductName,
    string VariantName,
    string Sku,
    int Quantity,
    decimal UnitPrice,
    decimal DiscountAmount,
    decimal Subtotal);

public sealed record CustomPackagingSummaryResponse(
    Guid Id,
    string CompanyName,
    string? CompanyLogoUrl,
    string? BoxDesignUrl,
    string PackagingType,
    CustomPackagingStatus Status,
    int RevisionsCount);
```

### 3.2. Phản hồi Chi tiết Tùy biến bao bì (`CustomPackagingDetailResponse`)

```csharp
namespace Mooncake.EcommercePlatform.Application.DTOs.Packaging.Responses;

using Mooncake.EcommercePlatform.Domain.Enums;

public sealed record CustomPackagingDetailResponse(
    Guid Id,
    Guid OrderId,
    string CompanyName,
    string? CompanyLogoUrl,
    string? BoxDesignUrl,
    string PackagingType,
    string? ColorScheme,
    string? SpecialMessage,
    string? Notes,
    CustomPackagingStatus Status,
    DateTime CreatedAtUtc,
    IReadOnlyList<CustomPackagingRevisionResponse> Revisions);

public sealed record CustomPackagingRevisionResponse(
    Guid Id,
    int RevisionNumber,
    string MockupUrl,
    string? SupplierNotes,
    string? CustomerNotes,
    decimal DesignFee,
    CustomPackagingStatus Status,
    DateTime SubmittedAtUtc,
    DateTime? ReviewedAtUtc);
```

---

## IV. BẢNG MÃ LỖI CHUẨN HÓA (STABLE ERROR CODES)

Khi xảy ra lỗi nghiệp vụ, API trả về mã HTTP tương ứng kèm `code` và `message` tiếng Anh rõ ràng (Frontend ánh xạ sang nhãn tiếng Việt hiển thị cho người dùng):

| HTTP Status | Error Code (Máy đọc) | Ý nghĩa nghiệp vụ |
| :--- | :--- | :--- |
| `400 Bad Request` | `ORDER_INVALID_VARIANT` | Biến thể không tồn tại hoặc không thuộc sản phẩm đang bán |
| `400 Bad Request` | `ORDER_BELOW_MIN_QTY` | Số lượng đặt nhỏ hơn số lượng tối thiểu cho phép |
| `400 Bad Request` | `ORDER_EXCEEDS_MAX_QTY` | Số lượng đặt vượt quá giới hạn tối đa cho phép |
| `400 Bad Request` | `PROMOTION_INVALID_RANGE` | Khoảng số lượng không hợp lệ (`MinQty > MaxQty`) |
| `400 Bad Request` | `PROMOTION_OVERLAPPING_TIER` | Khoảng số lượng bị trùng lặp với quy tắc đang hoạt động khác |
| `404 Not Found` | `ORDER_NOT_FOUND` | Đơn hàng không tồn tại hoặc không thuộc quyền sở hữu |
| `409 Conflict` | `ORDER_INSUFFICIENT_STOCK` | Tồn kho biến thể không đủ đáp ứng số lượng đặt |
| `409 Conflict` | `ORDER_INVALID_STATUS_TRANSITION` | Trạng thái hiện tại của đơn không cho phép thực hiện thao tác này |
| `409 Conflict` | `IDEMPOTENCY_KEY_MISMATCH` | Idempotency-Key đã dùng với một nội dung request khác |
| `409 Conflict` | `PACKAGING_ALREADY_APPROVED` | Bản vẽ bao bì đã được phê duyệt, không thể thay đổi tiếp |
| `429 Too Many Requests`| `ORDER_RATE_LIMIT_EXCEEDED` | Khách hàng thực hiện tạo đơn quá nhanh liên tục |

