namespace Mooncake.EcommercePlatform.Application.Common.Constant;

/// <summary>Constants, limits, and user-facing messages for product wishlist workflows.</summary>
public static class WishlistConstants
{
    public const int DefaultPageSize = 12;
    public const int MaxPageSize = 50;
    public const int MaxBatchCheckIds = 100;
    public const int CursorLifetimeMinutes = 15;

    public const string ListedSuccess = "Lấy danh sách sản phẩm yêu thích thành công.";
    public const string AddedSuccess = "Đã thêm vào sản phẩm yêu thích.";
    public const string RemovedSuccess = "Đã gỡ khỏi sản phẩm yêu thích.";
    public const string BatchCheckedSuccess = "Kiểm tra danh sách yêu thích thành công.";

    public const string ProductNotFound = "Sản phẩm không tồn tại hoặc đã bị xóa.";
    public const string InvalidProductId = "Mã sản phẩm không hợp lệ.";
    public const string EmptyBatchIds = "Danh sách mã sản phẩm không được để trống.";
    public const string InvalidProductIdList = "Danh sách mã sản phẩm không hợp lệ.";
    public const string InvalidCursor = "Con trỏ phân trang không hợp lệ hoặc đã hết hạn.";
    public const string InvalidPageSize = "Số lượng sản phẩm mỗi trang phải từ 1 đến 50.";
    public const string InvalidBatchSize = "Chỉ có thể kiểm tra tối đa 100 sản phẩm mỗi lần.";
}

