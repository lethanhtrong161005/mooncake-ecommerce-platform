# [MOBILE-RESPONSIVE] THIẾT KẾ TRẢI NGHIỆM MOBILE-FIRST CHO VẬN HÀNH & THỐNG KÊ TRÊN NỀN TẢNG WEB
## Tài Liệu Thiết Kế Chi Tiết (Detail Design Document - Part 04)

---

## I. TỔNG QUAN VÀ NGUYÊN TẮC THIẾT KẾ MOBILE-FIRST

### 1.1. Bối cảnh & Chỉ đạo chiến lược
Theo chỉ đạo nghiệp vụ: *"Chỗ nào đang yêu cầu mobile thì làm plan cho mobile, làm UI đó trên web và có responsive mobile luôn; còn mobile app native sẽ code sau"*.
Trong mùa cao điểm Trung thu, các chủ doanh nghiệp mua quà tặng, chủ xưởng sản xuất bánh và ban quản trị sàn thường xuyên phải di chuyển, thị sát xưởng hoặc tham gia các sự kiện khách hàng. Việc theo dõi thông báo khẩn cấp, duyệt hồ sơ tranh chấp, và kiểm tra biến động doanh số GMV trên điện thoại thông minh (Smartphones) là nhu cầu sống còn.

### 1.2. 4 Trụ Cột Trải Nghiệm Mobile Trên Nền Tảng Web
1. **Thiết kế thích ứng tự nhiên (Adaptive Responsive Breakpoints):**
   - Màn hình di động ($< 768\text{ px}$): Tự động chuyển đổi toàn bộ Drawer bên phải thành **Bottom Sheet** trượt từ đáy màn hình lên, bảng dữ liệu chuyển thành dạng thẻ tóm tắt (Card-based layout), các nút bấm đạt chuẩn chạm $\ge 44 \times 44\text{ px}$.
   - Màn hình máy tính bảng ($768\text{ px} - 1024\text{ px}$): Bố cục lưới 2 cột, bảng dữ liệu hỗ trợ thanh cuộn ngang mượt mà.
   - Màn hình máy tính ($\ge 1024\text{ px}$): Giao diện đa cột tiêu chuẩn máy tính để bàn.
2. **Thao tác 1-chạm & Cảm ứng vuốt (Touch Gestures):** Hỗ trợ vuốt xuống để đóng Bottom Sheet thông báo, vuốt ngang để đánh dấu đã đọc hoặc giải quyết nhanh tranh chấp.
3. **Âm thanh chuông báo nguyên bản (Native Web Audio API):** Phát tiếng chuông vàng ngân vang khi có thông báo mới mà không cần nạp file `.mp3` dung lượng nặng.
4. **Hiển thị biểu đồ phân tích trực quan co giãn (Responsive Charts):** Phễu chuyển đổi RFQ và biểu đồ doanh số tự động điều chỉnh tỷ lệ khung nhìn, dễ đọc trên màn hình điện thoại từ 4.7 inch đến 6.7 inch.

---

## II. WIREFRAME GIAO DIỆN DI ĐỘNG (MOBILE RESPONSIVE WIREFRAMES)

### 2.1. Trung Tâm Thông Báo Di Động (Mobile Bottom Sheet - $< 768\text{ px}$)

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

### 2.2. Giao Diện Xử Lý Tranh Chấp Di Động Dành Cho Admin (Mobile Dispute Arbitration Card)

```
┌────────────────────────────────────────┐
│ ←  ⚖️ PHÂN XỬ TRANH CHẤP HỢP ĐỒNG      │
├────────────────────────────────────────┤
│ Hợp đồng: CTR-2026-00012               │
│ Bên mua: Doanh Nghiệp Hoa Mai          │
│ Bên bán: Xưởng Bánh Tràng An Nghệ Nhân │
│ Số dư Ký Quỹ Escrow: 120.000.000 ₫     │
├────────────────────────────────────────┤
│ ⚠️ Lý do khiếu nại:                     │
│ "Xưởng giao chậm 7 ngày so với cam kết │
│ mốc M2, ảnh PoD xác nhận trễ hạn."     │
│ 📎 Bằng chứng: [Ảnh PoD] [Biên bản]    │
├────────────────────────────────────────┤
│ ⚖️ Phán quyết trọng tài Admin:         │
│ (•) Lỗi thuộc về Nhà Cung Cấp          │
│ ( ) Lỗi thuộc về Khách Hàng            │
│ ( ) Thỏa thuận hai bên hỗ trợ          │
│                                        │
│ 💰 Số tiền hoàn lại bên mua:           │
│ [ 120.000.000 ₫ (Hoàn cọc 100%)      ] │
│                                        │
│ 📉 Chế tài uy tín: Trừ 20 điểm uy tín  │
├────────────────────────────────────────┤
│ [   XÁC NHẬN PHÁN QUYẾT & HOÀN CỌC   ] │  <-- Nút bấm to 48px chạm an toàn
└────────────────────────────────────────┘
```

