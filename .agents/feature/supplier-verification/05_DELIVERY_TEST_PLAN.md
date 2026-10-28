# [SUPPLIER-VERIFICATION] — IMPLEMENTATION, TEST & ROLLOUT PLAN

Trạng thái: plan trước triển khai, ngày 2026-10-24. Các checkbox chưa tick là công
việc tương lai, không phải test đã chạy. Phạm vi có real-time theo yêu cầu bổ sung.

## 1. Thứ tự thực hiện và đầu ra review được

### Phase 0 — Chốt contract và dữ liệu hiện hữu

- [ ] **SV-00.1 — Business:** xác nhận loại giấy tờ bắt buộc/ngoại lệ, định dạng MST,
  gửi lại sau thu hồi, dung lượng, rate limit, retention và SLA đề xuất trong detail.
- [ ] **SV-00.2 — Inventory:** rà tất cả consumer apply/pending/me; tài liệu auth cũ
  khác source phải được ghi rõ, không tự đưa flow OTP cũ vào Supplier feature.
- [ ] **SV-00.3 — Schema:** kiểm tra staging profile/doc rows, orphan, TaxCode trùng
  sau normalize, file public/private legacy, migration đang chạy và naming cột thực.
- [ ] **SV-00.4 — Contract:** review examples/API errorCode/version/idempotency và
  event schema; xác nhận notification read-all dùng user sequence + signed cutoff token.
- [ ] **SV-00.5 — Security/ops:** chọn pipeline kiểm tra nội dung file và private read
  adapter; xác nhận production SignalR một hay nhiều instance/backplane/HTTPS.

**Exit:** backlog và policy được thống nhất; không có migration phá dữ liệu chưa khảo sát.

### Phase 1 — Domain, DB và transaction foundation

- [ ] **SV-01.1:** thêm profile version/current-submission/revoke metadata; submission
  snapshot và document ownership/provider metadata; giữ existing indexes cần thiết.
- [ ] **SV-01.2:** cấu hình concurrency token, unique TaxCode canonical/UserId/revision,
  partial Pending constraint và FK cross-profile consistency.
- [ ] **SV-01.3:** Outbox/Idempotency schema; mở rộng Notification unique event/user/type,
  audience/sequence/read metadata và User.NotificationSequence; index query worker/unread.
- [ ] **SV-01.4:** repository lock primitives và unit-of-work; phân biệt Add/Update
  rõ ràng cho entity GUID mới. Tách mapping helper đúng quy tắc BE.
- [ ] **SV-01.5:** migration/backfill dry run, snapshot assertions và restore rehearsal.

**Exit:** schema expand chạy được trên DB mẫu có dữ liệu legacy; transaction/concurrency
tests dùng PostgreSQL thật, không chỉ EF InMemory.

### Phase 2 — Backend core và tài liệu private

- [ ] **SV-02.1:** self API cho requestedSupplier role Customer; NotSubmitted response;
  `/auth/me`/auth response bổ sung requestedRole/verification/capabilities.
- [ ] **SV-02.2:** document multipart/status/content/gỡ staged; tái sử dụng Cloudinary
  upload service, validation/scan readiness và ownership ở feature layer.
- [ ] **SV-02.3:** apply/resubmit immutable snapshot; idempotency và version trước commit.
- [ ] **SV-02.4:** list/detail/history projection có pagination và query-count test.
- [ ] **SV-02.5:** approve/reject/revoke atomic profile+role+audit+event+outbox.
- [ ] **SV-02.6:** chuẩn hóa errors/messages tiếng Anh trong phạm vi sửa, traceId,
  rate limit/quota/cancellation; không log body pháp lý hoặc secrets.

**Exit:** contract và integration tests qua HTTP đủ quyền và lỗi, chưa phụ thuộc FE mới.

### Phase 3 — Quyền xuyên module và stale session

- [ ] **SV-03.1:** current-account principal validation trước role authorization;
  không để JWT cũ giữ role Supplier/Admin đã bị thu hồi.
- [ ] **SV-03.2:** inventory seller commands: tạo shop, sản phẩm/promotion liên quan,
  submit bid, accept bid của Customer, tạo order mới vào shop.
- [ ] **SV-03.3:** tại command có nguy cơ TOCTOU, cùng lock/check Supplier eligibility
  trong transaction; không chỉ check policy trước transaction.
