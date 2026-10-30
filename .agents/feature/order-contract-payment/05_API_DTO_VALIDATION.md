# [M10-F25.5] ĐẶC TẢ API, DTO & FLUENT VALIDATION: THANH TOÁN & ESCROW

---

## I. DANH SÁCH ENDPOINT HỆ THỐNG

| STT | Phương thức | Đường dẫn API | Vai trò (Role) | Mô tả nghiệp vụ |
| :--- | :--- | :--- | :--- | :--- |
| 1 | `POST` | `/api/v1/payments/orders/vnpay-url` | Customer | Sinh liên kết thanh toán VNPay Sandbox cho Đơn hàng |
| 2 | `POST` | `/api/v1/payments/contracts/vnpay-url` | Customer | Sinh liên kết thanh toán VNPay Sandbox cho Mốc hợp đồng (M1/M2/M3) |
| 3 | `POST` | `/api/v1/payments/bank-transfer` | Customer | Khởi tạo giao dịch Chuyển khoản VietQR |
| 4 | `POST` | `/api/v1/payments/{paymentId}/evidence` | Customer | Tải lên ảnh / file scan Ủy nhiệm chi (UNC) chứng từ chuyển khoản |
| 5 | `GET` | `/api/v1/payments/vnpay/ipn` | Anonymous | Webhook IPN nhận kết quả tự động từ cổng VNPay Sandbox |
| 6 | `GET` | `/api/v1/payments/vnpay/return-callback`| Anonymous / Customer | Xác thực kết quả thanh toán khi khách hàng quay về web |
| 7 | `GET` | `/api/v1/payments/mine` | Customer, Supplier | Lấy danh sách lịch sử giao dịch của người dùng hiện tại |
| 8 | `GET` | `/api/v1/payments/{paymentNumber}` | Authorized | Lấy chi tiết biên lai giao dịch theo mã thanh toán |
| 9 | `POST` | `/api/v1/payments/{paymentId}/confirm` | Admin | Quản trị viên/Kế toán duyệt giao dịch chuyển khoản |
| 10 | `POST` | `/api/v1/payments/{paymentId}/refund` | Admin | Quản trị viên thực hiện hoàn tiền cho đơn hàng/hợp đồng |
| 11 | `GET` | `/api/v1/payments/escrow/summary` | Admin, Supplier | Xem tổng quan sổ ký quỹ Escrow và dòng tiền bảo chứng |

---

## II. ĐẶC TẢ CÁC DTO YÊU CẦU & PHẢN HỒI (C# DTOs)

### 2.1. Request DTOs

```csharp
namespace Mooncake.EcommercePlatform.Application.DTOs.Payments.Requests;

using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Request tạo URL thanh toán VNPay cho đơn hàng.</summary>
public record CreateOrderVnPayUrlRequest(
    Guid OrderId,
    string ReturnUrl);

/// <summary>Request tạo URL thanh toán VNPay cho mốc hợp đồng.</summary>
public record CreateContractVnPayUrlRequest(
    Guid ContractId,
    Guid MilestoneId,
    string ReturnUrl);

/// <summary>Request khởi tạo chuyển khoản VietQR.</summary>
public record CreateBankTransferRequest(
    Guid? OrderId,
    Guid? ContractId,
    Guid? MilestoneId);

/// <summary>Request tải lên chứng từ Ủy nhiệm chi (UNC).</summary>
public record UploadPaymentEvidenceRequest(
    string EvidenceUrl,
    string? Notes);

/// <summary>Request xác nhận thanh toán chuyển khoản từ Admin.</summary>
public record ConfirmPaymentRequest(
    string Reason);

/// <summary>Request hoàn tiền giao dịch từ Admin.</summary>
public record RefundPaymentRequest(
    string Reason);
```

### 2.2. Response DTOs

```csharp
namespace Mooncake.EcommercePlatform.Application.DTOs.Payments.Responses;

using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Thông tin phản hồi khi sinh URL thanh toán VNPay.</summary>
public record VnPayUrlResponse(
    Guid PaymentId,
    string PaymentNumber,
    decimal Amount,
    string PaymentUrl);

/// <summary>Thông tin phản hồi khi khởi tạo chuyển khoản VietQR.</summary>
public record VietQrPaymentResponse(
    Guid PaymentId,
    string PaymentNumber,
    decimal Amount,
    string BankName,
    string BankBin,
    string AccountNumber,
    string AccountName,
    string TransferMemo,
    string QrCodeUrl,
    DateTime ExpiresAt);

/// <summary>Thông tin biên lai giao dịch thanh toán.</summary>
public record PaymentResponse(
    Guid Id,
    string PaymentNumber,
    Guid? OrderId,
    Guid? ContractId,
    Guid? MilestoneId,
    decimal Amount,
    string Currency,
    PaymentMethod Method,
    PaymentStatus Status,
    DateTime? PaidAt,
    DateTime? ExpiresAt,
    string? EvidenceUrl,
    decimal RefundedAmount,
    string? RefundReason);

/// <summary>Kết quả đối soát VNPay Return Callback.</summary>
public record VnPayReturnCallbackResponse(
    string PaymentNumber,
    Guid? OrderId,
    Guid? ContractId,
    Guid? MilestoneId,
    decimal Amount,
    bool IsSuccess,
    string ResponseCode,
    string? BankCode,
    string? TransactionNo,
    DateTime? PaidAt);

/// <summary>Báo cáo tổng hợp số dư và dòng tiền ký quỹ Escrow.</summary>
public record EscrowSummaryResponse(
    decimal TotalEscrowBalance,
    int TotalActiveContracts,
    decimal PendingDisbursementAmount,
    decimal DisputedEscrowAmount,
    IReadOnlyList<EscrowLedgerItemDto> EscrowLedgerItems);

public record EscrowLedgerItemDto(
    Guid ContractId,
    string ContractNumber,
    string CustomerName,
    string SupplierName,
    decimal ContractTotalValue,
    decimal HeldEscrowAmount,
    int PaidMilestonesCount,
    int TotalMilestonesCount,
    string EscrowStatus,
    DateTime? LastDepositDate);
```

