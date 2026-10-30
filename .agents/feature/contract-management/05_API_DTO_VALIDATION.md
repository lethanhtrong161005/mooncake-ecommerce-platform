# [CONTRACT-05] - ĐẶC TẢ API, DTO VÀ RÀNG BUỘC VALIDATION

---

## I. DANH SÁCH RESTFUL ENDPOINTS

Tất cả các API tuân thủ tiêu chuẩn Clean Architecture, sử dụng định dạng JSON, xác thực Bearer JWT và trả về cấu trúc chuẩn `ApiResponse<T>`.

| Phương thức | Đường dẫn Endpoint | Quyền hạn (Roles) | Mô tả nghiệp vụ |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/v1/contracts/mine` | Customer, Supplier | Lấy danh sách hợp đồng của người dùng hiện tại |
| `GET` | `/api/v1/contracts/{id}` | Customer, Supplier | Lấy thông tin chi tiết hợp đồng và các mốc milestones |
| `POST` | `/api/v1/contracts/{id}/submit-for-signature` | Customer, Supplier | Chuyển hợp đồng từ `Draft` sang `PendingSignature` |
| `POST` | `/api/v1/contracts/{id}/signature-otp` | Customer, Supplier | Gửi mã OTP xác thực ký số về email đăng ký |
| `POST` | `/api/v1/contracts/{id}/sign` | Customer, Supplier | Thực hiện ký số bằng mã OTP và ghi nhận dấu vân tay SHA-256 |
| `GET` | `/api/v1/contracts/{id}/milestones` | Customer, Supplier | Lấy danh sách các mốc tiến độ thanh toán của hợp đồng |
| `POST` | `/api/v1/contracts/{id}/milestones/{milestoneId}/submit-progress` | Supplier | Nộp bằng chứng hình ảnh & tài liệu tiến độ mốc |
| `POST` | `/api/v1/contracts/{id}/milestones/{milestoneId}/verify` | Customer | Khách hàng nghiệm thu hoặc từ chối mốc tiến độ |
| `POST` | `/api/v1/contracts/{id}/extension-request` | Supplier | Xưởng nộp đơn xin gia hạn ngày giao hàng |
| `POST` | `/api/v1/contracts/{id}/extension-request/respond` | Customer | Khách hàng phê duyệt hoặc bác bỏ yêu cầu gia hạn |
| `POST` | `/api/v1/contracts/{id}/dispute` | Customer, Supplier | Khởi tạo tranh chấp khiếu nại chất lượng / cọc |
| `POST` | `/api/v1/contracts/{id}/request-final-acceptance` | Supplier | Xưởng yêu cầu nghiệm thu bàn giao tổng thể 100% |
| `POST` | `/api/v1/contracts/{id}/confirm-final-acceptance` | Customer | Khách hàng ký biên bản nghiệm thu & hoàn tất hợp đồng |
| `POST` | `/api/v1/admin/contracts/{id}/resolve-dispute` | Admin | Quản trị viên phân xử tranh chấp và quyết định hoàn tiền |

---

## II. ĐẶC TẢ DTO REQUESTS VÀ RESPONSES (C# 12 / .NET 8)

### 2.1. Request DTOs

```csharp
namespace Mooncake.EcommercePlatform.Application.DTOs.Contracts.Requests;

using System.ComponentModel.DataAnnotations;

public sealed record SignContractRequest
{
    [Required, RegularExpression(@"^\d{6}$", ErrorMessage = "OTP code must be exactly 6 digits.")]
    public string OtpCode { get; init; } = string.Empty;

    [Required]
    [Range(typeof(bool), "true", "true", ErrorMessage = "You must confirm and accept the contract terms.")]
    public bool ConfirmTerms { get; init; }
}

public sealed record SubmitMilestoneProgressRequest
{
    [Required, MinLength(5), MaxLength(2000)]
    public string ProgressNotes { get; init; } = string.Empty;

    [Required, MinLength(1), MaxLength(10)]
    public List<string> EvidenceUrls { get; init; } = [];
}

public sealed record VerifyMilestoneRequest
{
    [Required]
    public bool IsApproved { get; init; }

    [MaxLength(1000)]
    public string? RejectionReason { get; init; }
}

public sealed record RequestContractExtensionRequest
{
    [Required]
    public DateOnly ProposedDueDate { get; init; }

    [Required, MinLength(10), MaxLength(2000)]
    public string Reason { get; init; } = string.Empty;
}

public sealed record RespondContractExtensionRequest
{
    [Required]
    public bool IsApproved { get; init; }

    [MaxLength(1000)]
    public string? ResponseNote { get; init; }
}

public sealed record RaiseContractDisputeRequest
{
    [Required, MinLength(20), MaxLength(4000)]
    public string DisputeReason { get; init; } = string.Empty;

