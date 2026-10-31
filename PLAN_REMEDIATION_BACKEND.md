# Kế hoạch rà soát và chỉnh sửa kiến trúc BE Mooncake

Ngày rà soát: 2026-10-28  
Phạm vi: `BE/mooncake-ecommerce-platform/src`  
Mục đích: đối chiếu 9 yêu cầu với code đang có và đề xuất phần còn thiếu. Đây là kế hoạch để xác nhận; chưa thay đổi code, chưa chạy build/test và chưa thay đổi database.

## Tóm tắt hiện trạng

| # | Yêu cầu | Hiện trạng | Kết luận |
|---|---|---|---|
| 1 | Regex validation cho dữ liệu đầu vào | Có một số Regex/validation: `PhoneNumberUtils`, Regex cho Idempotency-Key, DataAnnotations trên DTO; `[ApiController]` bật model validation tự động. Nhiều trường vẫn chỉ kiểm tra độ dài/Required hoặc được validate thủ công trong Service; chưa có quy chuẩn Regex tập trung theo từng định dạng. | Đã có một phần, cần rà soát và bổ sung theo trường nghiệp vụ. |
| 2 | Rà soát Round, nhất là tiền tệ | Dùng `decimal` và `decimal.Round(..., 2, MidpointRounding.AwayFromZero)` tại OrderService/RfqService và ContractRepository. Chưa thấy `Math.Round` hoặc kiểu `double/float` trong các đường tính tiền đã rà; tuy nhiên cách làm tròn chưa được thống nhất thành chính sách dùng chung, và một phép tính deposit còn nằm trong Repository. | Nền tảng tốt, cần chuẩn hóa quy tắc và vị trí tính toán. |
| 3 | Service chỉ làm việc qua DTO, giảm Entity/Model coupling | Nhiều service nhận Request DTO và trả Response DTO. Nhưng service implementation import Domain.Entities, đọc/sửa Entity do Repository trả về và tạo Entity để lưu. Repository interface cũng nhận/trả Entity. `IRfqService.AwardBidAsync` còn trả thẳng `Contract`. | Chưa đạt yêu cầu tách biệt Entity khỏi Service contract/logic. |
| 4 | GenericRepository và Unit of Work | Có các Repository theo nghiệp vụ (`OrderRepository`, `PaymentRepository`, `SupplierRepository`, ...), nhưng không tìm thấy GenericRepository hay UnitOfWork. Nhiều repository tự gọi `SaveChangesAsync`. | Chưa có GenericRepository/UoW; cần thiết kế có chọn lọc để không làm mất các truy vấn/chuyển trạng thái đặc thù. |
| 5 | ShopRepository và OrderRepository tách nghiệp vụ | Đã có `OrderRepository` riêng. Chưa thấy `ShopRepository`; các thao tác Shop xuất hiện trong `AdminPlatformRepository` và một số repository khác. `IOrderRepository` cũng đọc Shop/Product/Variant/Promotion để phục vụ tạo Order. | Order tách riêng một phần; Shop chưa tách thành repository nghiệp vụ độc lập. Cần xác định ranh giới aggregate để tránh tách máy móc. |
| 6 | Supplier là User và phân quyền các vai trò | Có `UserRole` gồm Customer/Supplier/Admin, profile Supplier gắn với User, JWT role authorization và `[Authorize(Roles=...)]` trên các controller. Có quy trình duyệt Supplier. | Đã có cơ chế nền; cần audit toàn bộ endpoint, ownership và nhất quán giữa role trong token, trạng thái verified và shop active. |
| 7 | Transaction cho cập nhật nhiều bảng và rollback | Có transaction/commit cho các luồng như tạo Order, Payment, RFQ, Supplier, Contract; một số luồng có rollback catch rõ ràng (ví dụ Order/Payment). Các transaction được đặt trong từng Repository; chưa thấy UnitOfWork điều phối nhiều Repository. Một số luồng nhiều cập nhật cần kiểm tra xem mọi nhánh lỗi đều rollback/atomic hay chưa. | Đã có một phần quan trọng; cần audit theo use case và thống nhất cơ chế điều phối/rollback. |
| 8 | Logic tính toán đặt ở Service/lớp phù hợp | Phần lớn phép tính Order/RFQ nằm trong Service. Nhưng tính deposit nằm trong `ContractRepository`; một số repository còn tổng hợp số liệu báo cáo (đây có thể là projection truy vấn, cần phân biệt với quy tắc nghiệp vụ). | Đa số đúng hướng, cần chuyển quy tắc nghiệp vụ ra khỏi Repository và chuẩn hóa lớp tính tiền. |
| 9 | Request/Response DTO riêng cho API/nghiệp vụ | Có cây thư mục DTO Request/Response theo nghiệp vụ, và phần lớn Service API trả response riêng. Một số trường hợp còn dùng kiểu chung/đơn giản, service contract RFQ trả Entity Contract; cần rà soát toàn bộ endpoint và DTO nested/validation. | Đã có cấu trúc DTO đáng kể, nhưng chưa nhất quán hoàn toàn. |

