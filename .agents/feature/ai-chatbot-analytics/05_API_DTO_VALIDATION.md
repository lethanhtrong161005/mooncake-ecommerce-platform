# TÍNH NĂNG #23 & #24: ĐẶC TẢ KỸ THUẬT API, DTOS & VALIDATION
## TÀI LIỆU THIẾT KẾ CHI TIẾT (API SPECIFICATION & VALIDATION RULES)

---

## 1. DANH SÁCH ENDPOINTS CỦA MODULE AI

| Phương thức | Đường dẫn API | Xác thực | Vai trò cho phép | Mục đích nghiệp vụ |
| :--- | :--- | :---: | :---: | :--- |
| `POST` | `/api/v1/ai/chat/sessions` | Tùy chọn | All / Guest | Khởi tạo hoặc khôi phục phiên chat với Trợ lý AI |
| `POST` | `/api/v1/ai/chat/messages` | Tùy chọn | All / Guest | Gửi câu hỏi, thực thi RAG Catalog & nhận phản hồi từ Gemini |
| `GET` | `/api/v1/ai/chat/sessions/{sessionToken}/history` | Tùy chọn | All / Guest | Lấy lịch sử tin nhắn trong phiên hội thoại |
| `DELETE` | `/api/v1/ai/chat/sessions/{sessionToken}` | Tùy chọn | All / Guest | Kết thúc phiên chat |
| `POST` | `/api/v1/ai/analytics/generate` | Bắt buộc | Admin, Supplier | Kích hoạt AI phân tích doanh thu & sinh báo cáo chiến lược |
| `GET` | `/api/v1/ai/analytics/reports` | Bắt buộc | Admin, Supplier | Xem danh sách các báo cáo AI đã hoàn tất |
| `GET` | `/api/v1/ai/analytics/reports/{id}` | Bắt buộc | Admin, Supplier | Xem chi tiết nội dung báo cáo & kết quả phân tích |
| `GET` | `/api/v1/ai/analytics/dashboard-summary` | Bắt buộc | Admin, Supplier | Lấy nhanh các chỉ số định lượng thời gian thực (Realtime KPIs) |

---

## 2. CHI TIẾT CÁC REQUEST PAYLOAD & DTOs (C# / APPLICATION LAYER)

### 2.1. Nhóm DTOs Trợ Lý Ảo Chatbot (Feature #23)

```csharp
namespace Mooncake.EcommercePlatform.Application.DTOs.Ai.Requests;

using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Request to initiate or resume an AI chat session.</summary>
public class StartAiSessionRequest
{
    public string? ExistingSessionToken { get; set; }
    public AiConversationType ConversationType { get; set; } = AiConversationType.General;
}

/// <summary>Request to send a message to the AI Consultant.</summary>
public class SendAiMessageRequest
{
    public string SessionToken { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public AiConversationType ConversationType { get; set; } = AiConversationType.ProductInfo;
    public decimal? TargetBudget { get; set; }
    public int? TargetQuantity { get; set; }
}
```

```csharp
namespace Mooncake.EcommercePlatform.Application.DTOs.Ai.Responses;

using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Response containing the AI conversation session state.</summary>
public class AiSessionResponse
{
    public Guid ConversationId { get; set; }
    public string SessionToken { get; set; } = string.Empty;
    public AiConversationType ConversationType { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Response for an individual chat interaction turn.</summary>
public class AiMessageResponse
{
    public Guid MessageId { get; set; }
    public AiMessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public int TokensUsed { get; set; }
    public string ModelUsed { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<AiProductRecommendationDto> RecommendedProducts { get; set; } = new();
    public List<string> SuggestedFollowUps { get; set; } = new();
}

/// <summary>Product card metadata returned alongside the AI text response.</summary>
public class AiProductRecommendationDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ShopName { get; set; } = string.Empty;
    public string? PrimaryImageUrl { get; set; }
    public decimal BasePrice { get; set; }
    public int MinOrderQty { get; set; }
    public string? VariantName { get; set; }
    public decimal? VariantPrice { get; set; }
    public string? ReputationBadgeTitle { get; set; }
    public decimal ReputationScore { get; set; }
    public string RecommendationReason { get; set; } = string.Empty;
}
```

### 2.2. Nhóm DTOs Phân Tích Bán Hàng (Feature #24)

```csharp
namespace Mooncake.EcommercePlatform.Application.DTOs.Ai.Requests;

using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Request to generate or refresh an intelligent analytics report.</summary>
public class GenerateAiAnalyticsReportRequest
{
    public AiReportType ReportType { get; set; } = AiReportType.SalesTrend;
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public Guid? TargetShopId { get; set; }
    public string? CategoryFilter { get; set; }
}
```

