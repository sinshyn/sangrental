# Upload ảnh, GUID v7 và cache — 13/11/2026

Trạng thái: đã triển khai và kiểm thử local theo xác nhận của chủ dự án. Browser visual/mobile/a11y và CI remote chưa xác minh. Không production-ready.

## Mục đích và phạm vi

- Ảnh nội dung (lát đầu: ảnh mẫu xe) decode/encode một lần thành WebP mặc định hoặc JPEG cấu hình; mỗi upload thành công có GUID v7, file và row riêng, không dedupe theo hash/tên.
- CCCD, GPLX, giấy tờ, bằng chứng bàn giao: giữ nguyên byte/định dạng/EXIF; private, không qua bộ chuyển đổi. Có primitive bảo toàn/encryption để module sau dùng, chưa tạo API hồ sơ khách/handover khi chưa có aggregate/quyền/retention.
- URL ảnh công khai bất biến cache dài hạn; preview nháp chỉ Admin/no-store. Không thay logic Fleet, booking, giá, payment hoặc Identity.

## Trước / sau

Trước: có schema VehicleModelImages, chưa upload/storage/media delivery. Sau: storage ngoài wwwroot, xử lý ảnh giới hạn tài nguyên, upload/list/preview/quản lý ảnh mẫu xe qua Admin/API; ảnh riêng tư tách pipeline. ID SQL bigint giữ nguyên, GUID v7 dùng cho tên file/storage key. Không cần migration dự kiến; không chạy SQL Development.

## Luồng và nguyên tắc

Upload nội dung → quyền/CSRF/HTTPS → giới hạn bytes/pixels/định dạng → decode, orientation, encode, bỏ metadata → GUID v7/FileMode.CreateNew → metadata/audit commit. Không ghi đè. Nếu metadata không commit, dọn đúng file mới; không xóa ảnh cũ. Mỗi lần upload là mới, UI chặn double-submit, không tự retry create.

Upload tài liệu tương lai → validation không sửa byte → lưu private mã hóa bằng Data Protection → đọc giải mã trả byte gốc. Không có public mapping cho private storage. Quyền theo hồ sơ/audit truy cập/retention còn thuộc module giấy tờ; không dùng dữ liệu khách thật trong lát này.

Ảnh public: chỉ qua endpoint kiểm điều kiện công khai; Cache-Control public,max-age=31536000,immutable và ETag. Preview: private,no-store. Nội dung/API danh mục không cache theo ảnh để tránh giữ URL cũ. Ảnh đã tải công khai có thể còn trong cache sau ẩn; không coi hide là thu hồi dữ liệu đã phát hành. Browser có thể xóa cache hoặc hard reload, không hứa chỉ tải đúng một lần mãi mãi.

## Database

Tận dụng VehicleModelImages/Translations hiện có. Không thêm bảng/cột, không tạo database/migrations hoặc SQL giả khi không đổi DB. EF model check sẽ xác minh. OPEN-07 và Identity Development giữ nguyên.

## Cách test

Kiểm thực byte đầu ra, orientation/alpha, metadata, giới hạn, file hỏng; cùng tên/nội dung thành file/ID mới; byte gốc và hash private giữ nguyên sau decrypt; private không có URL. SQL/API/Admin: quyền/CSRF, upload/metadata, version, audit, image primary/hide, cache/ETag/304, draft/private 404/no-store, không path traversal, file mới cleanup khi DB lỗi. Hồi quy bộ tests hiện có.

## Rollback

1. Đặt `Media__Enabled=false` cho **cả API và Admin**, khởi động lại host: dừng mọi mutation ảnh (upload/approve/primary/hide/retire), giữ đọc ảnh hiện có. Nếu cần ẩn liên kết UI thì hoàn nguyên riêng hunk “Quản lý ảnh” trong Catalog.razor.
2. Giữ nguyên Media:RootPath, database và Data Protection key ring; sao lưu đồng bộ trước khi thay host. Không xóa row/blob/key ring.
3. Nếu rollback code hoàn toàn, khôi phục đúng các hunk được liệt kê ở bảng file dưới đây và remove các file **mới của lát này** sau review. Không `git reset --hard`, không restore toàn bộ file dựa HEAD vì nhiều file đã có thay đổi/untracked từ phiên trước. Restore lockfiles theo snapshot tương ứng, restore/build/test lại. Có thể giữ additive services/package và chỉ tắt flag để giảm rủi ro.
4. Không có SQL rollback/migration. Public URL đã cache không thể thu hồi từ browser bằng rollback code. Không tự xóa ảnh cũ hoặc đặt retention mới.

## Danh sách file thêm/sửa/xóa

