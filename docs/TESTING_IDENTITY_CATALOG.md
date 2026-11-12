# Hướng dẫn thử Identity và Catalog/Fleet local

## 13/11 — quản lý ảnh mẫu xe

Chạy API/Admin HTTPS với `--test-database` theo mục 3, đăng nhập Admin rồi `/catalog` → **Quản lý ảnh** bên cạnh mẫu xe. Chọn ảnh xe giả JPEG/PNG/WebP/GIF/BMP tĩnh, điền nguồn/quyền/mô tả VI. Mặc định giới hạn 10 MiB/20 triệu pixel. Upload hai lần cùng file: hai ID và file GUID v7 riêng. Preview WebP chỉ Admin; ảnh mới Draft. Duyệt sau xác minh nguồn/quyền, chọn ảnh chính, ẩn hoặc ngừng dùng; tab cũ không được ghi đè version mới. Không upload CCCD/GPLX/giấy tờ/handover tại đây; chưa có UI hồ sơ khách.

Public ảnh chỉ hoạt động sau khi ảnh approved/visible và model/category published hợp lệ; UI publication mẫu vẫn chưa triển khai, không sửa DB Development để vượt gate. Integration runner dùng fixture test để kiểm cache public/ETag/304. Primitive giấy tờ giữ raw bytes/metadata, mã hóa private được kiểm bằng `dotnet run --project tests/SangRental.Identity.Integration --no-build --no-restore -- --media-only` (33 checks, không SQL). Toàn bộ runner SQL hiện 246, guard 36; smoke 18. Hướng dẫn chi tiết/rollback ở [Changes](../Changes/2026-11-13-upload-anh-guid-v7-cache.md).

Cập nhật 10/11/2026. Chỉ dùng dữ liệu giả phục vụ kiểm thử, không nhập giấy tờ/thông tin khách thật. Các bước bên dưới là hướng dẫn; kết quả thực tế từng ngày xem [PROGRESS_LOG](PROGRESS_LOG.md). [API_CONTRACTS](API_CONTRACTS.md) mô tả quyền/DTO/lỗi.

## Checkpoint 12/11/2026 — thử quản lý hồ sơ và vòng đời xe

Đã chạy SQL integration **200 checks**, GuardSmoke **36**, notification/policy/form **15** và HTTP smoke **16**: PASS. Các giới hạn “chưa sửa hồ sơ/biển/retire/restore/activate” ở checkpoint cũ bên dưới đã được thay thế. Browser thực chưa kiểm; certificate localhost vẫn chưa trusted ở phiên người dùng. Chủ máy chạy `dotnet dev-certs https --trust` và chấp thuận trước khi đăng nhập; không bỏ TLS. Khởi động API/Admin trên **DB test** theo mục 3.

1. `/catalog`: tạo danh mục/hãng/mẫu nháp với mã riêng. `/fleet`: tạo xe không chọn kiểm tra ban đầu để có Draft.
2. Mở chi tiết xe, “Kích hoạt xe nháp”: nhập lý do, xác nhận đã kiểm tra, gửi và xác nhận SweetAlert2. Xe thành Active/Ready, có Passed inspection/audit trong test DB. Bỏ checkbox hoặc lý do phải bị chặn.
3. “Sửa hồ sơ xe”: đổi màu/năm/VIN hoặc ID mẫu Active, ghi lý do. Mã tài sản giữ nguyên. Nếu đổi VIN thành VIN xe khác, cần lý do chấp nhận trùng. Không coi form là xác minh đăng ký hợp pháp.
4. “Thêm hoặc đổi biển số”: quốc gia `VN`, biển **giả** có hậu tố riêng, lý do. Lưu lần đầu, sau đó đổi biển khác: bảng lịch sử giữ biển trước và thời điểm/lý do đóng. Gán cùng biển (bỏ dấu chấm/gạch/khoảng trắng, không phân biệt hoa/thường) cho xe khác cùng quốc gia phải bị chặn. Không dùng biển khách thật.
5. Mở cùng xe trong hai tab. Tab A sửa/lưu; tab B gửi form cũ: báo dữ liệu thay đổi, không ghi đè. Tải lại trước khi quyết định gửi lại.
6. “Ngừng khai thác”: nhập lý do, xác nhận; hồ sơ Retired và mọi lịch sử được giữ. Nếu còn bất kỳ Planned/InProgress maintenance, phải hoàn tất đợt trước khi retire; không tự hủy lịch/checklist.
7. “Khôi phục khai thác”: nhập lý do, xác nhận kiểm tra đạt; xe Active/Ready, có inspection mới. Không khôi phục qua luồng này khi LostLocked/AccidentLocked hoặc có bảo trì đã đến giờ chưa xong.

Swagger: GET `/api/admin/fleet/vehicles/{id}` lấy `rowVersion`; PUT profile, POST registrations/activate/retire/restore dùng `If-Match: "<rowVersion>"`. Phải GET lại sau mỗi lần ghi vì version đổi; thiếu version 428, sai định dạng 400, version cũ 412, guard/duplicate 409. GET `/vehicles/{id}/registrations` xem lịch sử. Chi tiết request ở [FLEET_LIFECYCLE_CONTRACT](FLEET_LIFECYCLE_CONTRACT.md).

