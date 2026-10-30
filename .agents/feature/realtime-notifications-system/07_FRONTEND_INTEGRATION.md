# [FRONTEND-REACT] TÍCH HỢP GIAO DIỆN REACT 19, SIGNALR CLIENT & TRUNG TÂM THÔNG BÁO HOÀNG GIA
## Tài Liệu Thiết Kế Chi Tiết (Detail Design Document - Part 07)

---

## I. TỔNG QUAN VÀ MỤC TIÊU TRIỂN KHAI FRONTEND

Tầng giao diện người dùng của **Module 11 (Features #25 & #26)** được triển khai trong ứng dụng `mooncake-fe` (React 19, Vite, Ant Design v6, Tailwind CSS). Toàn bộ mã nguồn tuân thủ nghiêm ngặt các quy tắc tại `.agents/AGENTS.md`:
- **Indentation:** Chuẩn 2 spaces cho tất cả các file (`.js`, `.jsx`, `.css`, `.json`). Không dùng ký tự Tab.
- **Cú pháp:** Dấu chấm phẩy `;` cuối câu lệnh, nháy đơn `'...'` cho chuỗi thông thường, nháy kép `"..."` cho thuộc tính JSX.
- **Phong cách Luxury:** Tone màu chủ đạo Hoàng Gia Á Đông (Dark Stone `#1C1917`, Amber Gold `#D97706` / `#F59E0B`, Cream Silk `#FEF3C7`).
- **Khả năng tự vận hành độc lập (Mock Fallback Pattern):** Khi cờ môi trường `VITE_ENABLE_MOCK_FALLBACK=true`, hệ thống tự sinh mock notifications và mock SignalR streaming để kiểm thử giao diện trơn tru ngay cả khi Backend chưa bật.

---

## II. BỔ SUNG CONSTANTS & SERVICE LAYER

### 2.1. Cập Nhật `src/constants/apiEndpoints.js`
Khai báo đầy đủ các endpoints giao tiếp máy chủ:
```javascript
// src/constants/apiEndpoints.js
export const NOTIFICATION_ENDPOINTS = {
  LIST: '/api/v1/notifications',
  UNREAD_COUNT: '/api/v1/notifications/unread-count',
  MARK_READ: (id) => `/api/v1/notifications/${id}/read`,
  MARK_ALL_READ: '/api/v1/notifications/read-all',
  DELETE: (id) => `/api/v1/notifications/${id}`,
  PREFERENCES: '/api/v1/notifications/preferences',
  HUB_URL: '/api/v1/hubs/notifications',
};
```

### 2.2. Xây Dựng `src/services/signalrService.js` – Quản Lý Kết Nối WebSocket Tập Trung
Module singleton quản lý vòng đời kết nối SignalR:
```javascript
import * as signalR from '@microsoft/signalr';
import { NOTIFICATION_ENDPOINTS } from '../constants/apiEndpoints.js';

class SignalRManager {
  constructor() {
    this.connection = null;
    this.isConnecting = false;
    this.listeners = new Map();
  }

  async startConnection(token) {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      return this.connection;
    }
    if (this.isConnecting) return null;

    try {
      this.isConnecting = true;
      const baseURL = import.meta.env.VITE_API_URL || 'http://localhost:8089';
      const hubUrl = `${baseURL}${NOTIFICATION_ENDPOINTS.HUB_URL}`;

      this.connection = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl, {
          accessTokenFactory: () => token || localStorage.getItem('token') || '',
          skipNegotiation: false,
          transport: signalR.HttpTransportType.WebSockets,
        })
        .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
        .configureLogging(signalR.LogLevel.Warning)
        .build();

      // Đăng ký lại các listeners khi tái kết nối
      this.listeners.forEach((callbacks, eventName) => {
        callbacks.forEach((cb) => this.connection.on(eventName, cb));
      });

      await this.connection.start();
      return this.connection;
    } catch {
      return null;
    } finally {
      this.isConnecting = false;
    }
  }

  on(eventName, callback) {
    if (!this.listeners.has(eventName)) {
      this.listeners.set(eventName, new Set());
    }
    this.listeners.get(eventName).add(callback);

    if (this.connection) {
      this.connection.on(eventName, callback);
    }
  }

  off(eventName, callback) {
    if (this.listeners.has(eventName)) {
      this.listeners.get(eventName).delete(callback);
    }
    if (this.connection) {
      this.connection.off(eventName, callback);
    }
  }

  async stopConnection() {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
    }
  }
}

export const signalRManager = new SignalRManager();
```

### 2.3. Xây Dựng `src/services/notificationService.js` – Gọi API Kèm Mock Fallback
```javascript
import apiClient from './apiClient.js';
import { NOTIFICATION_ENDPOINTS } from '../constants/apiEndpoints.js';

// Dữ liệu mẫu khởi tạo phong cách Hoàng Gia (Mock Fallback)
const MOCK_NOTIFICATIONS = [
  {
    id: 'mock-notif-1',
    type: 'payment_vnpay_success',
    category: 'payment',
    title: 'Thanh toán cọc M1 thành công qua VNPay Sandbox',
    content: 'Giao dịch PAY-2026-00045 trị giá 120.000.000 ₫ cho Hợp đồng CTR-2026-00012 đã được xác nhận an toàn tại Quỹ Escrow.',
    referenceId: 'ctr-001',
    referenceType: 'contract',
    actionUrl: '/#/hop-dong-tien-do',
    priority: 'high',
    isRead: false,
    createdAt: new Date(Date.now() - 5 * 60000).toISOString(),
  },
  {
    id: 'mock-notif-2',
    type: 'delivery_pod_uploaded',
    category: 'order',
    title: 'Đã giao hàng thành công & Có ảnh PoD',
    content: 'Xưởng Bánh Kinh Đô đã bàn giao 500 hộp bánh Trung thu đợt 1. Ảnh xác minh giao nhận thực tế (PoD) đã sẵn sàng.',
    referenceId: 'del-001',
    referenceType: 'delivery',
    actionUrl: '/#/giao-nhan-pod',
    priority: 'medium',
    isRead: false,
    createdAt: new Date(Date.now() - 45 * 60000).toISOString(),
  },
  {
    id: 'mock-notif-3',
    type: 'bid_submitted',
    category: 'rfq',
    title: 'Nhà cung cấp gửi báo giá chào thầu mới',
    content: 'Xưởng Hữu Nghị vừa nộp hồ sơ báo giá chào thầu cho yêu cầu RFQ-2026-00089. Giá chào: 85.000.000 ₫.',
    referenceId: 'rfq-001',
    referenceType: 'rfq',
    actionUrl: '/#/rfq-dau-thau',
    priority: 'medium',
    isRead: true,
    createdAt: new Date(Date.now() - 120 * 60000).toISOString(),
  },
];

export const notificationService = {
  async getNotifications(params = {}) {
    try {
      const response = await apiClient.get(NOTIFICATION_ENDPOINTS.LIST, { params });
      return response.data;
    } catch (error) {
      if (import.meta.env.VITE_ENABLE_MOCK_FALLBACK === 'true') {
        const category = params.category || 'all';
        let filtered = [...MOCK_NOTIFICATIONS];
        if (category !== 'all') {
          filtered = filtered.filter((n) => n.category === category);
        }
        return {
          status: 200,
          data: {
            items: filtered,
            totalItems: filtered.length,
            page: 1,
            pageSize: 20,
            unreadCount: filtered.filter((n) => !n.isRead).length,
          },
        };
      }
      throw error;
    }
  },

  async getUnreadCount() {
    try {
      const response = await apiClient.get(NOTIFICATION_ENDPOINTS.UNREAD_COUNT);
      return response.data;
    } catch (error) {
      if (import.meta.env.VITE_ENABLE_MOCK_FALLBACK === 'true') {
        return {
          status: 200,
          data: {
            unreadCount: MOCK_NOTIFICATIONS.filter((n) => !n.isRead).length,
          },
        };
      }
      throw error;
    }
  },

  async markAsRead(id) {
    try {
      const response = await apiClient.patch(NOTIFICATION_ENDPOINTS.MARK_READ(id));
      return response.data;
    } catch (error) {
      if (import.meta.env.VITE_ENABLE_MOCK_FALLBACK === 'true') {
        const item = MOCK_NOTIFICATIONS.find((n) => n.id === id);
        if (item) item.isRead = true;
        return { status: 200, data: { id, isRead: true } };
      }
      throw error;
    }
  },

  async markAllAsRead(category = 'all') {
    try {
      const response = await apiClient.post(NOTIFICATION_ENDPOINTS.MARK_ALL_READ, {
        category,
        readThroughTimestamp: new Date().toISOString(),
      });
      return response.data;
    } catch (error) {
      if (import.meta.env.VITE_ENABLE_MOCK_FALLBACK === 'true') {
        MOCK_NOTIFICATIONS.forEach((n) => {
          if (category === 'all' || n.category === category) {
            n.isRead = true;
          }
        });
        return { status: 200, data: { unreadCount: 0 } };
      }
      throw error;
    }
  },
};
```

---

## III. THIẾT KẾ REACT HOOK: `useNotifications.js`

Hook tách biệt hoàn toàn state quản lý thông báo, kết nối SignalR, và âm thanh khỏi các component hiển thị:
```javascript
// src/hooks/useNotifications.js
import { useState, useEffect, useCallback } from 'react';
import { notificationService } from '../services/notificationService.js';
import { signalRManager } from '../services/signalrService.js';
import { playLuxuryChime } from '../utils/audioUtils.js';

export const useNotifications = () => {
  const [notifications, setNotifications] = useState([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [isLoading, setIsLoading] = useState(false);
  const [selectedCategory, setSelectedCategory] = useState('all');

  const fetchNotifications = useCallback(async () => {
    setIsLoading(true);
    try {
      const res = await notificationService.getNotifications({ category: selectedCategory });
      if (res?.data) {
        setNotifications(res.data.items || []);
        setUnreadCount(res.data.unreadCount ?? 0);
      }
    } finally {
      setIsLoading(false);
    }
  }, [selectedCategory]);

  useEffect(() => {
    fetchNotifications();
  }, [fetchNotifications]);

  // Lắng nghe sự kiện đẩy qua WebSocket SignalR
  useEffect(() => {
    const handleReceiveNotification = (newNotif) => {
      setNotifications((prev) => [newNotif, ...prev]);
      setUnreadCount((prev) => prev + 1);
      playLuxuryChime();
    };

    const handleUnreadCountUpdated = ({ unreadCount: count }) => {
      setUnreadCount(count);
    };

    signalRManager.on('ReceiveNotification', handleReceiveNotification);
    signalRManager.on('UnreadCountUpdated', handleUnreadCountUpdated);

    // Bắt đầu kết nối SignalR
    signalRManager.startConnection();

    return () => {
      signalRManager.off('ReceiveNotification', handleReceiveNotification);
      signalRManager.off('UnreadCountUpdated', handleUnreadCountUpdated);
    };
  }, []);

  const markAsRead = async (id) => {
    await notificationService.markAsRead(id);
    setNotifications((prev) =>
      prev.map((n) => (n.id === id ? { ...n, isRead: true } : n))
    );
    setUnreadCount((prev) => Math.max(0, prev - 1));
  };

  const markAllAsRead = async () => {
    await notificationService.markAllAsRead(selectedCategory);
    setNotifications((prev) => prev.map((n) => ({ ...n, isRead: true })));
    setUnreadCount(0);
  };

  return {
    notifications,
    unreadCount,
    isLoading,
    selectedCategory,
    setSelectedCategory,
    markAsRead,
    markAllAsRead,
    refresh: fetchNotifications,
  };
};
```

---

## IV. BỘ COMPONENT GIAO DIỆN LUXURY GOLD

1. **`NotificationBell.jsx`:**
   - Đặt trên Top Header thanh điều hướng.
   - Hiển thị icon `BellOutlined` với `Badge` viền vàng hổ phách.
   - Hiệu ứng nhịp đập chuyển động (`animate-pulse`) khi có tin mới chưa đọc.
2. **`NotificationDrawer.jsx`:**
   - Ngăn kéo trượt phong cách Á Đông với các tab lọc: *Tất cả, Đơn hàng, Hợp đồng, Báo giá, Thanh toán, Hệ thống*.
   - Hỗ trợ cuộn mượt mà (smooth scrolling), phân biệt thẻ chưa đọc màu hổ phách dịu (`bg-amber-500/10`), thẻ đã đọc màu đá trầm.
   - Nút hành động nhanh: *"Đánh dấu đã đọc tất cả"* và *"Cài đặt thông báo"*.
   - Responsive hoàn hảo: Hiển thị Drawer bên phải trên máy tính, hiển thị Bottom Sheet tròn góc trên điện thoại.
3. **`NotificationItem.jsx`:**
   - Từng dòng thông báo hiển thị icon theo loại sự kiện (Thanh toán: Icon Tiền/Thẻ màu Emerald; Hợp đồng: Icon Bút ký màu Vàng; Giao hàng: Icon Xe tải màu Xanh dương).
   - Nút điều hướng trực tiếp (Action Deeplink) chuyển đến đúng màn hình chi tiết tương ứng.

