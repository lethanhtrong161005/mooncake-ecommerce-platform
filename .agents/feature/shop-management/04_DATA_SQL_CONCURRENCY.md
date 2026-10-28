# Thiết kế dữ liệu, SQL và tính nhất quán

Plan only, 2026-10-25. Các bảng/index/SQL dưới đây là thiết kế cần chuyển thành migration và test sau này, không phải đã chạy trên DB dự án.

## 1. Quy ước identity và DTO

`shops.supplier_id` phải tham chiếu `supplier_profiles.id`, không phải users.id. `supplier_profiles.user_id` mới là owner account. OrderRepository, PromotionRepository và RfqRepository hiện theo profile ID; AdminPlatformRepository metrics có join profile.UserId sai, cần chuyển về profile.Id.

Không sửa dữ liệu chỉ dựa vào tên cột hoặc comment. Preflight tìm orphan, trường hợp supplier_id trùng một User ID mà không trùng Profile ID, và trường hợp một UUID khớp cả hai loại nhưng khác owner. Chỉ remap theo bảng đối chiếu đã review; không fallback join `profile.Id = supplier_id OR profile.UserId = supplier_id` ở runtime vì có thể trả sai tenant.

Repository input/output là các persistence DTO ở 03. `ShopStateDto` là snapshot business immutable gồm current version, status, profile/owner flags, appearance/current revision. EF tracking và entity instances chỉ nằm trong Infrastructure. Conditional writes trả affected-row/result DTO, service giải thích business conflict.

## 2. Schema mở rộng đề xuất

### 2.1. shops

Giữ table, PK và các cột hiện có, không tạo bảng shop cạnh tranh.

| Cột mới/điều chỉnh | Kiểu/ràng buộc |
| --- | --- |
| supplier_id | Unique toàn bộ vòng đời như `shops_supplier_id_key`; FK supplier_profiles(id), ON DELETE RESTRICT |
| owner_user_id | UUID NOT NULL, backfill theo profile; composite FK (supplier_id,owner_user_id) → supplier_profiles(id,user_id) để chống sai owner |
| version | bigint NOT NULL default 1, CHECK >0, EF concurrency token; không dùng timestamp làm optimistic token |
| template_revision_id | UUID NOT NULL sau backfill; composite FK (template_id,template_revision_id) → shop_template_revisions(template_id,id) |
| name / slug | name NOT NULL <=255; slug <=80 lowercase regex + canonical uniqueness; preflight trước thu hẹp độ dài |
| public_contact_email / public_contact_phone / public_address | varchar(255)/varchar(20)/varchar(500), nullable, opt-in public |
| logo_asset_id / banner_asset_id | UUID nullable FK shop_media_assets(id); composite FK (asset_id,owner_user_id) → (id,owner_user_id) bảo đảm cùng owner |
| pre_suspension_status | shop_status nullable, chỉ Active/Inactive khi Suspended |
| suspended_at_utc / suspended_by_user_id / suspension_reason | timestamptz/UUID/text nullable; giữ lịch sử đầy đủ trong audit |
| custom_colors | Giữ JSONB, NOT NULL default '{}', CHECK jsonb_typeof='object'; service serialize typed tokens |
| custom_css | Giữ dữ liệu legacy để rollback/review, API mới không đọc ra hoặc execute; không nhận write field này |

CHECK trạng thái suspension nhất quán: Suspended cần pre_suspension_status hợp lệ, timestamp, actor, reason; ngoài Suspended thì current suspension fields null. Restore ghi audit old/current reason trước khi clear current fields. Admin actor FK RESTRICT, không hard-delete user làm mất audit.

Không đổi PostgreSQL enum thành chữ thường: API và enum DB hiện đang Active/Inactive/Suspended. Business dùng chữ thường chỉ là mô tả. Max-length constraints phải đồng bộ DTO–EF–DB.

### 2.2. shop_templates và revisions

| Thành phần | Thiết kế |
| --- | --- |
| shop_templates | Thêm code varchar(50) unique, version bigint >0, is_premium bool default false, current_revision_id UUID nullable lúc tạo head; metadata name/description/preview_asset_id/sort_order/is_active hiện hữu |
| shop_template_revisions | id UUID PK, template_id FK RESTRICT, revision int >0, renderer_key varchar(50), schema_version int=1, css_variables JSONB object, layout_config JSONB object, created_at/by |
| Revision uniqueness | UNIQUE(template_id,revision), UNIQUE(template_id,id) để FK cùng template |
| Head consistency | Composite FK (id,current_revision_id) → revisions(template_id,id), DEFERRABLE INITIALLY DEFERRED; active head phải có revision |
| shop_template_catalog_state | Singleton id=1 CHECK, default_template_id FK, version bigint; guard row serialize thay đổi visibility/default và minimum count |

