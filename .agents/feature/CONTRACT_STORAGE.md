# CONTRACT-STORAGE — Lưu file hợp đồng trên Supabase Storage

## 1. Mục tiêu và phạm vi

- Lưu file PDF của hợp đồng trong bucket private `contracts`.
- Chỉ Customer hoặc Supplier thuộc hợp đồng mới được upload và lấy liên kết tải.
- Secret key chỉ tồn tại ở Backend; Frontend luôn gọi Mooncake WebApi bằng JWT của hệ thống.
- File tối đa 10 MB, MIME `application/pdf`, phần mở đầu phải là `%PDF-`.
- Cho phép thay file khi hợp đồng còn `Draft`. Sau khi gửi ký, file bị khóa.
- Lưu object path vào `contracts.signed_document_url` và SHA-256 vào `contracts.signed_document_sha256`; không lưu signed URL hết hạn vào DB.

## 2. Cấu hình

`appsettings.json` bị Git ignore:

```json
{
  "SupabaseStorage": {
    "ProjectUrl": "https://<project-ref>.supabase.co",
    "PublishableKey": "<publishable-key>",
    "SecretKey": "<secret-key-chỉ-dùng-BE>",
    "ContractBucket": "contracts",
    "MaxContractFileBytes": 10485760,
    "SignedUrlLifetimeSeconds": 300
  }
}
```

Bucket đã cấu hình private, `allowed_mime_types = ["application/pdf"]`, `file_size_limit = 10485760`. Backend dùng secret key để gọi Storage API. Theo mô hình này không cần policy upload công khai; không được chuyển secret key sang `mooncake-fe`.

## 3. API contract

| Method | Endpoint | Body/Kết quả |
| --- | --- | --- |
| `POST` | `/api/v1/contracts/{contractId}/document` | `multipart/form-data`, field `file`; upload/thay PDF khi Draft |
| `GET` | `/api/v1/contracts/{contractId}/document` | Signed download URL có hiệu lực 300 giây và SHA-256 |
| `POST` | `/api/v1/contracts/{contractId}/submit-for-signature` | Chỉ thành công khi đã có PDF |

Ví dụ upload:

```bash
curl -X POST "http://localhost:8089/api/v1/contracts/{contractId}/document" \
  -H "Authorization: Bearer <access-token>" \
  -F "file=@contract.pdf;type=application/pdf"
```

Response tải file:

```json
{
  "status": 200,
  "message": "Đã tạo liên kết tải hợp đồng.",
  "data": {
    "downloadUrl": "https://<project>.supabase.co/storage/v1/object/sign/...",
    "expiresAtUtc": "2026-10-19T10:05:00Z",
    "sha256": "..."
  }
}
```

## 4. Bảo mật và tính toàn vẹn

- Bucket private; không trả public URL.
- BE kiểm tra quyền sở hữu hợp đồng trước mọi thao tác.
- Object path dùng contract UUID và UUID ngẫu nhiên, không dùng tên file do người dùng gửi.
- Mỗi lần thay file tạo object mới. DB cập nhật thành công rồi mới xóa object cũ.
- Nếu DB cập nhật thất bại, BE dọn object vừa upload.
- Signed URL sống 5 phút. SHA-256 được lưu để đối chiếu tài liệu khi ký.
- Supabase secret key bỏ qua RLS nên chỉ được giữ ở Backend và secret manager của môi trường deploy.

## 5. Kiểm thử

- Unit test: PDF hợp lệ, nội dung giả PDF, thay object cũ, signed URL, chặn gửi ký khi chưa có file.
- Storage smoke test: upload PDF thử, tạo signed URL, tải đúng từng byte và xóa object thử.
- Bucket kiểm tra là private, giới hạn 10 MB và chỉ nhận `application/pdf`.

Tài liệu tham khảo chính thức: [Supabase Storage access control](https://supabase.com/docs/guides/storage/security/access-control), [private buckets](https://supabase.com/docs/guides/storage/buckets/fundamentals), [standard uploads](https://supabase.com/docs/guides/storage/uploads/standard-uploads).
