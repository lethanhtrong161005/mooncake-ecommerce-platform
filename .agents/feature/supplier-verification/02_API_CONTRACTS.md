# [SUPPLIER-VERIFICATION] — API CONTRACTS

Trạng thái: **thiết kế đề xuất**, ngày 2026-10-24. Các endpoint ghi “mới” chưa có
trong source. Base URL lấy từ FE env; không đưa host/credential vào source.

## 1. Quy ước chung

- API version: `/api/v1`; JSON camelCase; enum string giữ tên BE:
  `Pending`, `Verified`, `Rejected`, `Revoked`.
- Success/error dùng `ApiResponse<T>` hiện có: status, message, data/errors,
  path, traceId, time. Không bắt FE parse message để quyết định nghiệp vụ.
- Đề xuất thêm **optional** `errorCode` ở cấp wrapper để FE dịch ổn định.
  Cần cập nhật wrapper/helper và contract test trước khi FE phụ thuộc field này;
  không tự coi field đã có. `errors` tiếp tục là danh sách string cho tương thích.
- Version profile là integer dương tăng dần; trước submission đầu là `0`.
  Write request thay đổi hồ sơ phải có `expectedVersion`, không lấy UpdatedAt làm version.
- Không dùng cả If-Match và body version trong phase này để tránh hai nguồn sự thật.
- Mỗi Supplier workflow write có `Idempotency-Key` UUID, scope actor + operation + resource. Cùng
  logical attempt retry dùng lại key; payload thay đổi dùng key mới. TTL đề xuất 24h.
- Key không đúng định dạng/thiếu: 400; cùng key khác payload: 409. Version sai: 409.
- Command response có `idempotentReplay` để FE biết replay; sau replay refetch self/detail
  vì trạng thái hiện tại có thể mới hơn response gốc.
- Không client-supplied ownerUserId, role, status, reviewer, submittedAt, FileUrl,
  PublicId hoặc cloud delivery type. DTO whitelist loại bỏ mass assignment.
- GET pháp lý/self/admin: `Cache-Control: private, no-store`. Không gửi signed URL
  trong danh sách hoặc lưu browser localStorage.

## 2. Ma trận quyền

| Actor thực tế trong DB | Self read/upload/apply | Admin list/detail/review/revoke | Public badge |
| --- | --- | --- | --- |
| Anonymous | 401 | 401 | Chỉ dữ liệu public từ shop API |
| Customer thuần | 403 onboarding | 403 | Như khách public |
| Customer + RequestedRole Supplier + email verified | Có, chỉ own profile và trạng thái cho phép | 403 | Không tự xác minh |
| Supplier Verified | Read/history; không apply thay thế hồ sơ đã duyệt | 403 | Badge từ dữ liệu server |
| Supplier bị reject/revoke, DB role Customer | Read/history; resubmit theo policy | 403 | Không badge |
| Admin active | Admin API; không tự nộp/duyệt thay owner | Có, cấm tự review/revoke chính mình | Không tự đổi badge phía FE |
| Account inactive/deleted | 403 | 403 dù JWT còn hạn | Không hiển thị shop bán mới |

## 3. Danh sách endpoint

| Method / path dưới `/api/v1` | Hiện trạng | Quyền | Mục đích |
| --- | --- | --- | --- |
| `GET /auth/me` | Mở rộng | Authenticated | Role DB + requestedRole + verification summary/capabilities |
| `GET /suppliers/me` | Sửa | Supplier onboarding owner | Self profile hoặc NotSubmitted |
| `POST /suppliers/me/documents` | Mới | Onboarding owner | Multipart upload private, trả document ID |
| `GET /suppliers/me/documents/{documentId}` | Mới | Owner | Trạng thái upload/kiểm tra nội dung trước apply |
| `DELETE /suppliers/me/documents/{documentId}` | Mới | Owner | Gỡ tài liệu staged chưa attach; không xóa lịch sử |
| `GET /suppliers/documents/{documentId}/content` | Mới | Owner hoặc Admin | Stream private qua BE, không fetch URL do client chỉ định |
| `POST /suppliers/me/apply` | Sửa request | Onboarding owner | Submit/resubmit snapshot |
| `GET /suppliers/me/history` | Mới | Owner | Submission và workflow history có phân trang |
| `GET /suppliers/applications` | Mới | Admin | Hàng đợi tất cả trạng thái, filter/search/page |
| `GET /suppliers/pending` | Hiện có, chuyển tiếp | Admin | Alias danh sách Pending có pagination contract mới |
| `GET /suppliers/{profileId}/verification` | Mới | Admin | Current profile + submission snapshot + metadata documents |
| `GET /suppliers/{profileId}/history` | Mới | Admin | Revision và quyết định/thay đổi trạng thái |
| `POST /suppliers/{profileId}/review` | Sửa request | Admin | Approve hoặc reject đúng submission/version |
| `POST /suppliers/{profileId}/revoke` | Mới | Admin | Thu hồi Verified, bắt buộc lý do |