Head được insert Inactive/null revision trước, thêm revision, trỏ head và activate trong transaction seed/publish phù hợp. Không cho endpoint sửa/xóa row revision; deploy app role không có update/delete grant cho table revision nếu vận hành cho phép tách quyền. Existing css_variables/layout_config trên head là legacy: source-of-truth mới là pinned revision, không dual-write hai nguồn sống lâu dài.

Tối thiểu 5 active/selectable templates, một safe default active không được ẩn. Guard row phải được seed trước khi API template được bật; thiếu guard fail closed. Hai Admin cùng ẩn mẫu phải được serialize trước count, không chỉ đọc `COUNT>=5` rồi update riêng.

### 2.3. Media và idempotency phạm vi shop

`shop_media_assets`: UUID PK, owner_user_id FK users, purpose enum/check Logo/Banner/TemplatePreview, upload_operation_id UUID, request_hash/SHA256 char(64), status Uploading/Ready/Failed/Quarantined, provider_public_id/resource_type/delivery_type, public_url, width/height/byte_count/content_type, created/updated UTC, is_deleted. UNIQUE(owner_user_id,upload_operation_id), UNIQUE(id,owner_user_id). Không dùng bảng legal documents; không coi URL tự gửi là ownership proof.

Storage-file service hiện có vẫn chỉ upload, không truy cập DB. Feature shop sở hữu metadata này qua repository DTO; không biến storage service thành generic media platform. Branding public được thông báo rõ trước upload; không đưa giấy tờ GPKD/ATTP vào public folder. Unattached public image không là tài liệu mật; URL CDN có thể còn truy cập được sau khi shop ẩn.

`shop_idempotency_records`: actor_user_id, operation, resource_id, key UUID, request_hash, response_json JSONB, http_status, created_at_utc, expires_at_utc. UNIQUE(actor_user_id,operation,resource_id,key). Lưu response DTO đã lọc, không lưu request password/file bytes/secret. Batch cleanup expired rows; không xóa key đang có transaction sử dụng. Sau TTL có thể là attempt mới nhưng unique supplier/slug vẫn ngăn tạo trùng.

Audit dùng bảng audit_logs hiện có, metadata typed gồm old/new status/version/template revision và reason moderation; không ghi cả file hoặc credentials. Actor/time từ service. Không lấy reason công khai qua public query. Không cần outbox mới cho chức năng theme/visibility đồng bộ v1; nếu bổ sung push/email phải thiết kế event khác nhóm SupplierVerification, không gửi shop event vào inbox/hub hiện chỉ hỗ trợ verification.

AdminAuditMiddleware hiện tự ghi ApiWrite sau response bằng DbContext, ngoài transaction business. Với shop/template endpoints đã audit nguyên tử, phải khai báo endpoint metadata để middleware bỏ generic write này; tránh replay idempotency vẫn sinh thêm audit và tránh DbContext write ngoài DTO/service boundary của feature. Giữ nguyên hành vi các nhóm legacy khác, không đặt cờ skip audit từ HTTP client.

## 3. Index và query budget

| Query | Index/ràng buộc đề xuất |
| --- | --- |
| Shop của Supplier | Existing unique shops(supplier_id), không tạo thêm duplicate non-unique index |
| Public slug | Canonical unique shops(slug); giai đoạn preflight check lower(slug) duplicates; không tái sử dụng slug soft-deleted |
| Admin list | (status,created_at DESC,id) WHERE NOT is_deleted; sort/name search có plan riêng, không tạo mọi combination theo cảm tính |
| Template referencing | Existing shops(template_id); bổ sung shops(template_revision_id), FK columns theo EXPLAIN |
| Template catalog | (is_active,sort_order,code,id) WHERE NOT is_deleted; revision unique indexes đã hỗ trợ lookup |
| Public products | (shop_id,status,created_at DESC,id) WHERE NOT is_deleted; (shop_id,category_id,created_at DESC,id) nếu workload chứng minh cần |
| Variant predicate | Existing product_variants(product_id); xem composite (product_id,flavor,filling,weight_gram) WHERE NOT is_deleted sau đo EXPLAIN; không index tất cả token/stock tùy tiện |
| Primary image | product_images(product_id,is_primary,sort_order,id) WHERE NOT is_deleted; query LIMIT 1 và order deterministic |
| Reviews | (supplier_id,created_at DESC,id) WHERE is_public AND NOT is_deleted |
| Media | (owner_user_id,created_at) WHERE NOT is_deleted; status/lease filter cho cleanup nếu có |
| Idempotency cleanup | (expires_at_utc,id), unique scope/key giữ nguyên |

