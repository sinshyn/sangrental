# Phase 2 — SQL Server database connection

Ngày: 07/11/2026 (Asia/Saigon). Phạm vi: kết nối và chuẩn bị EF Core; không tạo entity, schema nghiệp vụ, authentication, booking hoặc payment.

## Kết quả

**PASS: ASP.NET Core đã kết nối thực tế tới SangRentalDb thông qua DI, SangRentalDbContext và Database.CanConnectAsync().** API chạy Development đọc credentials từ User Secrets (không có environment connection tạm ở lần kiểm cuối), `/health/live` 200, `/health/ready` 200 với body `Healthy`. Nhánh thiếu config và DB unreachable trả 503 `Unhealthy`. Không apply migration; kiểm `sys.tables` cuối phiên có **0 user tables**.

| Hạng mục | Trạng thái / bằng chứng |
|---|---|
| Cấu trúc | PASS: giữ 4 backend project Clean Architecture, không scaffold trùng |
| DbContext | PASS: `SangRental.Infrastructure.Persistence.SangRentalDbContext`, scoped AddDbContext, chưa DbSet/entity nghiệp vụ |
| Composition root | PASS: Api Program gọi AddInfrastructure(builder.Configuration); Infrastructure đăng ký SQL provider/DbContext/readiness |
| Connection key | `ConnectionStrings:SangRental` (GetConnectionString("SangRental")); env override `ConnectionStrings__SangRental` |
| Development base | `Server=localhost,1436;Database=SangRentalDb;Encrypt=True;TrustServerCertificate=True;` trong appsettings.Development.json, không credentials |
| Credentials | PASS: UserSecretsId `SangRental-Api-development`; đã lưu 1 key connection bằng stdin từ credential Docker local sẵn có, không in giá trị, không ghi repo |
| Docker | PASS: CLI cài theo user, container sangrental-sqlserver đang chạy, `127.0.0.1:1436->1433/tcp`; không thay Docker/password |
| SQL DB | PASS: SELECT 1 trong SangRentalDb; ASP.NET Core CanConnectAsync true |
| Logging/health | PASS trong phạm vi kiểm: generic body, không trả exception, password hoặc connection string; không bật sensitive-data logging |
| EF CLI/migration | PASS: 10.0.13, liệt kê InitialFoundation offline, model không pending changes, sinh SQL thành công; không execute SQL |
| Schema | Không triển khai; snapshot/migration cũ giữ nguyên, bảng nghiệp vụ không được tạo |
| Production TLS/ít quyền | NOT VERIFIED: TrustServerCertificate=True và sa chỉ local; chưa provisioning app login/production cert/test DB riêng |

## Sự cố localhost và cách xử lý

Cổng TCP localhost:1436 nhận kết nối, nhưng SqlClient với localhost hoặc tcp:localhost trả SQL error **258** (timeout). DNS local trả cả ::1 và 127.0.0.1; Docker chỉ publish IPv4. Probe .NET dùng tcp:127.0.0.1 mở kết nối được. Tùy chọn IPAddressPreference=IPv4First đã thử nhưng không giải quyết trên môi trường này và **không giữ trong code cuối**.

Giải pháp cuối: `SqlServer:UseIPv4Loopback=true` trong Development. Infrastructure dùng SqlConnectionStringBuilder, chỉ đổi DataSource nếu đúng `localhost,1436` thành `tcp:127.0.0.1,1436`. Cấu hình gốc vẫn giữ host/port yêu cầu; runtime chọn loopback IPv4 đã kiểm. Không đổi database, user/password, Encrypt hoặc TrustServerCertificate; không rewrite server khác. Production không bật tùy chọn này mặc định. Không thay hosts/DNS, Docker hoặc password sa. Kết quả HTTP 200 chỉ được ghi sau khi kiểm workaround thực tế và sau khi API tự đọc User Secrets.

## File tạo/sửa

- Sửa `backend/SangRental.Api/appsettings.Development.json`: base connection không credential, opt-in local IPv4.
- Sửa `backend/SangRental.Infrastructure/DependencyInjection.cs`: normalize đúng endpoint local bằng SqlConnectionStringBuilder khi opt-in; giữ DbContext/provider/health hiện có.
- Tạo `scripts/set-database-secret.ps1`: nhập password ẩn, escape connection bằng builder, JSON stdin vào User Secrets, không đưa secret vào command arguments/history/repo. Chỉ set connection key, không đổi SQL login. Đã parse cú pháp; đường stdin user-secrets được thực thi thành công trong phiên. Không tự động chạy tương tác yêu cầu nhập password.
- Tạo `scripts/database-smoke.mjs`: khởi động/dừng API, kiểm config thật/thiếu config/unreachable, generic health; `--require-ready` bắt buộc configured 200. Không echo application log/secret.
- Tạo báo cáo này; sửa DEPLOYMENT, ARCHITECTURE, DECISIONS, OPEN_QUESTIONS, TASK_BOARD, SECURITY_PRIVACY, PROGRESS_LOG và README để ghi trạng thái hiện tại. Không sửa nguồn v2.7/HTML hoặc PHASE_1_READINESS_REPORT lịch sử.
- Ngoài repo: ghi key `ConnectionStrings:SangRental` vào .NET User Secrets của API. Không sao chép nội dung vào tài liệu. `.env` Docker chỉ đọc, giữ nguyên.
- Artifact gitignored: probe chẩn đoán SqlClient dưới `artifacts/sql-connection-probe/` và SQL script `artifacts/phase2-foundation.sql`, không đưa vào solution/CI và không chứa credential được ghi ra file.

