# B2B Ordering — Implementation Result

## Phạm vi đã hiện thực

- Bulk order dùng `Idempotency-Key`, kiểm tra variant/shop/số lượng/tồn kho và ghi movement tồn kho trong cùng transaction.
- Quote preview do server tính, chọn promotion hợp lệ có giá thấp nhất và không nhận giá từ client.
- Promotion lưu snapshot `PromotionRuleId` trên từng dòng đơn và chặn bậc giá active bị chồng khoảng.
- Custom packaging nhiều vòng: customer tạo brief, supplier gửi revision, customer duyệt hoặc yêu cầu sửa.
- Phí thiết kế chỉ cộng vào đơn khi revision được duyệt; thao tác review được khóa transaction để không cộng phí hai lần.
- Đơn có packaging chưa duyệt bị chặn chuyển sang `Processing`; packaging chuyển `InProduction` và `Completed` theo vòng đời đơn.
- Customer UI và supplier UI đã thay mock data bằng API thật.

## Endpoint hoàn chỉnh

| Method | Endpoint | Quyền |
| --- | --- | --- |
| `POST` | `/api/v1/orders/quote-preview` | Public |
| `POST` | `/api/v1/orders` | Customer |
| `GET` | `/api/v1/orders/mine` | Customer |
| `GET` | `/api/v1/orders/supplier` | Supplier |
| `POST` | `/api/v1/orders/{id}/confirm` | Supplier |
| `POST` | `/api/v1/orders/{id}/reject` | Supplier |
| `POST` | `/api/v1/orders/{id}/processing` | Supplier fulfillment |
| `POST` | `/api/v1/orders/{id}/ship` | Supplier fulfillment |
| `POST` | `/api/v1/orders/{id}/delivery/confirm` | Customer |
| `POST` | `/api/v1/orders/{id}/custom-packaging` | Customer |
| `GET` | `/api/v1/orders/{id}/custom-packaging` | Customer/Supplier |
| `POST` | `/api/v1/orders/{id}/custom-packaging/revisions` | Supplier fulfillment |
| `POST` | `/api/v1/orders/{id}/custom-packaging/revisions/{revisionId}/review` | Customer |

## Migration

- `20261029041407_B2BOrderingWorkflow`
- Thêm `orders.packaging_fee numeric(12,2) NOT NULL DEFAULT 0`.
- Mở rộng enum `custom_packaging_status` với `RevisionRequested` và `Completed`.
- Migration được tạo nhưng không tự động chạy vào database môi trường dùng chung.

## Kiểm thử ngày 2026-10-29

- `dotnet build Mooncake.EcommercePlatform.sln --no-restore`: pass.
- Unit tests mới `PromotionPricingHelperTests`, `OrderQuotePreviewTests` và `CustomPackagingServiceTests`: 9/9 pass.
- Full BE suite: 71 pass, 24 skip; 1 test cũ ngoài module fail do parse decimal `RangeAttribute` phụ thuộc culture trong `ShopPolicyTests`.
- Targeted ESLint cho các file B2B FE: pass.
- `npm run build`: pass; Vite chỉ cảnh báo kích thước chunk.

## Ghi chú triển khai

1. Rà SQL migration trên database staging trước khi chạy `dotnet ef database update`.
2. Không downgrade enum PostgreSQL khi đã có dữ liệu dùng trạng thái mới.
3. Logo và mockup hiện nhận URL HTTPS theo API contract; quy trình upload asset riêng cần trả URL trước khi gọi packaging API.