Chưa có publication/upload ảnh hoặc quy trình thuê xe; Ready vẫn không chứng minh lịch trống. Không chạy `database update` trên Development để thử các tính năng này; không cần migration mới.

## 1. Chuẩn bị

- Chạy từ repository root với SDK trong global.json, Node 24 và dependencies đã restore.
- SQL local endpoint `127.0.0.1:1436` phải truy cập được. Connection đầy đủ nằm trong API User Secrets `SangRental-Api-development`, không sao chép vào file hoặc output.
- Dùng đúng **SangRentalTestDb** đã được duyệt. API/Admin startup không tự migrate. Không chạy `database update` không chỉ định đích vì mặc định là SangRentalDb Development; migration Identity trên Development còn chờ review.
- HTTPS certificate local phải được hệ điều hành tin cậy. Dùng cùng hostname `localhost` cho cả hai ứng dụng; không trộn localhost/127.0.0.1 khi thử chia sẻ cookie.

```powershell
dotnet build SangRental.slnx --no-restore
npm --prefix frontend run lint
node --test scripts/notifications.test.mjs scripts/ui-policy.test.mjs
npm --prefix frontend run typecheck
npm --prefix frontend run build
node scripts/smoke.mjs
```

Nếu chưa restore, dùng các lệnh cài đặt trong README. Smoke khởi động ứng dụng riêng, không dùng SQL, key ring riêng dưới artifacts; kiểm live 200/ready 503 và các trang/assets. Smoke đạt không chứng minh login SQL hoặc schema sẵn sàng.

## 2. Kiểm thử SQL tự động

```powershell
dotnet run --project tests/SangRental.Identity.Integration --no-build --no-restore
dotnet run --project tests/SangRental.CatalogFleet.GuardSmoke --no-build --no-restore
```

Chạy tuần tự. Identity runner allowlist SQL loopback, chỉ Migrate/ghi SangRentalTestDb; giữ dữ liệu/audit kiểm thử, vô hiệu hóa tài khoản ngẫu nhiên và thu hồi phiên lúc cleanup. Có phép đọc SangRentalDb để xác minh vẫn 16 bảng Catalog, 0 bảng Identity và 2 migrations; assertion này cần cập nhật riêng khi Identity Development được duyệt áp dụng sau này. Không Drop/EnsureDeleted. Runner GuardSmoke dùng cùng allowlist test database, ghi transaction rồi rollback; không tự migrate. Không coi `dotnet test` exit 0 thay cho hai console runners.

Nếu lỗi kết nối: kiểm SQL/Docker đang chạy và port 1436, cấu hình User Secrets theo DEPLOYMENT; không in secret hoặc đổi mật khẩu/TrustServerCertificate để thử. Giữ trạng thái NOT VERIFIED khi chưa kết nối được.

## 3. Tạo Admin test và chạy hai ứng dụng

Chỉ khi test DB đã có đủ 3 migrations, dùng terminal tương tác:

```powershell
dotnet run --project backend/SangRental.Api --no-launch-profile -- --environment Development --test-database --provision-admin
```

CLI hỏi username/password và nhập lại; mật khẩu nhập ẩn, tối thiểu 12 ký tự theo Identity policy. Không truyền password qua command line. Không có tài khoản mặc định. Test mode cho tạo thêm Admin tại đúng DB test; bootstrap Development chỉ Admin đầu tiên.

Chạy hai terminal:

```powershell
dotnet run --project backend/SangRental.Api --no-launch-profile -- --urls https://localhost:7100 --environment Development --test-database
```

```powershell
dotnet run --project admin/SangRental.Admin --no-launch-profile -- --urls https://localhost:7200 --environment Development --test-database
```

Mở `https://localhost:7200/account/login`, đăng nhập bằng tài khoản vừa cấp. Mở `https://localhost:7100/swagger/index.html` trong cùng browser/profile. Cookie cùng hostname được API nhận; shared application name và protected key ring phải giống nhau. Profile/key lỗi phải được chẩn đoán riêng, không xóa key ring đang dùng hoặc tắt DPAPI.

## 4. Thử Catalog bằng Swagger

Swagger tự lấy `/auth/csrf` trước mutation. Nếu 401, kiểm login/session; nếu 400 CSRF, tải lại phiên và token, không tắt antiforgery.

1. POST `/api/admin/catalog/categories`:

```json
{"code":"qa-motorcycle-unique","vehicleFamilyCode":"Motorcycle","nameVi":"Danh mục kiểm thử"}
```

2. POST `/api/admin/catalog/brands`:

```json
{"code":"qa-brand-unique","displayName":"Hãng kiểm thử"}
```

