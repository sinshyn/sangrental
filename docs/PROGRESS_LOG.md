# Nhật ký tiến độ

## 2026-11-07 — Phase 2 SQL Server connection

Đã kiểm tài liệu và backend thực tế, giữ SangRentalDbContext/DI/migration nền/EF 10.0.13; không thêm package hoặc entity. Thêm base connection Development tới SangRentalDb, SQL local Encrypt=True/TrustServerCertificate=True. Docker publish 127.0.0.1:1436→1433; localhost SqlClient timeout 258 trong khi TCP IPv4 hoạt động. Opt-in Development UseIPv4Loopback ánh xạ đúng endpoint local sang tcp:127.0.0.1,1436; không thay Docker/password/TLS hoặc server khác.

Credential local có sẵn trong .env Docker chỉ được đọc trong bộ nhớ, không in; đã lưu key connection vào API User Secrets qua stdin. Kiểm API đọc User Secrets không env tạm: CanConnectAsync true, live/ready 200; thiếu config và unreachable DB 503, response generic. Restore/build cuối PASS 0 warning/error; EF tool/list offline/model/script PASS, không apply migration. SELECT cuối xác nhận 0 user tables. Chỉ thêm scripts nhập secret an toàn và database smoke. Chi tiết các lệnh/lần thất bại/giới hạn, file và package: PHASE_2_DATABASE_CONNECTION_REPORT.md. Các process kiểm thử đã dừng; OPEN/CR/OPS giữ nguyên. Bước tiếp: khóa thiết kế catalog/fleet/test DB; chưa booking/payment/authentication.

## 2026-11-07 — Phase 1 verification & readiness audit

Báo cáo chi tiết: [PHASE_1_READINESS_REPORT.md](PHASE_1_READINESS_REPORT.md), gồm inventory file/package, PASS/FAIL/NOT VERIFIED và các lần lệnh thất bại. Đã kiểm lại tài liệu, references/DI/config/source và Git; phần lớn scaffold vẫn untracked. Không đọc `.env`, không apply DB/seed/default account, không triển khai nghiệp vụ hoặc deploy.

Phát hiện lỗi thực: Windows EventLog provider thiếu quyền ghi khiến health ready thiếu DB thành 500/ngắt stream. Sửa API/Admin Program.cs dùng ClearProviders + AddJsonConsole, không thay quyền OS. Build cuối 5 project 0 warning/error; HTTP smoke cuối 12/12. Typecheck và production build Next PASS; development HTTP 200/lang vi PASS; logic/policy tests 11/11. npm audit 0 vulnerabilities, NuGet không báo vulnerable package. Locked restore/tool restore và EF offline list/model/script PASS khi dùng quyền phù hợp; sandbox không đọc NuGet.Config/tool cache. Có lần build bị process audit khóa apphost, đã dừng đúng cây process và kiểm lại sạch. Không thêm/nâng package.

dotnet test exit 0 nhưng không có test project, không coi là unit/integration tests đạt. Chưa lint, SQL connection/apply/ready 200, browser hydration/a11y/interop circuit, bundle analyzer, authenticated flow, clean npm ci hoặc CI remote. next dev tự sinh frontend/AGENTS.md; giữ lại để review. OPEN/CR/OPS/identity/SQL giữ nguyên. Sẵn sàng thiết kế schema/catalog có điều kiện; phải có SQL Server dev/test và hợp đồng module trước thực thi schema/nghiệp vụ.

## 2026-11-07 — Đối chiếu và hoàn thiện quy chuẩn UI/UX

Đã đọc đầy đủ SangRental_UIUX_SweetAlert2_FontAwesome_Standard.md (mục 1–8 và chỉ dẫn cuối), đối chiếu Master Prompt/đặc tả v2.7, AGENTS.md, kiến trúc/quyết định/kế hoạch hiện tại. Project hiện có được giữ nguyên. Không có xung đột nghiệp vụ/pháp lý; phụ lục cụ thể hóa lựa chọn icon/notification và design tokens. Không sửa hai nguồn v2.7 hoặc HTML Stitch; OPEN/CR giữ nguyên.

### Tệp tạo mới