Các file “sửa” là so với đầu phiên này; nhiều file đã untracked/modified từ trước. Không gộp những thay đổi trước đó vào phạm vi upload ảnh.

| Loại | File | Nội dung / hàm |
|---|---|---|
| Thêm | `backend/SangRental.Application/Media/MediaContracts.cs` | Purpose tách Catalog/IdentityDocument/DrivingLicense/Document/HandoverEvidence; DTO/interface/validation |
| Thêm | `backend/SangRental.Infrastructure/Media/MediaOptions.cs` | Enabled, root, giới hạn và output format/quality |
| Thêm | `backend/SangRental.Infrastructure/Media/ImageProcessingService.cs` | PrepareAsync: bounded read, kiểm magic/đuôi/codec/pixels, EXIF orientation, encode; nhánh private trả raw byte |
| Thêm | `backend/SangRental.Infrastructure/Media/LocalMediaStorage.cs` | SaveNewAsync GUID v7/FileMode.CreateNew; OpenCatalogRead tách ReadPrivateOriginal; root/key validation; encrypt private, cleanup uncommitted |
| Thêm | `backend/SangRental.Infrastructure/Media/CatalogMediaEndpoints.cs` | MapCatalogMediaEndpoints: list/upload/action/preview/public; CSRF/HTTPS/Admin, limits, cache và protected flash |
| Thêm | `backend/SangRental.Infrastructure/Persistence/CatalogImageService.cs` | UploadAsync metadata/translation/audit + compensation; ChangeAsync approve/primary/hide/retire + rowversion/SQL lock |
| Thêm | `admin/SangRental.Admin/Components/Pages/CatalogImages.razor` | Native multipart SSR, inline errors, preview/list/actions, confirmation/loading qua adapter hiện có |
| Thêm | `tests/SangRental.Identity.Integration/MediaTests.cs` | Native image/storage tests và SQL/API/SSR/cache/rollback tests |
| Thêm | `Changes/2026-11-13-upload-anh-guid-v7-cache.md` | Hồ sơ thay đổi này |
| Sửa | `backend/SangRental.Infrastructure/DependencyInjection.cs` | AddInfrastructure đăng ký options/service và validate config |
| Sửa | `backend/SangRental.Infrastructure/SangRental.Infrastructure.csproj` | Thêm SkiaSharp 4.156.0 |
| Sửa | `backend/SangRental.Api/Program.cs` | MapCatalogMediaEndpoints, không đổi auth/migrations |
| Sửa | `admin/SangRental.Admin/Program.cs` | MapCatalogMediaEndpoints UI mode; không re-execute lỗi /api và /media sang trang HTML |
| Sửa | `admin/SangRental.Admin/Components/Pages/Catalog.razor` | Liên kết quản lý ảnh theo mẫu |
| Sửa | `admin/SangRental.Admin/wwwroot/app.css` | Ảnh preview responsive |
| Sửa | `backend/.env.example` | Tên biến media/config mẫu; không secret |
| Sửa | `tests/SangRental.Identity.Integration/Program.cs` | --media-only, gọi media tests, kiểm quyền Staff/Manager |
| Sửa | `scripts/smoke.mjs` | Thêm 2 ca bảo vệ trang/API ảnh (18 tổng) |
| Sửa | `.github/workflows/ci.yml` | Chạy --media-only sau build; chưa chạy CI remote |
| Sửa | `backend/SangRental.Infrastructure/packages.lock.json`, `backend/SangRental.Api/packages.lock.json`, `admin/SangRental.Admin/packages.lock.json`, `tests/SangRental.Identity.Integration/packages.lock.json`, `tests/SangRental.CatalogFleet.GuardSmoke/packages.lock.json` | Lock SkiaSharp/native dependencies được truyền qua project references |
| Sửa | `README.md`, `docs/API_CONTRACTS.md`, `docs/ARCHITECTURE.md`, `docs/SECURITY_PRIVACY.md`, `docs/DEPLOYMENT.md`, `docs/TESTING_IDENTITY_CATALOG.md`, `docs/TEST_MATRIX.md`, `docs/TASK_BOARD.md`, `docs/PROGRESS_LOG.md`, `docs/DECISIONS.md` | Hướng dẫn, quyết định đã duyệt, hợp đồng và bằng chứng |
| Xóa | Không | Các file nguồn docs cũ hiện deleted trong git là trạng thái có sẵn trước phiên, không do lát này |

## Chi tiết trước / sau và phạm vi ảnh hưởng