REST notification APIs và hub `/hubs/supplier-verification` là phần bổ sung mới,
được đặc tả tại [06_REALTIME_NOTIFICATIONS.md](./06_REALTIME_NOTIFICATIONS.md).
Success command nói `notificationStatus=Queued`, không khẳng định đã push tới client.
Notification single-read là mutation trạng thái tự idempotent; ngoại lệ header key
được nêu tường minh trong contract realtime, không áp dụng cho review/revoke.

Không dùng generic `PATCH status` cho client tự truyền trạng thái bất kỳ.
`GET suppliers/me` hiện cho Admin là khả năng đọc own profile không hữu ích;
Admin sẽ dùng endpoint detail theo profileId có kiểm soát.

## 4. GET auth/me và self profile

Mở rộng các DTO thực sự dùng bởi login, refresh, Google login và getMe một cách
nhất quán; không chỉ sửa một UserResponse khác mà flow auth không sử dụng.
Current source auth user response mới có Id/Phone/FullName/Role.

```json
{
  "id": "af212d79-1c90-4558-97e8-68c12eec4a9c",
  "fullName": "Supplier account",
  "role": "Customer",
  "requestedRole": "Supplier",
  "supplierVerification": {
    "profileId": "c13ed165-b06a-449f-b26f-6b81f4bd4230",
    "onboardingState": "Pending",
    "verificationStatus": "Pending",
    "version": 3
  },
  "capabilities": {
    "canReadSupplierVerification": true,
    "canSubmitSupplierVerification": false,
    "canCreateShop": false,
    "canSubmitBid": false
  }
}
```

GET suppliers/me khi chưa gửi hồ sơ trả data:

```json
{
  "profileId": null,
  "version": 0,
  "onboardingState": "NotSubmitted",
  "verificationStatus": null,
  "currentSubmission": null,
  "stagedDocuments": [],
  "allowedActions": ["UploadDocument", "Submit"]
}
```

Khi đã có profile/submission, data có companyName/taxCode/address/description,
currentSubmission `{id, revision, status, submittedAtUtc, reviewedAtUtc,
rejectionReason, documents}`, lastVerificationDecision và revocation metadata
theo quyền. Staged documents và submitted documents tách riêng, tránh hiểu nhầm
file mới upload đã là tài liệu Admin đang xét.

## 5. Upload, trạng thái và đọc tài liệu

`POST /suppliers/me/documents`: `multipart/form-data` gồm `file`, `documentType`,
`expiresOn` nếu có. Axios để browser tự sinh multipart boundary. Không tự ghi
`Content-Type: application/json` cho request này; apiClient cần hỗ trợ override
header đúng cách mà vẫn giữ Bearer/timeout/error normalization.

Success 201 nếu file đã Ready; 202 nếu còn kiểm tra nội dung; data:

```json
{
  "documentId": "eb1ff4b2-b4bc-49d1-a7cf-87cbd30ec3ab",
  "documentType": "BusinessLicense",
  "originalFileName": "business-license.pdf",
  "contentType": "application/pdf",
  "fileSizeBytes": 820421,
  "uploadStatus": "Ready",
  "expiresOn": null
}
```

- Max policy áp dụng trước upload; không vượt storage config thực tế. Payload
  vượt giới hạn chặn cả ở server body limit và bộ đọc stream có bound.
- Upload retry cùng key trả cùng operation/document, không tạo thêm file nếu đã
  hoàn tất. Nếu upload đang chạy: 409 `DOCUMENT_UPLOAD_IN_PROGRESS` + retry hint.
- FE poll metadata endpoint chỉ khi Uploading; timeout UI không có nghĩa file Failed.
- DELETE chỉ soft-delete staged metadata thuộc owner; 409 nếu đã attach revision.
  Provider destroy/cleanup chỉ triển khai khi có adapter được kiểm thử; không giả
  định upload service hiện tại đã hỗ trợ xóa file.
- GET content trả byte stream, không bọc ApiResponse khi thành công. Lỗi trước
  khi stream vẫn dùng wrapper; lỗi khi đã bắt đầu stream phải hủy stream/log trace,
  không ghi JSON vào file đang trả. Dùng `Content-Disposition: attachment` mặc định,
  filename đã sanitize, `X-Content-Type-Options: nosniff`, no-store.
- FE tải bằng apiClient với `responseType=blob`, xử lý JSON lỗi từ blob an toàn,
  tạo object URL tạm và revoke sau dùng. PDF preview chỉ qua viewer sandbox phù hợp;
  không nhúng HTML/SVG trực tiếp. Không mở URL không kiểm soát trong iframe.

