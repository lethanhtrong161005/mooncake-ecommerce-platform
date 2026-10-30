# [MOBILE-RESPONSIVE] THIẾT KẾ TRẢI NGHIỆM THÔNG BÁO MOBILE-FIRST TRÊN NỀN TẢNG WEB
## Tài Liệu Thiết Kế Chi Tiết (Detail Design Document - Part 04)

---

## I. TỔNG QUAN VÀ MỤC TIÊU NGHIỆP VỤ

### 1.1. Thông tin tài liệu
- **Tên tài liệu:** Thiết Kế Trải Nghiệm Trung Tâm Thông Báo Đa Kênh Tương Thích Di Động (Mobile-First Responsive Web Notification Center)
- **Mã tính năng:** `mobile-responsive-notifications`
- **Phiên bản:** `1.0.0`
- **Trạng thái:** `Approved`

### 1.2. Mục tiêu nghiệp vụ & Nguyên tắc triển khai
Theo yêu cầu chỉ đạo: *"Chỗ nào đang yêu cầu mobile thì làm plan cho mobile, làm UI đó trên web và có responsive mobile luôn; còn mobile app native sẽ code sau"*.
Doanh nhân và quản lý xưởng bánh thường xuyên di chuyển và kiểm tra tiến độ giao dịch bánh Trung thu trên điện thoại thông minh (iOS Safari, Android Chrome). Vì vậy, giao diện Trung tâm Thông báo (Notification Center) được thiết kế theo tư duy **Mobile-First Responsive**, mang lại trải nghiệm mượt mà tương đương Native App ngay trên nền tảng Web:
1. **Khả năng tiếp cận 1-chạm (One-tap Accessibility):** Biểu tượng chuông thông báo nổi bật trên thanh điều hướng, kèm hiệu ứng nhịp đập vàng (Pulsing Gold Badge) khi có tin mới chưa đọc.
2. **Ngăn kéo thông báo phong cách Đáy trượt (Mobile Bottom Sheet / Drawer):** Trên màn hình máy tính hiển thị dạng Drawer trượt từ cạnh phải (`width: 420px`); trên màn hình di động ($< 768\text{ px}$) tự động chuyển thành Bottom Sheet trượt từ đáy màn hình lên với bo góc mềm mại (`rounded-t-3xl`), hỗ trợ vuốt xuống để đóng.
3. **Thanh cuộn bộ lọc ngang (Horizontal Chip Filters):** Giúp người dùng chuyển nhanh giữa các luồng nghiệp vụ: *Tất cả, Đơn hàng, Hợp đồng, Báo giá RFQ, Thanh toán VNPay, Hệ thống*.
4. **Phát âm thanh chuông báo Luxury Audio Synthesis:** Sử dụng Web Audio API tích hợp sẵn (không phụ thuộc file `.mp3` ngoại vi), phát âm vang chuông vàng êm dịu khi có thông báo mới mức ưu tiên cao.

---

## II. THIẾT KẾ GIAO DIỆN & BỐ CỤC RESPONSIVE (UI/UX WIREFRAMES)

### 2.1. Bố Cục Trên Màn Hình Máy Tính (Desktop Navbar & Drawer - $\ge 1024\text{ px}$)

```
┌────────────────────────────────────────────────────────────────────────────────────────┐
│  🌕 NGUYỆT PHỐ B2B    [Tìm kiếm sản phẩm...]    🛒 Giỏ hàng    🔔 (3)    👤 Doanh Nghiệp Hoa Mai │
└───────────────────────────────────────────────────────────────────┬────────────────────┘
                                                                    │ NGĂN KÉO THÔNG BÁO │
                                                                    │ ────────────────── │
                                                                    │ [Tất cả] [Đơn] [HĐ] [Tiền]│
                                                                    │ ────────────────── │
                                                                    │ 🌕 Cọc M1 HĐ CTR-012 đã pay│
                                                                    │    VNPay Sandbox: 120 tr ₫  │
                                                                    │    10 phút trước  [Chi tiết]│
                                                                    │ ────────────────── │
                                                                    │ 📦 Đã giao hàng & có PoD   │
                                                                    │    Xưởng Kinh Đô đã giao 500│
                                                                    │    1 giờ trước    [Xem ảnh] │
                                                                    │ ────────────────── │
                                                                    │ ⚙️ Cài đặt  |  ✓ Đọc tất cả│
                                                                    └────────────────────┘
```

### 2.2. Bố Cục Trên Màn Hình Di Động (Mobile Responsive View - $< 768\text{ px}$)

```
┌────────────────────────────────────────┐
│ ☰  🌕 NGUYỆT PHỐ             🔔 [3] 👤 │
├────────────────────────────────────────┤
│                                        │
│  [▲ Kéo lên để mở Trung tâm thông báo] │
│ ┌────────────────────────────────────┐ │
│ │ ─── (Thanh gạt vuốt đóng) ───      │ │
│ │ 🔔 Thông báo mới (3 chưa đọc)  ✓Đọc│ │
│ │ ────────────────────────────────── │ │
│ │ [Tất cả] [Thanh toán] [Hợp đồng]   │ │
│ │ ────────────────────────────────── │ │
│ │ 💳 [VNPay] Thanh toán cọc M1       │ │
│ │    120.000.000 ₫ • Hợp đồng CTR-012│ │
│ │    10:15 • Chưa đọc  [Xem ngay →]  │ │
│ │ ────────────────────────────────── │ │
│ │ 🚚 [PoD] Đã giao hàng thành công   │ │
│ │    Xác minh ảnh bàn giao đợt 1     │ │
│ │    09:30 • Đã đọc    [Xem ảnh →]   │ │
│ └────────────────────────────────────┘ │
├────────────────────────────────────────┤
│ 🏠 Trang chủ  📦 Đơn hàng  🔔 [3]  👤 Tôi│  <-- Mobile Bottom Navigation
└────────────────────────────────────────┘
```