- [ ] **SV-03.4:** public shop/search/product visibility và Verified badge derive từ
  active user + Verified profile + shop status, invalidation cache nếu có.
- [ ] **SV-03.5:** policy fulfillment cho giao dịch cũ sau revoke, ownership/timestamp
  và status transition cụ thể; account locked xử lý riêng, không cấp quyền rộng.
- [ ] **SV-03.6:** generic updateUser không bypass verification; role/account changes
  có shared lock boundary và thử race với review/revoke.

**Exit:** test stale JWT và revoke-versus-sale pass; chưa đạt phần này thì chưa coi
feature enterprise an toàn dù form duyệt đã hoạt động.

### Phase 4 — Email, durable notification và real-time

- [ ] **SV-04.1:** worker claim/lease/retry/dead-letter cho Email và InAppRealtime
  riêng; reuse IEmailSender; encode lý do trong email.
- [ ] **SV-04.2:** materialize Notification recipients bằng batch, unique event key,
  kiểm tra current Admin/account và audience filter khi đọc.
- [ ] **SV-04.3:** REST notification list/unread/read/read-all có ownership và cutoff.
  Cấp sequence trong User row lock, opaque token có purpose/expiry và keyring chia sẻ.
- [ ] **SV-04.4:** SignalR Hub/publisher adapter; auth/userId mapping; không client
  arbitrary subscribe/broadcast; minimal events sau commit.
- [ ] **SV-04.5:** access-token query restriction/redaction, CloseOnAuthenticationExpiration,
  current recipient authority và dependency wiring không vi phạm Clean Architecture.
- [ ] **SV-04.6:** backplane/scale-out staging nếu production đa instance; replay/crash,
  offline catch-up và ops metrics/alerts.

**Exit:** thông báo được giữ khi mất socket, channels độc lập; event không chứa PII.

### Phase 5 — FE Supplier/Admin integration

- [ ] **SV-05.1:** services constants/API client FormData/blob/errorCode/AbortSignal.
- [ ] **SV-05.2:** self verification hook + Ant Form/Steps/Upload/snapshot/history.
- [ ] **SV-05.3:** Admin Table server pagination, Drawer detail và modal review/revoke.
- [ ] **SV-05.4:** AuthContext refresh + onboarding guard cho Customer requestedSupplier;
  shop/menu/badge không hardcode Verified.
- [ ] **SV-05.5:** NotificationProvider/realtimeService một connection/session/tab;
  initial start retry, reconnect, dedup/version guard và cleanup.
- [ ] **SV-05.6:** Ant Badge/Dropdown/List sử dụng REST unread/history; realtime invalidates
  queue/detail/auth; fallback polling có giới hạn và không overwrite draft đang nhập.
- [ ] **SV-05.7:** thay success mock trong scope, không fallback 401/403/409 thành
  approve/reject/revoke thành công; demo tách rõ nếu còn giữ flag dự án.

**Exit:** E2E hai vai trò/two browser contexts, mobile/keyboard, network failure và build.

### Phase 6 — Hardening và rollout

- [ ] Query budget/load/race suite, security/permission tests và contract regression.
- [ ] Feature flags UI/command/realtime theo rollout; shared infrastructure config
  có review riêng khi triển khai, không đổi `.env`/appsettings trong bước plan.
- [ ] Ops runbook: stuck outbox, SMTP/Cloudinary/Redis outage, stale notification,
  reconciliation file legacy và tax-code conflict.
- [ ] Review migration scripts, backup/restore, canary và smoke test production
  dùng account/test data được cho phép; không gọi thử trên hồ sơ doanh nghiệp thật.

## 2. Test matrix nghiệp vụ và security

