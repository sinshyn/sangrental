# Phase 3 — Lát Catalog & Fleet cho migration đầu

**APPROVED FOR EF/MIGRATION REVIEW — DATABASE UPDATE NOT APPROVED**  
Ngày 08/11/2026, Asia/Saigon. Chủ dự án duyệt ở câu 39 để tạo EF entities/configurations, build, kiểm thử và sinh migration/SQL cho review. Phê duyệt này **không cho phép áp dụng migration vào SQL Server**. Đọc cùng [proposal Revision 6](PHASE_3_CATALOG_FLEET_SCHEMA_PROPOSAL.md), [ERD](PHASE_3_CATALOG_FLEET_ERD.md), [decision log](PHASE_3_SCHEMA_DECISION_LOG.md). Khi các revision cũ mâu thuẫn, các quyết định Q1–Q38 và tài liệu này là nội dung review hiện hành. Quy tắc nguồn v2.7 và OPEN nguồn giữ nguyên.

## Phạm vi đã chọn

Q29 chọn **16 entity Catalog/Fleet/checklist** cho migration đầu. Chưa có giá, discount, buffer, Booking, Payment, User, API, UI hoặc seed. Năm entity `VehicleModelRateVersion`, `VehicleModelDiscountPolicyVersion`, `VehicleModelDurationDiscountTier`, `VehicleModelRentalPolicyVersion`, `VehicleCategoryBufferPolicyVersion` chỉ còn trong ERD kế hoạch, **không đưa vào migration đầu**. Q28 về giá áp dụng cho lát giá sau.

| Nhóm | Entity dự kiến trong migration đầu | Quan hệ chính |
|---|---|---|
| Catalog (8) | `VehicleCategory`, `VehicleCategoryTranslation`, `VehicleBrand`, `VehicleModel`, `VehicleModelTranslation`, `VehicleModelSpecification`, `VehicleModelImage`, `VehicleModelImageTranslation` | Category/Brand 1:n Model; Model 1:0..1 Specification, 1:n Translation/Image; Image 1:n Translation |
| Fleet (7) | `PhysicalVehicle`, `VehicleRegistration`, `VehicleOperationalStatusHistory`, `VehicleInspection`, `MaintenanceEpisode`, `MaintenanceTaskTemplate`, `MaintenanceChecklistItem` | Model 1:n Unit; Unit 1:n Registration/StatusHistory/Inspection/Episode; Episode 1:n Item; Template 0..1:n Item; Inspection 0..1:n Episode release |
| Audit (1) | `CatalogFleetAuditEvent` | Không FK giả tới User; actor reference chỉ từ backend có danh tính đáng tin |

## Quy tắc hành vi đã trả lời

- Khách chọn `VehicleModel`, không chọn `PhysicalVehicle`, màu hoặc biển. Mỗi cấu hình khác biệt là model riêng. Model Nháp/Công khai/Ẩn, mặc định Nháp; cần tiếng Việt và ít nhất một ảnh thật hợp lệ có quyền sử dụng trước công khai. Thiếu dịch thì hiển thị tiếng Việt. Thiếu giá vẫn có thể công khai để Liên hệ. Không có xe Ready hoặc không trống trong khoảng khách chọn thì chỉ Liên hệ.
- Category phẳng gồm xe số, tay ga, xe côn, ô tô 4/7/16 chỗ; mỗi category thuộc Motorcycle hoặc Car. Đây là phân loại, không suy tự động số ghế của từng model.
- Unit có `RecordLifecycle Draft/Active/Retired` **đề xuất để hiện thực Q34–35**, tách `OperationalStatus` (PendingInspection/Ready/...). Xe Chờ kiểm tra là hồ sơ đã lưu. Admin tạo Ready phải đồng thời lưu kiểm tra Passed có actor/thời điểm. Unit có thể Ready khi chưa có biển, được phân bổ tạm nhưng phải nhập biển trước bàn giao. VIN không bắt buộc; trùng VIN cảnh báo, Admin override phải ghi lý do.
- Biển theo lịch sử trên `VehicleRegistration`, duy nhất cùng quốc gia trên toàn SangRental tại cùng thời điểm. Đổi biển giữ mã xe. Xe đã ngừng khai thác chỉ Admin khôi phục sau kiểm tra đạt. Model/unit có lịch sử thuê không hard delete. Admin được hard delete model/unit Nháp chưa có bất kỳ dữ liệu liên quan nào.
- Bảo trì theo `MaintenanceEpisode` với checklist chọn việc mẫu hoặc thêm việc riêng. Mỗi item có trạng thái/ghi chú; Skipped không bắt buộc lý do. Một xe không có hai đợt trùng thời gian. `ScheduledEndUtc` tùy chọn; hết giờ dự kiến không tự Ready. Kết thúc sớm khi checklist xử lý xong và kiểm tra Passed, Admin/quản lý được quyền xác nhận. Có thể ghi đợt bảo trì xung đột đơn nhưng cảnh báo, không tự hủy đơn.
- Thông số công khai: người có quyền sửa và Lưu thì thay ngay, có audit. ABS, dung tích động cơ và số ghế cần nguồn xác minh khi hiển thị; nguồn cho các thông số khác tùy chọn. Người duyệt ban đầu là Admin hoặc quản lý được quyền.

