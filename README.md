# SangRental

Website thuê xe máy và ô tô tại Đà Nẵng — Catalog/Fleet API và đăng nhập quản trị đang được triển khai, chưa production-ready.

**Checkpoint 13/11/2026 — upload ảnh:** `/catalog` → **Quản lý ảnh** theo mẫu xe. Native multipart upload tạo ảnh nháp WebP (hoặc JPG cấu hình), mỗi lần upload một file GUID v7 mới dù trùng tên/nội dung; preview chỉ Admin/no-store. Có duyệt quyền sử dụng, chọn ảnh chính, ẩn/ngừng dùng, giữ blob cũ. URL ảnh công khai immutable/ETag chỉ hoạt động khi ảnh và model/category đủ trạng thái public; chưa có UI publish model/category. CCCD/GPLX/giấy tờ/handover tách primitive giữ nguyên byte/EXIF, lưu mã hóa private, chưa có UI/API hồ sơ khách. **33 media + 246 SQL integration + 36 guards + 15 JS + 18 smoke PASS**; không đổi DB. Chi tiết file/cách test/rollback tại [Changes/2026-11-13-upload-anh-guid-v7-cache.md](Changes/2026-11-13-upload-anh-guid-v7-cache.md). Các checkpoint dưới là lịch sử và được cập nhật bởi phần này.

Ngày 08/11/2026: `SangRentalDb` đã có hai migration và 16 bảng Catalog/Fleet. Identity + cookie được chủ dự án duyệt; migration Identity chỉ áp dụng vào **SangRentalTestDb** để kiểm thử. SQL cho Development đang chờ review tại [ADMIN_IDENTITY_MIGRATION_REVIEW.sql](docs/ADMIN_IDENTITY_MIGRATION_REVIEW.sql).

- `backend/`: Domain, Application, Infrastructure, Api (.NET 10).
- `frontend/`: Next.js App Router + TypeScript + Tailwind 4, cấu trúc chuẩn bị shadcn/ui.
- `admin/SangRental.Admin/`: Blazor Web App Server, form đăng nhập Identity, dùng chung cookie với API.
- `docs/`: nguồn v2.7 và tài liệu quản lý; `scripts/`: HTTP smoke tests.

Môi trường đã kiểm tra: SDK 10.0.203, host 10.0.7, Node 24.18.0, npm **11.16.0** (khác 11.6.0 dự kiến), Git 2.54.0.windows.1. Không tự đổi phiên bản công cụ trên máy.

## Cài đặt và kiểm tra

```powershell
dotnet restore SangRental.slnx
dotnet tool restore
npm --prefix frontend ci
npm --prefix frontend run lint
node scripts/prepare-ui-assets.mjs
dotnet build SangRental.slnx --no-restore
node --test scripts/notifications.test.mjs scripts/ui-policy.test.mjs scripts/fleet-forms.test.mjs
npm --prefix frontend run typecheck
npm --prefix frontend run build
node scripts/smoke.mjs
```

## Chạy thử Identity và Swagger trên database test

**Checkpoint 12/11/2026:** Admin `/catalog` tạo/xem danh mục, hãng, mẫu nháp; `/fleet` tạo/xem xe và bảo trì/checklist. Trang chi tiết xe đã có **sửa hồ sơ, đổi mẫu gán có audit, đổi biển giữ lịch sử, kích hoạt nháp, ngừng/khôi phục khai thác**. Mọi thay đổi cần Admin, lý do và phiên bản dữ liệu; activate/restore cần xác nhận kiểm tra. SQL integration **200 checks + 36 guards PASS**, notification/policy/form **15/15**, HTTP smoke **16/16**, build .NET 0 warning/error; EF không có model changes. Không thêm package/migration. Xem [hợp đồng vòng đời xe](docs/FLEET_LIFECYCLE_CONTRACT.md).

Chưa có publication/ảnh/thông số công khai hoặc booking. Browser visual/a11y/mobile và CI remote chưa xác minh. Chứng chỉ localhost vẫn chưa trusted theo kiểm tra phiên người dùng; chạy `dotnet dev-certs https --trust` trong terminal của bạn và chấp thuận chứng chỉ development trước khi đăng nhập. Không dùng HTTP để đăng nhập.

