# Khắc phục upload tài liệu 503

> Hướng dẫn lịch sử. Upload giấy tờ Supplier hiện nhận mọi loại file và không dùng scanner; xem [chính sách mới](./11_SIZE_ONLY_DOCUMENT_UPLOAD.md).

Ngày kiểm tra: 2026-10-24.

## Nguyên nhân đã xác nhận

Request `/api/v1/suppliers/me/documents`, trace `3b8a3e035a0a481bb1487f93d8cbfaa7`, trả DOCUMENT_STORAGE_UNAVAILABLE. ClamAV tại 127.0.0.1:3310 chưa chạy. Upload thực hiện scan trước Cloudinary, nhưng catch chung trước đây chuyển lỗi scanner thành lỗi storage nên không phân biệt được nguyên nhân.

Đã bổ sung service `clamav` vào `docker/dev/docker-compose.dev.yml`, dùng image chính thức `clamav/clamav:stable`, bind TCP chỉ trên loopback và lưu signature database bằng volume. Đã chạy service, kiểm tra trạng thái healthy. Không bật chế độ bỏ qua scanner.

## Vận hành local

Docker Desktop cần chạy. Tại thư mục BE:

```powershell
docker compose -f docker/dev/docker-compose.dev.yml up -d clamav
docker inspect --format '{{.State.Health.Status}}' mooncake-clamav-dev
docker logs --tail 20 mooncake-clamav-dev
```

Chờ healthy trước khi upload, đặc biệt lần đầu tải signature database. BE chạy trên host dùng mặc định 127.0.0.1:3310. Nếu đóng gói BE trong container, cấu hình ClamAvHost theo tên service trên Docker network, không dùng localhost bên trong container BE.

Tham khảo image/healthcheck: [ClamAV Docker documentation](https://docs.clamav.net/manual/Installing/Docker.html).

## Source đã sửa

- Gateway phân biệt socket/timeout/scanner ERROR bằng HTTP 503 `DOCUMENT_SCANNER_UNAVAILABLE`; kết quả FOUND vẫn bị chặn 415. Không coi scanner lỗi là file sạch.
- SupplierService giữ mã lỗi scanner khi lưu metadata Failed, thay vì chuyển tất cả sang storage unavailable.
- Log scanner chỉ ghi loại exception, không ghi nội dung tài liệu, credentials hoặc URL storage.
- FE hiển thị thông báo scanner riêng và reload metadata khi upload lỗi, để người dùng thấy và gỡ bản Failed. Sau khi dịch vụ hoạt động, tải lại file; file Failed không được dùng để gửi hồ sơ.

## Kiểm chứng thực tế

Đã dùng ảnh PNG 1 pixel do công cụ tạo, không dùng tài liệu của người dùng:

1. ClamAV TCP INSTREAM scan: PASS.
2. Cloudinary authenticated raw upload qua CloudinaryStorageService thực tế: PASS.
3. Private download qua VerificationDocumentGateway, kiểm tra độ dài/SHA256: PASS.
4. Đã thực hiện xóa asset thử sau kiểm tra.

Không thay credentials, không tạo hồ sơ/submission thử trong database dự án. ClamAV được giữ chạy để phục vụ upload của ứng dụng.

BE: 40 tests pass, 5 PostgreSQL tests không chạy lại. Bốn scanner regression cases mới kiểm tra OK, FOUND, ERROR và socket unavailable qua TCP loopback. FE build/lint phần sửa pass; có thêm browser regression kiểm tra upload 503 hiện đúng lỗi, reload bản Failed, cho phép gỡ và không mở nút gửi hồ sơ.
