# [REGISTER-ACCOUNT] - TÀI LIỆU THIẾT KẾ CHI TIẾT TỔNG THỂ (OVERVIEW DETAIL DESIGN)

---

## I. TÊN TÀI LIỆU, TÍNH NĂNG & TỔNG QUAN HỆ THỐNG

### 1.1. Thông tin tài liệu
- **Tên tài liệu:** Thiết kế chi tiết tính năng Đăng ký tài khoản người dùng qua Số điện thoại & SMS OTP (Lưu trữ Database & Background Cleanup Job)
- **Mã tính năng:** `register-account`
- **Mô-đun:** Authentication & Authorization (`AuthenController` / `AuthenService`)
- **Phiên bản:** `1.1.0` (Cập nhật lưu trữ Database & Background Worker Service)
- **Hệ thống liên quan:**
  - Backend: `mooncake-ecommerce-platform` (ASP.NET Core 8 Web API - Clean Architecture)
  - Frontend: `mooncake-fe` (React 19, Tailwind CSS Luxury Theme, Axios apiClient)
- **Ngày lập:** 13/10/2026
- **Trạng thái:** `Under Review` (Chờ duyệt thiết kế kỹ thuật)

### 1.2. Mục tiêu nghiệp vụ (Business Objectives)
1. **Lưu trữ bền vững trong Database (Database-backed OTP Persistence):** Thay vì chỉ lưu bộ nhớ tạm thời RAM (`IMemoryCache`), toàn bộ phiên xác thực OTP và mã phiên đăng ký được lưu vào bảng cơ sở dữ liệu PostgreSQL `otp_verifications`. Đảm bảo tính nhất quán dữ liệu khi triển khai môi trường nhiều máy chủ (Multi-instance / Load Balancing) hoặc khi khởi động lại backend.
2. **Cơ chế dọn dẹp dữ liệu tự động (Background Worker Cleanup Service):** Xây dựng một Hosted Service nền `OtpCleanupBackgroundService` chạy định kỳ (quét mỗi 15 hoặc 30 phút) để tự động xóa sạch các bản ghi OTP đã hết hạn (`expires_at_utc < now`) hoặc đã sử dụng (`consumed_at_utc is not null`), giữ cho bảng `otp_verifications` luôn gọn nhẹ, tối ưu hóa dung lượng lưu trữ và tốc độ truy vấn.
3. **API Gửi lại OTP riêng biệt (Dedicated Resend OTP Endpoint):** Cung cấp API `POST /api/v1/auth/register/resend-otp` chuyên biệt cho nút bấm "Gửi lại mã OTP" trên giao diện popup, có logic quản lý số lần gửi lại, reset số lần thử sai, gia hạn thời gian sống và bảo vệ chống spam nghiêm ngặt (Cooldown 60s & Quota giờ).
4. **Chống lạm dụng & Spam SMS (Anti-Spam Cost Protection):**
   - **Cooldown 60 giây:** Mỗi số điện thoại chỉ được yêu cầu gửi/gửi lại OTP sau tối thiểu 60 giây kể từ lần gửi trước.
   - **Giới hạn số lần gửi (Rate Limit):** Tối đa 5 lượt yêu cầu OTP / số điện thoại trong 1 giờ; và IP Rate Limit 10 yêu cầu / 5 phút.
   - **Chống brute-force OTP:** Tối đa 5 lần nhập sai mã OTP; vượt quá sẽ hủy bỏ mã OTP ngay lập tức và bắt buộc yêu cầu mã mới.
