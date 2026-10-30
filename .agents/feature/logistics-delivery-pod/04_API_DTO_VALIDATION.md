# MODULE 7: ĐẶC TẢ RESTFUL API, DTOs & FLUENT VALIDATION (API CONTRACT & VALIDATION RULES)

---

## I. TỔNG QUAN DANH SÁCH API ENDPOINTS

Hệ thống Logistics & PoD cung cấp các endpoints chuẩn RESTful, kế thừa `BaseApiController` với tiền tố `/api/v1`:

| STT | HTTP Method | Endpoint | Quyền (Roles) | Mô tả tóm tắt |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `POST` | `/api/v1/deliveries` | Supplier, Admin | Khởi tạo chuyến giao hàng (từ Hợp đồng hoặc Đơn hàng) |
| 2 | `GET` | `/api/v1/deliveries` | Authenticated | Danh sách chuyến giao hàng (hỗ trợ phân trang, lọc theo status, type) |
| 3 | `GET` | `/api/v1/deliveries/{id}` | Authenticated | Chi tiết chuyến giao hàng kèm toàn bộ bằng chứng PoD liên quan |
| 4 | `GET` | `/api/v1/deliveries/contract/{contractId}` | Authenticated | Lấy danh sách các đợt giao của một hợp đồng B2B |
| 5 | `GET` | `/api/v1/deliveries/order/{orderId}` | Authenticated | Lấy danh sách chuyến giao của một đơn hàng trực tiếp |
| 6 | `PUT` | `/api/v1/deliveries/{id}/status` | Supplier, Driver, Admin | Cập nhật trạng thái chuyến giao hàng |
| 7 | `PUT` | `/api/v1/deliveries/{id}/carrier-info` | Supplier, Admin | Cập nhật thông tin xe, tài xế và mã vận đơn tracking |
| 8 | `POST` | `/api/v1/deliveries/{deliveryId}/proofs` | Supplier, Driver, Admin | Nộp bằng chứng PoD (Multipart form: ảnh hiện trường, chữ ký, GPS) |
| 9 | `GET` | `/api/v1/deliveries/{deliveryId}/proofs` | Authenticated | Danh sách tất cả bằng chứng của chuyến giao |
| 10 | `GET` | `/api/v1/deliveries/proofs/{proofId}` | Authenticated | Chi tiết một bằng chứng PoD kèm mã băm SHA-256 toàn vẹn |
| 11 | `POST` | `/api/v1/deliveries/webhook/carrier` | Anonymous (HMAC) | Cổng nhận Webhook tự động từ đối tác vận chuyển ViettelPost/GHTK |

---

## II. ĐẶC TẢ REQUEST & RESPONSE DTOs (C# 12 / .NET 8)

### 2.1. Request DTOs

```csharp
namespace Mooncake.EcommercePlatform.Application.DTOs.Requests;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Yêu cầu tạo mới chuyến giao hàng B2B.</summary>
public record CreateDeliveryRequest
{
    public Guid? ContractId { get; init; }
    public Guid? OrderId { get; init; }
    public DeliveryType DeliveryType { get; init; } = DeliveryType.SelfArranged;
    public DateOnly? ScheduledDate { get; init; }
    public string FromAddress { get; init; } = string.Empty;
    public string ToAddress { get; init; } = string.Empty;
    public string RecipientName { get; init; } = string.Empty;
    public string RecipientPhone { get; init; } = string.Empty;
    public string? TrackingCode { get; init; }
    public string? Notes { get; init; }
}

/// <summary>Yêu cầu cập nhật trạng thái chuyến giao.</summary>
public record UpdateDeliveryStatusRequest
{
    public DeliveryStatus Status { get; init; }
    public string? Notes { get; init; }
    public DateOnly? ActualDeliveryDate { get; init; }
}

/// <summary>Yêu cầu cập nhật thông tin hãng vận chuyển & tài xế.</summary>
public record UpdateCarrierInfoRequest
{
    public string TrackingCode { get; init; } = string.Empty;
    public string? RecipientName { get; init; }
    public string? RecipientPhone { get; init; }
    public string? Notes { get; init; }
}

/// <summary>Yêu cầu nộp bằng chứng PoD dạng Multipart Form-Data.</summary>
public record CreateDeliveryProofRequest
{
    public ProofType ProofType { get; init; } = ProofType.Delivery;
    public string? TakenBy { get; init; }
    public decimal? LocationLat { get; init; }
    public decimal? LocationLng { get; init; }
    public string? Notes { get; init; }
    public IFormFile? Photo { get; init; }
    public IFormFile? Signature { get; init; }
}

/// <summary>Payload Webhook từ hãng vận chuyển đối tác.</summary>
public record CarrierWebhookPayload
{
    public string TrackingCode { get; init; } = string.Empty;
    public string CarrierStatus { get; init; } = string.Empty; // e.g. "PICKED_UP", "DELIVERED", "FAILED"
    public string? LocationName { get; init; }
    public DateTime EventTimeUtc { get; init; }
    public string? RawPayload { get; init; }
}
```

