namespace Mooncake.EcommercePlatform.Application.Common.Constant;

/// <summary>Limits, realtime contract names, and user-facing messages for customer-supplier chat.</summary>
public static class ChatConstants
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 50;
    public const int MaxBodyLength = 4000;
    public const int MaxAttachmentsPerMessage = 10;
    public const int MaxUploadFiles = 10;
    public const long MaxFileBytes = 10L * 1024 * 1024;
    public const long MaxTotalUploadBytes = 50L * 1024 * 1024;
    public const long MaxUploadRequestBytes = MaxTotalUploadBytes + (1024 * 1024);
    public const int MaxFileNameLength = 200;
    public const int MaxPresenceIds = 50;
    public const int MaxCursorLength = 2048;
    public const int CursorLifetimeMinutes = 15;
    public const int PreviewLength = 120;
    public const int DownloadUrlLifetimeMinutes = 5;
    public const int EditWindowMinutes = 5;
    public const int PresenceOfflineGraceSeconds = 5;

    public const string HubPath = "/api/v1/hubs/chat";
    public const string SendRatePolicy = "chat-send";
    public const string UploadRatePolicy = "chat-upload";
    public const int SendPermitPerMinute = 60;
    public const int UploadPermitPerMinute = 10;

    public static string UserGroup(Guid userId) => $"chat-user:{userId}";
    public static string ThreadGroup(Guid threadId) => $"chat-thread:{threadId}";

    public const string EventMessageCreated = "MessageCreated";
    public const string EventMessageEdited = "MessageEdited";
    public const string EventMessageRecalled = "MessageRecalled";
    public const string EventReadUpdated = "ReadUpdated";
    public const string EventUnreadChanged = "UnreadChanged";
    public const string EventThreadUpdated = "ThreadUpdated";
    public const string EventTyping = "Typing";
    public const string EventPresenceChanged = "PresenceChanged";

    public static readonly IReadOnlySet<string> ImageExtensions =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

    public static readonly IReadOnlySet<string> DocumentExtensions =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".pdf", ".doc", ".docx", ".xls", ".xlsx" };

    public const string RecalledPreview = "Tin nhắn đã được thu hồi";
    public const string AttachmentPreview = "Đã gửi tệp đính kèm";
    public const string ProductPreview = "Đã gửi một sản phẩm";

    public const string ThreadOpened = "Đã mở cuộc trò chuyện.";
    public const string ThreadsListed = "Lấy danh sách cuộc trò chuyện thành công.";
    public const string ThreadFetched = "Lấy cuộc trò chuyện thành công.";
    public const string MessagesListed = "Lấy tin nhắn thành công.";
    public const string MessageSent = "Đã gửi tin nhắn.";
    public const string MessageEdited = "Đã chỉnh sửa tin nhắn.";
    public const string MessageRecalled = "Đã thu hồi tin nhắn.";
    public const string ThreadBlocked = "Đã chặn cuộc trò chuyện.";
    public const string ThreadUnblocked = "Đã bỏ chặn cuộc trò chuyện.";
    public const string AttachmentsUploaded = "Đã tải tệp lên.";
    public const string DownloadUrlCreated = "Đã tạo liên kết tải xuống.";
    public const string ReadMarked = "Đã đánh dấu đã đọc.";
    public const string UnreadFetched = "Lấy số tin chưa đọc thành công.";
    public const string PresenceFetched = "Lấy trạng thái hoạt động thành công.";
    public const string PresenceSettingsUpdated = "Đã cập nhật cài đặt trạng thái hoạt động.";

    public const string InvalidShopId = "Mã gian hàng không hợp lệ.";
    public const string InvalidThreadId = "Mã cuộc trò chuyện không hợp lệ.";
    public const string InvalidMessageId = "Mã tin nhắn không hợp lệ.";
    public const string InvalidAttachmentId = "Mã tệp đính kèm không hợp lệ.";
    public const string InvalidClientMessageId = "Mã định danh tin nhắn phía client không hợp lệ.";
    public const string InvalidPageSize = "Số lượng mỗi trang phải từ 1 đến 50.";
    public const string InvalidCursor = "Con trỏ phân trang không hợp lệ hoặc đã hết hạn.";
    public const string BodyTooLong = "Tin nhắn tối đa 4000 ký tự.";
    public const string EmptyMessage = "Tin nhắn cần có nội dung, tệp đính kèm hoặc sản phẩm.";
    public const string TooManyAttachments = "Mỗi tin nhắn tối đa 10 tệp đính kèm.";
    public const string DuplicateAttachments = "Danh sách tệp đính kèm bị trùng.";
    public const string InvalidProductId = "Mã sản phẩm không hợp lệ.";
    public const string NoFiles = "Vui lòng chọn ít nhất một tệp.";
    public const string TooManyFiles = "Tối đa 10 tệp mỗi lần tải lên.";
    public const string FileTooLarge = "Mỗi tệp tối đa 10MB.";
    public const string TotalTooLarge = "Tổng dung lượng tối đa 50MB mỗi lần tải lên.";
    public const string UnsupportedFile = "Chỉ hỗ trợ ảnh JPG, PNG, WEBP, GIF và tài liệu PDF, DOC, DOCX, XLS, XLSX.";
    public const string InvalidFileName = "Tên tệp không hợp lệ.";
    public const string InvalidFileContent = "Nội dung tệp không khớp định dạng hoặc bị hỏng.";
    public const string InvalidPresenceIds = "Danh sách người dùng không hợp lệ (1-50 mã, không trùng).";

    public const string ShopNotFound = "Không tìm thấy gian hàng.";
    public const string CannotChatOwnShop = "Bạn không thể nhắn tin với gian hàng của chính mình.";
    public const string ThreadNotFound = "Không tìm thấy cuộc trò chuyện.";
    public const string MessageNotFound = "Không tìm thấy tin nhắn.";
    public const string AttachmentNotFound = "Không tìm thấy tệp đính kèm.";
    public const string AttachmentsUnavailable = "Một số tệp đính kèm không tồn tại hoặc đã được sử dụng.";
    public const string ProductNotFound = "Sản phẩm không thuộc gian hàng này hoặc không còn tồn tại.";
    public const string NotMessageOwner = "Bạn chỉ có thể thao tác với tin nhắn của mình.";
    public const string EditWindowExpired = "Chỉ có thể chỉnh sửa tin nhắn trong vòng 5 phút sau khi gửi.";
    public const string CannotEditRecalledMessage = "Không thể chỉnh sửa tin nhắn đã thu hồi.";
    public const string ConversationBlocked = "Cuộc trò chuyện đang bị chặn, không thể gửi tin nhắn.";
    public const string CannotUnblockPartnerBlock = "Chỉ người đã chặn cuộc trò chuyện mới có thể bỏ chặn.";
}