Public products price filter/sort dùng computed variant price, không thể cam kết chỉ một B-tree sẽ giải quyết toàn bộ. Đo plan trên cardinality thực tế; nếu chậm, thiết kế read-model price summary riêng với invalidation khi product/variant đổi, không lưu summary mà bỏ cập nhật.

N+1/cartesian safeguards:

- Admin page: một count + một projection rows có aggregate product counts từ subquery/group join; không gọi GetShopProductCountAsync từng row. Query count tối đa 2 business queries, không phụ thuộc pageSize.
- Public profile: tối đa 3 business queries gồm profile/appearance và aggregate reviews; không include toàn bộ products/reviews vào profile.
- Product page: public gate query + một page query LIMIT pageSize+1 + tối đa một batch media query nếu không dùng lateral primary image; không tải full variant collection. Không COUNT cho cursor page mặc định.
- Product price/variant match dùng EXISTS/aggregate trên cùng tập variant. Media batch chỉ theo IDs đã chọn, trước đó đã LIMIT products, tránh cắt page sau khi join n ảnh × m variants.
- Review page: gate + page + tối đa một batch display-name projection; giữ anonymity policy, không trả user email.
- Template catalog: một page projection; không count shop usage theo từng template. Admin usage counts group once theo selected template IDs.
- Owner preview: owner/eligibility/shop DTO + tối đa 12 product cards/primary images bằng bounded batch; tối đa 3 business queries, không gọi public gate để vô tình chặn shop Inactive.
- Query-count tests loại riêng middleware account/eligibility queries, ghi cả total request query count làm baseline; không che N+1 bằng cách chỉ đếm một repository method.

## 4. SQL injection và JSON/CSS injection

- LINQ parameters hoặc FromSqlInterpolated/DbParameter cho mọi value. Không đưa q, slug, search/price, cursor value, role hoặc reason vào SQL concatenation.
- Sort map enum → expression cố định; không coi parameter là tên cột để ORDER BY. SQL identifiers do code kiểm soát.
- Search theo nghĩa literal substring: escape `%`, `_` và escape char trước ILIKE, tối đa 100 ký tự; không chạy regex người dùng. Nếu dùng pg_trgm cần migration/extension và EXPLAIN, không giả định đã có index phù hợp.
- Dùng decimal + PostgreSQL numeric, UTC timestamptz. Cấm float cho price. Không JSON parse bằng chuỗi SQL, không nhận predicate/sql từ DTO.
- JSONB shape có DB check object, depth/unknown keys/ranges ở DTO/service; không cấp raw custom_css. Tránh mass assignment `context.Update(entityFromHttp)`.
- Ảnh chỉ asset IDs đã quản lý; server không fetch remote URL người dùng nhập → không tạo SSRF qua banner/logo/thumbnail.

Ví dụ SQL minh họa mechanics (không phải script migration chạy ngay):

```sql
-- Giá trị bind; owner và expected version được lấy/kiểm tra trong cùng transaction.
UPDATE shops
SET name = @name, description = @description,
    version = version + 1, updated_at = @now
WHERE id = @shop_id AND owner_user_id = @actor_id
  AND version = @expected_version AND is_deleted = false;
-- Affected rows 0 → result DTO conflict/not-found, service map 404/409.
```

## 5. Transaction, race condition và retry

### 5.1. Thứ tự khóa chung