## 6. Submit/resubmit

`POST /suppliers/me/apply`, HTTP 200 để giữ status thành công hiện tại:

```json
{
  "expectedVersion": 0,
  "companyName": "An Nhien Bakery",
  "taxCode": "0312345678",
  "address": "Ho Chi Minh City",
  "description": "Enterprise mooncake supplier.",
  "documentIds": [
    "eb1ff4b2-b4bc-49d1-a7cf-87cbd30ec3ab",
    "3f3a50e6-d7c7-4aa0-8027-1f293e1b7543"
  ]
}
```

- Company 1–255, taxCode tối đa 50 + policy định dạng, address 1–500,
  description tối đa 2000; validate cả sau trim. GUID list không trùng, 1–10 file,
  đủ loại bắt buộc và tổng byte theo chính sách.
- expectedVersion=0 chỉ đúng lần đầu/shell chưa submit; server quyết định owner,
  revision, submittedAt và status.
- Từ chối khi Pending hoặc Verified. Resubmit hợp lệ từ Rejected/Revoked phải tạo
  revision mới, không ghi đè revision trước. Tài liệu dùng lại phải clone reference
  server-side sang row snapshot mới sau kiểm tra quyền/expiry, không attach một row
  đang thuộc hai submission; API phase đầu có thể yêu cầu upload lại rõ ràng.
- Client cũ gửi `documents[].fileUrl` phải nhận 400, không âm thầm nhận cả hai format.

Response data tối thiểu:

```json
{
  "profileId": "c13ed165-b06a-449f-b26f-6b81f4bd4230",
  "submissionId": "4be42eb3-23b2-4b83-9b1c-e604a1ca9348",
  "revision": 1,
  "version": 1,
  "verificationStatus": "Pending",
  "submittedAtUtc": "2026-10-24T03:00:00Z",
  "idempotentReplay": false
}
```

## 7. Admin list, detail và history

`GET /suppliers/applications?status=Pending&page=1&pageSize=20&sort=submittedAtAsc&q=An`

- page ≥1; pageSize 1–100; q trim, tối đa 100 ký tự; status allowlist, có thể bỏ
  để xem tất cả hồ sơ đã gửi. Không đưa shell chưa có submission vào hàng đợi.
- Sort allowlist `submittedAtAsc`, `submittedAtDesc`, `companyNameAsc`; luôn tie-break
  bằng profile ID. Không truyền cột SQL thẳng từ query string.
- Summary: profileId, submissionId, revision, version, companyName, taxCode,
  status, submittedAtUtc, reviewedAtUtc, documentCount. Không file URL hoặc blob.
- `data={items, page, pageSize, totalCount}`; list 2 query mục tiêu, không tải
  detail theo từng dòng. totalCount/page có thể lệch nhẹ khi concurrent submit;
  chấp nhận cho UI queue, không dùng tổng này để quyết định duyệt.
- Detail endpoint trả snapshot revision hiện tại và metadata document có đủ ID.
  History trả page/pageSize, items có eventId/type/actor display/UTC/revision/reason
  theo quyền. Tối đa 100/event page; không tải toàn bộ audit lúc mở bảng.
- Các link history/detail không thay thế ownership checks tại file content API.

## 8. Review và revoke

`POST /suppliers/{profileId}/review`:

```json
{
  "submissionId": "4be42eb3-23b2-4b83-9b1c-e604a1ca9348",
  "expectedVersion": 7,
  "approve": false,
  "rejectionReason": "The submitted certificate is incomplete."
}
```

- Approve bỏ rejectionReason; reject bắt buộc reason 10–1000 sau trim.
- `approve` phải được gửi tường minh: dùng nullable bool có Required/validator
  presence; bool non-nullable mặc định false không phân biệt được field bị bỏ.
- Sai submissionId/current revision/status/version đều 409; ID không tồn tại 404.
- Approve kiểm tra lại các tài liệu bắt buộc, Ready, chưa hết hạn; không chỉ có tick FE.
- `reviewedByUserId`, thời gian và trạng thái document do BE quyết định.

`POST /suppliers/{profileId}/revoke`:

```json
{
  "expectedVersion": 8,
  "reason": "The verification evidence requires re-assessment."
}
```

Chỉ Verified → Revoked; reason bắt buộc; trả version mới, revokedAtUtc,
verificationStatus và notificationStatus Queued. `lastVerifiedAtUtc` vẫn truy vết
được; không làm lịch sử trông như Supplier chưa từng được xác minh.

## 9. Mã lỗi và hành vi FE

