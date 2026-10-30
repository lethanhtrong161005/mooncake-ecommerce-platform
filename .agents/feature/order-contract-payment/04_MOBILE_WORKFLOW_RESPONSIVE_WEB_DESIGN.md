# [M10-F25.4] THIẾT KẾ GIAO DIỆN MOBILE & WEB RESPONSIVE: THANH TOÁN TIỆN LỢI

---

## I. TỔNG QUAN THIẾT KẾ MOBILE & WEB RESPONSIVE

### 1.1. Mục tiêu & Nguyên tắc chuyển đổi
Theo định hướng kiến trúc của dự án: **Toàn bộ trải nghiệm di động (Mobile App Flow) được thiết kế chuẩn mực theo Human Interface Guidelines / Material Design 3, và được hiện thực trực tiếp trên nền tảng Web Frontend (`mooncake-fe`) dưới dạng giao diện Web Responsive hoàn hảo**. Native mobile app (React Native / Flutter) sẽ kế thừa 100% tài liệu thiết kế này ở giai đoạn tiếp theo.

### 1.2. Các điểm chạm người dùng di động (Mobile Touchpoints)
1. **Khách hàng thanh toán trên điện thoại (Mobile Checkout):**
   - Mở màn hình thanh toán dạng **Bottom Sheet (Trượt từ dưới lên)** thay vì Modal desktop cồng kềnh.
   - Thao tác 1 chạm (1-tap interaction): Sao chép số tài khoản, số tiền, nội dung chuyển khoản nhanh chóng.
   - Mở trực tiếp App ngân hàng hoặc chuyển hướng cổng VNPay Sandbox tối ưu giao diện di động.
2. **Biên lai thanh toán điện tử trên di động (Mobile E-Receipt):**
   - Thẻ hóa đơn thanh toán sang trọng (Luxury Gold aesthetic), hiển thị rõ ràng mã giao dịch, thời gian, phương thức, trạng thái Escrow.
   - Hỗ trợ tải ảnh biên lai hoặc in ấn di động.

---

## II. WIREFRAME & GIAO DIỆN RESPONSIVE MOBILE

### 2.1. Mobile Bottom Sheet: Lựa Chọn Phương Thức Thanh Toán

```text
┌──────────────────────────────────────────────┐
│  ══════════════ [Gạt kéo xuống] ═══════════  │
│  Thanh Toán Đơn Hàng / Mốc Đặt Cọc M1         │
│  Số tiền: 45.000.000 ₫                        │
├──────────────────────────────────────────────┤
│  Chọn phương thức thanh toán an toàn:        │
│                                              │
│  ┌────────────────────────────────────────┐  │
│  │ (●) Cổng VNPay Sandbox (Khuyên dùng)   │  │
│  │     Quét QR, Thẻ ATM nội địa, Visa/JCB │  │
│  │     ⚡ Xử lý tức thì & Kích hoạt HĐ ngay│ │
│  └────────────────────────────────────────┘  │
│                                              │
│  ┌────────────────────────────────────────┐  │
│  │ ( ) Chuyển khoản Doanh nghiệp (VietQR) │  │
│  │     Tài khoản ký quỹ Escrow an toàn    │  │
│  │     📸 Đính kèm ảnh Ủy nhiệm chi (UNC) │  │
│  └────────────────────────────────────────┘  │
│                                              │
│  ┌────────────────────────────────────────┐  │
│  │ ( ) Thanh toán khi nhận hàng (COD)     │  │
│  │     ⚠️ Chỉ áp dụng đơn hàng ≤ 10tr     │  │
│  └────────────────────────────────────────┘  │
│                                              │
│  ┌────────────────────────────────────────┐  │
│  │ [ NÚT THANH TOÁN NGAY - 45.000.000 ₫ ] │  │
│  └────────────────────────────────────────┘  │
│  🔒 Được bảo hộ bởi Ký quỹ SafeTrade Nguyệt Phố│
└──────────────────────────────────────────────┘
```

### 2.2. Mobile VietQR Display View (Chuyển Khoản Nhanh 1-Chạm)

