# Chạy và cấu hình nền tảng

13/11: cấu hình media và rollback tại [Changes](../Changes/2026-11-13-upload-anh-guid-v7-cache.md#cấu-hình-và-vận-hành). API/Admin phải cùng `Media__RootPath`; Development mặc định `../../artifacts/media` theo project content root, production cần absolute durable directory ngoài **mọi** web root. Không cấu hình static-file mapping cho thư mục đó. Sao lưu file+DB+Data Protection key ring đồng bộ; không xóa key. `Media__Enabled=false` tắt mutation ảnh cả hai host, giữ đọc. SkiaSharp 4.156.0 native Windows đã test; Linux cần native assets/verification riêng. Không đổi SQL/migration cho lát ảnh, không cần chạy SQL. Chứng chỉ localhost vẫn chưa trusted, browser QA chưa đạt.

Checkpoint 12/11: chi tiết xe có sửa hồ sơ/gán mẫu, lịch sử biển và activate/retire/restore; xem [hướng dẫn kiểm tra](TESTING_IDENTITY_CATALOG.md#checkpoint-12112026--thử-quản-lý-hồ-sơ-và-vòng-đời-xe). SQL tests mới 200+36 PASS, không thêm migration/package. Sau test API/Admin đã được khởi động lại HTTPS 7100/7200 trên SangRentalTestDb, không tự migrate. Browser vẫn cần chủ máy trust development certificate; không bỏ TLS hoặc dùng Identity Development chưa được áp dụng để thử login. Các checkpoint dưới là lịch sử.

Checkpoint Fleet 11/11: Admin SSR `/fleet`, `/fleet/vehicles/{id}` và `/fleet/maintenance/{id}` có chức năng cơ bản. Dùng `--test-database` theo hướng dẫn để tránh DB Development chưa có Identity. SQL test mới PASS 112 + 22; localhost cert vẫn chưa trusted theo kiểm tra `dotnet dev-certs https --check --trust`. Chủ máy chạy `dotnet dev-certs https --trust` và chấp thuận trước khi mở browser. Không đổi SQL TLS hoặc bỏ HTTPS để vượt lỗi này; HTTP/SQL tests không chứng minh trust/browser QA.

Local đang có nền tảng Identity và một phần Catalog/Fleet API, chưa deploy/hosting. Lệnh từ repository root; SDK trong global.json, Node trong frontend/.nvmrc. Xem README để restore/build/run. CI dùng `dotnet restore --locked-mode` và `npm ci`; cập nhật lockfile có chủ đích khi đổi packages. Hướng dẫn HTTPS/Swagger/test DB: [TESTING_IDENTITY_CATALOG](TESTING_IDENTITY_CATALOG.md).

## SQL Server và secrets

SQL Server Docker đã được kiểm tra trong Phase 2: container `sangrental-sqlserver`, Windows `localhost:1436` → container `1433`, database `SangRentalDb`. Bằng chứng 08/11 ghi nhận InitialFoundation + CatalogFleetInitial và 16 bảng trên Development; Identity chỉ apply DB test. [Báo cáo Phase 2](PHASE_2_DATABASE_CONNECTION_REPORT.md) là lịch sử trước schema. Ngày 10/11 SQL TCP local timeout; chưa xác minh lại DB runtime.

Tên connection: `ConnectionStrings:SangRental`; environment override: `ConnectionStrings__SangRental`. API đã có UserSecretsId `SangRental-Api-development`; Development tự load secrets. Ưu tiên dùng User Secrets khi không có environment override. User Secrets chỉ dành cho development, không phải kho mã hóa production.

Development lưu phần không nhạy cảm trong appsettings.Development.json: `Server=localhost,1436;Database=SangRentalDb;Encrypt=True;TrustServerCertificate=True;`. User Secrets giữ connection đầy đủ cùng credentials, không commit. `TrustServerCertificate=True` là ngoại lệ local Docker theo yêu cầu; production vẫn cần `Encrypt=True;TrustServerCertificate=False` và chứng chỉ tin cậy.

Docker hiện chỉ bind IPv4 loopback. Đã tái hiện SQL timeout 258 với localhost; TCP IPv4 kết nối được. `SqlServer:UseIPv4Loopback=true` chỉ đặt trong Development: DI đổi đúng endpoint `localhost,1436` sang `tcp:127.0.0.1,1436`, giữ database/credentials/TLS. Không đổi server khác, DNS, Docker hoặc password. Có thể tắt bằng `SqlServer__UseIPv4Loopback=false` khi môi trường localhost hỗ trợ phù hợp.

Để tự thiết lập/cập nhật User Secrets bằng nhập mật khẩu ẩn (không truyền password trong command line):

```powershell
powershell -NoProfile -File scripts/set-database-secret.ps1
dotnet build SangRental.slnx --no-restore
node scripts/database-smoke.mjs --require-ready
```

Script hỏi SQL user (Enter dùng sa) và password bằng Read-Host -AsSecureString, build connection qua DbConnectionStringBuilder, truyền JSON qua stdin đến `dotnet user-secrets set --project backend/SangRental.Api`. Script ghi đè đúng key connection này, không đổi password SQL/Docker và không in secret. Không chạy `user-secrets list` để đưa output vào báo cáo. Phiên Phase 2 đã lưu key từ credential local có sẵn và kiểm chứng lại không cần env tạm. Không tự tạo login ứng dụng ở giai đoạn này; sa chỉ dùng local thiết lập/kiểm tra, cần tài khoản ít quyền trước môi trường thật.

`node scripts/database-smoke.mjs --require-ready` khởi động/dừng API riêng, kiểm configured ready 200, thiếu cấu hình 503 và endpoint DB không truy cập được 503. Response chỉ Healthy/Unhealthy; không xuất exception/config/log SQL. Cần build Debug trước; các port 15310–15312 phải trống.

`backend/.env.example` chỉ mô tả tên biến; dotnet không tự load .env. Frontend có thể dùng `.env.local` từ `.env.example`; API_BASE_URL server-only chưa được frontend gọi dù API Catalog/Fleet nội bộ đã có.

```powershell
dotnet tool restore
dotnet ef migrations list --project backend/SangRental.Infrastructure --startup-project backend/SangRental.Api --no-connect
dotnet ef migrations script --project backend/SangRental.Infrastructure --startup-project backend/SangRental.Api --output artifacts/initial-foundation.sql
dotnet ef migrations has-pending-model-changes --project backend/SangRental.Infrastructure --startup-project backend/SangRental.Api
```

Chỉ sau khi schema/migration được review và có yêu cầu áp dụng riêng mới chạy (không thuộc Phase 2):

```powershell
dotnet ef database update --project backend/SangRental.Infrastructure --startup-project backend/SangRental.Api
```

InitialFoundation là migration nền rỗng; hai migration đầu đã được chủ dự án apply local sau Phase 2. API/Admin không Migrate/EnsureCreated/seed startup. Identity migration trên SangRentalDb cần review riêng, không dùng lệnh update chung phía trên để thử login. Test runner Identity được duyệt Migrate riêng SangRentalTestDb trên allowlist SQL local; xem hướng dẫn test. Khi schema mới được duyệt, tạo migration bằng:

```powershell
dotnet ef migrations add ApprovedMigrationName --project backend/SangRental.Infrastructure --startup-project backend/SangRental.Api --context SangRentalDbContext --output-dir Persistence/Migrations
```

Không chạy lệnh add chỉ để thử khi chưa thay đổi model được duyệt. Review script, backup và thử restore trước apply. Rollback production cần runbook/forward fix, không mặc định drop DB.

## HTTPS và production

Cookie luôn Secure; HTTP local chỉ smoke trang chưa đăng nhập. Đã có signin/logout/CLI bootstrap, CSRF, lockout và revocation. Test HTTPS API/Admin phải cùng hostname, Identity scheme/application name và key ring được bảo vệ. Còn MFA/recovery, UI user/quyền và key management production. Không đổi key ring/xóa key profile đang dùng để vượt lỗi test.

API và Admin đều có `/health/live` (không kiểm DB) và `/health/ready` (SQL connection, 503 nếu thiếu/lỗi). Ready 200 không bảo đảm schema Identity đã apply. `scripts/smoke.mjs` chạy DLL Debug đã build, 14 HTTP assertions, connection rỗng cho cả hai ứng dụng, key directory riêng dưới artifacts. Script không cần credentials/SQL và không thay kiểm login HTTPS/SQL; `artifacts/smoke-results.json` giữ kết quả lần chạy gần nhất, không phải lịch sử đầy đủ.

Trước production: cập nhật runtime, SQL integration/concurrency/E2E, chốt OPEN/CR/pháp lý/OPS, secret manager, TLS/reverse proxy và trusted forwarded headers, origin, rate limit, backup/alert/rollback. Chưa kiểm chứng deployment hoặc vận hành thật.