| Trước | Sau |
|---|---|
| Chưa có upload ảnh mẫu xe | POST multipart qua Admin/API, ảnh mới Draft, nguồn/quyền/alt VI bắt buộc |
| Chưa có storage policy | File ngoài wwwroot; tên UTC yyyy/MM/dd + GUID v7, không tên gốc/không overwrite/không dedupe |
| Chưa có image codec | Decode JPEG/PNG/WebP/GIF/BMP thật; Catalog chỉ ảnh tĩnh, output WebP hoặc JPEG; sửa orientation, bỏ EXIF, giữ alpha WebP/JPEG nền trắng |
| Chưa có private-original primitive | Bốn purpose private giữ nguyên byte/format/EXIF, mã hóa Data Protection; read giải mã đúng raw bytes. Không có route public hoặc API upload giấy tờ |
| Chưa có media cache | Preview private,no-store; public chỉ khi approved+visible+not retired và model/category Published/Active, brand Active. URL chứa file GUID bất biến; Cache-Control public,max-age=31536000,immutable + ETag/304 |
| Một ảnh chính đã có DB index nhưng chưa workflow | Transaction Serializable + applock theo model, bỏ primary cũ/save rồi gán mới, giữ blob và history; rowversion chống stale/replay |

Không thay Customer/IdentityDocument/Handover aggregates chưa tồn tại. Không sửa bảng/entity/migration, Fleet lifecycle, auth provider/cookie/quyền sẵn có, booking/pricing/payment hoặc frontend preview. Không tự xuất bản mẫu hoặc biến toàn bộ nội dung thành public. Purpose do module server chọn, không nhận từ form: catalog không phải nơi upload giấy tờ. Việc nhận biết giấy tờ không dựa vào AI đoán nội dung ảnh.

## Hợp đồng HTTP

Các route được map trên API và Admin; dùng cùng service, store và key ring.

| Method/path | Quyền và hành vi |
|---|---|
| GET `/api/admin/catalog/models/{modelId}/images?page=1` | Admin; 50/trang; metadata/alt/version, private,no-store |
| POST cùng path | Admin + HTTPS + CSRF; multipart một `file`, `source`, `rights`, `altText`; 201 `{id}` |
| GET `…/images/{imageId}/content` | Admin; preview cả nháp/ảnh cũ, private,no-store |
| POST `…/images/{imageId}/{action}` | Admin + HTTPS + CSRF; action approve/primary/hide/retire; If-Match của **ảnh** quoted Base64 8 byte; 204 |
| GET `/media/catalog/{imageId}/{guid-file-name}` | Anonymous chỉ khi đủ điều kiện public; 404/no-store nếu không đủ hoặc filename không đúng; public cache/ETag nếu đủ |
| GET `/catalog/models/{modelId}/images` | Admin SSR quản lý ảnh; form native multipart gửi cùng endpoint trên Admin với ui=1; protected flash và redirect sau commit |

Mã lỗi: 400 file/format/metadata/CSRF, 401/403 auth/quyền, 404 không tìm thấy, 409 trạng thái không phù hợp, 412 stale version, 413 quá dung lượng/form, 428 thiếu If-Match, 503 disabled/storage/DB unavailable. API lỗi không trả filename/upload bytes/SQL stack. UI lỗi thông thường redirect về hiển thị inline, success từ flash bảo vệ sau commit; không lấy success từ query. SweetAlert2 dùng generic i18n keys hiện có, không thêm thư viện thông báo.

## Cấu hình và vận hành

Đọc env/user-secrets/config theo host .NET; `.env.example` không tự được load. Defaults:

| Key | Giá trị / ý nghĩa |
|---|---|
| `Media:Enabled` | true; false tắt mutation ảnh trên cả hai host |
| `Media:RootPath` | `../../artifacts/media` khi Development; resolve theo content root API/Admin (cùng vị trí repo); production bắt buộc absolute, durable, ngoài mọi web root |
| `Media:ContentFormat` | `webp`, hỗ trợ `jpg`; output type/đuôi thật đồng nhất |
| `Media:Quality` | 82, cấu hình 1–100 |
| `Media:MaxUploadBytes` | 10485760 (10 MiB), hard upper config 25 MiB |
| `Media:MaxPixels` / `MaxDimension` | 20 triệu / 10000; kiểm trước native allocation; tối đa 2 decode đồng thời mỗi host |

JPEG, PNG, WebP, BMP, GIF được kiểm codec+đuôi. Catalog GIF/WebP động bị từ chối, không âm thầm mất frame. HEIC/SVG/PDF chưa hỗ trợ; từ chối rõ, không đổi đuôi hoặc tự chuyển giấy tờ. Không resize ảnh. Private bản gốc trên đĩa là ciphertext; gọi primitive giải mã mới thu lại ảnh gốc. Hash private là hash raw bytes, hash catalog là hash output. Không lấy filename người dùng làm path.

