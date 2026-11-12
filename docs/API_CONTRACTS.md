# Hợp đồng API hiện hành

**Bổ sung 13/11:** đã có GET/POST `/api/admin/catalog/models/{modelId}/images`, preview `/{imageId}/content`, POST `/{imageId}/{approve|primary|hide|retire}` (If-Match ảnh) chỉ Admin/HTTPS/CSRF; public GET `/media/catalog/{imageId}/{guid-file-name}` có gate xuất bản và immutable cache/ETag. SSR `/catalog/models/{modelId}/images` trên Admin. Request multipart, giới hạn, trạng thái, file list/rollback tại [hồ sơ thay đổi](../Changes/2026-11-13-upload-anh-guid-v7-cache.md#hợp-đồng-http). Chưa có API tài liệu khách hoặc publication model/category. Các câu “chưa upload ảnh” ở checkpoint cũ đã được thay thế trong phạm vi ảnh mẫu xe này.

**Bổ sung 12/11/2026:** đã có GET chi tiết xe, PUT profile, GET/POST registrations và POST activate/retire/restore; DTO/quyền/guards/version/lỗi/SSR được khóa tại [FLEET_LIFECYCLE_CONTRACT.md](FLEET_LIFECYCLE_CONTRACT.md). Tất cả thao tác ghi mới chỉ Admin, If-Match của xe, HTTPS/cookie/CSRF; GET Fleet no-store. Profile/biển/vòng đời đã có UI ở trang chi tiết xe. Các mô tả “chưa đổi biển/retire/restore/edit hồ sơ” trong checkpoint cũ bên dưới đã được thay thế. Chưa publish/delete public API, booking hoặc inventory.

Cập nhật 10/11/2026 từ code thực tế. Hợp đồng này mô tả lát đã triển khai, không tuyên bố mọi endpoint đã được kiểm SQL hôm nay. Hướng dẫn thao tác: [TESTING_IDENTITY_CATALOG](TESTING_IDENTITY_CATALOG.md).

| Method/path | Môi trường/quyền | Kết quả |
|---|---|---|
| GET /health/live | API anonymous | 200 Healthy nếu process phục vụ, không kiểm DB |
| GET /health/ready | API anonymous | 200 nếu SQL kết nối được; 503 khi thiếu config/lỗi; không lộ exception |
| GET /openapi/v1.json | API Development anonymous | OpenAPI 200, có auth/Catalog/Fleet resources |
| GET /swagger/index.html | API Development anonymous | Swagger UI; interceptor lấy CSRF trước mutation |
| GET đường dẫn không có | API | 404 ProblemDetails application/problem+json |
| GET /health/live | Admin anonymous | 200 Healthy, không kiểm DB |
| GET /health/ready | Admin anonymous | 200 khi SQL kết nối được; 503 Unhealthy khi thiếu config/lỗi; không re-execute sang trang lỗi |
| GET /account/login | Admin anonymous | HTTPS: form login có antiforgery; HTTP: thông tin yêu cầu HTTPS |
| GET / | AdminAccess | Chưa login: challenge redirect đến login, không render dashboard |
| GET /auth/session | Blazor AdminAccess | Anonymous 401; role Admin 204; thiếu quyền 403 |

Health trả text; readiness chỉ chứng minh kết nối SQL, không kiểm migration/schema hoặc provider thanh toán. Không CORS wildcard. Health không yêu cầu đăng nhập.

## Identity HTTP API

| Method/path | Request / quyền | Response |
|---|---|---|
| GET /auth/csrf | HTTPS, anonymous | 200 `{requestToken}` + antiforgery cookie; no-store; HTTP 400 auth.https_required |
| POST /auth/login | HTTPS + X-CSRF-TOKEN + cookie CSRF; JSON `{username,password}` | 204 + auth cookie; sai/khóa/vô hiệu hóa: 401 auth.invalid_credentials; rate limit 429 |
| POST /auth/logout | Authenticated + HTTPS + CSRF | 204; thay security stamp thu hồi mọi phiên, xóa cookie |
| GET /auth/session | Authenticated | 200 `{userId,roles,permissions}`; no-store; anonymous 401 |

Blazor dùng form POST `/auth/login` với `username`, `password`, `__RequestVerificationToken`, redirect `/` hoặc `/account/login?error=invalid|unavailable`; POST logout có antiforgery và redirect login. Không có public registration, GET logout hoặc remember-me. Cookie HttpOnly/Secure/Strict, TTL 30 phút; mỗi request kiểm lại quyền/trạng thái user trong DB. Hai host cùng hostname và protected key ring/application name để dùng chung cookie; Blazor circuit revalidate mỗi phút. Admin UI hiện chỉ cho Admin vào dashboard; API permission riêng hỗ trợ Manager được cấp claim.

## Catalog/Fleet HTTP API

Mọi POST/PUT dưới đây cần HTTPS, cookie và `X-CSRF-TOKEN`. Actor/audit lấy từ server, không nhận actor từ request. Create trả 201 `{id}`; update/complete trả 204. Chưa có API public catalog, upload ảnh, publish, đổi biển, retire/restore hoặc delete; service có một số guard không đồng nghĩa có HTTP operation.

| Method/path | DTO request / response chính | Quyền |
|---|---|---|
| GET /api/admin/catalog/categories | page; `{id,code,vehicleFamilyCode,publicationStatus}[]` | Admin |
| POST /api/admin/catalog/categories | `{code,vehicleFamilyCode,nameVi}`; tạo Draft | Admin |
| GET /api/admin/catalog/brands | page; `{id,code,displayName}[]` | Admin |
| POST /api/admin/catalog/brands | `{code,displayName}` | Admin |
| GET /api/admin/catalog/models | page; `{id,code,commercialName,publicationStatus}[]` | Admin |
| POST /api/admin/catalog/models | `{categoryId,brandId,code,commercialName}`; tạo Draft | Admin |
| GET /api/admin/fleet/vehicles | page; `{id,modelId,assetCode,recordLifecycle,operationalStatus,rowVersion}[]` | Admin hoặc Manager + fleet.manage |
| POST /api/admin/fleet/vehicles | `{modelId,assetCode,vin?,confirmInitialInspection,duplicateVinReason?}` | Admin |
| GET /api/admin/fleet/vehicles/{id}/maintenance | page; `{id,vehicleId,scheduledStartUtc,scheduledEndUtc,status,actualEndUtc,rowVersion}[]` | Admin hoặc Manager + fleet.manage |
| POST /api/admin/fleet/vehicles/{id}/maintenance | `{scheduledStartUtc,scheduledEndUtc?,reason}` | Admin hoặc Manager + fleet.manage |
| GET /api/admin/fleet/maintenance/{id}/tasks | `{id,titleVi,status,rowVersion}[]`, tối đa 200 mục | Admin hoặc Manager + fleet.manage |
| POST /api/admin/fleet/maintenance/{id}/tasks | `{templateId?,customTitleVi?}`; template active hoặc tên riêng | Admin hoặc Manager + fleet.manage |
| PUT /api/admin/fleet/tasks/{id} | `{status,note?,skipReason?}` + If-Match | Completed: Admin hoặc Manager + fleet.manage; Skipped: Admin hoặc Manager có cả fleet.manage và fleet.approve |
| POST /api/admin/fleet/maintenance/{id}/complete | không body; If-Match của maintenance | Admin hoặc Manager có cả fleet.manage và fleet.approve |

Permission của group vẫn áp dụng cùng permission của endpoint/service. GET danh sách phân trang `page` mặc định 1, chấp nhận 1…100000, mỗi trang 50; không có total count. Tasks GET hiện giới hạn 200, chưa có pagination. List con không có bản ghi có thể trả mảng rỗng, không cam kết 404 parent.

Validation: code theo `^[a-z0-9][a-z0-9_-]{0,63}$`; family Motorcycle/Car; tên tối đa 160 ký tự; ID dương; asset code tối đa 64; VIN tối đa 100; reason tối đa 1000; note tối đa 2000; title tối đa 200. Thời gian UTC, end nếu có phải sau start. Task status Completed/Skipped. Đối chiếu DTO nguồn `CatalogFleetContracts.cs` trước khi thêm trường.

`confirmInitialInspection=true` chỉ khi Admin thực sự xác nhận đã kiểm tra: lưu Active/Ready + Passed inspection/history/audit cùng transaction; false tạo Draft/PendingInspection. VIN optional, không unique; trùng cần Admin ghi lý do. Không bắt buộc biển khi Ready; kiểm biển trước bàn giao thuộc module chưa triển khai.

Bảo trì khóa theo xe trong Serializable transaction; overlap bị chặn, đợt hết giờ dự kiến nhưng chưa release vẫn chặn. Hoàn tất cần checklist không rỗng, mọi mục Completed/Skipped, xe không bị khóa và đợt đã bắt đầu; tạo Passed inspection và release trong transaction. Chưa có worker kích hoạt Planned hoặc cảnh báo booking.

## Concurrency, lỗi và giới hạn idempotency

`If-Match` bắt buộc cho PUT task và complete maintenance: Base64 `rowVersion` 8 byte từ GET mới nhất, bọc bằng dấu nháy kép. Đây là concurrency token trong JSON, chưa có ETag response header. Không dùng version task để complete đợt bảo trì.

| HTTP | Code / trường hợp |
|---|---|
| 400 | auth.https_required, auth.csrf_invalid, request.invalid; If-Match sai định dạng |
| 401/403 | Chưa có phiên / thiếu quyền; middleware không cam kết mọi lỗi có cùng ProblemDetails code |
| 409 | resource.duplicate, resource.constraint_violation, maintenance.overlap/not_open/checklist_incomplete, vehicle.duplicate_vin_reason_required và các business guard khác |
| 412 | concurrency.resource_changed — GET lại trước quyết định gửi lại |
| 428 | concurrency.if_match_required |
| 429 | Login vượt giới hạn |
| 503 | database.unavailable_or_schema_pending |
| 500 | server.error |

Exception handler trả ProblemDetails có `code`, không gửi SQL payload/stack trace. Business keys unique chặn trùng mã, complete chặn replay theo trạng thái; **chưa có ledger Idempotency-Key**, không tự retry create sau mất phản hồi hoặc tuyên bố exactly-once.

## Admin Catalog SSR — lát tạo/đọc

`GET/POST /catalog` trên host Blazor yêu cầu AdminAccess. Ba form SSR có tên `create-category`, `create-brand`, `create-model`; antiforgery của Blazor và cookie được xác minh mỗi HTTP POST. Mỗi lần ghi gọi CatalogFleetSetupService hiện có trong request scope, giữ validation/transaction/audit/unique business keys như API. Không gọi ghi từ circuit với principal cũ. Validation hiển thị cạnh trường; mã trùng không tự retry. Thành công redirect GET và thông báo qua NotificationService; lỗi DB trả thông báo chung. Danh sách phân trang riêng theo query `page`, 50 bản ghi/nhóm; lựa chọn parent dùng ID hợp lệ từ người quản trị, backend kiểm lại Active. Chưa sửa/xóa/publish hoặc quản lý xe vật lý bằng UI.

Thông báo tạo thành công dùng cookie flash được Data Protection bảo vệ, HttpOnly/Secure/Strict, hết hạn một phút và xóa sau đọc; không lấy success từ query client. GET không cache. Không có API public mới hoặc migration mới.

## Admin Fleet SSR — đã triển khai tạo/đọc và bảo trì

`GET/POST /fleet`, `/fleet/vehicles/{id}` và `/fleet/maintenance/{id}` trên Blazor yêu cầu AdminAccess cho lát UI đầu. SSR POST cookie/antiforgery/HTTPS và actor hiện thời; service hiện có chịu trách nhiệm transaction, audit, lịch trùng, checklist và release. UI tạo xe nháp hoặc xác nhận kiểm tra ban đầu để Ready; tạo đợt bảo trì theo giờ Việt Nam (chuyển UTC server); thêm checklist riêng, Completed/Skipped (Skipped phải có lý do), hoàn tất sau xác nhận kiểm tra. Task/release gửi rowversion Base64 từ form, không lấy phiên bản mới thay input POST; thiếu/sai/stale token bị chặn. Các ID task phải thuộc đúng đợt trên route. Business errors hiển thị inline, lỗi hệ thống qua NotificationService; success chỉ sau service commit bằng protected flash cookie. Danh sách 50 bản ghi/trang; checklist phân trang để không bỏ sót. Chưa có edit hồ sơ/biển số/retire/restore, kích hoạt tự động bảo trì Planned hoặc inventory/booking.

Booking/quote/payment/cash/handover/return/extension/refund/offline/CMS/translation chưa có hợp đồng DTO/routes được khóa. Trước mỗi module phải ghi guard, permission, idempotency, audit, lỗi và kiểm thử. Không tạo endpoint giả thành công hoặc thay các quyết định OPEN bằng giả định.
