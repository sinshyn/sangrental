# SangRental — PHASE 1 READINESS REPORT

Ngày kiểm tra: **07/11/2026**, múi giờ Asia/Saigon. Phạm vi: xác minh nền tảng khởi tạo hiện có trước schema/nghiệp vụ. Không triển khai booking, payment hoặc authentication nghiệp vụ; không apply migration, đọc `.env`, tạo tài khoản, commit/push hay deploy.

## Kết luận

**Sẵn sàng có điều kiện để thiết kế schema/catalog; chưa đủ điều kiện xác nhận tích hợp database hoặc triển khai nghiệp vụ production.** Backend/Admin build được, frontend production build và development HTTP chạy được. Một lỗi logging thực tế đã được sửa và kiểm tra lại. Cần SQL Server dev/test cùng cấu hình kết nối an toàn trước migration apply/integration tests. UI browser QA và quyết định identity vẫn còn mở.

PASS chỉ áp dụng cho phạm vi và bằng chứng ghi trong từng dòng. FAIL là kiểm tra đã thất bại hoặc yêu cầu chưa có; NOT VERIFIED là chưa có bằng chứng thực thi. Các tính năng thuộc pha sau được ghi thiếu có chủ đích, không coi scaffold là sản phẩm hoàn chỉnh.

## Tài liệu và trạng thái repository

Đã đọc AGENTS.md; hai nguồn v2.7; quy chuẩn SweetAlert2/Font Awesome; bộ tài liệu API_CONTRACTS, ARCHITECTURE, DECISIONS, DEPLOYMENT, DESIGN_SYSTEM, OPEN_QUESTIONS, PROGRESS_LOG, PROJECT_RULES, REQUIREMENTS, SECURITY_PRIVACY, TASK_BOARD, TEST_MATRIX, UIUX_V27_ADDENDUM và README. Đã kiểm nội dung/tham chiếu HTML desktop/mobile: đây là nguồn thiết kế có dữ liệu minh họa, Material Symbols/CDN và cam kết chưa xác minh, không phải mã ứng dụng. Không thay nguồn v2.7/HTML.

Git đầu phiên: chỉ bốn nguồn v2.7/HTML được tracked; scaffold, AGENTS và các tài liệu dẫn xuất đang untracked. Không có thay đổi tracked trước audit. Việc build local đạt không chứng minh CI có đủ source trước khi chủ dự án review/add/commit các tệp cần thiết. `.env` tồn tại và bị ignore; nội dung không được đọc hoặc sao chép.

Môi trường thực tế: Windows, SDK 10.0.203, MSBuild 18.3.3, shared runtime ASP.NET/Core 10.0.7; Node 24.18.0, npm 11.16.0. SDK pin trong global.json, Node 24 trong frontend/.nvmrc và package engines. Không tìm thấy lệnh docker trong PATH; chưa xác minh Docker Engine hoặc SQL Server đang chạy. compose.yaml có SQL Server 2022 Developer, bind 127.0.0.1:1436, volume bền vững và password lấy từ environment; chưa chạy compose.

## Backend

| Hạng mục | Kết quả | Bằng chứng / giới hạn |
|---|---|---|
| Solution và 4 backend project | PASS | SangRental.slnx chứa Domain, Application, Infrastructure, Api và 1 Admin; tất cả net10.0 |
| References Clean Architecture | PASS | Application → Domain; Infrastructure → Application; Api → Application + Infrastructure; Domain không package/ref ngoài |
| Dependency injection | PASS | AddApplication đăng ký TimeProvider; AddInfrastructure đăng ký DbContext và health check; API resolve và phục vụ HTTP |
| EF Core 10 + SQL Server provider | PASS | EF SqlServer/Design 10.0.13, UseSqlServer; model/migration tooling chạy offline |
| Schema nghiệp vụ | NOT VERIFIED | DbContext chưa DbSet; InitialFoundation Up/Down rỗng; chưa aggregate/FK/index/rowversion/locking |
| OpenAPI Development | PASS | GET /openapi/v1.json 200, JSON có version; chưa có resource nghiệp vụ |
| OpenAPI bị ẩn Production | NOT VERIFIED | Có điều kiện IsDevelopment trong code; chưa chạy Production để kiểm HTTP |
| Health sống và thiếu DB | PASS | /health/live 200; /health/ready 503 với body Unhealthy khi override connection rỗng |
| Readiness kết nối SQL thật | NOT VERIFIED | Chưa cấp/kiểm connection, schema hoặc database |
| Configuration/secrets nền tảng | PASS | UserSecretsId API, configuration/environment, ví dụ connection rỗng; không hard-code credential tìm thấy trong source đã rà |
| Structured logging | PASS | JSON console thực tế; sửa lỗi EventLog thiếu quyền gây 500 ở readiness; smoke lại 503 đúng |
| ProblemDetails/exception nền tảng | PASS | AddProblemDetails, UseExceptionHandler/StatusCodePages; GET missing trả 404 application/problem+json; lỗi logging trước sửa đã trả JSON 500 nhưng ngắt stream |
| Exception nghiệp vụ/PII redaction đầy đủ | NOT VERIFIED | Chưa use case/middleware error codes/redaction tests; không EnableSensitiveDataLogging trong source |
| Restore/build/chạy API | PASS | Locked restore 5 project; build cuối 0 warning/error; HTTP smoke |
| Migration apply/transaction/concurrency | NOT VERIFIED | Chỉ list --no-connect, kiểm model và sinh SQL; không cập nhật DB |

