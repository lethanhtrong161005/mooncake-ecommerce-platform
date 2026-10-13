namespace Mooncake.EcommercePlatform.Application.Common.Constant;

/// <summary>Vietnamese messages returned by authentication endpoints.</summary>
public static class MessageConstant
{
    public const string InvalidRequest = "Dữ liệu đăng nhập không hợp lệ.";
    public const string PhoneRequired = "Vui lòng nhập số điện thoại.";
    public const string PhoneInvalid = "Số điện thoại không hợp lệ.";
    public const string PasswordRequired = "Vui lòng nhập mật khẩu.";
    public const string InvalidCredentials = "Số điện thoại hoặc mật khẩu không đúng.";
    public const string TooManyAttempts = "Bạn đã thử đăng nhập quá nhiều lần. Vui lòng thử lại sau.";
    public const string ServerError = "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.";
    public const string SessionExpired = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
    public const string LoginSuccess = "Đăng nhập thành công";
    public const string RefreshSuccess = "Làm mới phiên đăng nhập thành công.";
    public const string LogoutSuccess = "Đăng xuất thành công.";
    public const string GoogleUrlCreated = "Đã tạo liên kết đăng nhập Google.";
    public const string GoogleInvalidRequest = "Dữ liệu đăng nhập Google không hợp lệ.";
    public const string GoogleInvalidState = "Phiên đăng nhập Google không hợp lệ hoặc đã hết hạn. Vui lòng thử lại.";
    public const string GoogleInvalidIdentity = "Không thể xác thực tài khoản Google. Vui lòng thử lại.";
    public const string GoogleEmailConflict = "Email này đã có tài khoản. Vui lòng đăng nhập bằng phương thức hiện tại.";
    public const string GoogleUserInactive = "Không thể đăng nhập bằng Google. Vui lòng liên hệ hỗ trợ.";
    public const string GoogleUnavailable = "Không thể kết nối Google. Vui lòng thử lại sau.";
}
