# Kế hoạch triển khai đăng nhập bằng số điện thoại

Trạng thái: **Đã triển khai trong source ngày 13/10/2026**. Migration đã được tạo nhưng
chưa áp dụng lên database; cần kiểm tra số điện thoại trùng trước khi chạy migration.

## 1. Mục tiêu và phạm vi

- Người dùng đăng nhập bằng `phoneNumber` và `password`.
- BE trả `accessToken`, `refreshToken` và thông tin user tối thiểu khi đăng nhập thành công.
- FE lưu hai token vào `localStorage`, gắn access token vào request cần xác thực.
- Khi access token hết hạn, FE tự dùng refresh token gọi BE, cập nhật token và gửi lại request.
- Đăng xuất hoặc refresh thất bại phải kết thúc phiên đăng nhập trên FE.

Phạm vi đợt này gồm login, refresh token, logout, tích hợp giao diện login và trạng thái đăng
nhập. OTP, Google login, quên mật khẩu và luồng đăng ký hoàn chỉnh là các tính năng riêng.

## 2. Hiện trạng trước triển khai trong hai repo

| Khu vực | Hiện trạng cần xử lý |
| --- | --- |
| BE `AuthenController` (sẽ tạo) | Chưa tồn tại; CRUD user hiện nằm trong `UsersController`. |
| BE `AuthenService` (sẽ tạo) | Chưa tồn tại; `UserService` hiện lưu password dưới dạng Base64. |
| BE `User` | `Phone` cho phép null; DB chưa có ràng buộc duy nhất cho số điện thoại. |
| BE `Program.cs` | Có `UseAuthorization()` nhưng chưa cấu hình JWT authentication. |
| FE `LoginPopup.jsx` | Login đang thành công giả bằng `setTimeout`, chưa gọi API. |
| FE `authService.js` | Gửi `phone`, đọc `data.token`; chưa lưu hoặc dùng refresh token. |
| FE `apiClient.js` | Có Bearer interceptor nhưng gặp `401` là xóa phiên ngay. |
| FE `Header.jsx` | Dùng cờ `b2b_logged_in` riêng, chưa đồng bộ với `AuthContext`. |
| Cấu hình API | FE mặc định gọi cổng `5043`; BE mặc định chạy cổng `8089`. |

## 3. Contract API đề xuất

### 3.1. Login

`POST /api/v1/auth/login`

Request:

```json
{
  "phoneNumber": "0912345678",
  "password": "Example@123"
}
```

Response `200`, dùng `ApiResponse<T>` hiện có của BE:

```json
{
  "status": 200,
  "message": "Đăng nhập thành công",
  "data": {
    "accessToken": "<jwt>",
    "refreshToken": "<opaque-token>",
    "tokenType": "Bearer",
    "expiresIn": 900,
    "user": {
      "id": "<uuid>",
      "phone": "0912345678",
      "fullName": "Example User",
      "role": "Customer"
    }
  },
  "path": "/api/v1/auth/login",
  "traceId": "<trace-id>",
  "time": "<utc-time>"
}
```

`expiresIn` là số giây còn hiệu lực của access token. Đề xuất thời hạn: access token
15 phút, refresh token 7 ngày; hai giá trị cần cấu hình được ở BE.

#### Thông điệp lỗi login

`AuthenController`/`AuthenService` trả lỗi qua `ApiResponse<T>` hiện có. Nội dung
`message` trả cho người dùng phải bằng **tiếng Việt** và thống nhất giữa BE/FE:

