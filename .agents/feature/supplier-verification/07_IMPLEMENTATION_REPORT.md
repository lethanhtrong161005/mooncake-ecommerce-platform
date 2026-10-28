# Supplier verification — báo cáo triển khai

## Khắc phục MVC validation của revoke/read-all — 2026-10-24

Trace `8383dfeca753446cab5aa833ae749322` xác định lỗi 500 xảy ra trước controller/service: positional record RevokeSupplierRequest có validation annotations đặt ở property thay vì constructor parameter. Cùng lỗi được tìm thấy trên ReadVerificationNotificationsRequest. Đã chuyển annotations sang constructor parameters cho cả hai; JSON contract, expectedVersion/idempotency và chính sách thu hồi không thay đổi.

Thêm 8 MVC ObjectModelValidator regression cases, gồm đúng payload expectedVersion=2 với lý do tiếng Việt người dùng báo lỗi, version thiếu/âm, reason thiếu/quá ngắn và read-through token hợp lệ/thiếu/rỗng. Khác với test gọi service trực tiếp, các test này chạy qua metadata/record validation của MVC. Kết quả BE: 48 pass, 5 PostgreSQL tests không chạy lại. Không áp dụng quyết định thu hồi thử lên hồ sơ người dùng; request gây lỗi ban đầu chưa vào service nên chưa thực hiện thu hồi. Không cần migration mới.

## Xem trước tài liệu trong UI

Đã bổ sung icon con mắt trong component VerificationDocuments dùng chung cho tài liệu đang soạn, hồ sơ đã gửi, màn duyệt Admin và snapshot lịch sử. PDF mở bằng trình xem PDF của trình duyệt trong Modal; JPEG/PNG hiển thị bằng Ant Design Image. Nút tải xuống vẫn được giữ lại.

Preview gọi endpoint content hiện có bằng Bearer token, chỉ tạo blob URL trong trình duyệt sau khi API kiểm tra quyền; không mở URL Cloudinary trực tiếp và không đặt token trong URL. Chỉ chấp nhận MIME PDF/JPEG/PNG, không render HTML. Request bị hủy khi đóng, blob URL được thu hồi khi đóng/unmount; file chưa Ready không mở được preview. Lỗi quyền/storage hiển thị trong popup.

Browser regression kiểm tra Customer xem PDF/PNG, Admin xem PDF, không phát sinh download, thu hồi blob URL sau đóng và xử lý 403 không lộ nội dung. FE build/lint phần sửa pass. Không đổi API hoặc schema BE.

**Cập nhật upload:** [09_DOCUMENT_UPLOAD_OPERATIONS.md](./09_DOCUMENT_UPLOAD_OPERATIONS.md). Đã khởi chạy ClamAV và kiểm chứng thực tế scan → private Cloudinary upload → download/hash → cleanup bằng ảnh thử. Các ghi chú bên dưới về chưa kiểm thử các adapter này là trạng thái lịch sử trước lần khắc phục 503.

**Quy tắc UI hiện hành:** [08_CUSTOMER_TO_SUPPLIER_FLOW.md](./08_CUSTOMER_TO_SUPPLIER_FLOW.md). Đăng ký tài khoản chỉ tạo Customer. Customer đăng ký Supplier trên trang riêng dùng layout Customer; chỉ sau khi được duyệt mới dùng layout Supplier. Các mô tả lựa chọn Supplier lúc đăng ký ở các bản cập nhật trước bên dưới đã được thay thế.

Ngày kiểm chứng: 2026-10-24. Đã triển khai source BE và FE. Sau khi người dùng cho phép rõ ràng, đã áp dụng migration `20261024035322_SupplierVerificationWorkflow` vào database được cấu hình của dự án; EF báo `Done` và kiểm tra lại migration history không còn Pending. Thao tác xử lý lỗi thiếu cột `users.notification_sequence`. Không commit hoặc push Git. Báo cáo này phản ánh trạng thái thực tế; checklist trong 05 vẫn là kế hoạch nghiệm thu, bao gồm các bước vận hành production chưa thực hiện.