- docs/UIUX_V27_ADDENDUM.md: bảng đối chiếu từng nhóm yêu cầu, dependency và giới hạn scaffold.
- docs/DESIGN_SYSTEM.md: palette, responsive/focus, icon, service API, i18n, ownership, quy trình cập nhật assets và gate QA.
- frontend/src/design/coastal.css: shared Coastal Luxury tokens và CSS popup/focus/mobile/reduced-motion.
- admin/SangRental.Admin/wwwroot/vendor/coastal.css: bản CSS sinh từ nguồn chung.
- scripts/ui-policy.test.mjs: guard dependency, direct popup/whole-pack import, locale completeness, đồng bộ assets.

### Tệp cập nhật

- frontend/src/app/globals.css: import shared CSS, map semantic colors.
- frontend/src/lib/notification-core.ts: từ chối unknown key, dùng CSS brand tokens/custom popup class, xử lý loading promise reject theo ownership.
- admin/SangRental.Admin/Components/App.razor và wwwroot/app.css: load shared CSS, đồng bộ palette/focus/icon.
- admin/SangRental.Admin/wwwroot/notifications.js: kiểm tra engine tồn tại, giữ initialize dùng chung.
- admin/SangRental.Admin/wwwroot/vendor/notification-core.js: regenerate từ TS đã sửa; chạy script đồng bộ CSS/locales/license/assets hiện có.
- scripts/prepare-ui-assets.mjs, scripts/notifications.test.mjs, scripts/smoke.mjs, .github/workflows/ci.yml và README.md: đồng bộ assets, tests/policy/HTTP theme check và hướng dẫn.
- docs/ARCHITECTURE.md, PROJECT_RULES.md, DECISIONS.md, TASK_BOARD.md, TEST_MATRIX.md và nhật ký này: ghi chuẩn bổ sung và gate còn lại.

NotificationService TypeScript và NotificationService C# + JS Interop đã có từ lần scaffold trước, được kiểm tra và tái sử dụng. Không Swal.fire trong component hoặc thư viện thông báo bị cấm trong mã ứng dụng. Sáu locale vi/en/ko/zh-CN/ja/th và fallback VI được kiểm tra. Giữ shadcn/ui/Tailwind cho UI thông thường, inline field validation khi triển khai form.

### Package

Không cài mới hoặc nâng version trong phiên đối chiếu. `npm --prefix frontend ls --depth=0` xác nhận SweetAlert2 11.26.25; @fortawesome/react-fontawesome 3.5.0; @fortawesome/fontawesome-svg-core và free-solid-svg-icons 7.3.1. Các package đã cài và pin từ lần khởi tạo trước. Admin dùng vendor tự host cùng phiên bản/license, không thêm NuGet/CDN wrapper.

### Kiểm tra thực tế

- `node scripts/prepare-ui-assets.mjs`: PASS.
- `node --test scripts/notifications.test.mjs scripts/ui-policy.test.mjs`: **11/11 PASS** (8 logic, 3 policy).
- `npm --prefix frontend run typecheck`: PASS.
- `npm --prefix frontend run build`: PASS, Next 16.4.5 production/prerender.
- `dotnet build SangRental.slnx --no-restore`: PASS, **0 warning, 0 error**.
- `node scripts/smoke.mjs`: **12/12 PASS**, gồm health/auth, OpenAPI/ProblemDetails, notification assets/locales/shared Coastal CSS và frontend production HTTP. Process kiểm thử đã dừng.
- `git diff --exit-code` cho bốn nguồn ban đầu và `git diff --check`: PASS.

Chưa chạy workflow trên GitHub, browser keyboard/focus/screen reader/SSR hydration/responsive popup, Blazor disconnect, đo bundle runtime hoặc kiểm chứng dịch thuật người bản ngữ. Các test notification dùng engine giả; không tuyên bố đã kiểm SweetAlert DOM thực. Chưa có booking/payment/refund module hoặc mutation nghiệp vụ nên không tự tạo thông báo tài chính/quyền/idempotency giả; các gate đó thuộc pha sau. Không đổi DB/schema, dữ liệu khách, quyết định OPEN hoặc triển khai production.

## 2026-11-07 — Khởi tạo

