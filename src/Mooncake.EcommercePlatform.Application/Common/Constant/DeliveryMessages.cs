namespace Mooncake.EcommercePlatform.Application.Common.Constant;

/// <summary>English API and domain messages for shipment and Proof-of-Delivery workflows.</summary>
public static class DeliveryMessages
{
    public const string DeliveriesRetrieved = "Deliveries retrieved successfully.";
    public const string DeliveryRetrieved = "Delivery details retrieved successfully.";
    public const string ContractDeliveriesRetrieved = "Contract deliveries retrieved successfully.";
    public const string OrderDeliveriesRetrieved = "Order deliveries retrieved successfully.";
    public const string DeliveryCreated = "Delivery created successfully.";
    public const string DeliveryStatusUpdated = "Delivery status updated successfully.";
    public const string CarrierInfoUpdated = "Delivery carrier information updated successfully.";
    public const string ProofSubmitted = "Proof of delivery submitted successfully.";
    public const string ProofsRetrieved = "Delivery proofs retrieved successfully.";
    public const string ProofRetrieved = "Delivery proof details retrieved successfully.";
    public const string ProofVerified = "Delivery proof integrity verified successfully.";
    public const string ValidSessionRequired = "A valid authenticated user session is required.";

    public const string DeliveryNotFound = "Không tìm thấy chuyến giao. Hãy tải lại danh sách và thử lại.";
    public const string ProofNotFound = "Không tìm thấy bằng chứng giao hàng. Hãy tải lại trang và thử lại.";
    public const string AccessDenied = "Tài khoản của bạn chưa có quyền xem chuyến giao này. Hãy đăng nhập đúng tài khoản được giao đơn.";
    public const string ManageDenied = "Tài khoản của bạn chưa có quyền thao tác trên chuyến giao này. Hãy dùng tài khoản của xưởng sở hữu đơn.";
    public const string ProofSubmitDenied = "Tài khoản của bạn chưa có quyền gửi bằng chứng cho chuyến giao này. Hãy dùng tài khoản của xưởng sở hữu đơn.";
    public const string TargetLinkInvalid = "Chuyến giao cần gắn với đúng một hợp đồng hoặc một đơn hàng. Hãy chọn một trong hai.";
    public const string FromAddressRequired = "Hãy nhập địa chỉ xuất phát của chuyến giao.";
    public const string ToAddressRequired = "Hãy nhập địa chỉ nơi nhận hàng.";
    public const string RecipientNameRequired = "Hãy nhập tên người nhận hàng.";
    public const string RecipientPhoneInvalid = "Số điện thoại người nhận chưa hợp lệ. Hãy nhập số di động Việt Nam gồm 10 chữ số.";
    public const string ScheduledDateInPast = "Ngày giao dự kiến đã qua. Hãy chọn một ngày từ hôm nay trở đi.";
    public const string ScheduledDateRequired = "Hãy chọn ngày giao dự kiến.";
    public const string ScheduledDateTooSoon = "Ngày giao dự kiến cần cách thời điểm tạo chuyến ít nhất 24 giờ. Hãy chọn ngày muộn hơn.";
    public const string SingleDeliveryLimitExceeded = "Đơn hàng này đã có một chuyến giao đang xử lý. Hãy hoàn tất hoặc hủy chuyến đó trước khi tạo chuyến mới.";
    public const string ProofRequiredBeforeDelivered = "Hãy tải lên ảnh bằng chứng giao hàng trước khi đánh dấu chuyến đã giao.";
    public const string DeliveryAlreadyCompleted = "Chuyến giao này đã hoàn tất nên không thể thay đổi thêm.";
    public const string LatitudeInvalid = "Vĩ độ chưa hợp lệ. Hãy nhập giá trị từ -90 đến 90.";
    public const string LongitudeInvalid = "Kinh độ chưa hợp lệ. Hãy nhập giá trị từ -180 đến 180.";
    public const string PhotoRequired = "Hãy chụp ít nhất một ảnh tại điểm giao hàng làm bằng chứng.";
    public const string SignatureRequiredForContract = "Hãy lấy chữ ký người nhận trước khi gửi bằng chứng giao hàng cho hợp đồng.";
    public const string DefaultDriverLabel = "Delivery Driver";
    public const string ShareLinkRetrieved = "Delivery PoD share link status retrieved successfully.";
    public const string ShareLinkCreated = "Delivery PoD share link created successfully.";
    public const string ShareLinkRevoked = "Delivery PoD share link revoked successfully.";
    public const string ShareLinkTtlInvalid = "Thời hạn liên kết chia sẻ cần từ 1 đến 168 giờ. Hãy nhập lại giá trị trong khoảng này.";
    public const string ShareLinkNotFound = "Liên kết giao hàng không còn hiệu lực hoặc đã được thay bằng liên kết mới. Hãy nhờ xưởng gửi lại liên kết.";
    public const string ShareLinkRevokedError = "Xưởng đã thu hồi liên kết giao hàng này. Hãy liên hệ xưởng để nhận liên kết mới.";
    public const string ShareLinkExpiredError = "Liên kết giao hàng đã hết hạn. Hãy nhờ xưởng gửi liên kết mới.";
    public const string DeliveryCannotAcceptShareLink = "Chuyến đã hoàn tất hoặc đã hoàn trả nên không thể tạo liên kết mới cho tài xế.";
    public const string DriverPhotoUploaded = "Driver PoD photo uploaded successfully.";
    public const string DriverAssigned = "Driver assigned successfully.";
    public const string DriverUnassigned = "Driver unassigned successfully.";
    public const string DriverClaimed = "Driver claimed trip successfully.";
    public const string DriverAlreadyClaimedConflict = "Chuyến đã có tài xế khác nhận. Liên hệ xưởng nếu cần đổi tài xế.";
    public const string MustClaimTripFirst = "Hãy bấm Nhận chuyến trước khi chụp ảnh bàn giao.";
    public const string DriverNameRequired = "Hãy nhập tên tài xế.";
    public const string DriverPhoneRequired = "Hãy nhập số điện thoại tài xế.";
    public const string DriverPhoneInvalid = "Số điện thoại tài xế chưa hợp lệ. Hãy nhập số di động Việt Nam gồm 10 chữ số.";
    public const string VehiclePlateRequired = "Hãy nhập biển số xe.";
}

