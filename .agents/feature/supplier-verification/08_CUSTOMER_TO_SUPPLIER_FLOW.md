# Luồng UI đã chốt: Customer đăng ký Supplier

Ngày cập nhật: 2026-10-24. Quy tắc này thay thế các mô tả cũ về lựa chọn Supplier lúc tạo tài khoản và layout Supplier dành cho người đang chờ duyệt.

## Một luồng đăng ký

1. Tạo tài khoản: chỉ Customer. UI không hiển thị lựa chọn role; service gửi Customer. BE từ chối public register với role Supplier/Admin và luôn tạo requestedRole Customer.
2. Customer đăng nhập, bấm **Đăng ký mở gian hàng** từ homepage hoặc menu tài khoản.
3. Chuyển đến trang riêng `/#dang-ky-nha-cung-cap`, dùng CustomerHeader và CustomerFooter. Không hiển thị sidebar Supplier.
4. Customer xác nhận muốn đăng ký nhà cung cấp. POST `/api/v1/suppliers/me/onboarding` chỉ đổi requestedRole; role vẫn Customer.
5. Vẫn trên layout Customer: nhập thông tin, upload giấy tờ, gửi hồ sơ; theo dõi Pending, lý do từ chối và gửi lại. Trang thông báo cũng giữ layout Customer.
6. Khi Admin duyệt, auth được refresh qua SignalR/catch-up. Chỉ khi role Supplier và capability canCreateShop đều hợp lệ, UI mới dùng ColonyAdminLayout Supplier và mở chức năng gian hàng. requestedRole Supplier không tự mở layout/quyền bán hàng.

Các URL cũ `nha-cung-cap/xac-minh` và `nha-cung-cap/ho-so` vẫn đọc được, nhưng Customer luôn được render trang đăng ký theo layout Customer. Người bị từ chối hoặc thu hồi xem/gửi lại hồ sơ trên layout Customer. Không hạ trạng thái hồ sơ cũ khi đăng ký lại.

## Kiểm chứng

- Browser regression: Customer bấm CTA homepage, xác nhận opt-in, upload/gửi hồ sơ và trạng thái Pending không có sidebar Supplier.
- SignalR approval: trước sự kiện không có sidebar Supplier; sau khi nhận quyết định và refresh quyền, layout Supplier xuất hiện.
- Rejected/Revoked và notification page vẫn có CustomerHeader, không có sidebar Supplier.
- Form tạo tài khoản không có role selector, gửi Customer; OTP lỗi không báo thành công giả.
- BE có test chỉ chấp nhận Customer khi đăng ký, giữ quyền Customer trong onboarding, từ chối Admin và email chưa xác minh.
- FE build và lint các source cập nhật pass. BE: 36 unit tests pass, 5 PostgreSQL tests không chạy lại ở lần chỉnh UI này.

Không thay schema, không cần migration mới. Không commit/push Git.
