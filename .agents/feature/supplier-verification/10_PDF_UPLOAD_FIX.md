# Sửa lỗi upload PDF bị chặn nhầm

> Lịch sử bản sửa. Đã được thay thế theo yêu cầu mới: [chỉ kiểm tra dung lượng, nhận mọi loại file](./11_SIZE_ONLY_DOCUMENT_UPLOAD.md).

Ngày: 2026-10-28. Endpoint: POST /api/v1/suppliers/me/documents.

## Nguyên nhân đã xác nhận

Trace `7ee6fc650b544d71a9d5baf657834aef` trong log local trỏ tới `SupplierVerificationHelper.ValidateContent`, nhánh kiểm tra PDF ở dòng 134 của binary trước sửa. Lỗi xảy ra trước scanner/storage, không phải lỗi multipart FE hoặc Cloudinary.

Code cũ chuyển toàn bộ bytes PDF sang Latin1 rồi dùng Contains với /JavaScript, /JS, /Launch, /EmbeddedFile, /OpenAction. Cách này có thể chặn cả OpenAction hợp lệ dùng để đặt trang/zoom, tên tài nguyên bắt đầu bằng /JS, văn bản, comment hoặc dữ liệu nhị phân tình cờ trùng chuỗi. Chưa có file gốc của người dùng để kết luận token cụ thể trong file đó.

## Thay đổi

- Application giữ kiểm tra dung lượng, tên/đuôi và chữ ký định dạng; bỏ việc dò từ khóa trong toàn bộ bytes.
- Infrastructure thêm VerificationPdfSafetyHelper dùng [PDFsharp 6.2.4](https://www.nuget.org/packages/PDFsharp/6.2.4), giấy phép MIT; không đưa parser vào Application.
- Gateway tiếp tục quét ClamAV rồi kiểm tra dictionary/array/reference đã parse, gồm escaped names và reference gián tiếp. Dùng queue/visited set, giới hạn số item, tối đa hai PDF được inspect đồng thời.
- Cho phép OpenAction là destination array/named destination hoặc GoTo nội bộ. Chặn script, launch, tự mở tài nguyên ngoài, embedded files, XFA và các action không an toàn được quy định trong helper.
- Không rewrite file pháp lý: bytes upload, SHA-256 và chữ ký số được giữ nguyên.
- Tách error code và nội dung FE cho từng nguyên nhân; ảnh shop có mapping cho mã malware của scanner dùng chung.

| Error code | Ý nghĩa |
| --- | --- |
| DOCUMENT_TYPE_UNSUPPORTED | Định dạng ngoài PDF/JPEG/PNG hoặc nội dung không khớp đuôi file |
| DOCUMENT_PDF_INVALID | Không đọc được cấu trúc hoặc vượt giới hạn kiểm tra |
| DOCUMENT_PASSWORD_PROTECTED | PDF yêu cầu mật khẩu |
| DOCUMENT_PDF_UNSAFE | Script/action/attachment không phù hợp với giấy tờ pháp lý |
| DOCUMENT_MALWARE_DETECTED | ClamAV trả verdict FOUND |
| DOCUMENT_SCANNER_UNAVAILABLE | Scanner chưa sẵn sàng; không nhận file chưa quét |

ClamAV local được phát hiện đang dừng và đã khởi động lại container mooncake-clamav-dev, health trở về healthy. Không bật bypass scanner trong cấu hình ứng dụng. Không đổi .env, appsettings, schema hoặc quyền tài khoản.

## Kiểm chứng

- 97/97 BE tests pass, không skip PostgreSQL tests. HTTP multipart test chạy SupplierService + repository + gateway PDF thật trên database tạm: PDF local OpenAction trả 201/Ready, storage nhận đúng nguyên bytes; PDF JavaScript trả 415/DOCUMENT_PDF_UNSAFE, row Quarantined và không gọi upload provider.
- Regression bao phủ destination array, zoom XYZ, GoTo, indirect reference, named destination, văn bản/comment/tên tài nguyên; chặn JavaScript/Launch/URI tự động, escaped names, embedded files, PDF hỏng và có mật khẩu.
- Scanner tests giữ kiểm tra fail-closed và FOUND.
- 14/14 browser tests verification pass, gồm ba test lỗi PDF mới và không success giả.
- FE build/lint phần sửa pass; .NET build không có lỗi hoặc warning sau sửa nullable.
- Smoke thực tế: tạo PDF có local OpenAction → ClamAV thật → structural inspection → private Cloudinary upload → authenticated read → so sánh nguyên bytes. File thử đã xóa thành công.

Fixture chỉ dùng PostgreSQL loopback riêng, tạo/drop database test; không tạo hồ sơ, gửi thông báo hoặc sửa tài liệu tài khoản thật để thử lỗi này. API local được chạy lại bằng binary đã sửa.

Không commit, push hoặc bình luận GitHub.