1. Resolve owner identifiers bằng read DTO, chưa quyết định quyền từ pre-read.
2. Bắt đầu transaction; User IDs distinct ORDER BY id (actor và owner khi Admin).
3. SupplierProfile IDs ORDER BY id.
4. Shop IDs ORDER BY id nếu đã tồn tại.
5. Catalog guard id=1 nếu mutation thay availability/default hoặc đếm minimum.
6. Template head IDs ORDER BY id; revision chỉ đọc vì immutable.
7. Media asset IDs ORDER BY id nếu bind/unbind; sau đó ghi audit/idempotency.

Không bất kỳ code path nào khóa Template rồi đi khóa Shop, hoặc Shop rồi quay lại User. Admin publish template không khóa hàng nghìn shop dùng mẫu. Upload reserve/finalize chỉ khóa các resource cần thiết nhưng vẫn giữ thứ tự tương đối. Không gọi Cloudinary/scanner, email hoặc chờ UI trong transaction.

Service callback đọc fresh ShopBusinessStateDto từ các row đã khóa; kiểm tra active/verified/ownership dưới khóa. Repository không mở nested transaction; Unit of Work chia cùng DbContext cho shop, audit, idempotency. Rollback và clear tracking khi lỗi trước retry. READ COMMITTED + row locks + conditional version + unique constraints là baseline; không đặt SERIALIZABLE toàn hệ thống.

### 5.2. Các race phải có kết quả định nghĩa

| Race | Cơ chế/kết quả |
| --- | --- |
| Hai create cùng Supplier, key khác | User/Profile lock serialize + unique supplier; một 201, một 409 SHOP_ALREADY_EXISTS |
| Hai Supplier chọn cùng slug | Unique canonical slug phân xử dù đã precheck; một 201, một 409 SHOP_SLUG_IN_USE |
| Same key retry sau mất response | Check idempotency dưới lock trước expectedVersion; trả original result/status, không nhân audit |
| Update info và appearance cùng version | Shop aggregate version chung; chỉ một commit, request còn lại 409 |
| Suspend và Supplier activate/edit | Cùng User/Profile/Shop lock; nếu suspend commit trước thì Supplier bị chặn; không overwrite Suspended bằng stale update |
| Revoke/disable vs shop create/save/activate | Chia User → Profile locks với supplier-verification/Admin user update; quyết định quyền fresh trong transaction |
| Hide template vs chọn template | Chia template head lock; hidden không được chọn mới; shop pin trước đó vẫn hợp lệ |
| Hai Admin hide làm catalog <5 | Guard row serialize count + update cùng transaction; request phá invariant trả 409 |
| Publish revision vs Supplier chọn catalog cũ | Recheck exact current revision dưới head lock; 409 revision changed, không silent upgrade |
| Publish revision vs shop đang dùng revision cũ | Không mutation shop; existing revision/render vẫn như cũ |
| Asset finalize sau owner bị revoke | Finalize rechecks authority; không Ready/attach cho actor đã mất quyền; trạng thái Failed và cleanup theo retention |
| New order/bid vs suspend shop | Order create/confirm, bid create/award phải tham gia cùng Shop lock trước check Active; check ngoài transaction không đủ |

Điểm tích hợp order/RFQ: đưa fresh eligibility/shop DTO vào service callback trong **chính transaction ghi order/bid**, không mở một transaction kiểm tra shop rồi commit trước ghi đơn. Các module cũ có kiểm tra helper ngay trong repository cần chuyển gate này về service theo contract DTO khi tích hợp; không yêu cầu refactor toàn bộ workflow không liên quan. Test đồng thời thật là điều kiện bắt buộc để release shop moderation.

### 5.3. Retry và lỗi SQL

- 23505 map theo constraint allowlist supplier/slug/template code/idempotency, không map tất cả unique thành cùng một message.
- 23503 input FK không tồn tại xử lý service 404; FK bị đổi giữa read/write → rollback/map conflict nếu là race đã biết, lỗi schema thật vẫn 500 + trace nội bộ.
- 23514 constraint input đã biết → 400/409; không phơi tên table, SQL statement hay raw exception ra FE.
- 40P01 deadlock, 40001 serialization: tối đa 2 retries nội bộ jitter có bound, tạo fresh context và chạy lại toàn transaction cùng key. Không retry khi cancellation hoặc business conflict. Hết retry/lock timeout trả 503 SHOP_BUSY + Retry-After.
- Mất kết nối lúc commit là trạng thái không chắc chắn: client retry **cùng key**; không gửi key mới. Never replay network upload trong DB retry loop.
- DB statement/lock timeout đặt per transaction trong implementation và load test; không đổi global database config chỉ để che query chậm.

