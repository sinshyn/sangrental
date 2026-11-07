# SangRental

Website thuê xe máy và ô tô tại Đà Nẵng — nền tảng khởi tạo, chưa production-ready.

Phase 2: đã kiểm kết nối SQL Server Docker và User Secrets, chưa apply schema. Xem [báo cáo kết nối database](docs/PHASE_2_DATABASE_CONNECTION_REPORT.md) và [hướng dẫn thiết lập](docs/DEPLOYMENT.md).

- `backend/`: Domain, Application, Infrastructure, Api (.NET 10).
- `frontend/`: Next.js App Router + TypeScript + Tailwind 4, cấu trúc chuẩn bị shadcn/ui.
- `admin/SangRental.Admin/`: Blazor Web App Server, cookie auth scaffold.
- `docs/`: nguồn v2.7 và tài liệu quản lý; `scripts/`: HTTP smoke tests.

Môi trường đã kiểm tra: SDK 10.0.203, host 10.0.7, Node 24.18.0, npm **11.16.0** (khác 11.6.0 dự kiến), Git 2.54.0.windows.1. Không tự đổi phiên bản công cụ trên máy.

## Cài đặt và kiểm tra

```powershell
dotnet restore SangRental.slnx
dotnet tool restore
npm --prefix frontend ci
node scripts/prepare-ui-assets.mjs
dotnet build SangRental.slnx --no-restore
node --test scripts/notifications.test.mjs scripts/ui-policy.test.mjs
npm --prefix frontend run typecheck
npm --prefix frontend run build
node scripts/smoke.mjs
```

## Chạy local (ba terminal)

```powershell
dotnet run --project backend/SangRental.Api --no-launch-profile -- --urls http://localhost:5100 --environment Development
dotnet run --project admin/SangRental.Admin --no-launch-profile -- --urls http://localhost:5200 --environment Development
npm --prefix frontend run dev -- --port 3000
```

Frontend http://localhost:3000, API `/health/live`, `/health/ready`, `/openapi/v1.json`; admin `/account/login`. Chưa cấu hình DB thì readiness **503 là trạng thái đúng**. Không có đăng nhập thật, booking, thanh toán hoặc dữ liệu khách.

SweetAlert2 là thư viện thông báo duy nhất, Font Awesome là thư viện icon chính. NotificationService frontend ở `frontend/src/lib/notification-service.ts`; Blazor ở `admin/SangRental.Admin/Services/NotificationService.cs`. Chỉ gọi sau interactive render, dùng translation keys. `loading()` trả token; truyền token đó vào `closeLoading(token)` để tránh đóng loading của tác vụ khác. Không có nút demo hoặc thông báo nghiệp vụ giả trên giao diện.

[Design system Coastal Luxury](docs/DESIGN_SYSTEM.md) · [Phụ lục đối chiếu UI/UX với v2.7](docs/UIUX_V27_ADDENDUM.md).

[Chạy và cấu hình DB](docs/DEPLOYMENT.md) · [Kết quả kiểm tra](docs/PROGRESS_LOG.md) · [Kiến trúc](docs/ARCHITECTURE.md) · [Vấn đề OPEN](docs/OPEN_QUESTIONS.md).