5. **Bảo mật chuyển bước (Zero-Trust Step Transition):** Người dùng không thể bỏ qua bước xác thực OTP để gọi thẳng API đăng ký; Backend sinh một `verificationToken` một lần dùng có thời hạn 10 phút lưu trực tiếp vào bản ghi DB gắn chặt với số điện thoại đã xác minh.
6. **Phân quyền mặc định an toàn:** Tài khoản tạo mới thành công được gán vai trò mặc định là `UserRole.Customer` (`Customer`), trạng thái kích hoạt `IsActive = true`, mật khẩu băm PBKDF2-SHA256 theo chuẩn bảo mật cao nhất của nền tảng.
7. **Tích hợp SMS giai đoạn phát triển:** Ở giai đoạn này ghi log mã OTP ra SMS Console (`TODO: Gửi qua SMS Console`) để đội phát triển kiểm thử nhanh chóng mà không phụ thuộc hạ tầng nhà mạng bên ngoài.

### 1.3. Bảng phân rã các API trong tính năng
Tính năng `register-account` được phân tách độc lập thành 4 API con theo đúng chuẩn RESTful Clean Architecture:

| STT | API Endpoint | Phương thức | Tài liệu chi tiết | Tóm tắt chức năng |
| :---: | :--- | :---: | :--- | :--- |
| 1 | `/api/v1/auth/register/send-otp` | `POST` | [01_SEND_OTP_API.md](./01_SEND_OTP_API.md) | Kiểm tra số điện thoại, chặn spam, sinh OTP, lưu vào DB `otp_verifications`, ghi log OTP console. |
| 2 | `/api/v1/auth/register/resend-otp` | `POST` | [02_RESEND_OTP_API.md](./02_RESEND_OTP_API.md) | Kiểm tra cooldown 60s & quota giờ từ DB, cập nhật OTP mới vào DB, reset failed attempts, ghi log console. |
| 3 | `/api/v1/auth/register/verify-otp` | `POST` | [03_VERIFY_OTP_API.md](./03_VERIFY_OTP_API.md) | Kiểm tra mã OTP trong DB, khóa khi sai 5 lần, cấp `verificationToken` lưu vào DB có hạn 10 phút. |
| 4 | `/api/v1/auth/register` | `POST` | [04_REGISTER_API.md](./04_REGISTER_API.md) | Xác minh `verificationToken` từ DB, tạo User Role Customer, đánh dấu `consumed_at_utc`. |

---

## II. FLOW CHART TOÀN BỘ LUỒNG (END-TO-END WORKFLOW)

### 2.1. Sequence Diagram toàn bộ luồng đăng ký (Actor - FE - BE - DB - SMS Console - Background Job)

