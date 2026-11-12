# Phase 3 — EF entities và migration SQL để review

**MIGRATION APPLIED TO LOCAL DEVELOPMENT DATABASE — APPLICATION GUARDS PENDING**  
## Cập nhật sau khi chủ dự án chạy database update — 08/11/2026

Chủ dự án chạy `dotnet ef database update CatalogFleetInitial --project backend/SangRental.Infrastructure --startup-project backend/SangRental.Api` tại root. Output được cung cấp cho thấy build thành công, áp dụng `20261107050736_InitialFoundation` và `20261107173802_CatalogFleetInitial`, tạo schema/bảng/index, ghi hai migration vào `__EFMigrationsHistory`, kết thúc `Done.` Không thấy lỗi trong output được gửi. Lệnh chỉ đọc `dotnet ef migrations list --project backend/SangRental.Infrastructure --startup-project backend/SangRental.Api --no-build` do Codex chạy sau đó liệt kê cả hai migration **không có nhãn pending**. Vì vậy trạng thái migration trên database Development hiện tại là **PASS**. Chưa kiểm đếm trực tiếp 16 bảng qua `sys.tables` trong lượt này; số 16 dựa trên SQL script và output tạo bảng.

Không chạy lại `database update`, không tạo seed hoặc sửa dữ liệu. Các guard ở bảng phát hiện bên dưới vẫn chưa triển khai; migration thành công không đồng nghĩa Catalog/Fleet API hoặc booking đã hoạt động. Phần báo cáo bên dưới phản ánh trạng thái **trước khi chủ dự án áp dụng migration** và được giữ làm lịch sử kiểm tra.

Ngày 08/11/2026, Asia/Saigon. Căn cứ phê duyệt Q39 trong [bản review 16 entity](PHASE_3_FIRST_MIGRATION_REVIEW.md). Migration hiện hành `20261107173802_CatalogFleetInitial` và [SQL idempotent để review](PHASE_3_CATALOG_FLEET_MIGRATION_REVIEW.sql) **chưa chạy trên database**. Timestamp migration do công cụ EF sinh; không phải thời điểm phê duyệt. Bản `20261107172713` đã được thay trước khi áp dụng để thêm CHECK cho `[Code]` theo góp ý của chủ dự án.

## File đã tạo/sửa

| Loại | Đường dẫn / nội dung |
|---|---|
| Tạo | `backend/SangRental.Domain/CatalogFleet/CatalogFleetEntities.cs`: 16 entity, không tham chiếu EF/SQL trong Domain |
| Tạo | `backend/SangRental.Infrastructure/Persistence/CatalogFleetModelConfiguration.cs`: SQL Server mappings, PK/FK, check/index, rowversion |
| Sửa | `backend/SangRental.Infrastructure/Persistence/SangRentalDbContext.cs`: nạp cấu hình Catalog/Fleet |
| Tạo tự động | `backend/SangRental.Infrastructure/Persistence/Migrations/20261107173802_CatalogFleetInitial.cs` và `.Designer.cs`; snapshot `SangRentalDbContextModelSnapshot.cs` được cập nhật |
| Tạo | `docs/PHASE_3_CATALOG_FLEET_MIGRATION_REVIEW.sql`: SQL idempotent từ InitialFoundation đến CatalogFleetInitial, chỉ dùng để review |
| Sửa/tạo tài liệu | Proposal, ERD, decision log, first migration review, báo cáo này, task board, progress log |

Không thêm NuGet package. Giữ EF Core SQL Server 10.0.13 và dotnet-ef 10.0.13, không thêm DbSet/API/write workflow. `SangRentalDbContext` vẫn schema mặc định `sangrental`; connection key không đổi `ConnectionStrings:SangRental`. Không đọc hoặc in mật khẩu.

## Kết quả kiểm tra đã chạy