```csharp
namespace Mooncake.EcommercePlatform.Application.DTOs.Ai.Responses;

using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Response representing an AI sales analysis report.</summary>
public class AiAnalyticsReportResponse
{
    public Guid ReportId { get; set; }
    public AiReportType ReportType { get; set; }
    public AiReportStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public AiReportResultDto? Result { get; set; }
}

/// <summary>Structured natural language insights returned by Gemini.</summary>
public class AiReportResultDto
{
    public string ExecutiveSummary { get; set; } = string.Empty;
    public List<string> KeyHighlights { get; set; } = new();
    public List<AiRiskAlertDto> RiskAlerts { get; set; } = new();
    public List<AiStrategicActionDto> StrategicRecommendations { get; set; } = new();
    public AiReportMetricsSummaryDto MetricsSummary { get; set; } = new();
}

public class AiRiskAlertDto
{
    public string Severity { get; set; } = "Medium"; // Low | Medium | High | Critical
    public string Description { get; set; } = string.Empty;
    public string PotentialImpact { get; set; } = string.Empty;
}

public class AiStrategicActionDto
{
    public string Priority { get; set; } = "High"; // Urgent | High | Medium
    public string Action { get; set; } = string.Empty;
    public string ExpectedOutcome { get; set; } = string.Empty;
}

public class AiReportMetricsSummaryDto
{
    public decimal TotalGmv { get; set; }
    public decimal GmvGrowthRate { get; set; }
    public int TotalOrders { get; set; }
    public int TotalContracts { get; set; }
    public decimal OnTimeDeliveryRate { get; set; }
    public decimal RfqWinRate { get; set; }
}
```

---

## 3. QUY TẮC KIỂM TRA ĐẦU VÀO (FLUENTVALIDATION RULES)

### 3.1. SendAiMessageRequestValidator
1. `SessionToken`: Bắt buộc, không được để trống, độ dài từ 10 đến 255 ký tự.
2. `Content`:
   - Bắt buộc, không được để trống.
   - Độ dài tối thiểu 2 ký tự, tối đa 1,000 ký tự.
   - Không chứa mã kịch bản độc hại (`<script>`, `javascript:`).
3. `TargetBudget`: Nếu có truyền thì phải $> 0$.
4. `TargetQuantity`: Nếu có truyền thì phải $\ge 1$.

### 3.2. GenerateAiAnalyticsReportRequestValidator
1. `ReportType`: Phải thuộc các giá trị Enum hợp lệ (`SalesTrend`, `SupplierPerformance`, `DemandForecast`, `RevenueSummary`).
2. `FromDate` & `ToDate`:
   - Nếu có truyền, `FromDate` phải nhỏ hơn hoặc bằng `ToDate`.
   - Khoảng thời gian tối đa giữa `FromDate` và `ToDate` không vượt quá 365 ngày (1 năm).
   - `ToDate` không được vượt quá ngày hiện tại (UTC).

---

## 4. BẢNG MÃ LỖI VÀ THÔNG ĐIỆP PHẢN HỒI (ERROR CODE MATRIX)

| HTTP Status | Mã lỗi nội bộ | Thông điệp tiếng Việt |
| :--- | :--- | :--- |
| `400 Bad Request` | `INVALID_AI_MESSAGE_CONTENT` | `"Nội dung tin nhắn không hợp lệ hoặc vượt quá 1.000 ký tự."` |
| `400 Bad Request` | `INVALID_DATE_RANGE` | `"Khoảng thời gian báo cáo không hợp lệ (tối đa 365 ngày)."` |
| `401 Unauthorized` | `AUTHENTICATION_REQUIRED` | `"Vui lòng đăng nhập để tạo báo cáo phân tích kinh doanh."` |
| `403 Forbidden` | `ACCESS_DENIED_SHOP_REPORT` | `"Nhà cung cấp chỉ được phép xem báo cáo phân tích của gian hàng mình."` |
| `404 Not Found` | `AI_SESSION_NOT_FOUND` | `"Phiên trò chuyện không tồn tại hoặc đã kết thúc."` |
| `429 Too Many Requests` | `RATE_LIMIT_EXCEEDED` | `"Quý khách thao tác quá nhanh. Vui lòng đợi vài giây trước khi gửi tiếp."` |
| `502 Bad Gateway` | `GEMINI_UPSTREAM_ERROR` | `"Dịch vụ AI tạm thời gián đoạn. Hệ thống đang chuyển sang chế độ hỗ trợ tự động."` |