## Package

Không cài/thêm/nâng NuGet package. Infrastructure sử dụng Microsoft.EntityFrameworkCore.SqlServer 10.0.13, Microsoft.Extensions.Diagnostics.HealthChecks 10.0.13 và System.Security.Cryptography.Pkcs 10.0.13 hiện có. Microsoft.Data.SqlClient 6.1.7 là dependency transitive của SQL Server provider, dùng builder chuẩn; không thêm reference trùng. Api có Microsoft.EntityFrameworkCore.Design 10.0.13 (PrivateAssets=all), Microsoft.AspNetCore.OpenApi 10.0.13. Application chỉ DI abstractions 10.0.13; Domain không EF/package ngoài. dotnet-ef local tool 10.0.13 từ .config/dotnet-tools.json.

## Lệnh đã chạy

| Lệnh/kiểm tra | Kết quả |
|---|---|
| git status, rg --files, đọc AGENTS/docs/code/csproj | Đã kiểm thực tế; scaffold vẫn phần lớn untracked, không commit/push |
| dotnet restore SangRental.slnx --locked-mode | PASS; dùng quyền phù hợp cho NuGet.Config/cache ngoài sandbox |
| dotnet tool restore; dotnet ef --version | PASS, 10.0.13 |
| dotnet list SangRental.slnx package --no-restore | Sandbox bị chặn NuGet.Config; quyền phù hợp PASS, phiên bản trên |
| dotnet build SangRental.slnx --no-restore | Lần cuối PASS cả 5 project, 0 warning/0 error. Một lần thử dùng GetValue bị CS1061 do chưa reference Configuration.Binder; đã sửa bằng bool.TryParse, không cài package |
| TCP localhost:1436 | PASS; không coi TCP accept là xác minh SQL login |
| Docker ps + SELECT 1 qua sqlcmd stdin | PASS; lần đầu quote query lỗi syntax, sửa stdin rồi SELECT 1 thành công; không thay schema |
| SqlClient diagnostic | localhost timeout 258; tcp:127.0.0.1 PASS; chỉ xuất mã lỗi, không exception/password |
| node scripts/database-smoke.mjs --require-ready | Trước workaround FAIL configured 503; sau workaround PASS 200/503/503 với env tạm; sau lưu User Secrets PASS lại 200/503/503 không env tạm |
| Parser PowerShell cho set-database-secret.ps1; node --check scripts/database-smoke.mjs | PASS cú pháp |
| dotnet ef migrations list --project backend/SangRental.Infrastructure --startup-project backend/SangRental.Api --no-build --no-connect | PASS: 20261107050736_InitialFoundation; offline nên không xác định trạng thái applied từ lệnh này |
| dotnet ef migrations has-pending-model-changes --project backend/SangRental.Infrastructure --startup-project backend/SangRental.Api --no-build | PASS: không thay model |
| dotnet ef migrations script --project backend/SangRental.Infrastructure --startup-project backend/SangRental.Api --no-build --output artifacts/phase2-foundation.sql | PASS: chỉ SQL history từ migration nền; không execute |
| SELECT COUNT(*) FROM sys.tables trong SangRentalDb | PASS: 0 user tables cuối phiên |

Không chạy migrations add/database update/EnsureCreated, seed, auth hoặc transaction nghiệp vụ. Không chạy lại frontend/admin UI tests vì không sửa frontend/admin. Chưa kiểm SQL concurrency/idempotency/booking/payment hay production deployment; OPEN-01/05/07, CR-01…04 và OPS giữ nguyên.

## Cấu hình lại và migrations tương lai

Không cần gửi mật khẩu vào chat. Khi cần tự nhập hoặc đổi credential local trong User Secrets:

```powershell
powershell -NoProfile -File scripts/set-database-secret.ps1
dotnet build SangRental.slnx --no-restore
node scripts/database-smoke.mjs --require-ready
```

Chạy API bình thường bằng:

```powershell
dotnet run --project backend/SangRental.Api --no-launch-profile -- --urls http://localhost:5100 --environment Development
```

Environment connection nếu có sẽ override User Secrets; .NET không tự load `.env`. Không đưa secret vào appsettings/git, không dùng user-secrets list trong output chia sẻ. User Secrets không phải encrypted production store.

Sau khi thiết kế model/schema được duyệt, có thể tạo migration (không chạy trong Phase 2):

```powershell
dotnet ef migrations add ApprovedMigrationName --project backend/SangRental.Infrastructure --startup-project backend/SangRental.Api --context SangRentalDbContext --output-dir Persistence/Migrations
dotnet ef migrations script --project backend/SangRental.Infrastructure --startup-project backend/SangRental.Api --output artifacts/review-migration.sql
```

Review schema/script trước mọi apply. Bước tiếp theo là thiết kế catalog/fleet và kế hoạch FK/index/soft-disable/test DB riêng; chưa triển khai booking/payment/authentication. Chốt tài khoản ứng dụng ít quyền và cert tin cậy trước môi trường thật.