## Frontend

| Hạng mục | Kết quả | Bằng chứng / giới hạn |
|---|---|---|
| Next App Router/React/TypeScript | PASS | src/app/layout.tsx/page.tsx; Next 16.4.5, React 19.3.1, TS strict 5.9.3; typecheck/build đạt |
| Tailwind và chuẩn bị shadcn/ui | PASS | Tailwind/PostCSS 4.3.5, globals tokens, components.json, alias @, cn utility, components/ui/button.tsx; Button nội bộ, chưa tuyên bố toàn bộ shadcn đã cài |
| Font Awesome import chọn lọc | PASS | Named import faCompass, config.autoAddCss=false, CSS core riêng, SVG trang trí aria-hidden; policy guard đạt |
| Tree-shaking/bundle đo thực tế | NOT VERIFIED | Không whole-pack import trong source; chưa dùng bundle analyzer/định lượng bundle. Không suy từ named import thành kết quả đo |
| NotificationService tập trung | PASS | Client module, dynamic import SweetAlert2, kiểm window khi invoke, shared notification-core; không Swal.fire trong component |
| API notification/confirm/loading/toast | PASS | 8 logic tests dùng engine giả: boolean confirm/cancel, ownership loading, lỗi loader/reject, khóa lời gọi trùng, text an toàn, key lạ |
| Thư viện thông báo bị cấm | PASS | Rà source/dependencies và policy test: không browser alert/confirm/prompt, Sonner, React Hot Toast, React Toastify, shadcn toast trong ứng dụng |
| i18n thông báo | PASS | vi mặc định, en/ko/zh-CN/ja/th, dictionary đủ key và fallback VI; tests đạt |
| i18n toàn website/locale routing | FAIL | UI shell còn chuỗi tiếng Việt trực tiếp; chưa locale routes/selector/Google Translation/cache. Thiếu có chủ đích ở scaffold |
| Production build và HTTP | PASS | Next build/prerender / và /_not-found; smoke / 200, lang=vi, thông báo chưa nhận đặt xe |
| Development server | PASS | next dev frontend trên 127.0.0.1:13003, GET / 200 và lang=vi bằng Node fetch |
| Lint | FAIL | package.json chưa có script lint/cấu hình ESLint; chưa chạy lint, không gộp typecheck thành lint |
| Browser SSR/hydration/a11y/responsive | NOT VERIFIED | HTTP SSR đạt không chứng minh hydration; chưa keyboard/focus/screen reader/SweetAlert DOM/visual QA |
| Payment/refund success đáng tin cậy | NOT VERIFIED | Chưa có module hoặc message thành công tài chính; không tạo success giả để kiểm |

## Blazor Admin