- Git ban đầu main...origin/main, working tree sạch; bốn file nguồn docs, ba thư mục ứng dụng trống. Không tìm thấy Word nguồn hoặc AGENTS.md trong repo.
- Đọc Master Prompt, đặc tả v2.7, desktop.html/mobile.html; giữ nguyên nguồn.
- SDK 10.0.203/host 10.0.7; Node 24.18.0; npm 11.16.0 (khác 11.6.0); Git 2.54.0.windows.1.
- Tạo solution, bốn backend projects, Blazor admin, Next frontend và tài liệu.
- Template restore cảnh báo Microsoft.OpenApi 2.0.0 high; nâng package Microsoft 10.0.13. Cảnh báo transitive Pkcs 9.0.11 tiếp tục được sửa bằng direct reference 10.0.13. Không tắt audit.
- EF lần đầu thiếu provider khi chưa có secret thất bại; sửa SQL provider không connection để sinh model design-time, tạo migration thành công. Không apply DB.
- Sửa UTF-8 các tệp scaffold tiếng Việt trước verification cuối.
- npm install thành công, audit 0 vulnerabilities; TypeScript pass. Kết quả cuối bổ sung dưới đây.

AT-01…24 chưa triển khai/chưa chạy. Không provider thật/default password/dữ liệu khách/publish/commit/push.

### Kết quả hoàn thiện

- AGENTS.md và docs/SangRental_UIUX_SweetAlert2_FontAwesome_Standard.md xuất hiện trong lúc làm; đã đọc toàn bộ và bổ sung NotificationService Next/Blazor, sáu locale, Font Awesome hai icon chọn lọc, SweetAlert2 tự host kèm license. Giữ nguyên các tệp nguồn mới này.
- `dotnet restore SangRental.slnx`, `dotnet tool restore`, `dotnet build SangRental.slnx --no-restore`: thành công, 5 project, 0 warning/0 error.
- `dotnet ef migrations add InitialFoundation ...`: thành công sau sửa provider; `migrations script ...`: thành công, chỉ bảng lịch sử migration; `migrations has-pending-model-changes ...`: không có thay đổi model. Bỏ scan configuration assembly rỗng để tránh warning model vô nghĩa. Chưa apply SQL Server.
- `dotnet list SangRental.slnx package --vulnerable --include-transitive`: không có vulnerable packages theo nguồn NuGet hiện tại.
- `npm install`, đồng bộ lockfile, `npm audit --omit=dev`: 0 vulnerabilities; audit toàn dependency khi install cũng 0.
- Một số lệnh npm build/save package từ context ngoài sandbox gặp EPERM trên tệp do sandbox tạo; sửa package qua workspace và chạy build trong sandbox đã thành công. Không đổi ACL hệ thống. Restore NuGet cần context ngoài sandbox vì không đọc được NuGet.Config của người dùng ở context mặc định.
- `npm --prefix frontend run typecheck`, `npm --prefix frontend run build`: thành công Next 16.4.5, TS strict, route / prerender.
- `node --test scripts/notifications.test.mjs`: 5/5 PASS. Tests dùng engine giả, không tuyên bố browser/a11y đã kiểm.
- HTTP smoke ban đầu phát hiện auth session 302; sửa cookie events và loại /auth khỏi status-page re-execute. Lần sau 8/8 PASS: live 200, ready 503 thiếu DB, OpenAPI 200, missing 404 ProblemDetails, admin login 200/dashboard 302/session 401, frontend production 200. Bổ sung smoke assets admin ở bước cuối.
- Thêm GitHub Actions scaffold build/typecheck/migration model/notification tests/HTTP smoke trên Windows. Chưa chạy workflow trên GitHub.

### Giới hạn còn lại

Kết quả cuối: `node scripts/smoke.mjs` **11/11 PASS**, bao gồm 3 kiểm tra assets thông báo admin; `dotnet restore SangRental.slnx --locked-mode` PASS; model không pending changes. `git diff --exit-code` cho bốn nguồn ban đầu và `git diff --check` đều thành công. HTTP smoke đã dừng các process do script tạo. Kết quả máy đọc ở artifacts/smoke-results.json (gitignored).

Không SQL Server dev được cấp nên chưa integration DB/apply migration/readiness happy path/concurrency tests. Login admin chưa bật, chưa identity store/provisioning/MFA/revocation hoặc authenticated flow. Chưa browser visual QA/E2E/accessibility. OPEN-01/05/07, CR-01…04 và OPS giữ nguyên. Runtime host 10.0.7 chưa được cập nhật bởi phiên này. Các process smoke được dừng sau kiểm tra.