| ID | Kịch bản | Expected / lớp test |
| --- | --- | --- |
| B01 | RequestedSupplier, role Customer, email verified, chưa có hồ sơ | Self200/NotSubmitted, form accessible; integration + E2E |
| B02 | Upload đủ file Ready, submit lần đầu | Một profile/revision Pending, event/outbox đúng; integration |
| B03 | Admin approve đúng version | Verified/Role Supplier, audit/email/in-app, owner mở shop; E2E |
| B04 | Reject có lý do, owner resubmit | Lý do thấy đúng quyền; revision2, revision1 nguyên vẹn; integration/E2E |
| B05 | Revoke Verified rồi gửi lại/approve lại | Mất quyền tức thời; có history, reapply không tự cấp quyền; integration |
| B06 | Apply khi Verified hoặc đang Pending | 409, không đổi file/revision; integration |
| B07 | Lý do empty/space/ngoài max; approve boolean bị bỏ | 400, không quyết định bằng default bool vô tình; contract/unit |
| B08 | TaxCode/company/address whitespace, Unicode/control chars | Normalize hoặc validation có kiểm soát; unit/contract |
| B09 | File hết hạn trước lúc review | Approve bị chặn dù lúc submit chưa hết hạn; integration với fake clock |
| B10 | Admin tự review/revoke hồ sơ own user | 403, audit denied phù hợp; integration |
| S01 | Anonymous hoặc Customer thuần gọi APIs | 401/403 đúng; không tiết lộ hồ sơ; HTTP integration |
| S02 | OwnerA dùng document/profile/history của OwnerB | 404, không attach/preview được; integration |
| S03 | Client gửi owner/role/status/reviewer/FileUrl/publicId | Không được bind để bypass; contract/security |
| S04 | Upload HTML/SVG/archive giả PDF, MIME sai/malware/quarantine | 415/validation failure; không Ready hoặc submit; integration |
| S05 | Quá byte giới hạn, chunked non-seekable, empty file | Dừng đọc có bound, 413/400; không gọi provider vô hạn |
| S06 | Filename path traversal/CRLF, lý do chứa HTML/script | Sanitize/escape; không XSS/header injection; unit/E2E |
| S07 | SQL payload `' OR 1=1 --` ở search/MST, sort identifier giả | Dữ liệu parameterized/400; không mở rộng query hoặc thực thi SQL |
| S08 | Nhập URL loopback/metadata/cloud URL của người khác | Apply không nhận URL; content không nhận remote URL; integration |
| S09 | Token Supplier cũ sau revoke; token Admin cũ sau demotion | Quyền DB hiện tại chặn; không cần chờ expiry; integration |
| S10 | Account inactive khi kết nối/hay đang có hub | Không đọc/ghi/nhận sensitive content; session test |
| S11 | Rate limit/quota/cancellation nhiều instance | 429 + Retry-After; tài khoản khác không bị chặn theo sai key |
| S12 | Log/capture HTTP+hub/error/metrics | Không secret/token/query access_token/file body/PII reason |

**Contract bool quan trọng:** request review phải phân biệt field `approve` bị bỏ
với giá trị false hợp lệ (nullable required hoặc validator presence). `[Required]`
trên bool non-nullable hiện tại không chứng minh client đã gửi field này.

## 3. Race, idempotency và PostgreSQL integration tests

| ID | Đồng thời / failure injection | Assertion |
| --- | --- | --- |
| C01 | 20 submit cùng user, same key | Một logical submission/event; các replay trả cùng result |
| C02 | 20 submit cùng user, different keys | Một Pending; request còn lại409, không mất documents |
| C03 | Hai user cùng canonical MST | Một owner/profile thắng unique; error409 không500 |
| C04 | Hai Admin approve/reject cùng version | Một commit, một409; không vừa có email approved và rejected |
| C05 | Review cũ đối đầu resubmit | Quyết định không gắn nhầm revision, currentId/version được so sánh |
| C06 | Revoke đối đầu bid/create-shop/create-order/award | Một thứ tự commit xác định; không bán mới sau revoke commit |
| C07 | Admin lock account/updateRole đối đầu review | Không cấp Supplier cho inactive; invariants và role consistent |
| C08 | Network timeout sau commit, retry cùng key | Không thêm audit/outbox/notification; same hash replay |
| C09 | Same key khác payload hoặc khác actor/resource | Mismatch409 hoặc scope độc lập đúng, không đọc result người khác |
| C10 | Exception sau profile update trước audit/outbox | Toàn TX rollback, role/status không lệch |
| C11 | Deadlock transient/bounded retry | Không duplicate side effects; key giữ; version conflict không auto retry |
| C12 | File upload xong provider, metadata commit lỗi | Không file Ready giả; operation được đối soát |
| C13 | Worker crash sau SMTP send trước ack | Có thể email trùng; không đổi business decision hoặc mất event |
| C14 | Worker crash sau Notification commit trước push | Unique recipient notice, retry/catch-up không mất |

