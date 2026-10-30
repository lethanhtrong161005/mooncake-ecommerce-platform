# MODULE 8: TÍCH HỢP GIAO DIỆN FRONTEND (FRONTEND INTEGRATION)
## Tài Liệu Thiết Kế Chi Tiết (React 19, Luxury Design System & Tailwind CSS)

---

## I. KIẾN TRÚC THÀNH PHẦN GIAO DIỆN (COMPONENT HIERARCHY)

Tất cả các thành phần giao diện của Module 8 được xây dựng dựa trên React 19, hệ thống màu sắc Luxury Gold / Deep Stone của nền tảng B2B Mooncake, tuân thủ nghiêm ngặt quy tắc định dạng **2-space indentation**, không dùng `var`, và tối ưu hóa trải nghiệm trên mọi kích thước màn hình.

### 1.1. Cây Cấu Trúc Thành Phần (Component Tree)

```text
src/
├── constants/
│   └── apiEndpoints.js                   # Bổ sung REVIEWS & REPUTATION endpoints
├── services/
│   └── reviewService.js                  # Tầng giao tiếp mạng qua apiClient + Mock Fallback
├── components/
│   └── reviews/
│       ├── TransactionReviewModal.jsx    # Modal chấm 4 tiêu chí sao cho đơn hàng/hợp đồng
│       ├── ProductReviewStream.jsx       # Luồng bình luận mạng xã hội (Shopee/TikTok style)
│       ├── RatingHistogram.jsx           # Biểu đồ thanh phân bố tỷ lệ 1-5 sao màu vàng Gold
│       ├── FilterChipBar.jsx             # Thanh bộ lọc trượt ngang (Tất cả, Có ảnh, 5★, Biến thể)
│       ├── WriteProductReviewModal.jsx   # Form viết review cho khách đã mua hàng (Verified Purchase)
│       ├── ReputationBadge.jsx           # Huy hiệu cấp bậc uy tín (Mới, Tin cậy, Uy tín, Đối tác Vàng)
│       ├── ReputationTimeline.jsx        # Dòng thời gian trực quan nhật ký biến động điểm
│       └── SupplierReviewReplyModal.jsx  # Modal phản hồi chính thức 1 lần cho Nhà cung cấp
└── Pages/
    ├── Products/
    │   └── ProductInformation.jsx       # Nhúng luồng Review & Thống kê dưới chân trang chi tiết bánh
    ├── Customer/
    │   └── PaymentAndRate.jsx            # Nâng cấp trang Đánh giá giao dịch & Lịch sử nhận xét
    ├── PublicShopPage.jsx                # Nâng cấp Tab Đánh giá gian hàng hiển thị 4 tiêu chí
    ├── Supplier/
    │   └── SuppilerProfile.jsx           # Thêm Tab "Hồ sơ Điểm Uy Tín & Huy Hiệu Sàn"
    └── Admin/
        └── TradingOperating.jsx          # Bổ sung Bảng kiểm duyệt đánh giá và Chế tài phạt điểm
```

---

## II. THIẾT KẾ CÁC LINH KIỆN CỐT LÕI (CORE LUXURY COMPONENTS)

### 2.1. `ReputationBadge.jsx` (Huy Hiệu Đẳng Cấp Nhà Cung Cấp)
Huy hiệu hiển thị tại Header Gian hàng, Thẻ sản phẩm, và Bảng so sánh báo giá RFQ:

```jsx
import { Tag, Tooltip } from 'antd';
import { CrownOutlined, SafetyCertificateOutlined, StarFilled } from '@ant-design/icons';

export const REPUTATION_TIERS = {
  New: { title: 'Mới', color: 'stone', min: 0, max: 20, icon: <SafetyCertificateOutlined /> },
  Reliable: { title: 'Tin cậy', color: 'blue', min: 21, max: 50, icon: <SafetyCertificateOutlined /> },
  Reputable: { title: 'Uy tín', color: 'purple', min: 51, max: 80, icon: <StarFilled /> },
  GoldPartner: { title: 'Đối tác Vàng', color: 'gold', min: 81, max: 999, icon: <CrownOutlined /> },
};

export default function ReputationBadge({ score = 0, size = 'default' }) {
  const tierKey = score >= 81 ? 'GoldPartner' : score >= 51 ? 'Reputable' : score >= 21 ? 'Reliable' : 'New';
  const tier = REPUTATION_TIERS[tierKey];

  return (
    <Tooltip title={`Điểm tín nhiệm: ${score}đ · Cấp bậc: ${tier.title}`}>
      <Tag
        color={tier.color}
        className={`inline-flex items-center gap-1.5 font-semibold uppercase tracking-wider rounded-full px-2.5 py-0.5 border shadow-sm ${
          tierKey === 'GoldPartner'
            ? 'bg-amber-50 text-amber-800 border-amber-300 font-bold'
            : ''
        }`}
      >
        {tier.icon}
        <span>{tier.title}</span>
        <span className="text-xs opacity-80">({score}đ)</span>
      </Tag>
    </Tooltip>
  );
}
```

---

### 2.2. `RatingHistogram.jsx` (Biểu Đồ Thanh Phân Bổ 1–5 Sao)
Hiển thị tổng quan điểm trung bình và tỉ lệ phần trăm số lượt đánh giá từng mức sao:

