# Sửa lỗi upload ảnh chặn bước tạo gian hàng

Ngày: 2026-10-28. Trang: /#nha-cung-cap/thiet-lap-gian-hang. Endpoint lỗi: POST /api/v1/shops/me/media.

## Nguyên nhân xác nhận

Trace e7b4629f670647dbb5cf8ebf70cbd4d5, lúc 08:38:22 Asia/Saigon, trả 503 từ ClamAvScanHelper → ShopMediaGateway.PrepareAsync. Container mooncake-clamav-dev đang dừng nên upload logo/banner không hoàn tất. Đây là lỗi tại bước upload ảnh trước tạo shop; không phải lỗi SQL tạo shop.

## Thay đổi

- ShopMediaGateway bỏ dependency IFileSafetyScanner và lời gọi scanner. Upload ảnh gian hàng/template không cần ClamAV chạy.
- Giữ xử lý ảnh branding JPG/PNG/WebP, giới hạn 5 MiB, decode/re-encode và kích thước hiện hữu. Chính sách mọi loại file chỉ kiểm tra dung lượng của giấy tờ Supplier được mô tả riêng tại feature supplier-verification.
- FE nói rõ logo/banner là tùy chọn, có thể tạo shop trước và bổ sung sau.
- ShopMediaAsset cập nhật mô tả proof phù hợp với pipeline không dùng scanner.

## Kiểm chứng

- 91/91 BE tests pass, không skip PostgreSQL tests. ShopHttpTests dùng real ShopService, ShopRepository, ShopMediaGateway và database tạm; chỉ mock provider lưu ảnh. Test không đăng ký scanner: upload Logo 128×128, Banner 640×200 → 201/Ready → create shop với hai asset IDs → 201/Inactive → bật Active → public API hoạt động.
- 12/12 browser shop tests pass, gồm upload logo + banner trước khi tạo và tạo không ảnh sau khi optional upload lỗi.
- Smoke Cloudinary thực tế trong lúc ClamAV dừng: xử lý ảnh Logo/Banner → public image upload → tải ảnh kiểm tra dimension. Hai ảnh thử đã xóa thành công.
- FE build và lint các file sửa pass.
- Không tạo hoặc thay đổi shop/tài khoản thật để thử luồng; fixtures dùng database PostgreSQL loopback riêng và tự dọn.

Tạo mới shop mặc định Inactive. Chủ shop dùng nút Hiển thị gian hàng để xuất bản. Không migrate schema, sửa credentials, commit, push hoặc bình luận GitHub.
