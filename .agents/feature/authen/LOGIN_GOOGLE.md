# Plan đăng nhập bằng Google

Trạng thái: **Đã triển khai code và áp dụng migration `20261013110052_AddGoogleLogin` lên Supabase; chờ kiểm thử OAuth thủ công với tài khoản Google**. Tài liệu này mô tả flow FE nhận
authorization `code` từ Google, gửi `code` về BE, rồi BE xác thực tài khoản và cấp
access/refresh token của Mooncake.

## 1. Flow được chọn

1. Người dùng bấm **Tiếp tục với Google**. FE gọi
   `POST /api/v1/auth/google/authorization-url`.
2. BE tạo `state` ngẫu nhiên, `nonce` và cặp PKCE `code_verifier` /
   `code_challenge` (`S256`). BE lưu tạm bản ghi một lần dùng trong DB, thời hạn
   5 phút, rồi trả `authorizationUrl` và `state`.
3. FE lưu `state` trong `sessionStorage` của tab hiện tại, sau đó chuyển trình
   duyệt đến `authorizationUrl`.
4. Google đưa trình duyệt về **FE callback** với `?code=...&state=...` hoặc
   `?error=access_denied&state=...`. FE so sánh `state` nhận được với giá trị đã
   lưu. Nếu không khớp, FE dừng và báo lỗi.
5. FE gửi `{ code, state }` qua `POST /api/v1/auth/google/login`. BE dùng state
   một lần để lấy PKCE verifier, gọi Google token endpoint bằng `code`,
   `client_id`, `client_secret`, cùng **đúng** `redirect_uri` ở bước 2.
6. BE kiểm tra chữ ký và các claim của Google ID token, tìm tài khoản theo Google
   `sub`, tạo tài khoản mới nếu hợp lệ, rồi cấp cặp token Mooncake theo contract
   login hiện tại. FE lưu cặp token và user bằng cơ chế auth hiện có.

Google authorization `code` chỉ đi qua FE callback và request POST đến BE.
Google token, Google client secret và PKCE verifier không được lưu vào
`localStorage` hoặc trả về FE. Chỉ access/refresh token **của Mooncake** được lưu
theo flow hiện tại. Scope chỉ gồm `openid email profile`; không xin Google
offline access/refresh token vì tính năng này không gọi Google API sau login.
BE dùng authorization endpoint `https://accounts.google.com/o/oauth2/v2/auth`
và token endpoint `https://oauth2.googleapis.com/token` theo Google OIDC
discovery; đây là endpoint của Google, không phải URI cần thêm vào Console.

