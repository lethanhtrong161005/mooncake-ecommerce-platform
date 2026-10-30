# [MODULE-11] MA TRẬN SỰ KIỆN THÔNG BÁO XUYÊN SUỐT TOÀN SÀN B2B (CROSS-MODULE EVENT CATALOG)
## Tài Liệu Thiết Kế Chi Tiết (Detail Design Document - Part 03)

---

## I. TỔNG QUAN VÀ MỤC TIÊU NGHIỆP VỤ

### 1.1. Thông tin tài liệu
- **Tên tài liệu:** Danh mục Sự Kiện & Ma Trận Phân Phối Thông Báo Toàn Sàn B2B (Cross-Module Event Catalog & Dispatch Matrix)
- **Mã tính năng:** `cross-module-event-notifications`
- **Phiên bản:** `1.0.0`
- **Trạng thái:** `Approved`

### 1.2. Mục tiêu nghiệp vụ
Hệ thống sàn giao dịch B2B Nguyệt Phố bao gồm chuỗi liên hoàn 10 phân hệ nghiệp vụ. Mục tiêu của tài liệu này là chuẩn hóa toàn bộ các điểm kích hoạt sự kiện (Event Triggers), đối tượng thụ hưởng thông báo, cấp độ khẩn cấp (Severity), mẫu nội dung tiếng Việt trang trọng chuẩn văn hóa biếu tặng doanh nghiệp, và đường dẫn điều hướng trực tiếp (Action Deeplink) cho từng sự kiện.

---

## II. MA TRẬN DANH MỤC SỰ KIỆN TOÀN BỘ 10 PHÂN HỆ