### 2.2. Response DTOs

```csharp
namespace Mooncake.EcommercePlatform.Application.DTOs.Responses;

using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Thông tin tóm tắt chuyến giao hàng.</summary>
public record DeliveryResponse
{
    public Guid Id { get; init; }
    public Guid? ContractId { get; init; }
    public Guid? OrderId { get; init; }
    public string DeliveryNumber { get; init; } = string.Empty;
    public DeliveryType DeliveryType { get; init; }
    public DeliveryStatus Status { get; init; }
    public DateOnly? ScheduledDate { get; init; }
    public DateOnly? ActualDeliveryDate { get; init; }
    public string? FromAddress { get; init; }
    public string? ToAddress { get; init; }
    public string? RecipientName { get; init; }
    public string? RecipientPhone { get; init; }
    public string? TrackingCode { get; init; }
    public string? Notes { get; init; }
    public int ProofCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>Bằng chứng giao nhận PoD.</summary>
public record DeliveryProofResponse
{
    public Guid Id { get; init; }
    public Guid DeliveryId { get; init; }
    public ProofType ProofType { get; init; }
    public string? PhotoUrl { get; init; }
    public string? SignatureUrl { get; init; }
    public string? TakenBy { get; init; }
    public DateTime? TakenAt { get; init; }
    public decimal? LocationLat { get; init; }
    public decimal? LocationLng { get; init; }
    public string? Notes { get; init; }
    public string? TamperProofHash { get; init; }
    public bool IsVerified { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>Chi tiết chuyến giao hàng kèm danh sách toàn bộ bằng chứng PoD.</summary>
public record DeliveryDetailResponse : DeliveryResponse
{
    public IReadOnlyList<DeliveryProofResponse> Proofs { get; init; } = Array.Empty<DeliveryProofResponse>();
    public string? ContractNumber { get; init; }
    public string? OrderNumber { get; init; }
    public string? CustomerName { get; init; }
    public string? SupplierName { get; init; }
}
```

---

## III. FLUENT VALIDATION RULES (VALIDATORS CHẶT CHẼ)

### 3.1. `CreateDeliveryRequestValidator.cs`

```csharp
namespace Mooncake.EcommercePlatform.Application.Validators;

using FluentValidation;
using Mooncake.EcommercePlatform.Application.DTOs.Requests;

public class CreateDeliveryRequestValidator : AbstractValidator<CreateDeliveryRequest>
{
    public CreateDeliveryRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => x.ContractId.HasValue || x.OrderId.HasValue)
            .WithMessage("Chuyến giao hàng bắt buộc phải gắn với một Hợp đồng (Contract) hoặc Đơn hàng (Order).");

        RuleFor(x => x.FromAddress)
            .NotEmpty().WithMessage("Địa chỉ xuất hàng không được để trống.")
            .MaximumLength(500).WithMessage("Địa chỉ xuất hàng không vượt quá 500 ký tự.");

        RuleFor(x => x.ToAddress)
            .NotEmpty().WithMessage("Địa chỉ nhận hàng không được để trống.")
            .MaximumLength(500).WithMessage("Địa chỉ nhận hàng không vượt quá 500 ký tự.");

        RuleFor(x => x.RecipientName)
            .NotEmpty().WithMessage("Tên người nhận không được để trống.")
            .MaximumLength(150).WithMessage("Tên người nhận không vượt quá 150 ký tự.");

        RuleFor(x => x.RecipientPhone)
            .NotEmpty().WithMessage("Số điện thoại người nhận không được để trống.")
            .Matches(@"^(0|\+84)(3|5|7|8|9)[0-9]{8}$")
            .WithMessage("Số điện thoại người nhận không đúng định dạng di động Việt Nam.");

        RuleFor(x => x.ScheduledDate)
            .Must(date => !date.HasValue || date.Value >= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)))
            .WithMessage("Ngày dự kiến giao không được ở trong quá khứ.");
    }
}
```

### 3.2. `CreateDeliveryProofRequestValidator.cs`