## Bằng chứng đại diện đã kiểm tra

- `WebApi/Controllers/BaseApiController.cs`: có `[ApiController]`; `WebApi/Configurations/WebApiServiceCollectionExtensions.cs` cấu hình `InvalidModelStateResponseFactory`.
- `Application/Common/Utils/PhoneNumberUtils.cs`: Regex số điện thoại Việt Nam; `OrderService.cs`: Regex Idempotency-Key.
- DTO như `Application/DTOs/Orders/Requests/CreateOrderRequest.cs` và `DTOs/Users/Requests/CreateUserRequest.cs` có DataAnnotations, nhưng phone ở CreateUserRequest hiện chỉ có `MaxLength`, không có `[Phone]`/Regex tương ứng.
- `Application/Services/Implementations/OrderService.cs`: phép tính Order dùng `decimal` và rounding 2 chữ số với `AwayFromZero`, đồng thời service trực tiếp thao tác `Order`, `OrderItem`, `Shop`, `Product`, `ProductVariant`, `PromotionRule`.
- `Application/Services/Implementations/RfqService.cs` và `Application/Services/Interfaces/IRfqService.cs`: xử lý Domain Entity trực tiếp; `AwardBidAsync` trả `Contract`.
- `Application/Common/Interfaces/IOrderRepository.cs`: Repository interface nhận/trả Domain Entity. Không thấy interface GenericRepository, UnitOfWork hoặc `ShopRepository` trong cây source đã rà.
- `Infrastructure/Repositories/OrderRepository.cs`, `PaymentRepository.cs`, `RfqRepository.cs`, `SupplierRepository.cs`: có transaction cụ thể cho một số thao tác. `ContractRepository.cs` chứa phép tính deposit và có transaction cho các cập nhật hợp đồng.
- `Domain/Enums/UserRole.cs`, `WebApi/Controllers/SuppliersController.cs`, `SupplierPromotionsController.cs`, `AdminPlatformController.cs`: có vai trò và quy tắc authorize rõ ở một số endpoint.

## Kế hoạch sửa đề xuất

### Pha 1 — Chốt quy ước và lập danh sách endpoint/luồng

1. Lập danh mục DTO Request/Response và endpoint theo controller; ghi nhận endpoint dùng DTO riêng, endpoint còn trả kiểu không phù hợp.
2. Lập danh mục dữ liệu đầu vào có định dạng (email, phone, tax code, slug, SKU, URL, mã giao dịch/idempotency, tên đăng nhập nếu có); chốt quy tắc cho phép ký tự, độ dài và chuẩn hóa trước validate.
3. Chốt chính sách tiền tệ: kiểu `decimal`, đơn vị tiền, số chữ số lưu trữ/hiển thị, thời điểm làm tròn (đơn giá, từng dòng, tổng, chiết khấu, thuế, phí, deposit) và `MidpointRounding` thống nhất.
4. Chốt quy ước tầng: Controller nhận/trả DTO; Service xử lý input/output DTO và use case; lớp Domain giữ quy tắc/Entity nội bộ; Repository giữ truy cập persistence và trả projection/DTO nội bộ khi phù hợp. Không đưa DTO API vào Domain nếu gây phụ thuộc ngược.

### Pha 2 — Validation và DTO

1. Bổ sung Regex hoặc validation chuyên biệt cho định dạng cần kiểm tra; ưu tiên validator/attribute dùng lại thay vì rải Regex literal.
2. Bổ sung giới hạn/range và các kiểm tra quan hệ giữa trường; thống nhất lỗi validation 400 theo response envelope hiện có.
3. Hoàn thiện DTO Response riêng cho RFQ Award/Contract và rà soát tất cả endpoint không để Domain Entity lọt qua Service/API.
4. Tách Request DTO và Response DTO khỏi Domain; đảm bảo không nhận các trường nhạy cảm như role do client tự chỉ định ở luồng không được phép.

### Pha 3 — Quy tắc tiền và trường tính toán

1. Kiểm kê toàn bộ trường dẫn xuất và công thức: Order subtotal/discount/total, RFQ bid item/total, Contract deposit, payment amount/fee và báo cáo GMV.
2. Đưa công thức nghiệp vụ khỏi Repository; đặt trong Service hoặc calculator/domain policy phù hợp. Repository chỉ truy vấn/lưu và có thể project phép tổng hợp phục vụ đọc báo cáo khi đó là logic truy vấn.
3. Áp dụng chính sách rounding đã chốt, tránh dùng `double`/`float` cho tiền; kiểm tra overflow/range, giá trị âm và tổng các dòng.
4. Giữ phép tính nhất quán giữa giá trị lưu, API response, payment request và callback reconciliation.

### Pha 4 — Repository, Shop và Unit of Work