Dùng hai DbContext/connection/transaction riêng với barrier đồng bộ để tạo race,
assert dữ liệu và số side effects sau cả hai task. Test trên PostgreSQL local/
container hoặc môi trường disposable được cấp quyền; EF InMemory/SQLite không
thay thế được row lock/index/SQLSTATE của PostgreSQL.

## 4. Real-time và frontend tests

| ID | Kịch bản | Assertion |
| --- | --- | --- |
| R01 | Owner submit, Admin online context khác | Queue indicator cập nhật, REST có hồ sơ mới, không N+1 |
| R02 | Approve/reject/revoke khi owner online | Notice+status+capability refetch; không lấy event làm quyền |
| R03 | Owner offline lúc có quyết định | Reconnect/REST inbox bắt kịp đúng notification |
| R04 | Duplicate và event version8 tới trước7 | Toast/count không lặp; state không rollback |
| R05 | REST snapshot đang chạy khi event tới | Dirty invalidation refetch sau snapshot, không mất update |
| R06 | JWT expiry, single-flight refresh, start lỗi, reconnect thất bại | Backoff có bound, REST vẫn dùng được, fallback rõ |
| R07 | Logout/user switch/StrictMode/chuyển page nhiều lần | Không leak listeners/timers/connection, không state user trước |
| R08 | Client muốn join userId/Admin group khác | Không hub method hoặc bị chặn; private recipient server-only |
| R09 | Admin bị demote khi socket vẫn mở | Không đọc admin notices/detail; publisher không tin group cũ |
| R10 | Hai instances + Redis, worker khác node client | Fanout đúng; Redis outage dùng REST/inbox, không rollback command |
| R11 | Read-all chạy cùng notification mới; hai tab read | Notice sau cutoff unread, count không âm, ownership đúng |
| F01 | Submit khi thiếu tài liệu/file còn Uploading | Validation rõ, disabled đúng; BE vẫn chặn trực tiếp |
| F02 | 400/413/415/429/storage5xx | Giữ form/file hợp lệ, retry đúng logical attempt |
| F03 | Admin đang nhập lý do nhận version changed | Không mất reason, disable quyết định, buộc tải lại |
| F04 | Search gõ nhanh/filter đổi khi request cũ chậm | Chỉ query mới cập nhật table, page reset đúng |
| F05 | Preview tài liệu private và đóng Drawer | Bearer đúng, blob lỗi được hiểu, revoke object URL |
| F06 | Tab focus/polling fallback/realtime recovery | Không request storm; dừng timer khi hidden/logout |
| F07 | Keyboard/mobile/screen reader | Form labels, focus, modal, text status và role guard phù hợp |
| F08 | Mock flag bật nhưng API trả401/403/409 | Không success giả cho review/revoke/submit production |

## 5. Performance và reliability nghiệm thu

- Seed 100/1.000/10.000 profile; đo list page20 và page100 bằng command interceptor:
  số query feature là hằng số2, không 1+N; detail/document count có bound.
- Đo EXPLAIN ANALYZE staging, memory/payload/list search và notification unread;
  không thêm index theo cảm tính hoặc thử truy vấn nặng trên production.
- Mục tiêu ban đầu: list p95<500ms; transaction nghiệp vụ p95<1s không tính upload;
  push online commit-to-client p95≤3s tại tải và topology được ghi trong báo cáo.
- SMTP/storage/backplane mất kết nối không tạo success giả. Outbox lag/dead-letter
  có alert; DB Notification giữ lịch sử khi push lỗi.
- Fanout nhiều Admin dùng query/batch insert có bound, không N+1 recipient/file.
  Nếu số Admin tăng lớn, fanout paging worker và unique key đảm bảo retry an toàn.

## 6. Migration/backfill và phát hành

1. Backup và kiểm tra restore trên staging. Không dùng tự động migrate production
   bằng app startup cho thay đổi chưa review.
2. **Expand:** thêm nullable columns/tables/index cần thiết, deploy BE đọc được
   dữ liệu cũ. PostgreSQL tạo index concurrently nếu cần phải tách migration không
   chạy trong transaction của lệnh đó và có runbook retry/invalid-index cleanup.
3. **Backfill:** tạo legacy submission snapshot theo bằng chứng hiện có, không
   giả reviewer/ngày duyệt. Gắn document đúng profile; dữ liệu không xác định hoặc
   thiếu privacy metadata đưa vào reconciliation, không tự cấp Verified mới.