Docker Desktop được khởi động lại ngày 12/11; container SangRental SQL local đã chạy và kiểm thử DB test đạt. Lint/typecheck/frontend build/audit online gần nhất ở checkpoint 11/11; phiên này không đổi frontend hoặc dependencies, không chạy lại các lệnh đó. Baseline vẫn chưa commit/push. Các lần SQL FAIL trước đây là lịch sử trong PROGRESS_LOG.

Xem [hướng dẫn thử từng API](docs/TESTING_IDENTITY_CATALOG.md). Khởi tạo Admin test bằng terminal tương tác, mật khẩu nhập ẩn (không tài khoản mặc định):

```powershell
dotnet run --project backend/SangRental.Api --no-launch-profile -- --environment Development --test-database --provision-admin
```

Sau đó chạy hai terminal cùng hostname `localhost`:

```powershell
dotnet run --project backend/SangRental.Api --no-launch-profile -- --urls https://localhost:7100 --environment Development --test-database
dotnet run --project admin/SangRental.Admin --no-launch-profile -- --urls https://localhost:7200 --environment Development --test-database
```

Admin: `https://localhost:7200/account/login`; Swagger: `https://localhost:7100/swagger`. Cần development HTTPS certificate tin cậy; xem DEPLOYMENT. Cờ `--test-database` chỉ chuyển database sang SangRentalTestDb trên đúng SQL local, không tự migrate. Hai host đọc chung User Secrets Development. Không thêm `--test-database` khi muốn chạy Development thật sau review migration.

## Kiểm tra HTTP nền tảng và frontend

```powershell
dotnet run --project backend/SangRental.Api --no-launch-profile -- --urls http://localhost:5100 --environment Development
dotnet run --project admin/SangRental.Admin --no-launch-profile -- --urls http://localhost:5200 --environment Development
npm --prefix frontend run dev -- --port 3000
```

Frontend http://localhost:3000 còn là trang xem trước. API `/health/live`, `/health/ready`, `/openapi/v1.json`; admin `/account/login` và hai health endpoints tương tự. Live không phụ thuộc SQL; ready chỉ kiểm kết nối SQL, không bảo đảm Identity migration đã áp dụng. HTTP chỉ kiểm nền tảng, đăng nhập yêu cầu HTTPS. Chưa có booking/thanh toán hoặc dữ liệu khách.

Ngày 10/11: smoke đã sửa và PASS 14/14 với connection rỗng/key directory riêng cho hai ứng dụng. SQL runners chưa xác minh lại được do SQL local TCP 1436 timeout. Theo dõi công việc hiện hành trong TASK_BOARD và bằng chứng theo ngày trong PROGRESS_LOG, không lấy kết quả lịch sử làm trạng thái runtime hôm nay.

Kiểm thử SQL riêng (từ root, dùng SQL local và secret có sẵn):

```powershell
dotnet run --project tests/SangRental.Identity.Integration --no-build --no-restore
dotnet run --project tests/SangRental.CatalogFleet.GuardSmoke --no-build --no-restore
```

Identity integration chỉ migrate/ghi SangRentalTestDb; giữ dữ liệu test và audit để xem, vô hiệu hóa các tài khoản sinh ngẫu nhiên khi kết thúc. GuardSmoke chỉ ghi trong transaction rồi rollback. Không có Drop/EnsureDeleted. Không coi `dotnet test` thay thế hai lệnh console này.

SweetAlert2 là thư viện thông báo duy nhất, Font Awesome là thư viện icon chính. NotificationService frontend ở `frontend/src/lib/notification-service.ts`; Blazor ở `admin/SangRental.Admin/Services/NotificationService.cs`. Chỉ gọi sau interactive render, dùng translation keys. `loading()` trả token; truyền token đó vào `closeLoading(token)` để tránh đóng loading của tác vụ khác. Không có nút demo hoặc thông báo nghiệp vụ giả trên giao diện.

[Design system Coastal Luxury](docs/DESIGN_SYSTEM.md) · [Phụ lục đối chiếu UI/UX với v2.7](docs/UIUX_V27_ADDENDUM.md).

[Chạy và cấu hình DB](docs/DEPLOYMENT.md) · [Kết quả kiểm tra](docs/PROGRESS_LOG.md) · [Kiến trúc](docs/ARCHITECTURE.md) · [Vấn đề OPEN](docs/OPEN_QUESTIONS.md).