```mermaid
sequenceDiagram
  autonumber
  actor User as Người dùng (Khách hàng)
  participant FE as Frontend (RegisterPopup)
  participant CTRL as AuthenController (WebApi)
  participant SVC as AuthenService (Application)
  participant DB as PostgreSQL (otp_verifications & users)
  participant SMS as SMS Service / Console Logger
  participant BG as OtpCleanupBackgroundService

  Note over User, SMS: GIAI ĐOẠN 1: NHẬP SỐ ĐIỆN THOẠI & GỬI OTP LẦN ĐẦU
  User->>FE: 1. Mở RegisterPopup, nhập số điện thoại (VD: 0912 345 678)
  User->>FE: 2. Nhấn "Tiếp tục nhận mã OTP"
  FE->>CTRL: 3. POST /api/v1/auth/register/send-otp { phoneNumber }
  CTRL->>SVC: 4. SendRegisterOtpAsync(request)
  SVC->>SVC: 5. Chuẩn hóa SĐT (PhoneNumberUtils.Normalize -> 0912345678)
  SVC->>DB: 6. UserRepository.GetByPhoneAsync(phone)
  alt Số điện thoại đã được đăng ký và hoạt động
    DB-->>SVC: Trả về bản ghi User tồn tại
    SVC-->>CTRL: Throw HttpException(409, "Số điện thoại đã được đăng ký tài khoản...")
    CTRL-->>FE: HTTP 409 Conflict
    FE-->>User: Báo lỗi & gợi ý chuyển sang "Đăng nhập ngay"
  else Số điện thoại chưa đăng ký
    SVC->>DB: 7. OtpRepository.GetLatestActiveAsync(phone, purpose: "RegisterAccount")
    alt Bản ghi cũ còn cooldown (< 60s kể từ LastSentAtUtc)
      SVC-->>CTRL: Throw HttpException(429, "Vui lòng chờ X giây...")
      CTRL-->>FE: HTTP 429 Too Many Requests
    else Vượt quá 5 lần gửi trong 1 giờ
      SVC-->>CTRL: Throw HttpException(429, "Bạn đã yêu cầu gửi mã quá nhiều lần...")
      CTRL-->>FE: HTTP 429 Too Many Requests
    else Đủ điều kiện gửi OTP
      SVC->>SVC: 8. Sinh OTP 6 chữ số ngẫu nhiên (100000 - 999999)
      SVC->>DB: 9. Tạo mới bản ghi OtpVerification trong DB (OtpHash SHA-256, TTL 5 phút, RetryCount=0)
      SVC->>SMS: 10. TODO: Gửi SMS Brandname - Hiện tại ghi log OTP ra Console
      Note right of SMS: [SMS CONSOLE] Gửi OTP 648291 tới SĐT 0912345678 (Hết hạn sau 5 phút)
      SVC-->>CTRL: Trả về SendOtpResponse (cooldown: 60, expiresIn: 300)
      CTRL-->>FE: HTTP 200 OK
      FE->>FE: 11. Chuyển sang Bước 2: Nhập OTP, kích hoạt đồng hồ đếm ngược 60s
      FE-->>User: Hiển thị popup nhập 6 số OTP & trạng thái đếm ngược
    end
  end

  Note over User, SMS: GIAI ĐOẠN 2A: GỬI LẠI MÃ OTP KHI CHƯA NHẬN ĐƯỢC (RESEND OTP)
  opt Người dùng bấm "Gửi lại mã OTP" sau khi hết 60s đếm ngược
    User->>FE: Bấm nút "Gửi lại mã OTP"
    FE->>CTRL: POST /api/v1/auth/register/resend-otp { phoneNumber }
    CTRL->>SVC: ResendRegisterOtpAsync(request)
    SVC->>DB: Lấy bản ghi OtpVerification hiện tại theo phone
    alt Chưa đủ 60 giây Cooldown
      SVC-->>CTRL: Throw HttpException(429, "Vui lòng chờ X giây...")
      CTRL-->>FE: HTTP 429 Too Many Requests
    else Vượt quá 5 lần gửi lại
      SVC-->>CTRL: Throw HttpException(429, "Bạn đã yêu cầu gửi lại quá giới hạn...")
      CTRL-->>FE: HTTP 429 Too Many Requests
    else Thỏa mãn điều kiện
      SVC->>SVC: Sinh OTP 6 chữ số mới
      SVC->>DB: Cập nhật OtpHash mới, gia hạn ExpiresAtUtc = Now + 5m, LastSentAtUtc = Now, Reset FailedAttempts = 0
      SVC->>SMS: Ghi log mã OTP mới ra SMS Console (TODO)
      SVC-->>CTRL: HTTP 200 OK { cooldownSeconds: 60, expiresInSeconds: 300 }
      CTRL-->>FE: HTTP 200 OK
      FE->>FE: Reset đồng hồ đếm ngược 60s mới
      FE-->>User: Thông báo: "Đã gửi lại mã OTP mới. Vui lòng kiểm tra tin nhắn!"
    end
  end

  Note over User, SMS: GIAI ĐOẠN 2B: XÁC THỰC MÃ OTP (VERIFY OTP)
  User->>FE: 12. Nhập mã OTP 6 chữ số và bấm "Xác nhận mã OTP"
  FE->>CTRL: 13. POST /api/v1/auth/register/verify-otp { phoneNumber, otp }
  CTRL->>SVC: 14. VerifyRegisterOtpAsync(request)
  SVC->>DB: 15. OtpRepository.GetLatestActiveAsync(phone, purpose: "RegisterAccount")
  alt Không tìm thấy hoặc OTP đã quá hạn (ExpiresAtUtc < Now)
    SVC-->>CTRL: Throw HttpException(400, "Mã OTP đã hết hạn hoặc không tồn tại...")
    CTRL-->>FE: HTTP 400 Bad Request
  else Tìm thấy bản ghi OTP trong DB
    alt FailedAttempts >= 5 (Nhập sai quá số lần)
      SVC->>DB: Đánh dấu bản ghi bị khóa (IsDeleted = true hoặc IsLocked = true)
      SVC-->>CTRL: Throw HttpException(400, "Bạn đã nhập sai mã OTP quá 5 lần...")
      CTRL-->>FE: HTTP 400 Bad Request
    else So khớp mã OTP (SHA-256)
      alt Mã OTP sai
        SVC->>DB: Tăng FailedAttempts += 1 và lưu DB
        SVC-->>CTRL: Throw HttpException(400, "Mã OTP không chính xác. Bạn còn X lần thử.")
        CTRL-->>FE: HTTP 400 Bad Request
      else Mã OTP hoàn toàn chính xác
        SVC->>SVC: 16. Sinh verificationToken ngẫu nhiên an toàn 64 ký tự
        SVC->>DB: 17. Cập nhật bản ghi: IsVerified = true, VerifiedAtUtc = Now, VerificationToken = token, TokenExpiresAtUtc = Now + 10m
        SVC-->>CTRL: Trả về VerifyOtpResponse { verificationToken, expiresInSeconds: 600 }
        CTRL-->>FE: HTTP 200 OK
        FE->>FE: 18. Lưu verificationToken trong memory component, chuyển sang Bước 3
        FE-->>User: Hiển thị Form điền FullName & Password
      end
    end
  end

  Note over User, SMS: GIAI ĐOẠN 3: ĐIỀN THÔNG TIN & HOÀN TẤT ĐĂNG KÝ
  User->>FE: 19. Nhập FullName, Password, Confirm Password, tích đồng ý điều khoản
  User->>FE: 20. Bấm "Hoàn tất đăng ký"
  FE->>CTRL: 21. POST /api/v1/auth/register { phoneNumber, verificationToken, fullName, password, confirmPassword }
  CTRL->>SVC: 22. RegisterAccountAsync(request)
  SVC->>DB: 23. OtpRepository.GetByVerificationTokenAsync(verificationToken)
  alt Token không tồn tại, sai SĐT, đã hết hạn (> 10m) hoặc đã bị Consumed
    SVC-->>CTRL: Throw HttpException(400, "Phiên xác thực đã hết hạn hoặc không hợp lệ...")
    CTRL-->>FE: HTTP 400 Bad Request
  else Token hợp lệ
    SVC->>SVC: 24. Validate fullName, password match, password strength
    SVC->>DB: 25. Double check race condition: UserRepository.GetByPhoneAsync(phone)
    SVC->>SVC: 26. Băm mật khẩu an toàn: PasswordHashUtils.Hash(password) (PBKDF2-SHA256)
    SVC->>DB: 27. Tạo User mới trong bảng users (Role = Customer, IsActive = true)
    SVC->>DB: 28. Cập nhật bản ghi OTP: ConsumedAtUtc = Now (Chống Replay)
    SVC-->>CTRL: Trả về RegisterResponse { userId, phone, fullName, role: "Customer" }
    CTRL-->>FE: HTTP 201 Created
    FE->>FE: 29. Hiển thị màn hình thành công (Bước 4)
    FE-->>User: Chúc mừng! Tự động chuyển sang LoginPopup sau 1.8s
    FE->>FE: 30. Tự động mở LoginPopup với SĐT điền sẵn
  end

  Note over BG, DB: TIẾN TRÌNH NỀN TỰ ĐỘNG DỌN DẸP (BACKGROUND CLEANUP JOB)
  loop Quét định kỳ mỗi 15 phút (PeriodicTimer)
    BG->>DB: DELETE FROM otp_verifications WHERE (expires_at_utc < (Now - 1 hour)) OR (consumed_at_utc IS NOT NULL AND updated_at < (Now - 24 hours))
    DB-->>BG: Trả về số lượng dòng đã dọn dẹp sạch sẽ
  end
```