## 6. Public consistency, cache và pagination

Default v1 không cache toàn bộ public DTO chứa eligibility và không cache 404 lâu. Mỗi profile/product/review/categories request phải gate DB mới nhất. Public multi-query response dùng một read-only REPEATABLE READ transaction ngắn để cùng snapshot; quyết định hiển thị được hiểu tại snapshot request, không thể thu hồi response đang bay trên mạng. Order/bid vẫn check lại fresh dưới lock khi ghi.

Immutable template revisions và public image assets có thể cache theo revision/content hash. Không cache active/verified/suspended từ head theo TTL rồi dùng làm authority. Khi thêm CDN full-page sau này cần invalidation thiết kế riêng; public contact/image vốn công khai không thể hứa xóa khỏi máy người đã tải.

Cursor được ký purpose riêng bằng Data Protection: shopId, filter hash, sort, last sort tuple, expiry đề xuất 15 phút. Newest tuple (created_at,id), price tuple (computedDisplayPrice,id); category (name,id), review (created_at,id). Không có SQL trong token. PageSize+1 để hasMore/nextCursor; cursor cũ filter khác bị 400. Admin offset pagination được cap page<=1000, không dùng unbounded export; export nếu cần là job riêng.

## 7. Quyền DB và Supabase

Frontend chỉ gọi BE, không direct table REST bằng secret/service-role key. Migration role tách runtime role với quyền tối thiểu. Kiểm tra grants/RLS thực tế trước rollout; không giả định app pool role đã được RLS bảo vệ. Không tạo policy `auth.uid()` rồi cho rằng JWT tự ký của BE tự động thành Supabase Auth session.

Bảng shop private metadata/audit/idempotency/media/revisions không được mở write cho anon/authenticated qua PostgREST. Nếu thêm RLS cho backend owner scope, phải dùng transaction-local identity server-set và role tương thích, test pool reuse không rò tenant; đó là defense-in-depth có kiểm chứng, không thay service authorization. Không revoke grants toàn schema một cách mù quáng làm hỏng module khác.

## 8. Migration và reconciliation

1. Chụp migration history/schema/constraints/indexes actual trên bản sao; không chỉ dựa `.agents/db/mcake.sql` vì source EF đã thay đổi qua nhiều migration.
2. Preflight orphan shop/template, profile identity ambiguity, duplicate canonical slug/supplier, slug quá dài/reserved, null/blank name, unknown status, invalid JSON/CSS, template không map được renderer. Xuất báo cáo sửa dữ liệu; không xóa/gộp shop tự động.
3. Expand: thêm cột nullable và tables mới; tạo revisions cho template hiện có theo mapping đã review. Classic seed có thể map heritage; template không có renderer tương thích phải chốt mapping trước migration, không âm thầm đổi theme.
4. Backfill owner từ profile ID, version=1; current head/revision và shop revision liên kết đúng. Legacy custom_colors chỉ giữ các tokens hợp lệ, báo cáo giá trị bị loại. Không execute custom_css. Branding legacy không có ownership proof giữ cột gốc để review, public DTO không trả URL chưa được xác thực/allowlist; owner được nhắc upload lại.
5. Seed ≥5 mẫu bằng code/UUID ổn định, idempotent, defaults/contrast/layout hợp lệ, catalog guard/default template; không ghi đè template Admin đã sửa.
6. Thêm/validate FK và check sau backfill. PostgreSQL không có `ADD CONSTRAINT IF NOT EXISTS`; dùng migration history hoặc pg_constraint guard với conrelid. Large indexes dùng CONCURRENTLY ngoài transaction khi cần; EF suppressTransaction phù hợp, kiểm tra invalid index sau lỗi.
7. Deploy BE mới + FE phối hợp để status DTO/shape không làm hỏng adminService cũ; khóa mọi old shop writer bỏ version. Chỉ bật feature sau khi tất cả instance hiểu schema/revision.
8. Smoke trên bản sao, rồi môi trường đích đã chọn; backup và recovery drill. Không down migration xóa revisions khi shop đang dùng. Rollback bằng feature flag + binary tương thích, giữ bảng/cột mới cho forward fix.

Không sửa connection string, credentials hay tự áp dụng migration ở bước lập plan. SQL script production và EF migration được tạo trong bước implementation sau review plan.