4. Canonical MST trước unique mới/đổi format: phát hiện xung đột rồi xử lý có owner,
   không tự merge hai doanh nghiệp hoặc xóa record để migration pass.
5. Lưu Verified hợp lệ hiện có; nếu thiếu chứng từ/bằng chứng kiểm toán, gắn cờ
   “needs reconciliation” cho vận hành, không tự thu hồi hàng loạt trong migration.
6. **Deploy BE additive:** new read/file/history/revoke/notification/hub; adapt
   `/pending` response cho consumer hiện hữu. Đổi apply khỏi arbitrary URL có kế
   hoạch compat window rõ, ưu tiên bảo vệ file chứ không giữ endpoint unsafe vô hạn.
7. **Deploy FE:** Ant UI/guards/API + realtime; canary admin nội bộ, supplier test,
   sau đó bật rộng. Feature flag không tắt ownership/transaction/authority checks.
8. **Contract:** tắt legacy payload/adapter sau kiểm kê client; siết NOT NULL/FK
   khi backfill/reconciliation đạt điều kiện. Không làm contract migration sớm.

**Rollback:** tắt write UI/new submissions nếu incident; giữ schema expand và audit
đã ghi. Không rollback business decisions bằng down migration hoặc quay về backend
không hiểu revoke/permission checks. Có thể tắt realtime riêng và dùng REST polling,
không xóa notification/outbox. Worker ngừng/khởi động lại theo runbook giữ lease và
idempotency. Data correction là audited compensating command, không UPDATE thủ công
không ghi nhận.

## 7. Definition of Done và lệnh kiểm tra khi triển khai

- [ ] Business acceptance B01–B10; security S01–S12; concurrency C01–C14 pass.
- [ ] Realtime R01–R11 và FE F01–F08 pass với BE/FE thật; role pending và revoked đủ.
- [ ] Query-count/latency evidence, traceability, dead-letter/orphan runbook có review.
- [ ] Source tuân thủ Clean Architecture, English BE constants/helpers, FE Ant Design.
- [ ] API docs/migration/backfill/compatibility và feature flags cập nhật đúng release.
- [ ] Build BE, test dự án test thực tế sau khi bổ sung; không ghi lệnh trỏ project
  test chưa tồn tại như thể hiện tại đã chạy thành công.
- [ ] Tại `mooncake-fe`: `npm run build`; `npm run lint` và giải trình lỗi baseline
  ngoài scope nếu có; component/E2E runner được cấu hình rõ trong PR triển khai.
- [ ] Tại BE: `dotnet build Mooncake.EcommercePlatform.sln`, `dotnet test` với suite
  mới được thêm theo quyết định team. Không kéo test vào app runtime production.

**Bàn giao ở bước hiện tại:** chỉ bộ tài liệu trong folder này, kiểm tra cấu trúc/
liên kết và tính nhất quán. Chưa sửa source BE/FE, chạy migration, cài dependency,
gọi Cloudinary/SMTP hoặc benchmark chức năng.

## 8. Traceability từ yêu cầu tới nghiệm thu

| Yêu cầu | Thiết kế | Test/phase |
| --- | --- | --- |
| Chờ xác minh không mở shop/bid | Detail III.1/6, data2.4, FE1 | B01/B02, S09, C06; phase3 |
| Admin thẩm định giấy tờ riêng tư | API5/7/8, data4, FE3 | S02/S04/S05/S08, B09/B10 |
| Approve và badge Verified | Detail III.5/6, FE2.2 | B03, C04, R02; phase2/3/5 |
| Reject lý do và resubmit | Immutable revision, API6/8 | B04/B07, C05, F03 |
| Thu hồi khi vi phạm | Detail2.3/III.6, data2.4 | B05, S09, C06/C07 |
| Email kết quả | Outbox channels, data5 | C10/C13; phase4 |
| Audit không mất lịch sử | Submission + WorkflowEvent + AuditLog | B04/B05, C08/C10 |
| Real-time bổ sung của user | Realtime1–8, FE5 | R01–R11; phase4/5 |
| N+1 | Data3 query budget | Performance suite; SV-02.4 |
| Race condition/idempotency | Data2; inbox sequence/cutoff | C01–C14, R04/R05/R11 |
| SQL injection và quyền object | Data4, API9 | S01/S02/S03/S07/S08 |