```csharp
namespace Mooncake.EcommercePlatform.Application.Validators;

using FluentValidation;
using Mooncake.EcommercePlatform.Application.DTOs.Requests;
using Mooncake.EcommercePlatform.Domain.Enums;

public class CreateDeliveryProofRequestValidator : AbstractValidator<CreateDeliveryProofRequest>
{
    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxPhotoBytes = 10 * 1024 * 1024; // 10MB
    private const long MaxSignatureBytes = 2 * 1024 * 1024; // 2MB

    public CreateDeliveryProofRequestValidator()
    {
        RuleFor(x => x.ProofType)
            .IsInEnum().WithMessage("Loại bằng chứng PoD không hợp lệ.");

        RuleFor(x => x.LocationLat)
            .InclusiveBetween(-90m, 90m).When(x => x.LocationLat.HasValue)
            .WithMessage("Vĩ độ (Latitude) phải nằm trong khoảng [-90, 90].");

        RuleFor(x => x.LocationLng)
            .InclusiveBetween(-180m, 180m).When(x => x.LocationLng.HasValue)
            .WithMessage("Kinh độ (Longitude) phải nằm trong khoảng [-180, 180].");

        // Khi giao hàng thành công (Delivery), bắt buộc phải có chữ ký
        RuleFor(x => x.Signature)
            .NotNull().When(x => x.ProofType == ProofType.Delivery)
            .WithMessage("Chữ ký số của người nhận là bắt buộc khi xác nhận bàn giao thành công.");

        // Bắt buộc phải có ảnh chứng minh cho mọi loại Proof (trừ trường hợp đặc biệt)
        RuleFor(x => x.Photo)
            .NotNull().WithMessage("Ảnh chụp hiện trường là bắt buộc.")
            .Must(photo => photo == null || photo.Length <= MaxPhotoBytes)
            .WithMessage("Dung lượng ảnh hiện trường không được vượt quá 10MB.")
            .Must(photo => photo == null || AllowedImageExtensions.Contains(System.IO.Path.GetExtension(photo.FileName).ToLowerInvariant()))
            .WithMessage("Định dạng ảnh không hợp lệ. Chỉ chấp nhận JPG, JPEG, PNG, WEBP.");

        RuleFor(x => x.Signature)
            .Must(sig => sig == null || sig.Length <= MaxSignatureBytes)
            .WithMessage("Dung lượng file chữ ký không được vượt quá 2MB.")
            .Must(sig => sig == null || AllowedImageExtensions.Contains(System.IO.Path.GetExtension(sig.FileName).ToLowerInvariant()))
            .WithMessage("Định dạng chữ ký không hợp lệ. Chỉ chấp nhận PNG, JPG, WEBP.");
    }
}
```

---

## IV. BẢNG MÃ LỖI HTTP CHUẨN HÓA (HTTP STATUS CODES & LOCALIZED MESSAGES)

Mọi phản hồi lỗi đều tuân thủ `ApiResponse<T>` với format chuẩn:

| HTTP Status | Ngữ cảnh nghiệp vụ | Message trả về Client |
| :--- | :--- | :--- |
| `400 Bad Request` | Thiếu ContractId/OrderId hoặc sai định dạng số điện thoại | `"Dữ liệu gửi lên không đúng định dạng hoặc thiếu trường bắt buộc."` |
| `400 Bad Request` | Nộp `Delivery` proof nhưng không có ảnh chụp hoặc chữ ký | `"Chữ ký người nhận và ảnh hiện trường là bắt buộc khi giao thành công."` |
| `400 Bad Request` | Vi phạm chuyển đổi trạng thái (VD: từ `Pending` nhảy cóc lên `Delivered`) | `"Chuyển đổi trạng thái chuyến giao không hợp lệ."` |
| `401 Unauthorized` | Không có Bearer token hoặc token đã hết hạn | `"Phiên làm việc đã hết hạn. Vui lòng đăng nhập lại."` |
| `403 Forbidden` | Supplier cố tình xem hoặc sửa chuyến giao của xưởng khác | `"Bạn không có quyền thao tác trên chuyến giao hàng này."` |
| `404 Not Found` | Không tìm thấy chuyến giao hàng theo ID | `"Không tìm thấy thông tin chuyến giao hàng."` |
| `409 Conflict` | Chuyến giao hàng đã hoàn tất `Delivered` nhưng cố tình nộp thêm PoD | `"Chuyến giao hàng này đã được xác nhận hoàn tất trước đó."` |
| `429 Too Many Requests` | Nộp bằng chứng quá 10 lần trong 1 phút | `"Bạn đã gửi yêu cầu quá nhanh. Vui lòng thử lại sau giây lát."` |
| `500 Server Error` | Lỗi ghi nhận file storage hoặc lỗi hệ thống | `"Đã xảy ra lỗi hệ thống khi lưu trữ bằng chứng giao nhận."` |