| Tình huống | HTTP | `message` hiển thị |
| --- | --- | --- |
| Thiếu body, JSON sai cấu trúc hoặc sai kiểu dữ liệu | `400` | `Dữ liệu đăng nhập không hợp lệ.` |
| Thiếu hoặc để trống `phoneNumber` | `400` | `Vui lòng nhập số điện thoại.` |
| `phoneNumber` không đúng định dạng sau khi chuẩn hóa | `400` | `Số điện thoại không hợp lệ.` |
| Thiếu hoặc để trống `password` | `400` | `Vui lòng nhập mật khẩu.` |
| Số điện thoại không tồn tại | `401` | `Số điện thoại hoặc mật khẩu không đúng.` |
| Mật khẩu không đúng | `401` | `Số điện thoại hoặc mật khẩu không đúng.` |
| Tài khoản bị vô hiệu hóa hoặc đã xóa | `401` | `Số điện thoại hoặc mật khẩu không đúng.` |
| Vượt giới hạn số lần thử đăng nhập | `429` | `Bạn đã thử đăng nhập quá nhiều lần. Vui lòng thử lại sau.` |
| Lỗi hệ thống ngoài dự kiến | `500` | `Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.` |

Các trường hợp `401` dùng cùng một message để không tiết lộ số điện thoại nào đã đăng ký
hoặc trạng thái tài khoản. Không trả mật khẩu, hash hoặc chi tiết lỗi nội bộ. Nếu có nhiều
lỗi dữ liệu đầu vào, ưu tiên theo thứ tự: body → số điện thoại trống → số điện thoại sai
định dạng → mật khẩu trống. Login chỉ yêu cầu password không rỗng; không áp quy tắc độ
dài của đăng ký để tránh từ chối tài khoản cũ trước khi BE kiểm tra mật khẩu.

Ví dụ response khi sai thông tin đăng nhập:

```json
{
  "status": 401,
  "message": "Số điện thoại hoặc mật khẩu không đúng.",
  "path": "/api/v1/auth/login",
  "traceId": "<trace-id>",
  "time": "<utc-time>"
}
```

FE dùng `message` từ BE cho lỗi HTTP. Khi request **không có response từ BE**, FE tự
hiển thị `Không thể kết nối đến máy chủ. Vui lòng thử lại.`; khi timeout, hiển thị
`Kết nối quá thời gian chờ. Vui lòng thử lại.`. Hai lỗi này không được biến thành login
thành công bằng mock fallback.

### 3.2. Refresh token

`POST /api/v1/auth/refresh-token`

Request:

```json
{
  "refreshToken": "<opaque-token>"
}
```

Response `200`: `data` gồm `accessToken`, `refreshToken`, `tokenType`, `expiresIn` theo
cùng định dạng với login. BE thu hồi refresh token cũ ngay khi cấp cặp token mới. Refresh
token sai, hết hạn, đã thu hồi hoặc đã dùng lại trả `401` với message
`Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.`.

### 3.3. Logout

`POST /api/v1/auth/logout` nhận refresh token hiện tại để BE thu hồi phiên tương ứng.
FE xóa dữ liệu phiên ở `localStorage` và `AuthContext` khi logout hoàn tất; nếu request
logout lỗi mạng, FE vẫn xóa phiên cục bộ.

## 4. Công việc BE

1. Bổ sung DTO `LoginRequest`, `RefreshTokenRequest` và các response DTO trong Application.
   Tạo đúng một cặp `IAuthenService` (Interface) và `AuthenService` (Implementation) theo
   cấu trúc `Services/Interfaces` và `Services/Implementations` hiện có. Tạo
   `AuthenController` trong WebApi để nhận request và gọi `IAuthenService`; không đặt
   nghiệp vụ xác thực trong controller.
2. Chuẩn hóa số điện thoại theo một quy tắc thống nhất trước khi lưu và tra cứu. Thêm hàm
   tra cứu user theo phone trong `IUserRepository`/`UserRepository`. Kiểm tra dữ liệu trùng
   hiện có, rồi thêm unique index phù hợp qua EF migration. Không cho login user có
   `IsActive = false` hoặc `IsDeleted = true`.