```jsx
import { Progress, Rate } from 'antd';

export default function RatingHistogram({ summary }) {
  const total = summary?.totalReviews || 1;
  const dist = summary?.starDistribution || { star5: 0, star4: 0, star3: 0, star2: 0, star1: 0 };

  const stars = [5, 4, 3, 2, 1];

  return (
    <div className="bg-white p-6 rounded-2xl border border-stone-200/80 shadow-sm flex flex-col md:flex-row items-center gap-8">
      {/* Cột trái: Điểm số trung bình */}
      <div className="text-center md:border-r md:pr-8 border-stone-200">
        <div className="text-5xl font-extrabold text-amber-900 tracking-tight">
          {(summary?.averageRating || 5.0).toFixed(1)}
        </div>
        <div className="my-2">
          <Rate disabled allowHalf value={summary?.averageRating || 5} className="text-amber-500 text-lg" />
        </div>
        <div className="text-xs font-medium text-stone-500">
          Dựa trên {summary?.totalReviews || 0} đánh giá xác thực
        </div>
      </div>

      {/* Cột phải: Thanh Progress 5 mức sao */}
      <div className="flex-1 w-full space-y-1.5">
        {stars.map((star) => {
          const count = dist[`star${star}`] || 0;
          const percent = Math.round((count / total) * 100);
          return (
            <div key={star} className="flex items-center gap-3 text-xs">
              <span className="w-10 font-medium text-stone-700">{star} sao</span>
              <div className="flex-1">
                <Progress percent={percent} strokeColor="#d97706" showInfo={false} size="small" />
              </div>
              <span className="w-8 text-right text-stone-500">{count}</span>
            </div>
          );
        })}
      </div>
    </div>
  );
}
```

---

### 2.3. `ProductReviewStream.jsx` (Luồng Review Mạng Xã Hội Shopee/TikTok Style)
- Tích hợp bộ lọc FilterChipBar (Tất cả, Có ảnh/video, Đã mua hàng, Lọc sao).
- Thẻ đánh giá hiển thị Avatar, Tên công ty, Tag biến thể cụ thể (`Bánh Nướng Đậu Xanh 150g`).
- Thư viện ảnh dạng Grid (click mở Modal Lightbox xem chi tiết).
- Nút Thả Like tương tác thời gian thực với hiệu ứng rung nhẹ.
- Khung phản hồi chính thức từ Nhà cung cấp viền vàng Luxury.

---

## III. TÍCH HỢP MẠNG & DỊCH VỤ (`reviewService.js`)

Tuân thủ nguyên tắc Enterprise Layered Architecture và Mock Fallback Pattern:
- Mọi hàm bọc `try...catch` cẩn thận.
- Khi cờ `import.meta.env.VITE_ENABLE_MOCK_FALLBACK === 'true'` hoặc khi Backend chưa khởi động, tự động trả về dữ liệu mẫu sang trọng để giao diện hoạt động độc lập không bị gián đoạn.

```javascript
// src/services/reviewService.js
import apiClient from './apiClient';
import { API_ENDPOINTS } from '../constants/apiEndpoints';

const isMock = import.meta.env.VITE_ENABLE_MOCK_FALLBACK === 'true';

export const reviewService = {
  async getProductReviews(productId, params = {}) {
    try {
      const response = await apiClient.get(API_ENDPOINTS.REVIEWS.PRODUCT_REVIEWS(productId), { params });
      return response.data?.data || response.data;
    } catch (error) {
      if (isMock || !error.response) {
        return getMockProductReviews(productId, params);
      }
      throw error;
    }
  },

  async createProductReview(productId, payload) {
    try {
      const response = await apiClient.post(API_ENDPOINTS.REVIEWS.PRODUCT_REVIEWS(productId), payload);
      return response.data?.data || response.data;
    } catch (error) {
      if (isMock || !error.response) {
        return { reviewId: `mock-rev-${Date.now()}`, isVerifiedPurchase: true, createdAt: new Date().toISOString() };
      }
      throw error;
    }
  },

  async toggleLikeReview(reviewId) {
    try {
      const response = await apiClient.post(API_ENDPOINTS.REVIEWS.PRODUCT_REVIEW_LIKE(reviewId));
      return response.data?.data || response.data;
    } catch (error) {
      if (isMock || !error.response) {
        return { isLiked: true, likeCount: 29 };
      }
      throw error;
    }
  },

  async submitTransactionReview(payload) {
    try {
      const response = await apiClient.post(API_ENDPOINTS.REVIEWS.TRANSACTION, payload);
      return response.data?.data || response.data;
    } catch (error) {
      if (isMock || !error.response) {
        return {
          reviewId: `mock-tx-${Date.now()}`,
          reputationScoreDelta: payload.overallRating === 5 ? 3.0 : payload.overallRating === 4 ? 1.0 : -2.0,
          newReputationScore: 88.5,
        };
      }
      throw error;
    }
  },

  async getSupplierReputation(supplierId) {
    try {
      const response = await apiClient.get(API_ENDPOINTS.REVIEWS.SUPPLIER_REPUTATION(supplierId));
      return response.data?.data || response.data;
    } catch (error) {
      if (isMock || !error.response) {
        return getMockSupplierReputation(supplierId);
      }
      throw error;
    }
  },
};
```