| HTTP | errorCode đề xuất | Xử lý FE |
| --- | --- | --- |
| 400 | `SUPPLIER_INVALID_REQUEST` | Gắn lỗi field nếu map được; giữ form |
| 401 | `AUTHENTICATION_REQUIRED` | apiClient refresh một lần; thất bại thì về đăng nhập |
| 403 | `SUPPLIER_ONBOARDING_FORBIDDEN` / `SUPPLIER_OPERATION_FORBIDDEN` | Refetch quyền; không retry mutation tự động |
| 404 | `SUPPLIER_RESOURCE_NOT_FOUND` | Không phân biệt file không tồn tại với file không thuộc owner |
| 409 | `SUPPLIER_VERSION_CONFLICT` | Cảnh báo hồ sơ thay đổi, giữ lý do đang nhập, tải detail mới |
| 409 | `SUPPLIER_ALREADY_PENDING` / `SUPPLIER_ALREADY_VERIFIED` | Refetch trạng thái, dừng submit |
| 409 | `SUPPLIER_TAX_CODE_IN_USE` | Báo MST không khả dụng, không lộ chủ sở hữu |
| 409 | `IDEMPOTENCY_PAYLOAD_MISMATCH` | Tạo logical attempt mới sau khi người dùng sửa form |
| 409 | `DOCUMENT_NOT_READY` / `DOCUMENT_ALREADY_ATTACHED` | Chờ kiểm tra hoặc chọn file khác |
| 413 / 415 | `DOCUMENT_TOO_LARGE` / `DOCUMENT_TYPE_UNSUPPORTED` | Lỗi tại Upload, không mất form |
| 429 | `SUPPLIER_RATE_LIMITED` | Tôn trọng Retry-After, hiện countdown |
| 503 | `DOCUMENT_STORAGE_UNAVAILABLE` | Giữ form, retry upload theo operation key |
| 500 | `INTERNAL_ERROR` | Message chung + traceId; không báo thành công giả |

Lỗi nghiệp vụ chỉ map sau authorization. Không để tax-code/file validation oracle
cho anonymous. Tránh trả Postgres constraint name hoặc stack trace.

## 10. Rate limit đề xuất và tương thích

| Policy | Giới hạn khởi đầu cần load-test |
| --- | --- |
| `supplier-submit` | 5 lần/giờ/account; tính logical attempts, replay không tạo hồ sơ |
| `supplier-upload` | 20 lần/giờ/account + quota bytes; 2 upload đồng thời/account |
| `supplier-review` | 60 command/phút/Admin; chống accidental bulk click |
| `supplier-private-read` | 60 file/phút/actor, theo quota bandwidth |
| `supplier-query` | 120 request/phút/actor; UI polling không vượt ngưỡng |

Giới hạn phân tán bằng gateway/store chia sẻ khi nhiều instance; lưu ý IP chung
của doanh nghiệp, không khóa cả doanh nghiệp chỉ vì một account. Giới hạn body,
timeout, cancellation và Retry-After phải đồng bộ proxy/WebApi/storage.

Rollout là thay đổi contract có phối hợp: triển khai BE additive endpoints trước,
FE mới sau, rồi tắt request FileUrl cũ. `/pending` thay response list → paged object
cần compatibility adapter hoặc FE/BE release đồng bộ; không âm thầm đổi response
cho consumer chưa kiểm kê. Không ghi key/phiên/token thật trong tài liệu ví dụ.
# Bổ sung contract đã triển khai: snapshot lịch sử

### Customer hiện có đăng ký mở gian hàng

`POST /api/v1/suppliers/me/onboarding`, body `{}`. Yêu cầu đăng nhập, tài khoản active, email đã xác minh, role Customer hoặc Supplier. Không nhận userId/role do client chỉ định. Trả AuthenUserResponse trong ApiResponse chuẩn, cập nhật `requestedRole=Supplier` và audit cùng transaction khóa User. Role thực tế vẫn Customer đến khi hồ sơ được duyệt. Gọi lại có hiệu lực idempotent, không nhân audit hoặc reset trạng thái hồ sơ đã có. Admin, tài khoản bị khóa/xóa hoặc chưa xác minh email nhận 403. Không tạo submission hay outbox gửi duyệt tại bước đăng ký ý định này.

- `GET /api/v1/suppliers/me/submissions/{submissionId}`: chủ hồ sơ xem revision của mình.
- `GET /api/v1/suppliers/{profileId}/submissions/{submissionId}`: Admin xem revision thuộc đúng hồ sơ.
- Trả snapshot doanh nghiệp và metadata tài liệu theo quyền hiện tại; không trả storage URL. Truy cập tài liệu vẫn qua endpoint content có kiểm tra quyền. Submission khác chủ/khác profile không được trả dữ liệu.