---

## III. THIẾT KẾ CƠ SỞ DỮ LIỆU & TIẾN TRÌNH NỀN (DB & BACKGROUND JOB)

### 3.1. Thiết kế thực thể `OtpVerification` (Domain & Infrastructure)

#### 1. Entity: `Mooncake.EcommercePlatform.Domain.Entities.OtpVerification.cs`
Kế thừa `BaseEntity` (đã có sẵn `Id`, `CreatedAt`, `UpdatedAt`, `IsDeleted`, `CreatedBy`, `UpdatedBy`):

```csharp
namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>Represents a database-backed SMS OTP verification session.</summary>
public class OtpVerification : BaseEntity
{
    public string Phone { get; set; } = string.Empty;

    public string OtpHash { get; set; } = string.Empty;

    public string Purpose { get; set; } = "RegisterAccount"; // e.g. RegisterAccount, ForgotPassword

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime LastSentAtUtc { get; set; }

    public int ResendCount { get; set; } = 0;

    public int FailedAttempts { get; set; } = 0;

    public bool IsVerified { get; set; } = false;

    public DateTime? VerifiedAtUtc { get; set; }

    public string? VerificationToken { get; set; }

    public DateTime? TokenExpiresAtUtc { get; set; }

    public DateTime? ConsumedAtUtc { get; set; }
}
```