3. Ghi lại hai `id` từ response 201. POST `/api/admin/catalog/models` với `categoryId`, `brandId` vừa nhận, `code` riêng và `commercialName`. Không lấy ID ví dụ làm ID thật.
4. GET danh sách categories/brands/models: thấy bản ghi mới; model/category vẫn Draft. Gửi lại code cũ: 409; code có chữ hoa/khoảng trắng: 400. Mỗi lần thử dùng suffix mới để không lẫn dữ liệu.

## 5. Thử xe và bảo trì

1. POST `/api/admin/fleet/vehicles`: `modelId` từ bước trên, `assetCode` riêng; `confirmInitialInspection=false` tạo Draft. Chỉ thử true khi người thử xác nhận bước kiểm tra giả lập trên dữ liệu test; backend tạo inspection/history/audit, không có booking/allocation.
2. Với xe Active/Ready, POST `/api/admin/fleet/vehicles/{id}/maintenance`: `scheduledStartUtc` là UTC hiện tại hoặc trước hiện tại một phút (ISO 8601 kết thúc Z), `reason` mô tả kiểm thử; end có thể bỏ trống. Không dùng ngày cố định đã cũ cho mọi lần test.
3. POST `/api/admin/fleet/maintenance/{id}/tasks` với `{"customTitleVi":"Kiểm tra thử"}`. GET tasks để lấy `rowVersion` của đúng item.
4. PUT `/api/admin/fleet/tasks/{id}` với `{"status":"Completed","note":"Đạt trong kiểm thử"}`. Header `If-Match` là rowVersion Base64 bọc nháy kép. Thiếu header: 428; dùng version cũ sau cập nhật: 412.
5. GET maintenance để lấy rowVersion của đợt. POST `/api/admin/fleet/maintenance/{id}/complete` không body, If-Match của **đợt**. Đủ checklist/guard: 204; replay complete: 409.
6. Tạo hai đợt overlap: 409; đợt quá giờ nhưng chưa release vẫn chặn. Các race thật nằm trong SQL integration runner, không thay bằng click thủ công.

## 6. Quyền và logout

Admin có quyền lát Catalog/Fleet. Manager phải có permission tương ứng; Staff không có quyền Fleet mặc định. Chưa có giao diện/API cấp quyền; không sửa Identity DB thủ công hoặc mở endpoint tự cấp quyền để thử. Dùng integration runner cho các ca Manager/Staff/thu hồi quyền/lockout.

Đăng xuất tại Admin thu hồi mọi phiên của tài khoản, kể cả cookie đã sao chép và phiên API. GET session sau logout phải bị từ chối. Không coi popup thành công là bằng chứng DB đã ghi; kiểm HTTP và SQL runner.

## 7. Phần chưa có

### Admin Fleet mới

Sau đăng nhập Admin test, mở `https://localhost:7200/fleet`. Tạo xe với ID model lấy từ `/catalog`: bỏ chọn kiểm tra để tạo nháp; chỉ chọn xác nhận Ready khi đã thực sự kiểm tra. Mở chi tiết xe Active để lập lịch bảo trì bằng **giờ Việt Nam**. Trong đợt bảo trì, thêm checklist, đánh dấu Completed hoặc Skipped có lý do rồi xác nhận kiểm tra trước hoàn tất. Nếu dữ liệu thay đổi, tải lại trang trước quyết định gửi lại. Chỉ nhập dữ liệu thử, không coi Ready là bằng chứng còn lịch trống. Xe nháp chưa có luồng activate UI; chưa edit biển/hồ sơ/retire/restore hoặc tự kích hoạt đợt Planned.

Checkpoint mới: 112 SQL integration, 22 guards, 15 notification/policy/form tests và 16 smoke PASS. Thêm `scripts/fleet-forms.test.mjs` vào lệnh node --test ở phần chuẩn bị. Các đoạn “chưa UI xe/bảo trì” của checkpoint trước bên dưới đã được thay thế bởi lát này.

Checkpoint mới ngày 11/11: Admin có trang `/catalog` để xem/tạo danh mục, hãng và mẫu nháp. Sau đăng nhập mở `https://localhost:7200/catalog`; dùng ID ở bảng để tạo mẫu. Chưa sửa/xóa/publish/ảnh hoặc UI xe/bảo trì. Nếu HTTPS báo chưa tin cậy, chạy `dotnet dev-certs https --trust` trong terminal của bạn và chấp thuận chứng chỉ development, rồi mở lại browser. Không dùng HTTP để đăng nhập. SQL integration mới PASS 84 checks và GuardSmoke PASS 22, xem PROGRESS_LOG.

Chưa có UI CRUD đầy đủ Catalog/Fleet (hiện tạo/đọc catalog, tạo/đọc xe và bảo trì/checklist/release), API publish/ảnh/biển số/retire/restore, giá/availability/booking/payment hoặc dữ liệu khách. Không thể hoàn tất quy trình thuê xe bằng hướng dẫn này. Browser accessibility/SSR hydration/Blazor disconnect và acceptance AT-01…24 vẫn là các ticket riêng.