---

## III. FLUENT VALIDATION RULES

```csharp
namespace Mooncake.EcommercePlatform.Application.Validators.Payments;

using FluentValidation;
using Mooncake.EcommercePlatform.Application.DTOs.Payments.Requests;

public sealed class CreateOrderVnPayUrlRequestValidator : AbstractValidator<CreateOrderVnPayUrlRequest>
{
    public CreateOrderVnPayUrlRequestValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("Mã đơn hàng không được để trống.");

        RuleFor(x => x.ReturnUrl)
            .NotEmpty().WithMessage("Địa chỉ Return URL không được để trống.")
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out var parsed) &&
                         (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps))
            .WithMessage("Return URL phải là đường dẫn hợp lệ (http hoặc https).");
    }
}

public sealed class CreateContractVnPayUrlRequestValidator : AbstractValidator<CreateContractVnPayUrlRequest>
{
    public CreateContractVnPayUrlRequestValidator()
    {
        RuleFor(x => x.ContractId)
            .NotEmpty().WithMessage("Mã hợp đồng không được để trống.");

        RuleFor(x => x.MilestoneId)
            .NotEmpty().WithMessage("Mã mốc thanh toán không được để trống.");

        RuleFor(x => x.ReturnUrl)
            .NotEmpty().WithMessage("Địa chỉ Return URL không được để trống.")
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out var parsed) &&
                         (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps))
            .WithMessage("Return URL phải là đường dẫn hợp lệ (http hoặc https).");
    }
}

public sealed class ConfirmPaymentRequestValidator : AbstractValidator<ConfirmPaymentRequest>
{
    public ConfirmPaymentRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Lý do xác nhận đối soát không được để trống.")
            .MinimumLength(5).WithMessage("Lý do đối soát phải có ít nhất 5 ký tự.")
            .MaximumLength(500).WithMessage("Lý do đối soát không được vượt quá 500 ký tự.");
    }
}

public sealed class RefundPaymentRequestValidator : AbstractValidator<RefundPaymentRequest>
{
    public RefundPaymentRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Lý do hoàn tiền không được để trống.")
            .MinimumLength(5).WithMessage("Lý do hoàn tiền phải có ít nhất 5 ký tự.")
            .MaximumLength(500).WithMessage("Lý do hoàn tiền không được vượt quá 500 ký tự.");
    }
}
```

---

## IV. BẢNG MÃ LỖI & THÔNG ĐIỆP PHẢN HỒI

| Mã HTTP | Mã Lỗi Nội Bộ / Ngữ Cảnh | Thông Điệp Tiếng Việt Cho Người Dùng |
| :--- | :--- | :--- |
| `400 Bad Request` | `INVALID_PAYMENT_TARGET` | "Vui lòng cung cấp mã đơn hàng hoặc đồng thời mã hợp đồng và mã mốc tiến độ." |
| `400 Bad Request` | `INVALID_RETURN_URL` | "Địa chỉ chuyển hướng sau thanh toán không hợp lệ." |
| `401 Unauthorized` | `UNAUTHORIZED` | "Phiên làm việc đã hết hạn. Vui lòng đăng nhập lại." |
| `403 Forbidden` | `FORBIDDEN_PAYMENT_ACCESS` | "Bạn không có quyền thực hiện thao tác thanh toán trên tài nguyên này." |
| `404 Not Found` | `PAYMENT_TARGET_NOT_FOUND` | "Không tìm thấy đơn hàng hoặc mốc hợp đồng cần thanh toán." |
| `409 Conflict` | `ORDER_NOT_PAYABLE` | "Đơn hàng chưa được xác nhận hoặc đã bị hủy trước đó." |
| `409 Conflict` | `ALREADY_PAID` | "Khoản thanh toán này đã được hoàn tất trước đó." |
| `409 Conflict` | `IDEMPOTENCY_CONFLICT` | "Khóa Idempotency-Key này đã được dùng cho một giao dịch khác." |
| `422 Unprocessable` | `COD_LIMIT_EXCEEDED` | "Phương thức COD chỉ áp dụng cho đơn hàng dưới 10.000.000 ₫ và không in logo riêng." |
| `429 Too Many Req` | `RATE_LIMIT_EXCEEDED` | "Bạn đã tạo yêu cầu thanh toán quá nhiều lần. Vui lòng thử lại sau 1 phút." |