## Mapping SQL Server / EF Core để duyệt — PROPOSED

| Phần | Phương án kỹ thuật |
|---|---|
| Key và FK | Root `bigint IDENTITY`; translation composite PK `(OwnerId,Locale)`; specification shared PK ModelId. Mọi FK `NO ACTION` để không xóa dây chuyền lịch sử |
| Text / locale | Nội dung Unicode `nvarchar` có max length; code/slug `varchar` bounded; locale chỉ `vi`, `en`, `ko`, `zh-CN`, `ja`, `th`; tiếng Việt là nguồn. Slug unique theo locale; cách redirect khi đổi slug sẽ xử lý ở CMS sau |
| Chuẩn hóa `Code` | `VehicleCategory`, `VehicleBrand`, `VehicleModel`, `MaintenanceTaskTemplate`: code chữ thường, không chứa khoảng trắng/ASCII whitespace; SQL CHECK + unique BIN2. `AssetCode` không chịu CHECK này |
| Trạng thái | Lifecycle hồ sơ unit Draft/Active/Retired; operational PendingInspection/Ready/ReturnInspection/Maintenance/Suspended/LostLocked/AccidentLocked là **mã đề xuất**, chưa phải enum nghiệp vụ đã duyệt. Booking usage không gộp vào operational status |
| Thông số | Typed columns nullable; `HasAbs` bit NULL, `EngineDisplacementCc` decimal(7,2) NULL, `SeatCount` tinyint NULL. Ba source reference riêng nullable; application bắt buộc nguồn nếu giá trị tương ứng công khai. Không giả 0/false cho giá trị chưa biết |
| Ảnh | Metadata storage key/hash, quyền sử dụng/nguồn, trạng thái duyệt/hiển thị, một ảnh chính tối đa mỗi model. Blob/file không nằm trong SQL schema này; quyền sử dụng cần xác minh trước public |
| Fleet | `AssetCode` unique suốt vòng đời; VIN normalized index **không unique**; current registration unique filtered `(Jurisdiction,NormalizedPlate)` và unique current/unit. Khoảng đăng ký dùng `[from,to)` UTC. Registration 0..n cho cả unit Ready |
| Bảo trì | Episode `ScheduledEndUtc NULL`; item `TaskTemplateId NULL` cho việc riêng, có tên snapshot; status Pending/Completed/Skipped đề xuất. Interval overlap của episode cùng unit phải kiểm transaction/lock ở application; unique index thường không đủ. Chưa tạo thuật toán allocation |
| Audit / concurrency | `rowversion` cho mutable roots, append-only cho inspection/status/audit events; actor reference không có FK tới User chưa tồn tại. Mỗi thay đổi trạng thái và inspection liên quan ghi trong cùng transaction; không mở write runtime khi chưa có identity/authorization đáng tin |
| Hard delete Nháp | Chỉ model/unit Draft và không có bất kỳ FK/reference/history liên quan; database FK `NO ACTION`, application kiểm trước khi xóa. Không xóa ảnh hoặc chứng cứ đã được tham chiếu |

## Phần cần kiểm tiếp khi review SQL và implementation

Phạm vi và mapping trên đã được chủ dự án duyệt **để tạo EF/migration cho review** ở Q39. Chưa duyệt SQL để áp dụng database. Các mã enum là lựa chọn kỹ thuật trong migration dự thảo; status transitions và transaction protocol chưa triển khai. Cần review khi xem EF mappings/SQL sinh ra, đặc biệt: (1) unique current plate và race đổi biển; (2) overlap bảo trì không có end dự kiến; (3) hard delete Draft và mọi reference; (4) ảnh và thông số được công khai; (5) đồng bộ `VehicleInspection Passed` với trạng thái Ready. SQL migration không thể tự bảo đảm mọi guard xuyên bảng và khoảng thời gian. Không tuyên bố booking/availability đã hoạt động chỉ vì schema đã có.

OPEN-01/05/07, CR-01…04, dữ liệu vận hành OPS, identity provisioning và chi tiết pricing/buffer vẫn mở theo [OPEN_QUESTIONS](OPEN_QUESTIONS.md). Chúng không được giải quyết bởi migration lõi. Kết quả triển khai EF và SQL sinh ra ghi trong [báo cáo review migration](PHASE_3_EF_MIGRATION_REVIEW_REPORT.md). Việc áp dụng database là quyết định riêng sau khi xem SQL và kiểm tra DB mục tiêu.