[Google mô tả authorization code flow cho web server](https://developers.google.com/identity/protocols/oauth2/web-server),
[Google OIDC mô tả ID token và PKCE S256](https://developers.google.com/identity/openid-connect/reference).

## 2. URL cần khai báo trong Google Cloud Console

Tạo OAuth Client ID loại **Web application** và cấu hình OAuth consent screen.
Với FE local đang chạy ở cổng Vite mặc định `5173`:

| Trường trong Google Console | Giá trị local cần thêm |
| --- | --- |
| Authorized redirect URIs | `http://localhost:5173/auth/google/callback` |
| Authorized JavaScript origins | `http://localhost:5173` nếu sau này dùng Google JavaScript SDK; flow BE tạo URL hiện tại không cần mục này |

Môi trường production dùng `https://<ten-mien-fe>/auth/google/callback` trong
**Authorized redirect URIs**. Khai báo riêng cho từng domain/môi trường thực
sự sử dụng. `GoogleOAuth:RedirectUri` của BE phải giống URI đã khai báo **từng
ký tự** (scheme, host, port, path và dấu `/` cuối nếu có). Dùng thống nhất
`localhost`; nếu chạy FE bằng `127.0.0.1`, phải đăng ký thêm đúng URI tương
ứng và dùng cấu hình redirect phù hợp.

**Không đưa** `http://localhost:8089/api/v1/auth/google/authorization-url` hoặc
`http://localhost:8089/api/v1/auth/google/login` vào Authorized redirect URIs:
Google trả `code` cho trang callback của FE, không gọi hai endpoint BE này.
Backend vẫn phải dùng cùng redirect URI FE khi đổi code với Google.

FE hiện dùng `window.location.hash` để điều hướng. Callback OAuth phải là
**pathname thật** `/auth/google/callback`, không phải URL có `#`, và hosting FE
cần rewrite đường dẫn đó về `index.html` khi tải lại trang. Google yêu cầu
redirect URI khớp chính xác giá trị đã đăng ký.
[Hướng dẫn redirect URI của Google](https://developers.google.com/identity/protocols/oauth2/web-server),
[quy tắc redirect và origin](https://developers.google.com/identity/oauth2/web/guides/error).

## 3. Contract API BE

Mọi response dùng `ApiResponse<T>` và message tiếng Việt như login hiện có.
Hai endpoint Google không yêu cầu Bearer token và cần `Cache-Control: no-store`.

### 3.1. Lấy authorization URL

`POST /api/v1/auth/google/authorization-url`, body rỗng.

Response `200`:

```json
{
  "status": 200,
  "message": "Đã tạo liên kết đăng nhập Google.",
  "data": {
    "authorizationUrl": "https://accounts.google.com/o/oauth2/v2/auth?...",
    "state": "<opaque-random-state>",
    "expiresIn": 300
  }
}
```

BE cố định `redirect_uri` từ cấu hình allowlist, không nhận URI tùy ý từ FE.
URL có `response_type=code`, `scope=openid email profile`, `state`, `nonce`,
`code_challenge`, `code_challenge_method=S256` và `client_id`.

### 3.2. FE gửi code để đăng nhập

`POST /api/v1/auth/google/login`:

```json
{
  "code": "<authorization-code-from-google>",
  "state": "<state-from-google-callback>"
}
```

Response `200`: `data` giống `POST /api/v1/auth/login` hiện tại, gồm
`accessToken`, `refreshToken`, `tokenType`, `expiresIn`, `user`.
BE không trả Google access token, ID token hoặc refresh token cho FE.
Sau đó FE dùng `/api/v1/auth/refresh-token`, `/logout` và `/me` như login
bằng số điện thoại; không có endpoint Google refresh riêng.

### 3.3. Lỗi và message

| Trường hợp | HTTP | Message BE/FE hiển thị |
| --- | --- | --- |
| Thiếu/sai định dạng `code` hoặc `state` | 400 | `Dữ liệu đăng nhập Google không hợp lệ.` |
| Người dùng từ chối quyền ở Google (`access_denied`) | FE xử lý, không gọi BE | `Bạn đã hủy đăng nhập bằng Google.` |
| State sai, hết hạn, đã dùng hoặc không khớp với tab FE | 401 | `Phiên đăng nhập Google không hợp lệ hoặc đã hết hạn. Vui lòng thử lại.` |
| Code sai, hết hạn, đã dùng; ID token sai chữ ký/issuer/audience/nonce; email chưa xác minh | 401 | `Không thể xác thực tài khoản Google. Vui lòng thử lại.` |
| Email Google đã thuộc user chưa liên kết với Google | 409 | `Email này đã có tài khoản. Vui lòng đăng nhập bằng phương thức hiện tại.` |
| User đã liên kết bị khóa hoặc xóa | 401 | `Không thể đăng nhập bằng Google. Vui lòng liên hệ hỗ trợ.` |
| Vượt giới hạn thử đăng nhập | 429 | `Bạn đã thử đăng nhập quá nhiều lần. Vui lòng thử lại sau.` |
| Google token endpoint lỗi mạng/5xx | 502 | `Không thể kết nối Google. Vui lòng thử lại sau.` |
| Cấu hình OAuth thiếu hoặc lỗi BE ngoài dự kiến | 500 | `Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.` |

FE phải hiển thị lỗi mạng/timeout hiện có; không chuyển sang mock login.
Không log code, Google token, client secret, PKCE verifier hay JWT/refresh token.

## 4. BE: cấu hình, dữ liệu và xử lý

1. Thêm `GoogleOAuth` vào file `WebApi/appsettings.sample.json` với
   `ClientId`, `ClientSecret`, `AuthorizationEndpoint`, `RedirectUri`,
   `StateLifetimeMinutes`.
   Giá trị thật chỉ nằm trong `appsettings.json` đã ignore hoặc biến môi trường
   `GoogleOAuth__ClientId`, `GoogleOAuth__ClientSecret`,
   `GoogleOAuth__RedirectUri`. Không đưa client secret vào FE hoặc Git.
2. Mở rộng cặp `IAuthenService`/`AuthenService` hiện có cho hai thao tác Google.
   Thêm DTO request/response trong `Application/DTOs/Authen`; controller chỉ
   gọi service. Đặt message tiếng Việt trong
   `Application/Common/Constant/MessageConstant.cs`. Tách client gọi Google
   qua interface ở Application và implementation ở Infrastructure để có thể
   kiểm thử lỗi Google mà không gọi mạng thật.
3. Migration thêm bảng liên kết `user_external_logins` với
   `provider = Google`, `provider_subject = sub`, `user_id`, thời điểm tạo;
   unique `(provider, provider_subject)` và một liên kết Google tối đa cho mỗi
   user. Dùng `sub` làm khóa danh tính bền vững, **không dùng email làm khóa**
   vì email có thể đổi. [Google khuyến nghị dùng `sub`](https://developers.google.com/identity/openid-connect/reference).
4. Migration thêm bảng `google_login_attempts` lưu hash của state, PKCE
   verifier, nonce, thời điểm hết hạn và cờ đã dùng. BE tiêu thụ state bằng
   thao tác cập nhật có điều kiện/transaction để hai request không đổi cùng
   một code. Dọn bản ghi hết hạn định kỳ. Cách lưu DB hỗ trợ nhiều instance BE.
5. `users.password_hash` cần cho phép NULL đối với tài khoản chỉ dùng Google.
   Login số điện thoại phải từ chối an toàn nếu user không có password; đăng ký
   số điện thoại vẫn bắt buộc đặt password. `users.phone` đã nullable nên Google
   signup không tự tạo số điện thoại giả. Các nghiệp vụ cần phone sẽ yêu cầu
   bổ sung phone ở luồng riêng.
6. BE đổi code bằng HTTPS, gửi `code_verifier` và cùng `redirect_uri`; dùng
   Google OIDC/Google Auth library để kiểm tra chữ ký ID token theo JWKS, `iss`,
   `aud = ClientId`, `exp`, `nonce`, `sub`, `email_verified = true`.
   Chỉ dùng `email`, `name`, `picture` sau khi token hợp lệ; không tin
   profile do FE tự gửi. Không dùng Google `tokeninfo` làm bước xác thực
   production. [Các bước xác thực ID token của Google](https://developers.google.com/identity/openid-connect/openid-connect).
7. Nếu `sub` đã liên kết, kiểm tra user active/chưa xóa và giữ nguyên
   `user_id`, role, phone, password, email nội bộ. Nếu `sub` mới mà email
   đã thuộc user khác, trả 409; **không tự liên kết chỉ bằng email**. Nếu email
   chưa có, tạo user Customer active với email đã xác minh, tên/avatar tùy có,
   phone NULL, password NULL, rồi tạo liên kết trong cùng transaction. Bảo đảm
   xử lý đồng thời bằng unique constraint. Kiểm tra email trùng không phân biệt
   chữ hoa/thường trong dữ liệu hiện có, rồi tạo unique index `lower(email)`
   trên tất cả user để tránh tạo hai tài khoản chỉ khác chữ hoa/thường.
8. Tái sử dụng logic cấp access token, refresh token lưu hash và xoay token
   hiện có trong `AuthenService`. Google login thành công tạo đúng một phiên
   Mooncake; không lưu Google token trong DB khi không cần gọi Google API.
   Giới hạn tần suất hai endpoint theo IP; trạng thái code/state một lần dùng.

## 5. FE: callback và phiên đăng nhập

1. Sửa `authService.js`: thay hàm `loginWithGoogle(googleToken)` hiện là
   placeholder bằng hai hàm lấy URL và đổi `{code,state}`; cập nhật
   `apiEndpoints.js`. Hai request này không được interceptor refresh token
   tự gọi lại.
2. Nút Google trong `LoginPopup.jsx` gọi BE lấy URL, lưu state vào
   `sessionStorage`, rồi `window.location.assign(authorizationUrl)`.
   Chặn bấm lặp; lỗi lấy URL hiển thị tại popup.
3. Thêm màn hình callback cho pathname `/auth/google/callback` trong
   `App.jsx` trước logic trang theo hash. Đọc `code/state/error` từ query;
   nếu `error=access_denied` hoặc state không khớp thì báo lỗi, không gửi code
   đến BE. Xóa code/state khỏi thanh địa chỉ ngay bằng
   `history.replaceState`; không tải analytics/script bên thứ ba trên màn hình
   callback trước bước này.
4. Sau response thành công, lưu `accessToken`, `refreshToken`, `user`
   cùng key `auth_token`, `auth_refresh_token`, `auth_user` như login phone;
   phát `auth:changed`, cập nhật `AuthContext`, chuyển về trang ban đầu.
   Khi lỗi, xóa state tạm, hiện message Việt, cho người dùng thử lại.
5. Giữ flow refresh đồng thời, logout và `/me` hiện có. Kiểm tra FE host
   production rewrite `/auth/google/callback` về SPA để reload/direct URL
   không trả 404.

## 6. Kiểm thử và điều kiện hoàn thành

- FE nhận URL từ BE, Google redirect đúng URI đã khai báo, callback gửi
  `code/state`, BE trả cặp token Mooncake và FE duy trì phiên qua reload.
- State sai/hết hạn/dùng lại, code sai/dùng lại, Google từ chối, lỗi mạng Google,
  ID token sai claim, email chưa xác minh đều bị từ chối đúng status/message.
- Google `sub` đã liên kết đăng nhập đúng user cũ; tài khoản bị khóa không
  đăng nhập; email trùng chưa liên kết trả 409; hai lần tạo cùng `sub` đồng
  thời chỉ tạo một user/link.
- Google-only user không có phone/password vẫn đăng nhập và refresh được;
  login password đối với user không có password trả 401 thay vì lỗi 500.
- URL không chứa client secret/PKCE verifier; FE không lưu Google token;
  log không chứa code/token; callback xóa query sau khi đọc.
- BE build và migration áp dụng được trên DB thử nghiệm; FE build; kiểm tra
  thủ công trên `http://localhost:5173/auth/google/callback` và đường dẫn
  production thực tế.

## 7. Giới hạn của đợt này

Liên kết Google vào tài khoản phone/email đã tồn tại cần một flow riêng khi
người dùng **đã đăng nhập bằng phương thức cũ** để chứng minh sở hữu tài khoản.
Đợt này trả 409 khi email trùng; không tự gộp hai user. Không đổi cơ chế lưu
Mooncake token hiện tại trên FE và không thêm quyền truy cập Google API khác.