3. Thay Base64 bằng password hash có salt và thuật toán phù hợp. Với tài khoản đã tạo bằng
   Base64, xác thực theo định dạng cũ một lần rồi ghi lại hash mới sau khi login thành công;
   loại bỏ nhánh tương thích sau khi dữ liệu được nâng cấp. Chấp nhận hash bcrypt hiện có
   (`$2a$`, `$2b$`, `$2y$`) khi login và giữ nguyên hash bcrypt sau khi xác thực thành công.
4. Tạo class `JwtTokenUtils` riêng trong `Application/Common/Utils` để sinh JWT access
   token; `AuthenService` gọi class này, không tự viết logic tạo JWT. Claims tối thiểu gồm
   user ID và role. Cấu hình JWT Bearer kiểm tra chữ ký, issuer, audience và expiration;
   đặt `UseAuthentication()` trước `UseAuthorization()` trong `Program.cs`.
5. Tạo refresh token ngẫu nhiên, lưu **hash** cùng user ID, thời điểm hết hạn, thu hồi và
   thông tin xoay token. Refresh phải kiểm tra DB và xoay token theo một thao tác nhất quán,
   để token cũ không thể đổi thêm lần nữa. Logout thu hồi refresh token đang dùng.
6. Đăng ký `IAuthenService`/`AuthenService` và repository cần thiết. Khai báo `Jwt:Key`,
   `Jwt:Issuer`, `Jwt:Audience`, `Jwt:AccessTokenMinutes`, `Jwt:RefreshTokenDays` trong
   `WebApi/appsettings.json`; BE đọc cấu hình này khi sinh và kiểm tra token. Giá trị
   `Jwt:Key` commit vào repo chỉ là placeholder cho local setup; khóa thật được đặt ở
   cấu hình môi trường triển khai hoặc override bằng `Jwt__Key`. Cấu hình CORS cho origin
   FE và đồng bộ cổng API local giữa hai repo.
7. Bảo đảm response và log không chứa password, password hash hoặc refresh token.
8. Chuẩn hóa lỗi validation thành `ApiResponse<T>` và áp dụng đúng bảng message tiếng Việt
   ở mục 3.1. Giới hạn số lần thử login theo số điện thoại/IP và trả `429` khi vượt ngưỡng.

Các thay đổi cần đặt đúng tầng Domain → Application → Infrastructure → WebApi và giữ
`ApiResponse<T>`/quy ước lỗi đang dùng trong BE.

Ví dụ cấu trúc cấu hình dự kiến trong `appsettings.json`:

```json
{
  "Jwt": {
    "Key": "<replace-with-secret-key>",
    "Issuer": "Mooncake.EcommercePlatform",
    "Audience": "Mooncake.Frontend",
    "AccessTokenMinutes": 15,
    "RefreshTokenDays": 7
  }
}
```

## 5. Công việc FE

1. `authService.login` gửi đúng `{ phoneNumber, password }`, đọc `response.data`, lưu
   `accessToken`, `refreshToken` và user qua `storage.js`. Chỉ coi login thành công khi
   response có đủ dữ liệu cần thiết.
2. `LoginPopup` gọi `useAuth().login(...)` thay cho `setTimeout`. Hiển thị loading/lỗi
   từ API theo bảng ở mục 3.1, ngăn submit lặp và chỉ chuyển trang khi đăng nhập thành
   công. Đồng bộ validation FE với BE; không chặn login chỉ vì password dưới 6 ký tự.
3. `apiClient` tiếp tục gắn `Authorization: Bearer <accessToken>` cho request thường.
   Bổ sung response interceptor xử lý refresh theo flow ở mục 6.
4. `AuthContext` đồng bộ user/token sau login, refresh và logout; nhận sự kiện hết phiên
   để Header và các trang cập nhật ngay. `Header` dùng state này thay cho
   `b2b_logged_in`.
5. Tách mock auth khỏi luồng xác thực thật. Khi chạy với BE, lỗi login/refresh không được
   chuyển thành mock success. Chế độ mock, nếu cần, phải được bật rõ ràng cho môi trường
   phát triển và không phát token giả vào phiên thật.