### 2.3. Biểu Đồ Phễu Chuyển Đổi RFQ Di Động (Mobile Vertical Step Funnel)
Trên màn hình di động, phễu chuyển đổi ngang được tự động render thành dạng **Dòng thời gian dọc (Vertical Timeline Steps)** giúp xem rõ tỷ lệ mà không bị vỡ chữ:
```
┌────────────────────────────────────────┐
│ 📊 PHỄU CHUYỂN ĐỔI RFQ (THÁNG NÀY)     │
├────────────────────────────────────────┤
│ ① RFQ Đăng Tải: 150 yêu cầu (100%)    │
│    │                                   │
│    ▼ (Tỷ lệ chào thầu: 2.8 bid/RFQ)    │
│ ② Báo Giá Nộp: 420 hồ sơ               │
│    │                                   │
│    ▼ (Tỷ lệ trúng thầu: 88%)           │
│ ③ Báo Giá Trúng: 132 báo giá           │
│    │                                   │
│    ▼ (Tỷ lệ ký OTP: 85.3%)             │
│ ④ Hợp Đồng Ký Số: 128 hợp đồng         │
│    │                                   │
│    ▼ (Tỷ lệ hoàn thành: 76.7%)         │
│ ⑤ Nghiệm Thu Xong: 115 hợp đồng ✅     │
├────────────────────────────────────────┤
│ 🏆 Tỷ lệ thành công toàn diện: 76.7%   │
│ [ ✨ Bấm xem nhận định Gemini AI ]      │
└────────────────────────────────────────┘
```

---

## III. THIẾT KẾ KỸ THUẬT RESPONSIVE VÀ TRẢI NGHIỆM CẢM ỨNG (TECHNICAL IMPLEMENTATION)

### 3.1. Hook Nhận Diện Kích Thước Màn Hình: `useMobileLayout.js`
```javascript
// src/hooks/useMobileLayout.js
import { useState, useEffect } from 'react';

export const useMobileLayout = (breakpoint = 768) => {
  const [isMobile, setIsMobile] = useState(
    typeof window !== 'undefined' ? window.innerWidth < breakpoint : false
  );

  useEffect(() => {
    const handleResize = () => {
      setIsMobile(window.innerWidth < breakpoint);
    };

    window.addEventListener('resize', handleResize);
    return () => window.removeEventListener('resize', handleResize);
  }, [breakpoint]);

  return { isMobile };
};
```

### 3.2. Chuyển Đổi Drawer Thành Bottom Sheet Trong React 19
```jsx
// Sử dụng Ant Design Drawer với cấu hình động theo kích thước màn hình
<Drawer
  title={
    <div className="flex items-center justify-between text-amber-500 font-bold">
      <span>🔔 Trung Tâm Thông Báo Hoàng Gia</span>
      {unreadCount > 0 && (
        <Tag color="gold" className="rounded-full px-2 py-0.5 text-xs">
          {unreadCount} chưa đọc
        </Tag>
      )}
    </div>
  }
  placement={isMobile ? 'bottom' : 'right'}
  height={isMobile ? '88vh' : undefined}
  width={isMobile ? '100%' : 420}
  open={drawerOpen}
  onClose={() => setDrawerOpen(false)}
  styles={{
    wrapper: isMobile ? { borderRadius: '24px 24px 0 0', overflow: 'hidden' } : {},
    body: { padding: '12px 16px', backgroundColor: '#1C1917' },
  }}
>
  {/* Nội dung danh sách thông báo */}
</Drawer>
```

---

## IV. BẢNG MÃ MÀU VÀ TIÊU CHUẨN TIẾP CẬN DI ĐỘNG (ACCESSIBILITY STANDARDS)

1. **Vùng chạm tối thiểu (Touch Target Minimum):** Mọi nút bấm, tab chuyển đổi, và checkbox đều có vùng đệm `padding` đảm bảo kích thước chạm thực tế tối thiểu $44 \times 44\text{ px}$.
2. **Độ tương phản màu sắc (Contrast Ratio WCAG AAA):**
   - Văn bản vàng hổ phách `#F59E0B` trên nền đá trầm `#1C1917` đạt tỷ lệ tương phản $> 7:1$, giúp người dùng đọc rõ ràng ngay cả dưới trời nắng chói khi đi thị sát xưởng bánh.
3. **Phản hồi rung cảm ứng (Haptic Feedback):**
   - Khi Admin bấm xác nhận hoàn tiền cọc hoặc ký phân xử tranh chấp, gọi Web Vibration API: `navigator.vibrate?.([40, 60, 40])` để người dùng cảm nhận được hành vi xác thực an toàn.