| Lệnh/kiểm tra | Kết quả |
|---|---|
| `dotnet restore SangRental.slnx --locked-mode` | PASS khi chạy với quyền đọc NuGet.Config của user; lần đầu trong sandbox FAIL do Access denied đối với NuGet.Config. Config tạm trong workspace cũng không vượt được giới hạn đó và đã xóa |
| `dotnet tool restore` | PASS với quyền đọc NuGet.Config; dotnet-ef 10.0.13 |
| `dotnet build SangRental.slnx --no-restore` | PASS, 5 project, 0 warning/0 error sau khi thêm entity/config và sau khi sinh migration |
| `dotnet ef migrations add CatalogFleetInitial ... --no-build` | PASS, tạo migration trong Infrastructure. Lần thử đầu trong sandbox không thấy local tool, sau đó chạy với quyền đọc tool cache. Không kết nối database |
| `dotnet ef migrations script InitialFoundation CatalogFleetInitial ... --idempotent --output docs/PHASE_3_CATALOG_FLEET_MIGRATION_REVIEW.sql` | PASS sau khi build lại assembly có migration; lần thử trước build báo không tìm thấy migration, không sinh SQL |
| `dotnet ef migrations has-pending-model-changes ... --no-build` | PASS: không có thay đổi model chưa ghi migration |
| `dotnet ef migrations remove ... --no-build` rồi build/add lại | PASS: thay bản migration chưa áp dụng để thêm bốn CHECK `CodeCanonical`. Lệnh remove thực hiện `SELECT 1` và đọc `__EFMigrationsHistory`; không chạy DDL hoặc database update |
| Kiểm tĩnh SQL | PASS: đúng 16 `CREATE TABLE [sangrental]`, không có bảng giá/buffer/Booking/Payment/User, VIN index không unique, `ScheduledEndUtc` nullable, có nguồn riêng cho ABS/dung tích/số ghế và unique current plate |
| Kiểm tĩnh CHECK `Code` | PASS: đúng 4 CHECK cho Category, Brand, Model, MaintenanceTaskTemplate; chữ thường, không trống và không chứa ASCII space/tab/newline/form feed/vertical tab/carriage return. `AssetCode` không bị áp CHECK này |
| `git diff --check` | PASS; Git chỉ cảnh báo chuyển LF/CRLF của vài file đang sửa |

Chưa chạy integration test SQL constraints hoặc concurrency vì yêu cầu **không áp dụng migration**. Chưa chạy `database update`, `EnsureCreated`, script SQL, seed, API hoặc UI. `dotnet test` chưa có test project phù hợp cho entity/catalog; build và model/snapshot checks không thay cho test lưu dữ liệu thật.

## Phát hiện cần xem trước khi áp dụng SQL

| Mức | Vấn đề / giới hạn |
|---|---|
| HIGH — chưa có write workflow | Schema không tự bảo đảm `VehicleInspection Passed` cùng lúc chuyển xe Ready, ảnh hợp lệ trước publish, nguồn từng thông số công khai, hard delete chỉ Draft không reference, hoặc khóa overlap bảo trì. Không mở API ghi hoặc tuyên bố khả dụng khi các guard và kiểm thử cạnh tranh chưa có |
| HIGH — concurrency | Unique current plate chặn hai đăng ký current trùng, nhưng không chặn mọi khoảng lịch sử overlap hoặc bảo trì chồng thời gian. Transaction/lock protocol với inventory tương lai vẫn cần thiết; rowversion không đủ |
| MEDIUM — identity/audit | Chưa có identity provider, quyền Admin/manager và trusted actor pipeline. Entity audit chỉ là schema; chưa có service ghi audit bất biến. Không bootstrap Admin/mật khẩu mặc định |
| MEDIUM — dữ liệu công khai | SQL metadata không kiểm được file ảnh thật/quyền sử dụng hoặc tính tin cậy của nguồn thông số; cần storage/workflow xác minh trước public |
| OPEN | Giá/discount/buffer, OPEN-01/05/07, CR-01…04, dữ liệu OPS giữ nguyên; Q28 chưa triển khai vì nằm ở lát giá sau |

**Bước tiếp theo:** review [SQL script](PHASE_3_CATALOG_FLEET_MIGRATION_REVIEW.sql) và các guard còn thiếu. Chỉ xem xét áp dụng vào SQL Server sau khi có xác nhận riêng cho chính SQL/mục tiêu database và kế hoạch kiểm tra sau migration. Không tự chuyển sang booking/payment/authentication.