6. Đồng bộ `VITE_API_BASE_URL` với BE và kiểm tra CORS. Xử lý checkbox "Ghi nhớ đăng
   nhập": vì yêu cầu hiện tại là lưu token vào `localStorage`, cần chọn rõ hành vi của
   checkbox trước khi code; phương án đơn giản là bỏ checkbox để giao diện đúng thực tế.

## 6. Flow tự refresh khi access token hết hạn

1. FE gửi request kèm access token đang lưu.
2. BE trả `401` nếu access token hết hạn hoặc không hợp lệ. Interceptor chỉ thử refresh
   với request thường và chỉ khi FE đang có refresh token.
3. FE gọi `POST /api/v1/auth/refresh-token` bằng một request riêng, gửi refresh token trong
   body. Request login, refresh và logout không tự kích hoạt lại flow refresh.
4. Nếu nhiều request đồng thời nhận `401`, FE dùng chung **một Promise refresh đang chạy**;
   chỉ có một request refresh gửi tới BE. Các request còn lại chờ kết quả.
5. Khi refresh thành công, FE ghi đè **cả access token và refresh token** trong
   `localStorage`, cập nhật `AuthContext`, gắn access token mới rồi gửi lại mỗi request
   ban đầu tối đa **một lần**.
6. Nếu refresh trả `401`, lỗi mạng, hoặc request gửi lại vẫn `401`, FE xóa token và user,
   cập nhật UI về trạng thái chưa đăng nhập và yêu cầu người dùng login lại. Không tạo
   vòng lặp refresh vô hạn.
7. Khi logout, FE không cho các request refresh đang chờ ghi token trở lại storage; phiên
   đã bị kết thúc phải giữ trạng thái đăng xuất.

## 7. Kiểm thử và điều kiện hoàn thành

- Login đúng cấp đủ hai token; sai password/số điện thoại trả `401` giống nhau.
- Kiểm tra HTTP status và message tiếng Việt chính xác cho từng lỗi login ở mục 3.1,
  gồm dữ liệu thiếu/sai định dạng, tài khoản bị khóa, `429`, timeout và lỗi kết nối.
- Số điện thoại trùng, user bị khóa/xóa và dữ liệu Base64 cũ được xử lý đúng.
- Request có Bearer token hợp lệ đi qua endpoint bảo vệ; token sai/hết hạn bị từ chối.
- Access token hết hạn → refresh thành công → lưu cặp token mới → request gốc thành công.
- Nhiều request hết hạn cùng lúc chỉ gọi refresh một lần; mỗi request chỉ retry một lần.
- Refresh token hết hạn, thu hồi hoặc dùng lại bị từ chối; FE xóa phiên và cập nhật Header.
- Logout thu hồi refresh token; tải lại trang không khôi phục phiên đã logout.
- Login thất bại hoặc BE không chạy không thể tạo phiên mock ngoài chế độ mock được chọn.
- BE build/test đạt; FE `npm run build` đạt; kiểm tra tích hợp trên cùng cấu hình URL/CORS.

## 8. Lưu ý bảo mật và quyết định cần review

- `localStorage` là yêu cầu của tính năng này, nhưng script chạy cùng origin có thể đọc
  token nếu trang bị XSS. Khi triển khai cần kiểm soát nội dung đầu vào, phụ thuộc FE,
  CSP và thời hạn token. Có thể cân nhắc cookie `HttpOnly` cho refresh token ở một đợt
  thiết kế khác.
- Plan mặc định refresh token được **xoay sau mỗi lần sử dụng** và FE lưu token mới. Nếu
  BE chỉ muốn trả access token mới, cần đổi contract và chiến lược chống dùng lại token.
- Form đăng ký FE hiện là mock; có thể dùng user tạo qua BE để kiểm thử login trước.
  Đăng ký thật bằng số điện thoại cần một kế hoạch riêng.