| Phân hệ nguồn | Mã sự kiện (`eventType`) | Nhóm (`category`) | Cấp độ (`priority`) | Người nhận (`recipient`) | Tiêu đề mẫu (Tiếng Việt) | Mẫu nội dung hiển thị | Action URL (Deeplink) |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **M1 & M2: Định danh & Xác minh** | `supplier_verified` | `system` | `high` | Supplier | Hồ sơ doanh nghiệp đã được phê duyệt! | Chúc mừng! Hồ sơ năng lực pháp lý xưởng bánh của bạn đã được Admin duyệt. Gian hàng đã sẵn sàng mở bán. | `/#/gian-hang` |
| | `supplier_rejected` | `system` | `high` | Supplier | Hồ sơ năng lực cần bổ sung thông tin | Hồ sơ xác minh xưởng bánh chưa đạt yêu cầu: {reason}. Quý đối tác vui lòng cập nhật lại giấy tờ. | `/#/xac-minh-ncc` |
| **M3: Gian hàng & Catalog** | `shop_suspended` | `system` | `urgent` | Supplier | Gian hàng tạm thời bị tạm dừng hoạt động | Gian hàng của bạn đã bị tạm dừng do vi phạm chính sách: {reason}. Vui lòng liên hệ Admin để được hỗ trợ. | `/#/gian-hang` |
| **M4: Đặt hàng B2B (Bulk Order)** | `order_placed` | `order` | `high` | Supplier | Đơn đặt hàng số lượng lớn mới | Khách hàng {customerName} vừa đặt đơn bánh {orderNumber} với số lượng {totalQuantity} hộp. Vui lòng xác nhận đơn! | `/#/dat-hang-thiet-ke?tab=orders` |
| | `order_confirmed` | `order` | `medium` | Customer | Đơn hàng đã được xưởng xác nhận | Xưởng {shopName} đã xác nhận đơn hàng {orderNumber}. Đơn đang được chuẩn bị nguyên liệu sản xuất. | `/#/dat-hang-thiet-ke?tab=orders` |
| | `packaging_mockup_ready` | `order` | `high` | Customer | Bản thiết kế hộp bánh đã sẵn sàng để duyệt | Xưởng {shopName} vừa tải lên bản thiết kế (Mockup) hộp in logo cho đơn {orderNumber}. Mời quý khách vào duyệt mẫu! | `/#/dat-hang-thiet-ke?tab=mockup` |
| **M5: Yêu cầu Báo giá (RFQ & Bidding)** | `rfq_invitation_received` | `rfq` | `medium` | Supplier | Lời mời tham gia chào thầu RFQ mới | Doanh nghiệp {customerName} mời xưởng tham gia báo giá RFQ {rfqNumber} cho {quantity} hộp bánh Trung thu cao cấp. | `/#/rfq-dau-thau?tab=invitations` |
| | `bid_submitted` | `rfq` | `medium` | Customer | Nhà cung cấp vừa gửi báo giá chào thầu | Xưởng {shopName} đã gửi hồ sơ báo giá {bidNumber} cho yêu cầu RFQ của bạn. Giá chào: {bidPrice:N0} ₫. | `/#/rfq-dau-thau?tab=bids` |
| | `bid_accepted_won` | `rfq` | `high` | Supplier | Chúc mừng! Báo giá của bạn đã trúng thầu | Doanh nghiệp {customerName} đã chấp thuận báo giá {bidNumber}. Hệ thống đang tự động khởi tạo dự thảo Hợp đồng! | `/#/hop-dong-tien-do` |
| | `bid_rejected_lost` | `rfq` | `low` | Supplier | Thông báo kết quả chào thầu RFQ | Yêu cầu báo giá {rfqNumber} đã hoàn tất lựa chọn đối tác. Cảm ơn quý xưởng đã tham gia chào thầu. | `/#/rfq-dau-thau` |
| **M6: Hợp đồng điện tử & Ký số** | `contract_draft_ready` | `contract` | `high` | Customer, Supplier | Dự thảo hợp đồng điện tử mới cần ký kết | Dự thảo hợp đồng kinh tế {contractNumber} đã được hệ thống tạo lập từ kết quả thầu. Vui lòng ký số điện tử qua mã OTP. | `/#/hop-dong-tien-do?id={id}` |
| | `contract_signed_otp` | `contract` | `high` | Customer, Supplier | Hợp đồng đã được ký số thành công | Cả hai bên đã hoàn tất ký kết hợp đồng điện tử {contractNumber}. Bước tiếp theo: Thanh toán tiền cọc mốc M1 để kích hoạt sản xuất. | `/#/hop-dong-tien-do?id={id}` |
| | `milestone_due_warning` | `contract` | `high` | Supplier | Cảnh báo: Sắp đến hạn hoàn thành mốc {milestoneName} | Mốc tiến độ {milestoneName} của Hợp đồng {contractNumber} sẽ đến hạn trong vòng {daysLeft} ngày nữa. Vui lòng cập nhật tiến độ! | `/#/hop-dong-tien-do?id={id}` |
| | `milestone_overdue_breach` | `contract` | `urgent` | Supplier, Admin | Cảnh báo vi phạm: Mốc tiến độ đã quá hạn! | Mốc {milestoneName} thuộc Hợp đồng {contractNumber} đã quá hạn hoàn thành. Hệ thống tự động ghi nhận cảnh báo vi phạm hợp đồng! | `/#/hop-dong-tien-do?id={id}` |
| **M7: Logistics & Giao vận PoD** | `delivery_dispatched` | `delivery` | `medium` | Customer | Lô bánh Trung thu đang trên đường vận chuyển | Chuyến xe vận chuyển lô hàng thuộc Hợp đồng {contractNumber} đã xuất phát. Mã vận đơn: {trackingCode}. | `/#/giao-nhan-pod` |
| | `delivery_pod_uploaded` | `delivery` | `high` | Customer | Đã giao hàng thành công & Có ảnh bằng chứng PoD | Đơn vị vận chuyển đã giao hàng thành công. Ảnh chụp bàn giao thực tế và chữ ký người nhận (PoD) đã sẵn sàng để kiểm tra! | `/#/giao-nhan-pod?id={id}` |
| **M8: Đánh giá & Điểm uy tín** | `review_reminder` | `review` | `low` | Customer | Mời đánh giá chất lượng sản phẩm & dịch vụ | Lô bánh Trung thu của bạn đã được giao thành công. Hãy dành 1 phút để đánh giá chất lượng bánh và tiến độ giao hàng của xưởng nhé! | `/#/danh-gia-uy-tin?refId={id}` |
| | `reputation_score_boosted` | `system` | `high` | Supplier | Điểm uy tín xưởng bánh của bạn đã tăng lên! | Bạn vừa được cộng {points} điểm uy tín nhờ hoàn thành hợp đồng đúng hạn và nhận được đánh giá 5 sao. | `/#/danh-gia-uy-tin` |
| **M9: Trợ lý AI & Báo cáo** | `ai_analytics_ready` | `analytics` | `low` | Admin, Supplier | Báo cáo phân tích doanh thu AI đã sẵn sàng | Báo cáo xu hướng tiêu thụ bánh Trung thu cao cấp và phân tích dự báo nhu cầu thị trường do Gemini AI tổng hợp đã hoàn tất. | `/#/quan-tri?tab=ai-analytics` |
| **M10: Thanh toán & Ký quỹ Escrow** | `payment_vnpay_success` | `payment` | `high` | Customer, Supplier, Admin | Thanh toán VNPay Sandbox thành công | Giao dịch {paymentNumber} trị giá {amount:N0} ₫ đã hoàn tất qua VNPay Sandbox. Tiền đã vào tài khoản Ký quỹ an toàn. | `/#/thanh-toan/ket-qua?txn={txn}` |
| | `escrow_deposit_locked` | `payment` | `high` | Supplier | Tiền cọc Hợp đồng đã được ký quỹ an toàn | Khách hàng đã ký quỹ thành công số tiền cọc {amount:N0} ₫ tại sàn Nguyệt Phố. Hợp đồng chuyển sang trạng thái HOẠT ĐỘNG. | `/#/hop-dong-tien-do?id={id}` |
| | `escrow_milestone_released` | `payment` | `high` | Supplier | Giải ngân tiền thanh toán mốc {milestoneName} | Khách hàng đã nghiệm thu lô hàng. Số tiền ký quỹ {amount:N0} ₫ của mốc {milestoneName} đã được giải ngân về ví của quý xưởng. | `/#/hop-dong-tien-do?id={id}` |
| | `escrow_refund_approved` | `payment` | `high` | Customer, Supplier | Thông báo phê duyệt hoàn tiền cọc ký quỹ | Yêu cầu hoàn tiền cọc cho Hợp đồng {contractNumber} với số tiền {amount:N0} ₫ đã được Quản trị viên sàn phê duyệt và chuyển khoản trả lại. | `/#/hop-dong-tien-do?id={id}` |