| Hạng mục | Kết quả | Bằng chứng / giới hạn |
|---|---|---|
| Blazor Web App .NET 10/layout/DI | PASS | AddRazorComponents + InteractiveServerComponents, MainLayout/Routes/AuthorizeRouteView, NotificationService scoped; restore/build/HTTP chạy |
| Icon Font Awesome | PASS | faGauge được xuất thành đúng 1 SVG tự host; img trang trí ẩn screen reader; license giữ trong vendor |
| SweetAlert2 JS Interop | PASS | notifications.js ES module, engine Swal tự host, C# lazy module import; module/assets/6 locales trả HTTP 200 |
| NotificationService API | PASS | Success/Error/Warning/Info/Confirm/Loading/CloseLoading/Toast; semaphore khởi tạo module; IAsyncDisposable bắt JSDisconnectedException khi dispose |
| Vòng đời circuit và popup thực | NOT VERIFIED | Chưa màn hình interactive gọi service, chưa invoke qua circuit thực, disconnect/reconnect/disposal race hoặc browser QA |
| Auth shell không default account | PASS | Login khóa, không SignInAsync/bootstrap/seed account; cookie HttpOnly/Secure/Strict, fallback policy và AdminAccess server-side |
| Anonymous guard | PASS | Login 200, dashboard 302 tới login, /auth/session 401 |
| Authenticated roles/revocation/CSRF mutation | NOT VERIFIED | Không identity store/tài khoản/signin/mutation; cookie admin chưa chứng minh bảo vệ API liên ứng dụng |
| Production error pages | NOT VERIFIED | Có middleware; Error/NotFound vẫn template tiếng Anh và chưa [AllowAnonymous], cần rà trước khi mở production, chưa kiểm đường lỗi thực |

## Chất lượng và lỗi cần xử lý

| ID / mức độ | Kết quả | Phát hiện / hướng xử lý |
|---|---|---|
| P1-01 / Cao, đã sửa | PASS sau sửa; FAIL trước sửa | Windows EventLog provider ném Cannot open log for source '.NET Runtime', Access denied. Readiness thiếu DB thành 500 và stream bị ngắt. ClearProviders + AddJsonConsole ở API/Admin, không đổi nghiệp vụ hoặc quyền OS. Build cuối sạch, smoke 12/12 |
| P1-02 / Trung bình, gate DB | NOT VERIFIED | Chưa SQL Server dev/test, chưa apply migration/ready 200/integration. Chuẩn bị instance, TLS và least-privilege connection secret trước schema thực thi |
| P1-03 / Trung bình, gate UI | NOT VERIFIED | Chưa browser QA/tree-shaking định lượng/Blazor interop thực; hoàn tất trước màn hình tương tác đầu tiên |
| P1-04 / Trung bình, gate source delivery | FAIL | Phần lớn scaffold vẫn untracked. Review tệp, secrets rồi add/commit có chủ đích trước khi dùng CI/clone làm baseline; phiên audit không commit |
| P1-05 / Thấp | FAIL | Chưa lint frontend; thêm cấu hình/script/CI khi tiếp tục frontend |
| P1-06 / Thấp, pha sau | FAIL | Chưa i18n toàn ứng dụng, Admin Error/NotFound còn template tiếng Anh; không ảnh hưởng build nền tảng |
| P1-07 / Hạ tầng | NOT VERIFIED | Runtime host 10.0.7 khác package 10.0.13; chưa đánh giá mức vá mới nhất/support, không cập nhật cài đặt hệ thống trong audit |
| P1-08 / Phòng ngừa | NOT VERIFIED | .gitignore chưa có quy tắc riêng certificate/private key/backup/coverage. Chưa phát hiện tệp đó trong inventory; rà trước khi thêm artifact nhạy cảm |

Không tìm thấy mật khẩu/API key/connection credential hard-code hoặc default admin trong mã/config đã rà; đây là review source hiện tại, không thay secret scanner toàn lịch sử. `git ls-files` chỉ có 4 nguồn docs, không secrets tracked. `.env`, `.env.local`, appsettings.Local.json, bin/obj/node_modules/.next/artifacts/tsbuildinfo được git check-ignore xác nhận. `.env.example` được phép commit. Không đọc user-secrets hoặc thông tin đăng nhập.

`npm ls --depth=0` không báo unmet/invalid/extraneous; các dependency có mục đích (UI/CSS/icons/notification/typecheck). Pkcs direct pin theo DECISIONS để xử lý dependency transitive; không tự gỡ. npm audit toàn dependency và NuGet vulnerable --include-transitive hiện không báo vulnerability. Không tắt audit, không thêm package. Không coi kết quả registry hiện tại là cam kết an toàn vĩnh viễn.

## Lệnh và bằng chứng phiên này