## Chức năng đã triển khai

- Customer có requestedRole Supplier: tải tài liệu riêng tư, gửi hồ sơ, theo dõi trạng thái, xem revision/lịch sử và gửi lại sau từ chối.
- Admin: hàng đợi phân trang/tìm kiếm, xem hồ sơ và tài liệu, duyệt/từ chối/thu hồi với lý do, expectedVersion và Idempotency-Key. Hồ sơ đã gửi lưu snapshot bất biến.
- Quyền hiện tại được đọc từ DB mỗi request; JWT cũ không giữ quyền Supplier sau thu hồi. Các luồng tạo đơn, xác nhận đơn, bid và award hiện có kiểm tra lại eligibility trong transaction. Supplier bị thu hồi chỉ còn quyền thực hiện nghĩa vụ trước đó theo chính sách.
- Audit, cập nhật role/trạng thái, idempotency và outbox commit cùng transaction. Lock theo thứ tự User → SupplierProfile, kiểm tra version và ràng buộc DB chống ghi đè/xuyên hồ sơ.
- File kiểm tra dung lượng, signature, loại, ownership; quét ClamAV trước private Cloudinary upload. Download qua BE kiểm tra quyền, hash và dung lượng; không trả signed URL cho FE. File legacy được đánh dấu cần reconciliation, không coi URL cũ là file đã an toàn.
- Durable inbox/unread/read-all theo cutoff sequence, notification dedup, SignalR phát tới user sau commit. FE reconnect/catch-up REST và polling khi offline, làm mới quyền/trạng thái khi có sự kiện. Email và in-app/realtime dùng các outbox channel độc lập.
- Ant Design: form onboarding, tiến trình, tài liệu, lịch sử revision, Admin queue/detail/decision, chuông và trang thông báo. Request lỗi hoặc 409 không tạo thành công giả trên FE.

## Kiểm chứng

| Kiểm tra | Kết quả |
| --- | --- |
| BE build qua dotnet test | Thành công, không phát sinh cảnh báo build |
| Unit + PostgreSQL integration | 35/35 pass, không skip |
| Migration legacy thực thi trên PostgreSQL tạm | Pass: snapshot LegacyImport, liên kết ownership, operation ID riêng, tài liệu cần reconciliation |
| Hai Admin review đồng thời | Chỉ một quyết định commit, quyết định còn lại nhận conflict |
| Idempotency sau commit | Replay kết quả, không nhân audit/outbox |
| Query hàng đợi / SQL injection | Số query cố định tối đa 3 ở bài test; chuỗi injection không mở rộng kết quả |
| Inbox đồng thời / read-all | Không nhân bản; thông báo mới sau cutoff vẫn chưa đọc |
| EF pending model changes | Không có |
| FE production build | Pass; còn cảnh báo bundle lớn của ứng dụng |
| ESLint các source FE thay đổi/thêm mới | Pass |
| Playwright | 4/4 pass: onboarding upload+submit, phân quyền, conflict, SignalR client thực nhận sự kiện |
| ESLint toàn repository FE | Chưa pass: 159 errors, 1 warning ở các source cũ ngoài phần triển khai |

PostgreSQL integration dùng container tạm bind loopback, database test tự tạo/xóa; không kết nối database trong appsettings. Browser tests mock REST và SignalR server frames; chưa thay thế kiểm thử tích hợp end-to-end với các dịch vụ ngoài thực tế.

## Migration và chạy ứng dụng

Migration: `20261024035322_SupplierVerificationWorkflow`. Script [SUPPLIER_VERIFICATION_MIGRATION.sql](./artifacts/SUPPLIER_VERIFICATION_MIGRATION.sql) là script idempotent nâng cấp từ `20261013110052_AddGoogleLogin`, không phải script khởi tạo database trống.