---

## III. QUY TẮC ĐỊNH DẠNG & LỰA CHỌN KÊNH PHÂN PHỐI (CHANNEL DISPATCH RULES)

### 3.1. Phân cấp mức độ ưu tiên (Notification Priorities)
- 🔴 **Urgent (Khẩn cấp):** Kèm hiệu ứng rung/âm thanh báo động cao độ, ghim trên cùng thanh thông báo, đồng thời phát Web Push nếu người dùng cho phép (VD: Quá hạn hợp đồng vi phạm chế tài, nghi vấn gian lận thanh toán, đình chỉ gian hàng).
- 🟠 **High (Quan trọng):** Kèm âm thanh chuông báo chuẩn, tự động mở toast popup 5 giây ở góc màn hình (VD: Thanh toán thành công, có đơn hàng mới, trúng thầu, giao hàng có ảnh PoD).
- 🟡 **Medium (Bình thường):** Hiển thị toast nhẹ nhàng, cập nhật badge chuông (VD: Đơn hàng đã xác nhận, mời tham gia chào thầu, lô hàng bắt đầu giao).
- ⚪ **Low (Nhắc nhở):** Không gây gián đoạn màn hình, chỉ âm thầm tăng số đếm badge (VD: Lời nhắc đánh giá sau 3 ngày, báo cáo phân tích AI sẵn sàng).

### 3.2. Đa kênh phân phối (Multi-Channel Dispatch Strategy)
Mỗi sự kiện được điều hướng qua 3 kênh dựa trên Cài đặt người dùng (`NotificationPreferences`):
1. **In-App Real-time (Mặc định bắt buộc):** Luôn gửi qua WebSocket SignalR Hub tới tất cả các phiên đăng nhập đang hoạt động của người dùng.
2. **Web Push Notification (Trình duyệt):** Gửi qua Web Push API nếu trình duyệt cấp quyền (Notification Permission: `granted`) ngay cả khi người dùng đang chuyển tab khác hoặc ẩn trình duyệt.
3. **Email Fallback (Mock Service):** Tự động gửi email thông báo chi tiết đối với các sự kiện mức `high` và `urgent` (đặc biệt là hóa đơn thanh toán VNPay và hợp đồng ký kết).

---

## IV. TỔ CHỨC CƠ CHẾ EVENT BUS NỘI BỘ TRONG CLEAN ARCHITECTURE

Để đảm bảo các phân hệ (Orders, RFQ, Contracts, Deliveries, Payments) không bị phụ thuộc vòng (Circular Dependency) vào `NotificationService`, hệ thống sử dụng **MediatR / Domain Event Pattern**:
1. Khi một sự kiện nghiệp vụ xảy ra, Service nguồn phát ra một Notification Domain Event (VD: `ContractMilestonePaidDomainEvent`).
2. Tầng Application đăng ký các `INotificationHandler<TEvent>` tương ứng:
   ```csharp
   public sealed class ContractMilestonePaidEventHandler : INotificationHandler<ContractMilestonePaidDomainEvent>
   {
       private readonly INotificationService _notificationService;
       private readonly IRealtimeNotificationPublisher _realtimePublisher;
       
       public async Task Handle(ContractMilestonePaidDomainEvent notification, CancellationToken ct)
       {
           // 1. Tạo bản ghi thông báo bền vững cho Khách hàng & Nhà cung cấp
           await _notificationService.CreateAndDispatchAsync(...);
       }
   }
   ```
3. Đảm bảo tính mở rộng cao (Open-Closed Principle): Thêm sự kiện mới từ bất kỳ phân hệ nào không làm thay đổi kiến trúc thông báo lõi.