| Lệnh | Kết quả thực tế |
|---|---|
| git status --short; rg --files; git ls-files | Đã kiểm cấu trúc và trạng thái tracked/untracked |
| dotnet --info; node --version; npm --version | Phiên bản như mục môi trường |
| dotnet restore SangRental.slnx --locked-mode | Lần sandbox FAIL đọc NuGet.Config; chạy với quyền phù hợp PASS cả 5 project |
| dotnet tool restore | Sandbox FAIL; ngoài sandbox PASS dotnet-ef 10.0.13 |
| dotnet build SangRental.slnx --no-restore | PASS đầu phiên; một lần sau sửa bị process kiểm thử còn sống khóa apphost (MSB3026); dừng đúng cây process audit rồi build cuối PASS 0 warning/0 error |
| dotnet test SangRental.slnx --no-build --no-restore | Exit 0, không có test project/test count. NOT VERIFIED cho .NET unit/integration tests; không báo là có test pass |
| npm --prefix frontend ls --depth=0 | PASS; phiên bản cài đặt ở mục package |
| node --test scripts/notifications.test.mjs scripts/ui-policy.test.mjs | PASS 11/11 (8 logic + 3 policy), 0 fail |
| npm --prefix frontend run typecheck | PASS tsc --noEmit |
| npm --prefix frontend run build | PASS Next 16.4.5 Turbopack, compile/typecheck/prerender |
| node scripts/smoke.mjs | Hai lần FAIL ở readiness trước sửa logging; lần cuối PASS 12/12 sau sửa, process kiểm thử được dừng |
| node artifacts/phase1-dev-smoke.mjs | Lần ngoài sandbox gặp EPERM cache .next/dev; chạy cùng sandbox sở hữu cache PASS HTTP 200 và lang=vi. Lần ad hoc trước đó HTTP 200 nhưng assertion tiếng Việt hỏng do encoding PowerShell; không phải lỗi app |
| npm --prefix frontend audit --json | Sandbox lỗi registry/cache; quyền phù hợp PASS 0 info/low/moderate/high/critical |
| dotnet list SangRental.slnx package --vulnerable --include-transitive | PASS, 5 project không có vulnerable package theo api.nuget.org tại thời điểm chạy |
| dotnet ef migrations list --project backend/SangRental.Infrastructure --startup-project backend/SangRental.Api --no-build --no-connect | Sandbox không tìm tool đã restore ngoài sandbox; quyền phù hợp PASS liệt kê 20261107050736_InitialFoundation; trạng thái đã apply không xác định |
| dotnet ef migrations has-pending-model-changes --project backend/SangRental.Infrastructure --startup-project backend/SangRental.Api --no-build | PASS, không thay đổi model so với migration |
| dotnet ef migrations script --project backend/SangRental.Infrastructure --startup-project backend/SangRental.Api --no-build --output artifacts/phase1-foundation.sql | PASS; SQL chỉ tạo/ghi __EFMigrationsHistory; không execute SQL |
| git check-ignore các đường dẫn nhạy cảm/generated; git diff --check | PASS trong phạm vi được kiểm; diff không bao phủ nội dung untracked |
| git diff --exit-code -- bốn nguồn v2.7/HTML | PASS; nguồn tracked không đổi |

Không chạy npm ci/install trong phiên này: build dùng node_modules hiện có, npm ls và lockfile đã kiểm; khả năng clean npm install/CI trên máy mới NOT VERIFIED. Không chạy lint, browser E2E, AT-01…24, provider live, Docker, SQL integration, deployment hoặc GitHub Actions. Artifacts local bị ignore: smoke-results.json, phase1-foundation.sql, phase1-dev-smoke.mjs.

## Package thực tế

NuGet direct: Api — Microsoft.AspNetCore.OpenApi 10.0.13, Microsoft.EntityFrameworkCore.Design 10.0.13 (PrivateAssets=all); Application — Microsoft.Extensions.DependencyInjection.Abstractions 10.0.13; Infrastructure — Microsoft.EntityFrameworkCore.SqlServer 10.0.13, Microsoft.Extensions.Diagnostics.HealthChecks 10.0.13, System.Security.Cryptography.Pkcs 10.0.13. Domain/Admin không NuGet direct. EF CLI 10.0.13. Framework references theo SDK, không phải package tự thêm. Transitive closure lưu ở packages.lock.json từng project.

Frontend dependencies cài thực tế: next 16.4.5; react/react-dom 19.3.1; clsx 2.1.1; tailwind-merge 3.7.0; tw-animate-css 1.4.0; sweetalert2 11.26.25; @fortawesome/react-fontawesome 3.5.0; @fortawesome/fontawesome-svg-core và free-solid-svg-icons 7.3.1. Dev: typescript 5.9.3; @types/node 24.19.2; @types/react và @types/react-dom 19.3.0; tailwindcss và @tailwindcss/postcss 4.3.5. Admin tự host SweetAlert2/Font Awesome assets sinh từ npm, không package/CDN thứ hai. package-lock.json giữ closure npm. **Package thêm/nâng trong audit: không có.**

