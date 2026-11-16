namespace Mooncake.EcommercePlatform.Application.Common.Constant;

/// <summary>Stable messages for bulk ordering, pricing, and custom packaging workflows.</summary>
public static class B2BOrderingMessages
{
    public const string QuoteCalculated = "Quote preview calculated successfully.";
    public const string PackagingCreated = "Custom packaging request created successfully.";
    public const string PackagingLoaded = "Custom packaging details retrieved successfully.";
    public const string PackagingRevisionSubmitted = "Packaging mockup revision submitted successfully.";
    public const string PackagingRevisionReviewed = "Packaging mockup revision reviewed successfully.";
    public const string PackagingNotFound = "Không tìm thấy yêu cầu bao bì tùy chỉnh. Hãy tải lại trang và thử lại.";
    public const string PackagingAlreadyExists = "Đơn này đã có bao bì tùy chỉnh. Hãy mở yêu cầu hiện có để chỉnh sửa.";
    public const string PackagingOrderStatusInvalid = "Chỉ thêm được bao bì tùy chỉnh cho đơn đang chờ hoặc đã xác nhận. Hãy kiểm tra trạng thái đơn.";
    public const string PackagingRevisionLocked = "Mẫu thiết kế đã được duyệt nên không nộp thêm bản chỉnh sửa. Hãy liên hệ xưởng nếu cần đổi mẫu.";
    public const string PackagingRevisionNotFound = "Không tìm thấy bản chỉnh sửa mockup. Hãy tải lại trang và thử lại.";
    public const string PackagingRevisionStatusInvalid = "Chỉ bản thiết kế đang chờ duyệt mới duyệt được. Hãy tải lại trang để xem trạng thái mới nhất.";
    public const string PackagingRevisionNotesRequired = "Ghi chú chỉnh sửa cần có ít nhất 10 ký tự. Hãy mô tả rõ hơn điều cần chỉnh.";
    public const string PackagingRevisionSurchargeDisallowed = "Chỉ tính phụ phí từ lần chỉnh sửa thứ 5 trở đi. Hãy bỏ phụ phí cho lần chỉnh sửa này.";
    public const string PackagingProductionBlocked = "Custom packaging design must be approved before production can start.";
    public const string PackagingNotSupported = "Custom packaging is not supported for one or more products in this order.";
    public const string OrderNotFound = "Order was not found.";
    public const string PromotionRangeOverlap = "The quantity range overlaps with an existing active promotion rule.";
    public const string LogoFileRequired = "Please select a corporate logo image file.";
    public const string LogoFileTooLarge = "Corporate logo file size cannot exceed 10 MB.";
    public const string LogoStorageUnavailable = "Image storage service is currently unavailable.";
    public const string LogoUploaded = "Corporate logo uploaded successfully.";
}