```text
┌──────────────────────────────────────────────┐
│  < Quay lại            Chuyển khoản VietQR   │
├──────────────────────────────────────────────┤
│               [ MÃ QR NAPAS 247 ]            │
│               ┌────────────────┐             │
│               │   ██████████   │             │
│               │   ██  ██  ██   │             │
│               │   ██████████   │             │
│               └────────────────┘             │
│      Quét bằng App Ngân hàng bất kỳ          │
├──────────────────────────────────────────────┤
│  Ngân hàng: Ngân hàng Quân Đội (MB Bank)     │
│                                              │
│  Số tài khoản: 0988668899                    │
│  [ Sao chép STK ] 📋                         │
│                                              │
│  Số tiền: 45.000.000 ₫                       │
│  [ Sao chép số tiền ] 📋                     │
│                                              │
│  Nội dung CK: PAY PAY-20261029-C81B92D1      │
│  [ Sao chép nội dung ] 📋                    │
├──────────────────────────────────────────────┤
│  Đính kèm chứng từ chuyển khoản (UNC):       │
│  [ + Tải ảnh hóa đơn / File PDF UNC ]        │
│                                              │
│  [ NÚT: TÔI ĐÃ CHUYỂN KHOẢN XONG ]           │
└──────────────────────────────────────────────┘
```

### 2.3. Mobile E-Receipt & Callback View (Kết Quả Thanh Toán)

```text
┌──────────────────────────────────────────────┐
│                                              │
│                   ✅                         │
│       THANH TOÁN THÀNH CÔNG!                 │
│      Hợp đồng HD-2026-TEST01 đã kích hoạt    │
│                                              │
│  ┌────────────────────────────────────────┐  │
│  │ CHI TIẾT BIÊN LAI GIAO DỊCH             │  │
│  │                                        │  │
│  │ Mã giao dịch: PAY-20261029-C81B92D1    │  │
│  │ Mã tham chiếu VNPay: 14567890          │  │
│  │ Thời gian: 29/10/2026 15:45:12         │  │
│  │ Phương thức: VNPay Sandbox (NCB)       │  │
│  │ Số tiền: 45.000.000 ₫                  │  │
│  │ Trạng thái tiền: Đã nạp Ký quỹ Escrow  │  │
│  └────────────────────────────────────────┘  │
│                                              │
│  ┌────────────────────────────────────────┐  │
│  │ [ Xem Hợp Đồng & Tiến Độ Sản Xuất ]    │  │
│  └────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────┐  │
│  │ [ In Biên Lai Điện Tử / Tải PDF ]      │  │
│  └────────────────────────────────────────┘  │
└──────────────────────────────────────────────┘
```

---

## III. QUY CHUẨN THIẾT KẾ KỸ THUẬT CHO RESPONSIVE WEB (`mooncake-fe`)

### 3.1. Điểm ngắt Responsive (Breakpoints)
- **Mobile (`< 640px`):**
  - Mở dạng `Drawer` hoặc `BottomSheet` từ đáy màn hình với thuộc tính `placement="bottom"`, chiều cao tự co giãn `height="auto"`, bo góc trên `rounded-t-3xl`.
  - Nút bấm chính phủ kín chiều ngang (`w-full`), chiều cao tối thiểu `h-12` (48px) cho cảm giác chạm thoải mái.
- **Tablet / Desktop (`>= 640px`):**
  - Mở dạng `Modal` canh giữa màn hình `width={540}` sang trọng, viền vàng kim `border-amber-300`, nền `bg-stone-50`.

### 3.2. Tương tác 1-Chạm (1-Tap Copy UX)
- Khi người dùng bấm nút sao chép:
  - Sử dụng API `navigator.clipboard.writeText(text)`.
  - Hiển thị phản hồi `message.success("Đã sao chép nội dung chuyển khoản!")`.
  - Hiệu ứng đổi icon sang dấu check xanh lá trong 1.5 giây.

### 3.3. Xử lý Chuyển hướng VNPay trên Thiết bị di động
- Khi bấm "Thanh toán ngay":
  - Tránh mở tab mới trên Safari/Chrome iOS (`target="_blank"` thường bị trình duyệt chặn pop-up).
  - Sử dụng lệnh `window.location.assign(paymentUrl)` để chuyển hướng êm ái cùng cửa sổ.
  - Khi hoàn tất, VNPay chuyển hướng lại đúng hash route `/#/thanh-toan/ket-qua` mà không làm mất phiên đăng nhập `localStorage`.