## Chưa triển khai và OPEN

Chưa có schema nghiệp vụ, catalog API, pricing/rounding, booking/hold/allocation, payment/reconciliation, outbox/Brevo/translation, handover/return/offline/refund, private document storage, audit nghiệp vụ, identity store/provisioning/MFA/revocation, CMS/admin operations hoặc SQL concurrency tests. AT-01…24 vẫn chưa triển khai/chưa chạy. Giữ frontend preview/noindex và login khóa.

| OPEN | Trạng thái / chủ trì |
|---|---|
| OPEN-01 | SePay/bank compatibility/trial/cost/webhook chưa xác minh — chủ dự án + provider |
| OPEN-05 | Điều kiện/tỷ lệ hoàn, phí/bồi thường chưa duyệt — chủ dự án |
| OPEN-07 | Retention/căn cứ pháp lý/giấy tờ nước ngoài/chữ ký và dữ liệu xuyên biên giới — chủ dự án + pháp lý |
| CR-01 | Xác thực cuộc gọi phê duyệt offline — chủ dự án + bảo mật/vận hành |
| CR-02 | Ngoại lệ chứng từ nhưng không có tiền thực nhận — chủ dự án |
| CR-03 | Thứ tự xe thay và hủy xung đột BR-46 — chủ dự án |
| CR-04 | Bằng chứng thất bại cho phép retry refund — chủ dự án + bảo mật/vận hành |
| OPS | Liên hệ, địa chỉ, Maps/reviews, giá/phí/vùng giao, SLA/bảo hiểm/domain email chưa xác minh — chủ dự án + vận hành |
| Identity | Provider/provisioning/MFA/revocation, auth liên Admin/API chưa chốt — chủ dự án + kỹ thuật |
| SQL dev/test | Instance, TLS/cert, quyền DB/connection secret chưa kiểm — chủ dự án + hạ tầng |

## Thay đổi và bước tiếp theo

Audit sửa backend/SangRental.Api/Program.cs và admin/SangRental.Admin/Program.cs để cấu hình JSON console logging độc lập Windows EventLog; tạo báo cáo này và cập nhật PROGRESS_LOG/TASK_BOARD/DECISIONS. next dev tự sinh frontend/AGENTS.md (không thay AGENTS root); giữ nguyên để review cùng scaffold. Không thay quyết định nghiệp vụ.

Tiếp theo: (1) review và đưa scaffold cần thiết vào Git; (2) chuẩn bị SQL Server dev/test với secret qua environment/user-secrets, kiểm ready 200 và migration nền sau review SQL; (3) khóa schema/DTO catalog–fleet, FK/index/soft-disable và kế hoạch migration theo NEXT-02; (4) bổ sung .NET test project dùng SQL Server thật cho persistence, rồi mới triển khai schema; (5) hoàn tất lint/browser notification QA và chốt identity trước màn quản trị có mutation. Booking/payment chỉ bắt đầu sau hợp đồng module, chiến lược transaction/idempotency/overlap và test plan; OPEN pháp lý/tài chính giữ nguyên.

## Danh sách file thực tế

Danh sách bên dưới ghi nhận source/config/docs/lockfile hiện có; loại bin/obj/node_modules/.next/artifacts/.git, không liệt kê hoặc đọc secret local.