#### 2. EF Core Configuration: `OtpVerificationConfiguration.cs`
Bảng `otp_verifications` với các chỉ mục tăng tốc tra cứu và dọn dẹp:

```csharp
namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;

public class OtpVerificationConfiguration : IEntityTypeConfiguration<OtpVerification>
{
    public void Configure(EntityTypeBuilder<OtpVerification> builder)
    {
        builder.ToTable("otp_verifications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Phone).HasMaxLength(20).IsRequired().HasColumnName("phone");
        builder.Property(x => x.OtpHash).HasMaxLength(64).IsRequired().HasColumnName("otp_hash");
        builder.Property(x => x.Purpose).HasMaxLength(50).IsRequired().HasColumnName("purpose");
        builder.Property(x => x.ExpiresAtUtc).HasColumnName("expires_at_utc");
        builder.Property(x => x.LastSentAtUtc).HasColumnName("last_sent_at_utc");
        builder.Property(x => x.ResendCount).HasDefaultValue(0).HasColumnName("resend_count");
        builder.Property(x => x.FailedAttempts).HasDefaultValue(0).HasColumnName("failed_attempts");
        builder.Property(x => x.IsVerified).HasDefaultValue(false).HasColumnName("is_verified");
        builder.Property(x => x.VerifiedAtUtc).HasColumnName("verified_at_utc");
        builder.Property(x => x.VerificationToken).HasMaxLength(128).HasColumnName("verification_token");
        builder.Property(x => x.TokenExpiresAtUtc).HasColumnName("token_expires_at_utc");
        builder.Property(x => x.ConsumedAtUtc).HasColumnName("consumed_at_utc");

        builder.Property(x => x.IsDeleted).HasColumnName("is_deleted");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        // Indexes phục vụ tra cứu nhanh
        builder.HasIndex(x => new { x.Phone, x.Purpose, x.IsDeleted }, "idx_otp_phone_purpose");
        builder.HasIndex(x => x.VerificationToken, "idx_otp_verification_token").IsUnique();
        builder.HasIndex(x => x.ExpiresAtUtc, "idx_otp_expires_at");
    }
}
```

