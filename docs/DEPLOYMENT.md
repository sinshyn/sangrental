# Chạy và cấu hình nền tảng

Chỉ local scaffold, chưa deploy/hosting. Lệnh từ repository root; SDK trong global.json, Node trong frontend/.nvmrc. Xem README để restore/build/run. CI dùng `dotnet restore --locked-mode` và `npm ci`; cập nhật lockfile có chủ đích khi đổi packages.

## SQL Server và secrets

SQL Server Docker đã được kiểm tra trong Phase 2: container `sangrental-sqlserver`, Windows `localhost:1436` → container `1433`, database `SangRentalDb`. API đã đọc User Secrets và health ready trả 200 qua CanConnectAsync. Chưa apply migration/seed; xem [báo cáo Phase 2](PHASE_2_DATABASE_CONNECTION_REPORT.md).

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

`backend/.env.example` chỉ mô tả tên biến; dotnet không tự load .env. Frontend có thể dùng `.env.local` từ `.env.example`; API_BASE_URL server-only chưa được gọi vì chưa có API nghiệp vụ.

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

Migration InitialFoundation rỗng, apply sẽ chuẩn bị lịch sử migrations. Phase 2 không chạy database update, EnsureCreated hoặc auto migration startup. Khi schema được duyệt, tạo migration bằng:

```powershell
dotnet ef migrations add ApprovedMigrationName --project backend/SangRental.Infrastructure --startup-project backend/SangRental.Api --context SangRentalDbContext --output-dir Persistence/Migrations
```

Không chạy lệnh add chỉ để thử khi chưa thay đổi model được duyệt. Review script, backup và thử restore trước apply. Rollback production cần runbook/forward fix, không mặc định drop DB.

## HTTPS và production

Cookie luôn Secure; HTTP local chỉ smoke trang chưa đăng nhập. Identity thật cần HTTPS, provisioning riêng, MFA, revocation và Data Protection keys lưu bền/mã hóa. Hiện không signin endpoint/account bootstrap.

Trước production: cập nhật runtime, SQL integration/concurrency/E2E, chốt OPEN/CR/pháp lý/OPS, secret manager, TLS/reverse proxy và trusted forwarded headers, origin, rate limit, backup/alert/rollback. Chưa kiểm chứng deployment hoặc vận hành thật.