---

## III. MA TRẬN MÀU SẮC & THÀNH PHẦN GIAO DIỆN LUXURY (DESIGN SYSTEM)

Tuân thủ bảng màu sang trọng theo quy định tại `.agents/AGENTS.md`:

| Thành phần | Mã màu Tailwind CSS | Giá trị HEX | Ý nghĩa sử dụng |
| :--- | :--- | :--- | :--- |
| **Nền chính Drawer** | `bg-stone-900` / `bg-stone-950` | `#1C1917` / `#0C0A09` | Nền trầm tạo chiều sâu quý phái |
| **Màu viền & Ngăn cách** | `border-amber-500/20` | `rgba(217, 119, 6, 0.2)` | Ánh vàng kim mỏng nhẹ |
| **Badge Chuông Chưa Đọc** | `bg-gradient-to-r from-amber-500 to-yellow-400` | `#F59E0B` | Điểm nhấn thị giác thu hút sự chú ý |
| **Thẻ Chưa Đọc (Unread)** | `bg-amber-500/10 hover:bg-amber-500/15` | `rgba(245, 158, 11, 0.1)` | Phân biệt rõ rệt tin mới |
| **Thẻ Đã Đọc (Read)** | `bg-stone-800/50 hover:bg-stone-800/80` | `rgba(41, 37, 36, 0.5)` | Làm dịu màu cho các tin đã xem |
| **Icon Thanh Toán VNPay** | `bg-emerald-500/20 text-emerald-400` | `#10B981` | Biểu tượng an toàn, thanh toán thành công |
| **Icon Cảnh Báo Vi Phạm** | `bg-rose-500/20 text-rose-400` | `#F43F5E` | Cảnh báo quá hạn hợp đồng |

---

## IV. TƯƠNG TÁC CẢM ỨNG & HIỆU ỨNG ÂM THANH (GESTURES & SOUND)

### 4.1. Hiệu Ứng Âm Thanh Chuông Báo Bằng Web Audio API (Zero External Dependency)
Để đảm bảo thông báo luôn phát được âm thanh mà không bao giờ bị lỗi thiếu file tĩnh (404 Not Found), hệ thống tạo dao động âm thanh vàng nguyên bản:
```javascript
// Trích đoạn logic âm thanh trong useNotifications.js
export const playLuxuryChime = () => {
  try {
    const AudioContext = window.AudioContext || window.webkitAudioContext;
    if (!AudioContext) return;
    const ctx = new AudioContext();
    
    // Tần số nốt F5 (698.46 Hz) hòa âm nốt C6 (1046.50 Hz) tựa tiếng chuông chùa vàng
    const osc1 = ctx.createOscillator();
    const osc2 = ctx.createOscillator();
    const gain = ctx.createGain();

    osc1.type = 'sine';
    osc1.frequency.setValueAtTime(698.46, ctx.currentTime);
    osc2.type = 'triangle';
    osc2.frequency.setValueAtTime(1046.50, ctx.currentTime);

    gain.gain.setValueAtTime(0.15, ctx.currentTime);
    gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 1.2);

    osc1.connect(gain);
    osc2.connect(gain);
    gain.connect(ctx.destination);

    osc1.start();
    osc2.start();
    osc1.stop(ctx.currentTime + 1.2);
    osc2.stop(ctx.currentTime + 1.2);
  } catch {
    // Không chặn luồng giao diện nếu trình duyệt cấm autoplay
  }
};
```

### 4.2. Thao Tác Vuốt Cảm Ứng Di Động (Touch Gestures)
- **Vuốt ngang sang phải (Swipe Right):** Đánh dấu thông báo là "Đã đọc".
- **Vuốt ngang sang trái (Swipe Left):** Mở nút "Xóa thông báo".
- **Kéo xuống từ đỉnh Bottom Sheet (Pull down):** Thu nhỏ và đóng ngăn kéo thông báo về thanh điều hướng dưới.
- **Kích thước vùng chạm (Touch Target Size):** Mọi nút bấm, icon chuông và thanh chuyển tab đều đạt chuẩn WCAG $\ge 44 \times 44\text{ px}$, thuận tiện bấm bằng ngón tay cái khi dùng điện thoại một tay.

---

## V. CƠ CHẾ ĐIỀU HƯỚNG TRỰC TIẾP (DEEP-LINKING ROUTING)

Khi người dùng chạm vào một thông báo trên di động:
1. Hệ thống tự động gọi ngầm `markAsRead(notification.id)` để giảm số đếm badge.
2. Đóng ngăn kéo thông báo.
3. Điều hướng người dùng chính xác đến đúng màn hình và tab nghiệp vụ liên quan:
   - Thông báo thanh toán VNPay $\rightarrow$ `/#/hop-dong-tien-do?id=...` hoặc `/#/thanh-toan/ket-qua?txn=...`.
   - Thông báo bản vẽ thiết kế hộp in logo $\rightarrow$ `/#/dat-hang-thiet-ke?tab=mockup`.
   - Thông báo ảnh giao hàng PoD $\rightarrow$ `/#/giao-nhan-pod?id=...`.
   - Thông báo mời chào thầu $\rightarrow$ `/#/rfq-dau-thau?tab=invitations`.
4. Trên di động, trang đích tự động cuộn đến phần tử cần chú ý và làm nổi bật (Highlight pulse) trong 2 giây.