#### 3. Bổ sung `DbSet` vào `ApplicationDbContext.cs`
```csharp
public DbSet<OtpVerification> OtpVerifications => Set<OtpVerification>();
```

---

### 3.2. Thiết kế Background Worker Service dọn dẹp OTP hết hạn

Lấy cảm hứng từ mẫu `GoogleLoginAttemptCleanupService` hiện có của dự án:

#### File: `Mooncake.EcommercePlatform.Infrastructure/Services/OtpCleanupBackgroundService.cs`
```csharp
namespace Mooncake.EcommercePlatform.Infrastructure.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

/// <summary>
/// Background worker service executing periodic cleanup of expired and consumed OTP records.
/// </summary>
public class OtpCleanupBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<OtpCleanupBackgroundService> logger) : BackgroundService
{
    private const string CleanupFailureMessage = "OTP cleanup background execution failed.";
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15); // Quét mỗi 15 phút

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var clock = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
                var now = clock.UtcNow;

                // Xóa các bản ghi đã hết hạn hơn 1 giờ trước hoặc đã hoàn tất đăng ký hơn 24 giờ trước
                var expiredThreshold = now.AddHours(-1);
                var consumedThreshold = now.AddHours(-24);

                var deletedCount = await dbContext.OtpVerifications
                    .Where(x => x.ExpiresAtUtc < expiredThreshold ||
                               (x.ConsumedAtUtc != null && x.ConsumedAtUtc < consumedThreshold))
                    .ExecuteDeleteAsync(stoppingToken);

                if (deletedCount > 0)
                {
                    logger.LogInformation("[OTP CLEANUP] Đã dọn dẹp thành công {Count} bản ghi OTP hết hạn khỏi cơ sở dữ liệu.", deletedCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, CleanupFailureMessage);
            }
        }
    }
}
```

#### Đăng ký Hosted Service tại `Infrastructure/DependencyInjection.cs`:
```csharp
services.AddHostedService<OtpCleanupBackgroundService>();
```

---

## IV. BẢN ĐỒ PHÂN TẦNG VÀ TUÂN THỦ QUY TẮC MÃ NGUỒN

### 4.1. Clean Architecture Backend Mapping
- **Domain:**
  - Thực thể `OtpVerification : BaseEntity` trong `Domain/Entities/OtpVerification.cs`.
- **Application:**
  - Interface repository `IOtpRepository` trong `Application/Common/Interfaces/IOtpRepository.cs`.
  - Service `IAuthenService` bổ sung 3 method: `SendRegisterOtpAsync`, `ResendRegisterOtpAsync`, `VerifyRegisterOtpAsync`, `RegisterAccountAsync`.
  - Hằng số thông điệp tiếng Việt trong `Application/Common/Constant/MessageConstant.cs`.
- **Infrastructure:**
  - Hiện thực `OtpRepository` trong `Infrastructure/Repositories/OtpRepository.cs`.
  - Cấu hình EF Core `OtpVerificationConfiguration.cs` và migration.
  - Background Service `OtpCleanupBackgroundService.cs`.
- **WebApi:**
  - Controller `AuthenController.cs` tiếp nhận các endpoint `/api/v1/auth/register/*`.

### 4.2. Frontend Mapping (`mooncake-fe`)
- **Constants:** `src/constants/apiEndpoints.js` (Khai báo `SEND_OTP`, `RESEND_OTP`, `VERIFY_OTP`, `REGISTER`).
- **Services:** `src/services/authService.js` (Hàm `sendOtp`, `resendOtp`, `verifyOtp`, `register`).
- **Pages / Popups:** `src/Pages/RegisterPopup.jsx` (Giao diện 4 bước sang trọng, tích hợp nút gửi lại OTP đếm ngược 60s, form FullName & Password, chuyển mượt mà về `LoginPopup`).
