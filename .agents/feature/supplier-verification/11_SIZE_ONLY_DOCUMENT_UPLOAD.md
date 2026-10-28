# Chính sách upload mọi loại tài liệu — chỉ kiểm tra dung lượng

Ngày: 2026-10-28. Đây là yêu cầu mới nhất của người dùng, thay thế quy định định dạng/quét nội dung tại các tài liệu thiết kế, vận hành và bản sửa PDF trước đó.

## Hành vi áp dụng

Endpoint POST /api/v1/suppliers/me/documents nhận mọi loại file, có hoặc không có phần mở rộng. Chỉ kiểm tra dung lượng file: lớn hơn 0 và không quá 10 MiB, đồng thời không vượt giới hạn byte đang cấu hình cho storage.

Không kiểm tra allowlist đuôi file, MIME do client gửi, magic bytes để chấp nhận/từ chối, cấu trúc PDF, JavaScript, mật khẩu PDF hoặc virus. PDF contract gốc có lệnh tự in được nhận nguyên bytes; không cần tạo bản đã loại JavaScript.

Các điều kiện nghiệp vụ vẫn áp dụng: quyền owner, trạng thái hồ sơ cho phép upload, loại giấy tờ nghiệp vụ, ngày hết hạn, idempotency, quota, tối đa 10 file và tổng 50 MiB/hồ sơ. Không thay đổi quyền đọc private file hoặc quy trình Customer → Supplier.

## Source

- SupplierService.UploadAsync bỏ gọi CheckSafetyAsync, dùng UploadRawFileAsync.
- SupplierVerificationHelper.ValidateContent kiểm tra byte count và chuẩn hóa tên file. Nhận diện PDF/JPEG/PNG chỉ dùng làm metadata cho nút preview, không phải điều kiện upload. Các định dạng khác lưu application/octet-stream.
- CloudinaryStorageService có đường upload private raw không dùng AllowedFileExtensions. Vẫn có giới hạn byte và bảo toàn nguyên nội dung; không sửa file cấu hình.
- VerificationDocumentGateway chỉ phục vụ đọc private có kiểm tra SHA-256 và dung lượng. Đã bỏ scanner/parser khỏi gateway và gỡ dependency PDFsharp khỏi BE.
- FE bỏ thuộc tính accept trên Upload, chỉ kiểm tra file rỗng/quá 10 MiB. Nội dung hướng dẫn ghi rõ nhận mọi loại file.
- PDF/JPEG/PNG vẫn có nút xem trước; các định dạng khác dùng nút tải xuống để mở bằng ứng dụng phù hợp.

## Kiểm thử

- 91/91 kiểm thử BE pass, không skip các bài PostgreSQL.
- HTTP multipart + SupplierService + PostgreSQL thật xác nhận Word/ZIP/EXE/đuôi lạ/không có đuôi/PDF chứa JavaScript đều 201/Ready và giữ nguyên bytes. Đúng 10 MiB được nhận; 0 byte hoặc 10 MiB + 1 byte trả 413, không gọi upload provider.
- Unit test storage xác nhận bỏ whitelist raw và vẫn dùng raw/authenticated; quá byte limit không gọi provider.
- Scanner dùng cho ảnh shop được kiểm thử riêng, không còn là dependency của upload giấy tờ Supplier.
- 19/19 browser tests verification pass: nhận nhiều loại file, từ chối size sai trước khi gửi, storage errors và preview PDF/ảnh hiện có.
- Build .NET/FE và lint phần sửa pass.
- Smoke Cloudinary thực tế pass cho DOCX, ZIP, đuôi lạ, không có đuôi và PDF có JavaScript: upload private raw → authenticated download → so sánh nguyên bytes. Tất cả file thử đã xóa; không dùng tài liệu hoặc tài khoản thật để tạo fixture.

Không migrate schema, sửa .env/appsettings, commit, push hoặc bình luận GitHub.