1. Tách `ShopRepository` cho các truy vấn/cập nhật thuộc Shop hiện đang nằm trong `AdminPlatformRepository` hoặc nơi không đúng trách nhiệm; giữ `OrderRepository` cho nghiệp vụ truy vấn/lưu Order và phối hợp tồn kho theo use case.
2. Xác định GenericRepository chỉ cho thao tác CRUD lặp lại, tránh ép các truy vấn chuyên biệt vào abstraction chung.
3. Bổ sung Unit of Work để gom commit của nhiều Repository trong cùng DbContext; hạn chế mỗi Repository tự `SaveChangesAsync` khi một use case cần cập nhật nhiều aggregate.
4. Định nghĩa rõ ownership của transaction và cách xử lý concurrency/idempotency để tránh transaction lồng nhau hoặc hai nơi cùng commit.

### Pha 5 — Transaction và RBAC

1. Vẽ các bước cập nhật cho Order, Payment/callback, RFQ award → Contract, Supplier review và các luồng Admin/audit.
2. Với từng use case, bảo đảm toàn bộ cập nhật DB nằm trong transaction; lỗi trước commit rollback; không báo thành công trước commit. Tác động ngoài DB (email, gateway) cần idempotency/outbox hoặc cơ chế bù trừ phù hợp.
3. Rà soát mọi endpoint theo Customer/Supplier/Admin, kiểm tra cả role và quyền sở hữu tài nguyên; xác minh Supplier đã verified/Shop active ở các thao tác cần điều kiện này.
4. Kiểm tra quy trình cập nhật role/profile để trạng thái DB và claim trong token có cách đồng bộ/refresh rõ ràng.

### Pha 6 — Xác minh sau khi được duyệt kế hoạch

1. Thực hiện lần lượt từng pha, cập nhật checklist và ghi rõ migration có phát sinh hay không.
2. Sau khi thay đổi cấu trúc, build solution và rà soát API contract; kiểm tra các ca rounding biên, validation sai định dạng, quyền truy cập và lỗi giữa transaction.
3. Không áp dụng migration/database update nếu chưa có yêu cầu riêng.

## Thứ tự ưu tiên đề xuất

1. Chốt DTO boundary và sửa Service contract trả Entity (đặc biệt RFQ Award).
2. Audit transaction theo các workflow nhiều bảng; bổ sung điều phối Unit of Work.
3. Chuẩn hóa tính tiền và đưa deposit calculation khỏi Repository.
4. Tách trách nhiệm Shop/Order và thêm GenericRepository ở phạm vi CRUD thực sự chung.
5. Bổ sung validation Regex/data annotations theo danh mục trường; rà soát RBAC/ownership toàn bộ endpoint.

## Ngoài phạm vi file kế hoạch này

- Không sửa source code, không tạo hoặc áp dụng migration, không thay đổi cấu hình/secret.
- Đây là rà soát tĩnh theo các file source được tìm thấy; chưa xác minh hành vi runtime, schema database hiện tại hoặc coverage đầy đủ của mọi nhánh.

## Tiến độ triển khai sau khi được duyệt (2026-10-28)

### Đã triển khai

- Thêm Regex định dạng phone nhất quán với chuẩn hóa +84/84/0 trên đăng ký, tạo/cập nhật User và số điện thoại nhận Order; sửa `Range` của decimal RFQ về giới hạn decimal rõ kiểu.
- Thêm `MoneyRounding` với quy tắc 2 chữ số `AwayFromZero`; Order, RFQ, và tính phần trăm deposit dùng helper dùng chung.
- Thêm GenericRepository EF base và áp dụng cho concrete repositories; thêm `IUnitOfWork` dùng chung DbContext, có transaction/retry/rollback; Admin cập nhật cấu hình và cập nhật User + audit/thu hồi refresh session qua UoW.
- Tách `ShopRepository`; Shop quản trị không còn nằm trong `AdminPlatformRepository`. `OrderRepository` giữ luồng đặt hàng/tồn kho.
- Chuyển Application contract và Service sang DTO cho Order, Payment, Contract, Promotion, User management, Supplier, Admin và Platform Bootstrap. RFQ Award giờ trả `ContractResponse`.
- Hoàn tất chuyển Auth và RFQ Service sang DTO; các interface persistence tương ứng không nhận/trả Domain Entity. Mapping Entity được giữ bên Infrastructure. Không còn `using Domain.Entities` trong `Services/Implementations`.
- Giữ transaction hiện hữu cho Order/Payment/RFQ/Supplier/Contract và bổ sung rollback tường minh ở các nhánh lỗi tồn kho/trạng thái của Order.

### Còn cần xác minh

- Build solution đã thành công. Cảnh báo `NU1900` phát sinh vì môi trường không truy cập được NuGet vulnerability feed; không có lỗi biên dịch.
- Đã rà soát tĩnh các controller có khai báo role Customer/Supplier/Admin và các Service kiểm tra ownership/verified supplier/shop active ở những luồng liên quan. Cần xác minh runtime để khẳng định hành vi phân quyền end-to-end.
- Chưa chạy test suite theo phạm vi xác minh hiện tại; chưa chạy database update và không tạo migration mới.