1. Backup và thử trên bản sao database; rà soát MST trùng sau chuẩn hóa. Migration dừng nếu phát hiện xung đột, không tự xóa/gộp doanh nghiệp.
2. Kiểm tra migration history phù hợp; chọn đúng connection bằng secret/environment của môi trường. Áp dụng script đã review hoặc `dotnet ef database update` với Infrastructure project và WebApi startup project.
3. Cấu hình storage/ClamAV/SMTP trước khi thử upload và quyết định thật. Chạy BE rồi FE; base URL của FE phải trỏ BE tương ứng, origin phải nằm trong CORS allowlist.
4. Kiểm thử hai phiên Admin/Supplier: upload → submit → review → nhận thông báo → quyền Supplier; sau đó revoke và xác nhận JWT cũ bị chặn tạo giao dịch mới.

Không sửa `.env` hoặc `appsettings.json` chứa thông tin kết nối của người dùng.

## Cấu hình vận hành

| Biến môi trường | Ý nghĩa |
| --- | --- |
| `SupplierVerification__ClamAvHost` | Mặc định 127.0.0.1 |
| `SupplierVerification__ClamAvPort` | Mặc định 3310; scanner lỗi thì upload bị chặn |
| `SupplierVerification__AllowUnscannedDevelopmentFiles` | Chỉ cho phép bypass khi Development và giá trị true; không dùng production |
| `SupplierVerification__InstanceCount` | Số instance triển khai |
| `SupplierVerification__RedisConnection` | SignalR Redis backplane khi scale-out |
| `SupplierVerification__DataProtectionKeyDirectory` | Key ring dùng chung, được bảo vệ quyền truy cập; bắt buộc cùng Redis khi nhiều instance |
| `SMTP_HOST`, `SMTP_FROM_EMAIL` | Ưu tiên các khóa environment này; fallback sang `Smtp:Host` và `Smtp:FromEmail` trong configuration |
| `SMTP_PORT`, `SMTP_USERNAME`, `SMTP_PASSWORD`, `SMTP_USE_SSL` | Ưu tiên environment; fallback sang `Smtp:Port/Username/Password/UseSsl`; mặc định port 587, SSL bật |

Cloudinary dùng cấu hình storage hiện có; cần tài khoản hỗ trợ private authenticated raw assets. Production cần HTTPS, WebSocket/proxy phù hợp và không log query access_token tại reverse proxy. Theo dõi outbox retry/dead-letter và xử lý phát lại có kiểm soát; không đổi kết quả duyệt khi email lỗi.

## Giới hạn cần nghiệm thu môi trường

- Chưa gửi email thật, upload/download Cloudinary thật hoặc quét ClamAV thật trong lần kiểm thử này. Chưa chạy Redis nhiều instance, kiểm thử tải và diễn tập sự cố.
- File legacy cần kiểm tra/quét/chuyển storage riêng tư trước khi có thể tải; migration không làm lộ lại URL cũ.
- Rate limit HTTP chung là node-local; quota mutation quan trọng được kiểm tra bằng DB lock. Khi scale-out cần bổ sung giới hạn ở gateway theo tải vận hành.
- Các badge public cần catalog/shop API tương ứng; không tạo API catalog giả. Các màn mock khác ngoài supplier verification vẫn thuộc phạm vi chức năng riêng.
- Các ngưỡng/tài liệu bắt buộc là chính sách kỹ thuật theo plan, cần owner nghiệp vụ xác nhận trước production.

## Lệnh kiểm tra

BE: `dotnet test tests/Mooncake.EcommercePlatform.Verification.Tests`. Để chạy cả PostgreSQL tests, đặt `TEST_PG_CONNECTION_STRING` tới PostgreSQL test riêng trên loopback (có extension cần thiết, tài khoản có quyền tạo database); khi không đặt biến, các PostgreSQL tests sẽ skip.

FE: `npm ci`, `npm run build`, `npm run test:e2e`. Playwright mặc định Chrome đã cài, có thể đổi bằng `PLAYWRIGHT_CHANNEL`. Các kiểm thử browser chạy Vite ở port 5186.