    [Required, MinLength(1), MaxLength(10)]
    public List<string> EvidenceUrls { get; init; } = [];
}

public sealed record ConfirmFinalAcceptanceRequest
{
    [Required]
    [Range(typeof(bool), "true", "true", ErrorMessage = "Final inspection confirmation is required.")]
    public bool ConfirmedQualityAndQuantity { get; init; }

    [MaxLength(2000)]
    public string? AcceptanceNotes { get; init; }
}

public sealed record AdminResolveDisputeRequest
{
    [Required]
    [RegularExpression(@"^(FullRefund|PartialPayout|Dismiss)$", ErrorMessage = "Invalid dispute resolution type.")]
    public string ResolutionType { get; init; } = string.Empty;

    [Range(0, 999999999999.99)]
    public decimal RefundAmount { get; init; }

    [Required, MinLength(10), MaxLength(2000)]
    public string AdminVerdict { get; init; } = string.Empty;
}
```

### 2.2. Response DTOs

```csharp
namespace Mooncake.EcommercePlatform.Application.DTOs.Contracts.Responses;

using Mooncake.EcommercePlatform.Domain.Enums;

public sealed record ContractResponse(
    Guid Id,
    string ContractNumber,
    Guid? RfqId,
    string? Title,
    decimal? TotalValue,
    DateOnly? DeliveryDeadline,
    string? TermsAndConditions,
    ContractStatus Status,
    DateTime? SignedByCustomerAt,
    DateTime? SignedBySupplierAt,
    string? SignedDocumentSha256,
    decimal? DepositAmount,
    DateTime? DepositPaidAt,
    IReadOnlyList<ContractMilestoneResponse>? Milestones,
    ContractExtensionResponse? PendingExtension,
    ContractDisputeResponse? ActiveDispute
);

public sealed record ContractMilestoneResponse(
    Guid Id,
    Guid ContractId,
    int SequenceNumber,
    string Name,
    string? Description,
    bool IsDeposit,
    DateOnly? DueDate,
    DateOnly? CompletionDate,
    decimal? Amount,
    decimal? Percentage,
    MilestoneStatus Status,
    string? ProgressNotes,
    IReadOnlyList<string>? EvidenceUrls
);

public sealed record ContractExtensionResponse(
    Guid Id,
    Guid ContractId,
    DateOnly ProposedDueDate,
    string Reason,
    string Status, // Pending, Approved, Rejected
    DateTime RequestedAt
);

public sealed record ContractDisputeResponse(
    Guid Id,
    Guid ContractId,
    Guid RaisedByUserId,
    string DisputeReason,
    IReadOnlyList<string> EvidenceUrls,
    string Status, // Pending, Resolved
    string? AdminVerdict,
    DateTime RaisedAt
);
```

---

## III. BẢNG MÃ LỖI CHUẨN HÓA (ERROR CODE SPECIFICATIONS)

| HTTP Status | Error Code | Mô tả & Nguyên nhân |
| :--- | :--- | :--- |
| `400` | `TERMS_CONFIRMATION_REQUIRED` | Khách hàng hoặc xưởng chưa tích chọn đồng ý điều khoản khi ký |
| `400` | `INVALID_OTP_CODE` | Mã OTP ký số không đúng hoặc đã hết hạn 5 phút |
| `404` | `CONTRACT_NOT_FOUND` | Hợp đồng không tồn tại hoặc người dùng không có quyền truy cập |
| `409` | `CONTRACT_NOT_IN_DRAFT` | Hợp đồng không ở trạng thái `Draft` khi bấm gửi ký |
| `409` | `PARTY_ALREADY_SIGNED` | Người dùng đã ký số hợp đồng này trước đó, không thể ký lại |
| `409` | `CONTRACT_NOT_PENDING_SIGNATURE` | Hợp đồng chưa mở ký số hoặc đã bị hủy |
| `409` | `DEPOSIT_ALREADY_CONFIRMED` | Mốc cọc M1 đã được thanh toán hoàn tất trước đó |
| `409` | `MILESTONE_PREREQUISITE_NOT_MET` | Chưa hoàn thành mốc trước đó (VD: M1 chưa thanh toán không thể làm M2) |
| `409` | `EXTENSION_ALREADY_PENDING` | Hợp đồng đang có một yêu cầu xin gia hạn chưa được xử lý |
| `409` | `CONTRACT_ALREADY_DISPUTED` | Hợp đồng đã đang trong quá trình giải quyết tranh chấp |
| `403` | `SUPPLIER_SUSPENDED` | Xưởng bị khóa tư cách pháp nhân không được phép ký kết hoặc nhận tiền |