```text
.config\dotnet-tools.json
.github\workflows\ci.yml
.gitignore
admin\SangRental.Admin\appsettings.Development.json
admin\SangRental.Admin\appsettings.json
admin\SangRental.Admin\Components\_Imports.razor
admin\SangRental.Admin\Components\App.razor
admin\SangRental.Admin\Components\Layout\MainLayout.razor
admin\SangRental.Admin\Components\Layout\MainLayout.razor.css
admin\SangRental.Admin\Components\Layout\ReconnectModal.razor
admin\SangRental.Admin\Components\Layout\ReconnectModal.razor.css
admin\SangRental.Admin\Components\Layout\ReconnectModal.razor.js
admin\SangRental.Admin\Components\Pages\AccessDenied.razor
admin\SangRental.Admin\Components\Pages\Error.razor
admin\SangRental.Admin\Components\Pages\Home.razor
admin\SangRental.Admin\Components\Pages\Login.razor
admin\SangRental.Admin\Components\Pages\NotFound.razor
admin\SangRental.Admin\Components\Routes.razor
admin\SangRental.Admin\packages.lock.json
admin\SangRental.Admin\Program.cs
admin\SangRental.Admin\Properties\launchSettings.json
admin\SangRental.Admin\SangRental.Admin.csproj
admin\SangRental.Admin\Services\NotificationService.cs
admin\SangRental.Admin\wwwroot\app.css
admin\SangRental.Admin\wwwroot\notifications.js
admin\SangRental.Admin\wwwroot\vendor\coastal.css
admin\SangRental.Admin\wwwroot\vendor\FontAwesome.LICENSE.txt
admin\SangRental.Admin\wwwroot\vendor\gauge.svg
admin\SangRental.Admin\wwwroot\vendor\notification-core.js
admin\SangRental.Admin\wwwroot\vendor\notifications.json
admin\SangRental.Admin\wwwroot\vendor\sweetalert2.all.min.js
admin\SangRental.Admin\wwwroot\vendor\SweetAlert2.LICENSE
admin\SangRental.Admin\wwwroot\vendor\sweetalert2.min.css
AGENTS.md
backend\.env.example
backend\SangRental.Api\appsettings.Development.json
backend\SangRental.Api\appsettings.json
backend\SangRental.Api\packages.lock.json
backend\SangRental.Api\Program.cs
backend\SangRental.Api\Properties\launchSettings.json
backend\SangRental.Api\SangRental.Api.csproj
backend\SangRental.Api\SangRental.Api.http
backend\SangRental.Application\DependencyInjection.cs
backend\SangRental.Application\packages.lock.json
backend\SangRental.Application\SangRental.Application.csproj
backend\SangRental.Domain\AssemblyMarker.cs
backend\SangRental.Domain\packages.lock.json
backend\SangRental.Domain\SangRental.Domain.csproj
backend\SangRental.Infrastructure\DependencyInjection.cs
backend\SangRental.Infrastructure\packages.lock.json
backend\SangRental.Infrastructure\Persistence\DatabaseReadinessCheck.cs
backend\SangRental.Infrastructure\Persistence\Migrations\20261107050736_InitialFoundation.cs
backend\SangRental.Infrastructure\Persistence\Migrations\20261107050736_InitialFoundation.Designer.cs
backend\SangRental.Infrastructure\Persistence\Migrations\SangRentalDbContextModelSnapshot.cs
backend\SangRental.Infrastructure\Persistence\SangRentalDbContext.cs
backend\SangRental.Infrastructure\SangRental.Infrastructure.csproj
compose.yaml
Directory.Build.props
docs\API_CONTRACTS.md
docs\ARCHITECTURE.md
docs\DECISIONS.md
docs\DEPLOYMENT.md
docs\DESIGN_SYSTEM.md
docs\desktop.html
docs\mobile.html
docs\OPEN_QUESTIONS.md
docs\PHASE_1_READINESS_REPORT.md
docs\PROGRESS_LOG.md
docs\PROJECT_RULES.md
docs\REQUIREMENTS.md
docs\SangRental_Codex_Master_Prompt_v2_7.md
docs\SangRental_DacTa_ChiTiet_v2_7.md
docs\SangRental_UIUX_SweetAlert2_FontAwesome_Standard.md
docs\SECURITY_PRIVACY.md
docs\TASK_BOARD.md
docs\TEST_MATRIX.md
docs\UIUX_V27_ADDENDUM.md
frontend\.env.example
frontend\.nvmrc
frontend\AGENTS.md
frontend\components.json
frontend\next.config.ts
frontend\next-env.d.ts
frontend\package.json
frontend\package-lock.json
frontend\postcss.config.mjs
frontend\README.md
frontend\src\app\globals.css
frontend\src\app\layout.tsx
frontend\src\app\page.tsx
frontend\src\components\layout\site-footer.tsx
frontend\src\components\layout\site-header.tsx
frontend\src\components\ui\button.tsx
frontend\src\design\coastal.css
frontend\src\hooks\.gitkeep
frontend\src\lib\notification-core.ts
frontend\src\lib\notification-service.ts
frontend\src\lib\utils.ts
frontend\src\messages\notifications.json
frontend\tsconfig.json
global.json
README.md
SangRental.slnx
scripts\notifications.test.mjs
scripts\prepare-ui-assets.mjs
scripts\smoke.mjs
scripts\ui-policy.test.mjs
```