## Bổ sung tích hợp UI/API — 2026-10-24

### Customer mở gian hàng từ homepage

Đã sửa CTA homepage trỏ sai route `thiet-lap-gian-hang` và đổi link footer về `nha-cung-cap/xac-minh`. Customer thông thường có thể vào trang này, đọc quy trình và xác nhận muốn trở thành nhà cung cấp. Chỉ sau thao tác xác nhận, FE gọi POST `/api/v1/suppliers/me/onboarding`, refresh `/auth/me` rồi hiển thị form tài liệu/hồ sơ. BE khóa User, cập nhật requestedRole và audit cùng transaction; giữ role Customer, không tự cấp quyền bán hàng. Gọi lặp không tạo audit lặp. Các route gian hàng/giao dịch Supplier vẫn kiểm tra capability. Khách chưa đăng nhập được giữ lại điểm đến khi đăng nhập bằng tài khoản Customer.

Kiểm tra bổ sung cho lỗi này: 3 unit tests mới bao phủ Customer opt-in không nâng quyền, Admin bị chặn, email chưa xác minh bị chặn. BE tổng 33 unit tests pass, 5 PostgreSQL tests không chạy lại. Browser regression trực tiếp click CTA homepage bằng Customer requestedRole Customer, xác nhận form opt-in → gọi onboarding → hiện nút upload và vẫn không có canCreateShop. Không cần migration mới. BE local đã rebuild và khởi động lại với endpoint mới.

Đã phát hiện popup đăng ký cũ vẫn dùng timer giả lập thay vì API. Đã thay bằng form Ant Design chọn Customer/Supplier, nhập email/phone/password → POST register → POST verify-email → đăng nhập. POST resend-verification dùng email, không dùng các endpoint SMS OTP không tồn tại. Không tạo thành công giả dù bật mock fallback. Có lối tiếp tục xác minh cho tài khoản đã đăng ký nhưng chưa xác minh; API sai mã giữ nguyên form.

Menu tài khoản CustomerHeader có lối vào duyệt Supplier khi đăng nhập Admin. Menu Supplier đưa mục xác minh thành mục riêng, khóa chức năng chưa được cấp quyền; đăng nhập Google từ trang chủ cũng điều hướng theo vai trò. REST và SignalR dùng chung nguồn cấu hình backend, kể cả khi không có .env.

BE SMTP sender đã hỗ trợ section Smtp đang có trong configuration và giữ ưu tiên legacy environment keys, không sửa file chứa credentials. Không tự gửi thư thử đến người dùng.

Kiểm tra bổ sung: FE build pass, lint các file cập nhật pass, 6/6 Playwright tests pass (5 browser tests dùng mock transport, 1 smoke test read-only gọi BE thật ở localhost:8089 để kiểm tra Swagger contract, CORS và 401 khi không đăng nhập). BE chạy lại 30 unit tests pass; 5 PostgreSQL tests skip trong lần này vì container test đã được dọn sau lần kiểm chứng 35/35 trước đó. Sau đó tách cấu hình URL chung và kiểm thử lại các luồng liên quan.

Luồng kiểm tra thủ công:

1. Đăng ký chọn Nhà cung cấp, xác minh mã email thật, đăng nhập bằng số điện thoại/mật khẩu.
2. Mở `/#nha-cung-cap/xac-minh`: GET hồ sơ/lịch sử, POST upload hai loại giấy tờ, POST apply.
3. Đăng nhập Admin, mở `/#admin/phe-duyet`: GET applications/detail/history, POST review hoặc revoke; theo dõi inbox và SignalR.

Đã khởi chạy lại BE cập nhật tại localhost:8089 để kiểm tra contract. Nếu email/storage/scanner trả lỗi, UI hiển thị lỗi thật; chưa kiểm thử gửi thư, quét và upload bằng các tài khoản dịch vụ bên ngoài. Các màn dashboard/catalog/giao dịch mock ngoài phạm vi xác minh Supplier vẫn chưa được chuyển toàn bộ sang API.