SkiaSharp **4.156.0** trực tiếp, native Win32/macOS tương ứng đi theo dependency. Windows được kiểm thực; Linux cần thêm native asset phù hợp và kiểm triển khai riêng trước hosting Linux. [NuGet chính thức](https://www.nuget.org/packages/SkiaSharp/4.156.0), giấy phép MIT. Không thêm Redis/service worker/CDN hoặc kho giấy tờ khách. Data Protection key ring phải được sao lưu/bảo vệ dùng chung trước môi trường thật; không xóa key để thử.

Nếu ghi DB thất bại: chỉ dọn file mới sau khi kiểm tra DB thành công xác nhận không có row tham chiếu. Nếu commit/connection không rõ hoặc cleanup lỗi, giữ file và cảnh báo chung để đối soát; không xóa mù dữ liệu có thể đã commit. Không có job tự xóa file hoặc chính sách retention mới.

## Kiểm thử thực tế và cách chạy

Từ root, đã chạy:

```powershell
dotnet restore SangRental.slnx --locked-mode
dotnet build SangRental.slnx --no-restore
dotnet run --project tests/SangRental.Identity.Integration --no-build --no-restore -- --media-only
dotnet run --project tests/SangRental.Identity.Integration --no-build --no-restore
dotnet run --project tests/SangRental.CatalogFleet.GuardSmoke --no-build --no-restore
node --test scripts/notifications.test.mjs scripts/ui-policy.test.mjs scripts/fleet-forms.test.mjs
node scripts/smoke.mjs
dotnet ef migrations has-pending-model-changes --project backend/SangRental.Infrastructure --startup-project backend/SangRental.Api --no-build
dotnet list backend/SangRental.Infrastructure/SangRental.Infrastructure.csproj package --vulnerable --include-transitive
```

Kết quả: restore locked PASS; .NET build 0 warning/error; **33 media primitive**, **246 SQL Identity/API/SSR**, **36 SQL guards**, **15 JS tests**, **18 smoke** PASS; EF không pending changes; NuGet audit online không có vulnerable packages trên Infrastructure theo nguồn hiện hành. Hai SQL runners chạy tuần tự, chỉ SangRentalTestDb được ghi/migrate, Development vẫn 16 Catalog/0 Identity/2 migrations. Guard runner rollback. Media-only không dùng SQL, giữ fixture dưới artifacts/media-tests; SQL runner giữ fixture/audit và disable/revoke tài khoản automation như trước.

Test ảnh kiểm byte decode thực, EXIF rotation/strip, alpha, JPEG output, 8 upload cùng nội dung đồng thời khác GUID/file, purpose private decrypt giống raw bytes, ciphertext khác raw, key traversal bị chặn, bytes/pixels/extension/disabled. SQL kiểm cùng tên+nội dung thành row/file mới, source/rights/alt/audit, nháp không public, approval/version/primary race, ETag 304 và immutable, retire giữ blob, invalid/oversize không metadata, cleanup khi DB constraint fail, form SSR multipart + antiforgery và oversized inline. Hồi quy quyền Manager/Staff và Fleet.

Chưa chạy browser visual/mobile/a11y, CI remote, Linux hoặc filesystem crash/load test. HTTPS cert localhost còn hạn nhưng chưa trusted, chưa sửa trust store. Frontend/dependencies frontend không đổi nên không chạy lại Next build/lint/typecheck; HTTP smoke dùng bản frontend build đã có. Không chứng nhận production từ các test này.

## Chủ dự án tự kiểm tra

1. Dùng API/Admin HTTPS trên DB test theo README, đăng nhập Admin; nếu lỗi certificate, chạy `dotnet dev-certs https --trust` và chấp thuận trên máy của bạn.
2. `/catalog` → mẫu xe → **Quản lý ảnh**. Chỉ upload ảnh xe/dữ liệu giả, điền source/rights/alt.
3. Upload cùng file hai lần: thấy hai ID; kiểm SQL row và storage key khác nhau. Preview là WebP mặc định; không xuất bản mẫu tự động.
4. Duyệt sau xác minh quyền/ảnh, chọn ảnh chính; tab cũ gửi version cũ phải bị chặn. Ẩn/ngừng sử dụng giữ file cũ.
5. Public URL chỉ hoạt động khi module xuất bản đã tạo trạng thái hợp lệ. Hiện chưa có UI publish model/category; không tự sửa DB Development để “thử public”. Bộ integration có fixture DB test để kiểm Cache-Control/ETag/304. Khi có trang chính/API catalog thật sẽ dùng URL bất biến này, không dùng query timestamp mỗi lần render.
6. CCCD/GPLX/giấy tờ/handover không upload tại màn hình này. Nhánh giữ gốc/private đã được unit kiểm chứng; UI/API theo hồ sơ sẽ triển khai cùng module đó, giữ OPEN-07.
