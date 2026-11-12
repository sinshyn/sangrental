# Phase 3 — Catalog & Fleet schema proposal

**DESIGN REVIEW — NOT APPROVED**  
Ngày: 08/11/2026, Asia/Saigon. Revision: 6. Đây là thiết kế để review, không phải schema đã triển khai hoặc lệnh tạo migration. Phần cập nhật mới nhất thay thế các lựa chọn cũ mâu thuẫn với câu trả lời của chủ dự án; dictionary còn lại vẫn là PROPOSED.

## Cập nhật Q37–Q38 — nguồn thông số công khai

**APPROVED về hành vi:** khi người có quyền lưu thông số để công khai, chỉ **ABS, dung tích động cơ và số ghế** bắt buộc có nguồn/bằng chứng xác minh. Với các thông số khác trong §5.6, nguồn có thể lưu nhưng không bắt buộc. Q19 vẫn áp dụng: người có quyền bấm Lưu thì thay đổi hiển thị ngay, không chờ một bước duyệt lại; phải giữ audit/revision lịch sử.

**PROPOSED mapping cho lát đầu:** không dùng một `SourceReference` bắt buộc chung cho toàn bộ specification như Revision 1 §5.6. Thêm `HasAbsSourceReference`, `EngineDisplacementSourceReference`, `SeatCountSourceReference` (nvarchar(500) NULL) gắn từng giá trị. Nếu trường tương ứng khác NULL và sẽ được hiển thị công khai thì nguồn tương ứng phải có; không yêu cầu nguồn cho giá trị NULL/không hiển thị. `SourceReference` chung, nếu giữ, chỉ là nguồn tổng quát tùy chọn. Không đưa nguồn chứa bí mật, signed URL hoặc giấy tờ khách vào cột. Guard publish/save ở application; DB CHECK cho cặp giá trị/nguồn có thể thêm sau khi review nullability và sửa dữ liệu cũ. Cách lưu bằng chứng/file thực tế chưa chọn và không được giả là đã tích hợp.

## Cập nhật Q36 — kiểm tra khi tạo xe Sẵn sàng

**APPROVED về hành vi:** khi Admin hoàn tất hồ sơ xe và chọn Sẵn sàng ngay lúc tạo, cùng thao tác Lưu phải tạo `VehicleInspection` kết quả Passed, có người xác nhận và thời điểm kiểm tra. Không chỉ lưu boolean hoặc đổi `OperationalStatus` đơn lẻ. Bản ghi này gắn đúng `PhysicalVehicleId`, ghi audit cùng transaction với chuyển `RecordLifecycle=Active` và `OperationalStatus=Ready`. Không suy từ đây rằng xe đang trống trong khoảng khách chọn; booking/availability vẫn ở module sau.

**PROPOSED mapping:** dùng `InspectionTypeCode=InitialReadiness` và `ResultCode=Passed`; `InspectorReference`/`InspectedAtUtc` bắt buộc. `InitialReadiness` là mã kỹ thuật chưa được duyệt như enum cuối. Không tạo bảng mới; vẫn 16 entity trong migration đầu.

## Cập nhật Q34–Q35 — hồ sơ xe vật lý

**APPROVED về hành vi:** PhysicalVehicle có trạng thái **hồ sơ Nháp** riêng, không đồng nghĩa `OperationalStatus=PendingInspection` (Chờ kiểm tra). Hồ sơ Chờ kiểm tra đã lưu không thuộc nhóm được hard delete theo Q33. Khi Admin hoàn tất hồ sơ và chọn Sẵn sàng ngay lúc tạo, thao tác Lưu chuyển hồ sơ sang **Đang khai thác** đồng thời ghi xác nhận đã kiểm tra. Xe chưa có biển vẫn có thể Ready theo Q31, nhưng phải bổ sung biển trước bàn giao theo Q32.

**PROPOSED mapping:** thay `ServiceLifecycle Active/Retired` ở §6.1 bằng `RecordLifecycle Draft/Active/Retired`; giữ `OperationalStatus` độc lập. `Draft` không đóng góp inventory dù có dữ liệu nhập dở; chuyển `Draft→Active` cùng transaction với ghi nhận kiểm tra khi chọn Ready. Nếu chưa xác nhận kiểm tra, có thể chuyển `Draft→Active` ở `PendingInspection` hoặc tiếp tục Draft theo thao tác Admin; chi tiết UI chưa được phê duyệt. Hard delete chỉ dành cho `RecordLifecycle=Draft` không có bất kỳ reference/history; FK NO ACTION và application guard, không cascade. Không dùng `PendingInspection` làm điều kiện xóa. Đây là mapping đề xuất cần review trước EF configuration.

## Cập nhật Q29–Q33 — phạm vi migration đầu

**APPROVED về hành vi/phạm vi:** Q29 chọn migration đầu chỉ Catalog & Fleet, gồm checklist bảo trì; hoãn 5 bảng giá/discount/rental policy/buffer. Theo ERD Revision 2, lát đầu có **16 entity đề xuất**, không có Booking/Payment/User. Q30 chọn danh mục phẳng: xe số, tay ga, xe côn, ô tô 4/7/16 chỗ, mỗi loại thuộc nhóm Motorcycle hoặc Car. Mã category/group là cấu hình quản trị; không seed mẫu xe, giá hoặc dữ liệu vận hành giả.

Q31 cho phép xe vật lý ở trạng thái Ready dù chưa nhập biển. Q32 cho phép phân bổ tạm xe đó nhưng **phải bổ sung biển trước bàn giao**. Vì thế `VehicleRegistration` là quan hệ 0..n; không thêm check bắt buộc plate vào `PhysicalVehicle` hoặc gate Ready. Guard bàn giao thuộc module sau, chưa triển khai booking/handover. Hai unique filtered current plate theo unit và theo `(RegistrationJurisdictionCode, NormalizedPlate)` chỉ áp dụng khi có registration.

Q33 cho Admin xóa hẳn model/unit **còn nháp và chưa có bất kỳ dữ liệu liên quan nào**. BR-28 vẫn cấm xóa vĩnh viễn model/unit có lịch sử thuê. **PROPOSED để review:** application kiểm mọi FK/references và audit trong transaction trước hard delete; DB dùng `NO ACTION`, không cascade. Không tự xóa ảnh/file hoặc history có tham chiếu. Cần định nghĩa chính xác “nháp” của PhysicalVehicle vì Revision 2 chỉ có `ServiceLifecycle` và `OperationalStatus`, chưa có `Draft` lifecycle. Đây là blocker schema nhỏ trước EF mapping, không suy `PendingInspection` đồng nghĩa Nháp.

Revision 1/2 phía dưới nói mọi draft đều retire hoặc 21 entity cùng migration; các câu đó đã bị Q29/Q33 thay thế. Phần giá Q28 vẫn là quyết định hành vi cho lát sau, chưa đưa bảng giá vào migration đầu.

## Cập nhật review sau 28 câu trả lời của chủ dự án

Các câu trả lời Q1–Q28 là **APPROVED về hành vi** trong đúng phạm vi câu hỏi. Tên bảng, cột, index, constraint, quyền thực thi và chiến lược transaction bên dưới vẫn **PROPOSED**. Câu 25 ban đầu (cho đặt dù chưa có xe) đã bị chủ dự án sửa: chỉ cho **Liên hệ**. Câu 26 ban đầu phụ thuộc câu trả lời cũ đã bị hủy; Q26 mới áp dụng cho khoảng ngày không còn xe trống.

| Nhóm | Quyết định chủ dự án | Thay đổi thiết kế cần review |
|---|---|---|
| Model Q1–3 | Mỗi cấu hình khác biệt là một mẫu riêng; khách không chọn màu; Admin sửa gán nhầm model của unit có lý do, audit, giữ nguyên lịch sử | Không thêm VehicleVariant hoặc lựa chọn màu công khai. Correction thay model phải có event, không viết lại snapshot/biên bản cũ; kiểm xung đột lịch tương lai trước thao tác là guard kỹ thuật đề xuất |
| Biển/VIN Q4–7 | Giữ AssetCode khi đổi biển và lưu lịch sử; biển cùng quốc gia không trùng trên toàn SangRental tại cùng thời điểm. VIN luôn tùy chọn; trùng VIN chỉ cảnh báo, Admin có thể lưu với lý do | Giữ VehicleRegistration theo khoảng. **Bỏ unique filtered VIN** ở §6.1/§9; giữ index tra cứu VIN không unique, chuẩn hóa để cảnh báo. Lý do override VIN trùng là bắt buộc; không bắt buộc nhập/xác minh VIN để khai thác |
| Xe vận hành Q8–10, Q17 | Admin có thể chọn Ready ngay lúc tạo nếu xác nhận đã kiểm tra; nếu không thì PendingInspection. Admin hoặc quản lý được cấp quyền xác nhận mở lại sau bảo trì. Hết giờ dự kiến không tự mở. Unit đã ngừng khai thác chỉ Admin khôi phục sau kiểm tra đạt, giữ hồ sơ cũ | Ghi actor/thời điểm/evidence xác nhận kiểm tra khi tạo Ready; việc kiểm tra cần một VehicleInspection Passed hoặc record tương đương được review. Tìm lại xe báo mất vẫn chỉ Admin phê duyệt theo BR-37 |
| Bảo trì Q11–16, Q27 | Một đợt bảo trì chứa checklist công việc, chọn từ danh mục hoặc thêm mục riêng; từng mục có trạng thái và ghi chú. Có thể bỏ qua mục, lý do không bắt buộc. Không cho hai đợt cùng xe trùng thời gian; có thể kết thúc sớm khi checklist xử lý xong và kiểm tra đạt. Cho ghi nhận bảo trì dù xung đột đơn, cảnh báo để người có quyền xử lý, không tự hủy. Thời điểm dự kiến hoàn thành tùy chọn; xe vẫn bị chặn tới khi xác nhận | Thay `MaintenanceWindow` bằng `MaintenanceEpisode`; thêm `MaintenanceTaskTemplate` và `MaintenanceChecklistItem`. `ScheduledEndUtc` nullable. Bổ sung guard chống overlap dưới transaction; không thể bảo đảm bằng unique index thường. Checklist item `Pending/Completed/Skipped` là mã đề xuất; `SkipReason` nullable; ghi actor/thời điểm cho thao tác. `ReleasedAtUtc` chỉ có sau kiểm tra đạt và phê duyệt đúng quyền |
| Nội dung Q18–22 | Admin/quản lý có quyền duyệt thông số trước công khai; người có quyền sửa thông số và bấm Lưu thì nội dung mới thay ngay. Model mặc định Nháp, có Nháp/Công khai/Ẩn. Có thể công khai thiếu giá nhưng chỉ Liên hệ. Cần ít nhất một ảnh thật hợp lệ, xác minh và có quyền sử dụng trước công khai | Không đặt workflow bắt buộc chờ duyệt lại cho mỗi lần sửa thông số đã công khai; Lưu của actor có quyền là xác nhận mới và cần audit/revision. Gate công khai yêu cầu ảnh hợp lệ, nội dung VI, các thông số *được hiển thị* đã xác minh; giá không phải gate công khai |
| Ngôn ngữ Q23–24 | Đủ nội dung tiếng Việt là được công khai; dịch còn lại bổ sung sau. Phần thiếu bản dịch hiển thị tiếng Việt | Không yêu cầu đủ sáu translation row để publish. Fallback theo trường/nội dung thiếu; không bịa bản dịch hoặc hiển thị bản dịch stale như mới |
| Khả dụng Q25 sửa, Q26 mới | Có giá nhưng không có xe vật lý Ready: chỉ Liên hệ. Không còn xe trống trong khoảng ngày khách chọn: chỉ Liên hệ với khoảng đó, không Đặt ngay | Khả dụng theo khoảng phải do backend tính từ inventory tương lai; không dùng COUNT(Ready) làm kết luận cuối. Không tạo Booking hoặc thay quy tắc confirm/hold trong schema này |
| Giá Q28 | Giá mới có hiệu lực ngay khi lưu; giá cũ giữ trong lịch sử | Version giá giữ `EffectiveFromUtc` bằng thời điểm commit có thẩm quyền; đóng phiên bản cũ atomically. Bỏ yêu cầu Admin chọn ngày hiệu lực tương lai cho thay đổi giá thường. Snapshot đơn cũ không bị tính lại |

**Delta dictionary PROPOSED:** `MaintenanceEpisode(Id, PhysicalVehicleId, ScheduledStartUtc, ScheduledEndUtc NULL, ActualStartUtc NULL, ActualEndUtc NULL, StatusCode, ReleasedAtUtc NULL, ReleasedByReference NULL, ReleaseInspectionId NULL, RowVersion, audit fields)`; `MaintenanceTaskTemplate(Id, Code, TitleVi, IsActive, RowVersion, audit fields)`; `MaintenanceChecklistItem(Id, MaintenanceEpisodeId, TaskTemplateId NULL, TitleSnapshotVi, StatusCode, Note NULL, SkipReason NULL, ChangedAtUtc NULL, ChangedByReference NULL, RowVersion)`. Mục riêng có `TaskTemplateId NULL`; mục lấy từ danh mục vẫn chụp tên tại thời điểm thêm để lịch sử không đổi khi sửa template. FK NO ACTION. Unique template code; index `(MaintenanceEpisodeId,Id)` và `(PhysicalVehicleId,ScheduledStartUtc)`. Chưa chốt title/notes đa ngôn ngữ cho checklist nội bộ và thuật toán thời gian của đợt không có end dự kiến; đó là đề xuất kỹ thuật phải kiểm thử trước migration.

**Tính nhất quán:** Revision 1 phía dưới còn nhắc `MaintenanceWindow`, VIN unique, PendingInspection bắt buộc, `ScheduledEndUtc` NOT NULL, locale coverage và giá có thể lên lịch. Các câu/constraint đó **đã bị thay thế bởi Revision 2**, không được đưa vào EF mapping. ERD Revision 2 và decision log Revision 2 là tham chiếu hiện hành. Tổng số entity đề xuất tăng từ 19 lên **21** (thay 1, thêm 2), chưa phê duyệt để triển khai.

Tài liệu phối hợp: [ERD](PHASE_3_CATALOG_FLEET_ERD.md), [decision log](PHASE_3_SCHEMA_DECISION_LOG.md), [bằng chứng Phase 2](PHASE_2_DATABASE_CONNECTION_REPORT.md). Mọi entity, tên cột, kiểu, enum, constraint và index dưới đây là **PROPOSED**, kể cả khi hành vi mà nó bảo vệ là APPROVED. Chỉ chủ dự án có thể chuyển quyết định sang phê duyệt; không coi việc xuất hiện trong ERD là phê duyệt.

## 1. Baseline và phạm vi

Đầu phiên git status sạch, scaffold đã tracked. Đã kiểm source: bốn project backend, `SangRental.Infrastructure.Persistence.SangRentalDbContext` chưa có DbSet, schema mặc định `sangrental`, migration InitialFoundation rỗng. EF SQL Server/Design và dotnet-ef 10.0.13. API giữ DI/configuration đã hoạt động ở Phase 2.

Làm rõ cách gọi trong yêu cầu: `ConnectionStrings` là section; key đầy đủ thực tế vẫn là **ConnectionStrings:SangRental**, không đổi thành một key mới. Phase 2 ghi nhận CanConnectAsync true và 0 user tables; phiên này không truy cập SQL để xác minh lại số bảng/kết nối và không dùng kết quả cũ như một lần test mới.

Chỉ thiết kế Catalog/Fleet cùng điểm tiếp giáp giá, buffer, lịch sử và i18n. Không thiết kế lại booking transitions/allocation algorithm, không thêm bảng Booking/Payment/User/Refund, không seed, API/UI, entity C#, EF configuration, migration hoặc database update. Ba tài liệu mới và nhật ký/task board là toàn bộ thay đổi dự kiến.

Nguồn đã đối chiếu: AGENTS.md; Master Prompt v2.7 §1–9; đặc tả v2.7 §1–8, §11–15; quy chuẩn UI/UX; ARCHITECTURE, DECISIONS, OPEN_QUESTIONS, REQUIREMENTS, PROJECT_RULES, PROGRESS_LOG, TASK_BOARD, API_CONTRACTS, SECURITY_PRIVACY, Phase 2 và HTML Stitch đã đọc trong chuỗi làm việc. Không lấy số xe/giá/review/brand example/cam kết trong Stitch làm dữ liệu thật.

## 2. Phân tích yêu cầu và truy vết

APPROVED trong bảng này chỉ chỉ **quy tắc/yêu cầu nguồn có căn cứ**, không phê duyệt toàn bộ bản REVIEW DRAFT v2.7 hoặc phương án vật lý.

| Yêu cầu và căn cứ | Phân loại | Hệ quả thiết kế / giới hạn |
|---|---|---|
| Xe máy: xe số, tay ga, xe côn; ô tô 4/7/16 chỗ — Master §2, đặc tả §2 | APPROVED — phạm vi danh mục | VehicleCategory và mã họ phương tiện; không seed mẫu/giá. Phân biệt nhãn nhóm xe với thông số ghế thực tế |
| Khách chọn mẫu, backend phân bổ biển số nội bộ — Master §3.1, đặc tả §1/§3.2 | APPROVED | VehicleModel là đơn vị lựa chọn công khai; PhysicalVehicle chỉ nội bộ. Không bán/giữ một biển số qua catalog |
| Inquiry không giữ xe/thu tiền; booking hold 15 phút theo model — Master §3.1 | APPROVED | Catalog không có ReservedCount, HoldUntil hoặc payment status; module sau chịu trách nhiệm |
| Brand/manufacturer như entity riêng | PROPOSED | VehicleBrand để chuẩn hóa hãng. Nguồn chưa chốt sổ hãng, quan hệ nhà sản xuất pháp nhân hoặc hãng đa nhãn |
| Variant/trim, đời, dung tích, ABS so với không ABS | OPEN về granularity | Khuyến nghị mỗi cấu hình có thể thuê/thay thế đồng nhất là một VehicleModel riêng; EditionLabel hỗ trợ phân biệt. Chưa thêm VehicleVariant |
| Ảnh, thông số được xác minh, admin duyệt, không bịa safety score — Master §2, đặc tả §2/§14 | APPROVED về hành vi | Typed specification, image metadata và provenance; bộ cột/đơn vị/điều kiện duyệt là PROPOSED |
| Physical fleet theo model, kiểm tra/bảo trì, xe mất/tai nạn bị khóa — Master §3.1/§3.2, BR-27/33/37 | APPROVED | Tách lifecycle, operational status, inspection, maintenance; lịch thuê tương lai là aggregate khác |
| Không xóa model/unit có lịch sử — BR-28 | APPROVED | Retire/disable; FK NO ACTION; không cascade delete. Đề xuất giữ cả draft để giảm nhánh xóa |
| Biển số unique theo phạm vi kinh doanh — đặc tả §7, mô hình ĐỀ XUẤT | PROPOSED / OPEN scope | Không tự coi unique toàn cầu/lifetime là quyết định đã duyệt; registration history và quyết định S-06 |
| Giá >0, phí >=0, giảm 0–100%; trả trước >0 đến 100% tiền thuê sau giảm — Master §3.1, đặc tả §3.1 | APPROVED | Decimal/check; rental policy per model. Không tạo security-deposit riêng |
| Admin giá ngày/tuần/tháng, giảm theo thời lượng và online 10% mặc định cấu hình được — Master §2/§7 | APPROVED về khả năng cấu hình | Các version giá/discount; không tự tạo số tiền hoặc tỷ lệ từng mốc. Cách chọn rate/combine/rounding còn OPEN |
| Buffer cấu hình theo loại xe, UTC và lịch [start,end) — Master §3.1/§4/§5, đặc tả §7 | APPROVED về invariant | Version policy; chưa chọn số phút, cách áp trước/sau hoặc công thức capacity |
| VI nguồn, EN/KO/ZH/JA/TH, cache/fallback/admin override — Master §2, đặc tả §2, UI/UX §5 | APPROVED | Locale chuẩn vi/en/ko/zh-CN/ja/th; child translation rows. Cache/provider worker toàn cục thuộc pha sau |
| Hiển thị công khai, không công bố giá/SLA/thông số/review chưa xác minh — Master §2/§9 | APPROVED | Publication tách lifecycle; ẩn catalog không đồng nghĩa hủy đơn hoặc retire xe |
| Snapshot, giao dịch chốt bất biến; không confirm nếu chưa verified payment + allocation — Master §3.1/§4 | APPROVED | Version policies và boundary snapshot tương lai; không để sửa catalog làm tính lại lịch sử tài chính |

Không thấy yêu cầu đã duyệt buộc tách Manufacturer/Brand/ModelFamily/Variant thành bốn tầng. Không đưa các tầng này vào baseline chỉ vì tên xe thương mại có nhiều phiên bản.

## 3. Tổng thể 19 entity đề xuất

| Nhóm | Entity | Vai trò |
|---|---|---|
| Catalog | VehicleCategory, VehicleCategoryTranslation | Loại xe và nội dung sáu locale |
| Catalog | VehicleBrand | Nhãn hãng, không đại diện pháp nhân nhà sản xuất |
| Catalog | VehicleModel, VehicleModelTranslation | Mẫu/cấu hình cho thuê và nội dung công khai |
| Catalog | VehicleModelSpecification | Một bộ thông số typed tùy chọn cho model |
| Catalog | VehicleModelImage, VehicleModelImageTranslation | Ảnh model, provenance, alt/caption theo locale |
| Fleet | PhysicalVehicle | Xe vật lý, mã tài sản nội bộ, trạng thái hiện thời |
| Fleet | VehicleRegistration | Lịch sử biển số/đăng ký theo thời gian |
| Fleet | VehicleOperationalStatusHistory | Sự kiện đổi trạng thái vận hành |
| Fleet | VehicleInspection | Kết quả kiểm tra kỹ thuật bất biến |
| Fleet | MaintenanceWindow | Khoảng bảo trì dự kiến và thực tế |
| Giá liên quan | VehicleModelRateVersion | Giá ngày, tùy chọn tuần/tháng và hiệu lực |
| Giá liên quan | VehicleModelDiscountPolicyVersion, VehicleModelDurationDiscountTier | Online discount và các mốc thời lượng |
| Giá liên quan | VehicleModelRentalPolicyVersion | Tỷ lệ thuê trả trước theo model |
| Buffer | VehicleCategoryBufferPolicyVersion | Tham số buffer theo loại xe |
| Lịch sử | CatalogFleetAuditEvent | Audit giới hạn catalog/fleet, không giả là AuditLog toàn hệ thống đã hoàn tất |

Không quan hệ many-to-many trong baseline. Một model thuộc đúng một category/brand. Nếu cần một model nằm trong nhiều bộ sưu tập marketing, bổ sung CMS collection riêng sau, không làm category kho thay đổi theo campaign.

## 4. Quy ước vật lý chung — PROPOSED (S-01, S-02)

Các bảng dùng schema `sangrental`, tên bảng singular theo entity để dễ truy vết. Chưa phát sinh DDL. `NN` = NOT NULL, `NULL` = nullable. Không có default giá/discount/Ready hoặc dữ liệu hãng. Defaults kỹ thuật chỉ được ghi nơi nêu rõ.

| Trường/kiểu dùng lại | Định nghĩa và lý do |
|---|---|
| Id | bigint IDENTITY(1,1), NN, PK clustered; ID nội bộ không có ý nghĩa nghiệp vụ và không phải biện pháp bảo mật. Translation dùng composite PK thay Id; Specification dùng shared PK |
| M — nhóm audit/concurrency cho bảng mutable | CreatedAtUtc datetime2(7) NN, CreatedByReference nvarchar(128) NN; UpdatedAtUtc datetime2(7) NULL, UpdatedByReference nvarchar(128) NULL; RowVersion rowversion NN (SQL sinh, không datetime) |
| A — nhóm cho append-only | CreatedAtUtc datetime2(7) NN, CreatedByReference nvarchar(128) NN; không Updated*, không rowversion vì cấm sửa bản ghi đã ghi nhận. Closure metadata ngoại lệ nêu riêng |
| Audit guards M | UpdatedAtUtc/UpdatedByReference cùng null hoặc cùng có giá trị; UpdatedAtUtc >= CreatedAtUtc. CreatedAtUtc default SYSUTCDATETIME() đề xuất; actor do server cung cấp, không lấy tùy ý từ request |
| ActorReference | ID opaque từ identity provider/service đã xác minh; không FK User giả. Mapping/phạm vi identity còn OPEN; không dùng tên hiển thị/email làm khóa. Chưa identity thật thì chưa mở write runtime |
| Mã kỹ thuật `Code` | varchar(64), chữ thường, không chứa khoảng trắng/ASCII whitespace; unique và so sánh bằng BIN2. Database CHECK bảo vệ quy tắc, tầng ứng dụng có thể chuẩn hóa trước khi ghi. `AssetCode` là mã tài sản riêng, không áp quy tắc chữ thường này. Code không tái sử dụng sau retire |
| Chuỗi hiển thị | nvarchar, Unicode; NN không được trắng qua CHECK LEN(LTRIM(RTRIM(field))) > 0. Kiểm soát độ dài/Unicode whitespace đầy đủ ở application |
| Slug | varchar(160) chuẩn ASCII lowercase; nằm trong translation, unique theo locale/toàn nhóm entity, không dùng tên dịch làm PK. Đổi slug/redirect SEO là S-10 chưa chốt |
| Locale | varchar(5) COLLATE Latin1_General_100_BIN2, NN; CHECK IN ('vi','en','ko','zh-CN','ja','th'). Không lưu price/code/plate/status bằng chuỗi dịch |
| Tiền | decimal(19,2), không float/real/money; price >0, percent decimal(5,2). CurrencyCode char(3) NN, đề xuất CHECK = 'VND' cho scope đầu; quyết định mở rộng currency và VND rounding ở S-09, không suy ra đây là policy tài chính đã duyệt |
| Hiệu lực | EffectiveFromUtc datetime2(7) NN, EffectiveToUtc datetime2(7) NULL (mở vô hạn); CHECK end IS NULL OR end > start. Half-open [from,to), null là vô hạn, không tự dịch sang giờ local |
| FK | Required trừ chỗ ghi NULL; DeleteBehavior.NoAction/ON DELETE NO ACTION cho mọi quan hệ trong proposal. Không cascade xóa/update key; index FK chỉ khi đã nằm đầu PK/index khác thì không tạo trùng |

Chọn bigint để join/index gọn; không chọn GUID phân tán khi chưa có nhu cầu. FK không kiểm được parent đang active/published, đúng actor hay interval không overlap. EF concurrency cần IsRowVersion và xử lý DbUpdateConcurrencyException ở application sau; chưa có code nào được tạo.

## 5. Data dictionary: catalog — PROPOSED

### 5.1 VehicleCategory (M)

| Cột ngoài M | SQL type / null | Ràng buộc / ý nghĩa |
|---|---|---|
| Id | bigint NN | PK |
| Code | varchar(64) NN | Unique, mã bất biến |
| VehicleFamilyCode | varchar(16) NN | CHECK Motorcycle/Car — mã kỹ thuật đề xuất |
| LifecycleStatus | varchar(16) NN | CHECK Active/Retired; default Active chỉ khi duyệt S-03 |
| PublicationStatus | varchar(16) NN | Draft/Published/Hidden; default Draft |
| DisplayOrder | int NN | >=0, default 0; không cần unique |
| ContentRevision | int NN | >0, default 1; tăng khi nội dung nguồn của category thay đổi |
| RetiredAtUtc, RetirementReason | datetime2(7) NULL; nvarchar(1000) NULL | Cùng có khi Retired, cùng null khi Active |

Category phẳng trong scope sáu loại nguồn; không tạo cây vô hạn. Không ép SeatCount=4/7/16 bằng category trước khi rõ cách đếm ghế/tài xế. Publication category không thay operational fleet.

### 5.2 VehicleCategoryTranslation (M)

PK (VehicleCategoryId bigint NN FK Category.Id, Locale); Name nvarchar(160) NN; Description nvarchar(2000) NULL; Slug varchar(160) NN; TranslationStatus varchar(16) NN CHECK Draft/Reviewed; SourceRevision int NN >0; ReviewedByReference nvarchar(128) NULL; ReviewedAtUtc datetime2(7) NULL. Reviewed yêu cầu cả reviewer/time, Draft cả hai null. Unique (Locale,Slug). Nội dung vi cũng là row riêng, không có NameVi/NameEn.

### 5.3 VehicleBrand (M)

Id bigint NN PK; Code varchar(64) NN unique; DisplayName nvarchar(160) NN (tên thương mại, không tự dịch); LifecycleStatus varchar(16) NN Active/Retired; RetiredAtUtc/RetirementReason như Category. Không tên hãng seed, không bảng Manufacturer/country/legal entity. Brand localized marketing nếu cần là mở rộng, chưa yêu cầu.

### 5.4 VehicleModel (M)

| Cột ngoài M | SQL type / null | Ràng buộc / ý nghĩa |
|---|---|---|
| Id | bigint NN | PK, lựa chọn công khai tham chiếu model này |
| VehicleCategoryId, VehicleBrandId | bigint NN mỗi cột | FK Category/Brand |
| Code | varchar(64) NN | Unique, không dùng tên hãng+mẫu làm khóa |
| CommercialName | nvarchar(160) NN | Tên mẫu chuẩn, không phải nội dung marketing dịch |
| EditionLabel | nvarchar(80) NULL | Định danh bản/trim nếu có nguồn xác minh, không tự sinh VehicleVariant |
| ModelYear | smallint NULL | CHECK 1..9999 nếu có; unknown là null, không suy năm sản xuất từng unit |
| ContentRevision | int NN | >0, default 1; tăng khi nội dung nguồn/spec/ảnh thay đổi |
| LifecycleStatus | varchar(16) NN | Active/Retired |
| PublicationStatus | varchar(16) NN | Draft/Published/Hidden, default Draft |
| PublishedAtUtc, PublishedByReference | datetime2(7) NULL; nvarchar(128) NULL | Cùng có khi đã từng publish; không xóa lịch sử khi Hidden |
| IsFeatured, DisplayOrder | bit NN; int NN | false / >=0, default 0; không ảnh hưởng tồn kho |
| RetiredAtUtc, RetirementReason | datetime2(7) NULL; nvarchar(1000) NULL | Quy tắc lifecycle như Category |

Không unique theo CommercialName, EditionLabel hoặc ModelYear: draft có thể trùng tên và null không phản ánh identity. Duplicate detection là review nghiệp vụ. VehicleCategoryId/BrandId và nội hàm cấu hình không được âm thầm sửa làm đổi ý nghĩa unit/booking cũ; sau khi đã sử dụng cần model mới hoặc quy trình correction có audit/snapshot, quyết định S-04.

### 5.5 VehicleModelTranslation (M)

PK (VehicleModelId bigint NN FK Model.Id, Locale); DisplayName nvarchar(200) NN; Summary nvarchar(1000) NULL; Description nvarchar(max) NULL (plain text hoặc sanitized format, không raw HTML tin cậy); Slug varchar(160) NN; SeoTitle nvarchar(200) NULL; SeoDescription nvarchar(500) NULL. TranslationStatus/SourceRevision/ReviewedByReference/ReviewedAtUtc như §5.2. Unique (Locale,Slug). Không dịch ID/giá/plate; validation nội dung và provenance dịch thuộc application.

### 5.6 VehicleModelSpecification (M)

VehicleModelId bigint NN **PK đồng thời FK Model.Id**; không Id riêng. Model có 0..1 specification (draft thiếu được), không EAV/JSON cho thông số lọc chính.

| Cột | SQL type / null | CHECK / ý nghĩa |
|---|---|---|
| SeatCount | tinyint NULL | >0 nếu có; quy ước đếm ghế còn OPEN |
| EngineDisplacementCc | decimal(7,2) NULL | >0 nếu có; xe không có động cơ đốt trong không phải 0 giả |
| FuelConsumptionLitresPer100Km | decimal(6,3) NULL | >0 nếu có; điều kiện đo phải có trong provenance |
| LuggageCapacityLitres | decimal(8,2) NULL | >=0 nếu có; không suy luggage suitability score |
| WeightKg | decimal(8,2) NULL | >0 nếu có |
| TransmissionCode | varchar(24) NULL | Mã kỹ thuật từ taxonomy cần duyệt; không free-form text dịch |
| EnergyTypeCode | varchar(24) NULL | Taxonomy cần duyệt, không mặc định xăng/điện |
| HasAbs | bit NULL | Unknown khác false; không tự bịa chỉ số an toàn |
| VerificationStatus | varchar(16) NN | Draft/Verified; default Draft |
| SourceReference | nvarchar(500) NULL | Nguồn thông số, không secret/signed URL/giấy tờ khách |
| VerifiedAtUtc, VerifiedByReference | datetime2(7) NULL; nvarchar(128) NULL | Verified bắt buộc cả hai và SourceReference; Draft cả reviewer/time null |

Bộ trường trên là lựa chọn kỹ thuật cần S-05; không mặc định mọi loại xe phải có đủ tất cả. Thay bất kỳ giá trị đã xác minh phải reset verification, tăng Model.ContentRevision trong cùng transaction; thông số mới không tự xuất hiện công khai với dấu đã duyệt cũ. Nguồn từng thông số khác nhau cần tách provenance per-field sau review; chưa tuyên bố một link xác minh được mọi trường.

### 5.7 VehicleModelImage (M)

Id bigint NN PK; VehicleModelId bigint NN FK Model; StorageKey nvarchar(450) NN; ContentHash binary(32) NN (SHA-256 content, không phải quyền truy cập); MediaType varchar(64) NN; Width/Height int NN >0; DisplayOrder int NN >=0; IsPrimary bit NN default false; IsVisible bit NN default false; ApprovalStatus varchar(16) NN Draft/Approved/Rejected; SourceReference nvarchar(500) NULL; RightsReference nvarchar(500) NULL; ApprovedAtUtc datetime2(7) NULL; ApprovedByReference nvarchar(128) NULL; RetiredAtUtc datetime2(7) NULL.

Approved bắt buộc SourceReference/RightsReference/reviewer/time; IsVisible=1 đòi Approved và RetiredAtUtc null. IsPrimary=1 đòi IsVisible=1. Không suy ảnh internet là ảnh thực tế hoặc có quyền sử dụng. StorageKey không chứa token/query string; URL delivery sinh ở tầng ứng dụng/CDN. Nội dung file không overwrite cùng key; thay ảnh tạo row/key mới, ẩn row cũ và giữ blob khi có reference lịch sử. Dedupe hash không unique vì nhiều model có thể dùng cùng asset hợp lệ.

### 5.8 VehicleModelImageTranslation (M)

PK (VehicleModelImageId bigint NN FK Image, Locale); AltText nvarchar(300) NN; Caption nvarchar(1000) NULL; TranslationStatus/SourceRevision/ReviewedByReference/ReviewedAtUtc như §5.2. SourceRevision đối chiếu Model.ContentRevision thông qua Image; không FK composite cứng với revision đang thay đổi. Ảnh sản phẩm có nội dung phải có alt có nghĩa, không auto tạo từ biển số.

Ảnh xe vật lý/evidence **không** trộn vào ảnh model công khai. Thiết kế storage evidence/ảnh unit riêng ở pha fleet operations sau (S-05), tránh lộ biển số hoặc dùng catalog image làm bằng chứng bàn giao.

## 6. Data dictionary: fleet — PROPOSED

### 6.1 PhysicalVehicle (M)

Id bigint NN PK; VehicleModelId bigint NN FK Model; AssetCode varchar(64) NN unique/bất biến; ManufacturingYear smallint NULL CHECK 1..9999; ColorName nvarchar(80) NULL (nội bộ, chưa hỗ trợ lựa chọn màu); VinOrChassisNumber nvarchar(100) NULL; NormalizedVinOrChassisNumber varchar(100) NULL; ServiceLifecycle varchar(16) NN Active/Retired; OperationalStatus varchar(32) NN; LastReceivedAtUtc datetime2(7) NULL; ReadyForRentAtUtc datetime2(7) NULL; RetiredAtUtc datetime2(7) NULL; RetirementReason nvarchar(1000) NULL.

Vin pair phải cùng null hoặc cùng có; normalization policy ở S-06. Đề xuất unique filtered NormalizedVinOrChassisNumber IS NOT NULL, không ép cấu trúc 17 ký tự cho mọi xe máy. Default trạng thái nhập xe **PendingInspection**, không default Ready. Retire giống §5.1, không xóa row hoặc đổi model để tái sử dụng AssetCode.

OperationalStatus tập giá trị **PROPOSED**, chưa là enum đã duyệt: PendingInspection, Ready, ReturnInspection, Maintenance, Suspended, LostLocked, AccidentLocked. Không đưa BookingStatus/PaymentStatus vào đây. Rented/Handover có trong ví dụ state nguồn nhưng chưa khóa enum; khuyến nghị module usage/allocation tương lai biểu diễn đang thuê/bàn giao riêng, còn đây là khả năng vận hành. Quyết định này phải duyệt S-07 trước mapping enum.

ReadyForRentAtUtc/LastReceivedAtUtc không phải scheduled return và không chứng minh sẵn có cho một khoảng thuê. CHECK nếu cả hai có thì ready >= received; null cho xe chưa có mốc này. Chỉ workflow xác minh sau mới cập nhật Ready; tìm lại xe không tự Ready (BR-37), có tranh chấp tiền không tự khóa xe đã kiểm tra (BR-30). Chưa định nghĩa transition mới hoặc quyền manager/Admin ngoài nguồn.

### 6.2 VehicleRegistration (A + closure concurrency)

Id bigint NN PK; PhysicalVehicleId bigint NN FK Unit; RegistrationJurisdictionCode varchar(32) NN; PlateDisplay nvarchar(32) NN; NormalizedPlate varchar(32) NN; ValidFromUtc datetime2(7) NN; ValidToUtc datetime2(7) NULL; ClosedAtUtc datetime2(7) NULL; ClosedByReference nvarchar(128) NULL; ClosureReason nvarchar(1000) NULL; RowVersion rowversion NN.

CHECK valid end > start; ValidTo/ClosedAt/ClosedBy/ClosureReason cùng null hoặc cùng có. Plate/owner/start không sửa; closure là cập nhật một lần có audit, không reopen. Đổi biển: đóng row cũ + thêm row mới + bump Unit.RowVersion trong cùng transaction. Đề xuất unique current (jurisdiction,normalized plate) WHERE ValidToUtc IS NULL và unique current PhysicalVehicleId WHERE ValidToUtc IS NULL. Không filter theo unit Active để tránh reuse biển làm sai lịch sử xe retired. Không áp unique toàn lifetime; xử lý tái cấp biển và khoảng lịch sử overlap cần S-06. Draft unit được có 0 registration; điều kiện đưa vào khai thác cần duyệt, không giả vờ record là chứng nhận hợp lệ pháp lý.

### 6.3 VehicleOperationalStatusHistory (A)

Id bigint NN PK; PhysicalVehicleId bigint NN FK Unit; PreviousStatus varchar(32) NULL (chỉ sự kiện khởi tạo), NewStatus varchar(32) NN; OccurredAtUtc datetime2(7) NN; Reason nvarchar(1000) NN; CorrelationId uniqueidentifier NN; ExternalEventId varchar(128) NULL (chỉ khi có nguồn sự kiện tin cậy); SourceCode varchar(32) NN. Status CHECK theo tập ở §6.1, previous != new khi previous có giá trị. Unique filtered (SourceCode,ExternalEventId) khi ExternalEventId có để tránh ghi lại cùng event; CorrelationId không unique vì nhiều event có thể chung tác vụ.

Current status và history phải ghi atomically kèm Unit.RowVersion. Lịch sử này append-only, không thay hồ sơ incident/handover/approval tương lai. Idempotency key source được backend xác minh, không coi client truyền event là chứng minh BR-46.

### 6.4 VehicleInspection (A)

Id bigint NN PK; PhysicalVehicleId bigint NN FK Unit; InspectionTypeCode varchar(32) NN; ResultCode varchar(16) NN CHECK Passed/Failed/NeedsReview; InspectedAtUtc datetime2(7) NN; InspectorReference nvarchar(128) NN; OdometerKm decimal(12,1) NULL CHECK >=0; Findings nvarchar(2000) NULL; EvidenceReference nvarchar(450) NULL; SourceReference nvarchar(500) NULL.

Loại kiểm tra/checklist/tần suất/expiry/chứng cứ bắt buộc còn S-07. Không đồng nhất kiểm tra nội bộ với đăng kiểm pháp luật; không tự đặt hạn kiểm định. Kết quả đã ghi nhận không sửa; sai sót ghi audit/correction mới theo quy trình cần duyệt, không xóa report. Không có FK LastInspectionId vòng với Unit; truy vấn lịch sử bằng index, workflow kiểm quyền/đúng unit/thời điểm/result trước đánh dấu Ready. EvidenceReference là ID nội bộ storage, không public URL hoặc bản ký có thể thay đổi.

### 6.5 MaintenanceWindow (M)

Id bigint NN PK; PhysicalVehicleId bigint NN FK Unit; ScheduledStartUtc datetime2(7) NN; ScheduledEndUtc datetime2(7) NN; ActualStartUtc/ActualEndUtc datetime2(7) NULL; StatusCode varchar(16) NN CHECK Planned/InProgress/Completed/Cancelled; Reason nvarchar(1000) NN; ReleasedAtUtc datetime2(7) NULL; ReleasedByReference nvarchar(128) NULL; ReleaseInspectionId bigint NULL FK VehicleInspection.Id; CancellationReason nvarchar(1000) NULL.

CHECK scheduled end > start; actual end có thì start có và end >= start; released time/by/inspection cùng null hoặc cùng có; Completed bắt buộc actual start/end nhưng **không** đủ để tự ready; Cancelled cần reason và chưa release. FK đơn ReleaseInspectionId không chứng minh inspection đúng unit: application transaction bắt buộc kiểm, có thể thay bằng composite FK ở S-07. Không thiết kế trigger hoặc workflow release tự động ở lượt này.

Lịch dự kiến kết thúc không chứng minh xe đã bảo trì xong. Unreleased InProgress/Completed phải tiếp tục chặn khai thác theo workflow sau; planned interval tham gia kiểm khả dụng với buffer, không tự tạo allocation. Hủy/đổi lịch bảo trì có booking tương lai cần phát hiện xung đột dưới cùng protocol khóa của inventory tương lai. Có cho overlap maintenance không là S-07; unique index không thể giải bài toán khoảng.

## 7. Giá và policy liên quan — PROPOSED, chưa là pricing engine

Version tables dưới đây dùng M. Mỗi root có Id bigint PK, owner FK, VersionNumber int NN >0, EffectiveFromUtc/EffectiveToUtc như §4, PublicationStatus varchar(16) NN Draft/Published/Withdrawn, PublishedAtUtc/PublishedByReference nullable theo cặp; Published đòi cặp này có. **Unique (owner FK,VersionNumber)**; version number không dùng MAX+1 không khóa.

Published payload không sửa, không xóa: tạo version mới. Đóng EffectiveToUtc của version mở là ngoại lệ metadata một lần, audit trong transaction publish version tiếp; không đổi amount/percent đã published hoặc mở lại interval cũ. Version mới không hồi tố booking/transaction. Semantics withdraw/gap/backdated correction cần S-08/S-09. Không thể CHECK interval non-overlap cross-row bằng CHECK thường; chọn transaction/serialization riêng cho owner.

### 7.1 VehicleModelRateVersion

Owner VehicleModelId bigint NN FK Model; DailyRate decimal(19,2) NN >0; WeeklyRate/MonthlyRate decimal(19,2) NULL >0 nếu có; CurrencyCode char(3) NN CHECK VND (đề xuất scope đầu). Không default giá, không số tiền demo. Không tự suy weekly = daily*7 hoặc monthly = daily*30. Null nghĩa chưa cấu hình, không có nghĩa miễn phí.

Không được publish pricing thực thi trước khi chốt: 1 ngày là rolling 24h hay calendar day, tuần/tháng tính thế nào, làm tròn kỳ thuê, chọn rate tối ưu/tách gói, phụ phí cuối tuần/mùa, đơn vị/làm tròn tiền. Mục nguồn nói cần các khả năng giá, không cấp một công thức cụ thể. Vì vậy các cột này là proposal lưu dữ liệu chờ policy, không phải quote API.

### 7.2 VehicleModelDiscountPolicyVersion

Owner VehicleModelId bigint NN FK Model; OnlineDiscountPercent decimal(5,2) NN CHECK 0..100; CompositionRuleCode varchar(64) NULL; RoundingRuleCode varchar(64) NULL. Hai code là reference đến quy tắc cần review, không enum hoặc bảng giả đã duyệt. Draft được null; publish pricing dùng thật chỉ khi đã có rule hợp lệ phía server. Không DB default 10 hoặc seed; admin default 10% từ yêu cầu sẽ được đưa vào workflow sau khi duyệt config ban đầu, không coi mọi booking được giảm 10 bất kể policy version.

### 7.3 VehicleModelDurationDiscountTier (M)

Id bigint NN PK; DiscountPolicyVersionId bigint NN FK DiscountVersion; MinimumRentalDays int NN CHECK >=1; DiscountPercent decimal(5,2) NN CHECK 0..100; unique (DiscountPolicyVersionId,MinimumRentalDays). Không MaxDays nên không sinh khoảng ngày tier chồng nhau; **cách chọn tier vẫn OPEN**, không mặc định cao nhất hoặc cộng tất cả. Các mốc 3/7/15/30 là yêu cầu nguồn cần hỗ trợ; tỷ lệ từng mốc, có cho mốc khác và thứ tự online/duration chưa chốt. Child chỉ sửa khi parent Draft và bump parent RowVersion; sau publish bất biến, không cascade delete.

### 7.4 VehicleModelRentalPolicyVersion

Owner VehicleModelId bigint NN FK Model; PrepaymentPercent decimal(5,2) NN CHECK >0 AND <=100. Giá trị 100 biểu diễn trả trước toàn bộ, không thêm bool FullPayment gây trạng thái bất nhất. Không SecurityDepositAmount/RefundRate/default 30. Phần phải trả tính trên tiền thuê sau giảm theo nguồn; rate/tier/delivery/tax nằm trong quote/snapshot tương lai, không trong PhysicalVehicle. OPEN-05 và rounding không được giải quyết bằng tỷ lệ tự đặt.

### 7.5 VehicleCategoryBufferPolicyVersion

Owner VehicleCategoryId bigint NN FK Category; BufferMinutes int NN CHECK >=0. Cũng dùng version envelope/M ở đầu §7. Đây là **một entity ngoài 4 entity model pricing**, nhằm lưu tham số buffer được cấu hình theo loại xe, không chốt công thức. Số phút, buffer trước/sau, cách áp vào maintenance/actual ready, xử lý thay policy với booking đã giữ là S-11. Không default 0 khiến xe tự sẵn có; chưa policy hợp lệ thì availability phải từ chối/fail closed ở module tương lai.

## 8. CatalogFleetAuditEvent — PROPOSED (A)

Id bigint NN PK; AggregateType varchar(64) NN (allowlist các root trong proposal); AggregateId bigint NN; ActionCode varchar(64) NN; OccurredAtUtc datetime2(7) NN; ActorReference nvarchar(128) NN; Reason nvarchar(1000) NULL; CorrelationId uniqueidentifier NN; BeforeJson/AfterJson nvarchar(max) NULL, CHECK từng cột IS NULL OR ISJSON(...)=1. Application bắt buộc whitelist field, loại secrets/PII/customer document/raw provider payload.

AggregateId là logical reference theo AggregateType, **không FK đa hình tự nhận là toàn vẹn DB**. Transaction use case phải xác minh root tồn tại và append audit cùng mutation; không hard delete root đề xuất. Actor/CreatedByReference của A lấy cùng trusted actor, không hai nguồn client. Event không UPDATE/DELETE bằng application role sau triển khai; quyền DB/infrastructure kiểm chứng ở pha riêng. Không khẳng định chỉ có timestamp/JSON là audit bất biến. Temporal table/SQL ledger không được tự thêm, retention không tự đặt số ngày.

## 9. Constraint và index catalogue

### Unique / PK / filtered unique

| Bảng | Unique đề xuất ngoài PK | Vì sao / giới hạn |
|---|---|---|
| Category, Brand, Model | Code, so sánh normalization/BIN2 | Không tái sử dụng code retired; không lấy tên Unicode làm identity |
| CategoryTranslation, ModelTranslation | (Locale,Slug) | Không hai slug cùng locale trong cùng resource; khác resource có route prefix riêng, chưa khóa URL |
| ModelSpecification | PK/FK VehicleModelId | Tối đa một specification, không ép draft phải có |
| ModelImage | VehicleModelId WHERE IsPrimary=1 AND RetiredAtUtc IS NULL | Tối đa một primary, không đảm bảo published có ít nhất một; phải transaction guard |
| PhysicalVehicle | AssetCode; NormalizedVinOrChassisNumber WHERE IS NOT NULL | Chống trùng tài sản; VIN scope/correction cần S-06 |
| VehicleRegistration | PhysicalVehicleId WHERE ValidToUtc IS NULL; (RegistrationJurisdictionCode,NormalizedPlate) WHERE ValidToUtc IS NULL | Chỉ current; historical overlap và tái cấp còn cần guard/policy |
| StatusHistory | (SourceCode,ExternalEventId) WHERE ExternalEventId IS NOT NULL | Giảm event replay, không thay business idempotency |
| Mỗi bảng version §7 | (owner FK,VersionNumber) | Phân biệt version; không ngăn hai khoảng hiệu lực chồng |
| DurationDiscountTier | (DiscountPolicyVersionId,MinimumRentalDays) | Chống trùng mốc; không chốt thuật toán chọn tier |

### Nonclustered indexes theo truy vấn dự kiến

| Bảng | Key / INCLUDE gợi ý | Use case |
|---|---|---|
| VehicleModel | (VehicleCategoryId,LifecycleStatus,PublicationStatus,DisplayOrder,Id), INCLUDE VehicleBrandId,Code | Danh sách model public, cursor ổn định |
| VehicleModel | (VehicleBrandId,Id) | FK/review theo hãng; không duplicate index đầu category |
| VehicleModelImage | (VehicleModelId,IsVisible,DisplayOrder,Id) | Gallery đã duyệt; primary index riêng |
| PhysicalVehicle | (VehicleModelId,ServiceLifecycle,OperationalStatus,Id), INCLUDE ReadyForRentAtUtc | Lấy candidate units nội bộ; không đảm bảo availability |
| VehicleRegistration | (PhysicalVehicleId,ValidFromUtc,Id), INCLUDE ValidToUtc,NormalizedPlate | Truy vết biển theo thời điểm; index current riêng |
| StatusHistory | (PhysicalVehicleId,OccurredAtUtc,Id) | Timeline và thứ tự stable khi timestamps trùng |
| VehicleInspection | (PhysicalVehicleId,InspectedAtUtc DESC,Id DESC), INCLUDE ResultCode | Kết quả kiểm tra gần nhất; không tự kết luận Ready |
| MaintenanceWindow | (PhysicalVehicleId,ScheduledStartUtc,Id), INCLUDE ScheduledEndUtc,StatusCode,ReleasedAtUtc | Candidate blocking windows; OR/open blocking cần đo query plan |
| MaintenanceWindow | (ReleaseInspectionId) filtered IS NOT NULL | FK lookup nếu đã duyệt link inspection |
| Mỗi version §7 | (owner FK,PublicationStatus,EffectiveFromUtc,Id), INCLUDE EffectiveToUtc | Policy có hiệu lực; không INCLUDE giá/JSON tùy tiện trước đo |
| CatalogFleetAuditEvent | (AggregateType,AggregateId,OccurredAtUtc,Id); (CorrelationId,Id) | Truy vết root/tác vụ |

Translations có PK bắt đầu owner FK; DiscountTier có unique bắt đầu parent FK nên không thêm FK index lặp. Không index nvarchar(max)/JSON body hoặc mọi field chỉ để dự phòng. Filter clause chỉ dùng cột cùng bảng; không filter Unit.Active trong bảng Registration. Index name đề xuất theo UX_/IX_/CK_ + entity + mục đích, chốt ở EF review; chưa có SQL execution plan/benchmark.

### Những gì DB CHECK không bảo đảm

CHECK/unique/FK chỉ kiểm được value range, same-row/cross-key relationships nêu trên. Các điều kiện sau bắt buộc application transaction + concurrency và test SQL thật: parent active/published, ít nhất vi reviewed/current, đúng inspection unit, không overlap effective interval/registration lịch sử/maintenance/allocation, giữ payload bất biến sau publish, đúng actor/permission, tăng parent revision khi child đổi. Không mô tả các guard này như constraint DB đã tồn tại.

## 10. I18n và publication — PROPOSED (S-10)

VI là source; các locale còn lại theo CHECK §4. Chỉ trả reviewed translation có SourceRevision khớp model/category ContentRevision; không dùng bản dịch cũ có dấu Reviewed như thể còn đúng sau sửa nguồn. Sửa category translation vi cũng phải tăng root revision, invalidate review các bản dịch liên quan và bump root rowversion trong cùng transaction.

Admin override cập nhật translation row, giữ reviewer/provenance và audit; provider-generated text khởi đầu Draft. Chưa tạo TranslationCache global/worker/API key/storage hoặc claim bản dịch được người bản ngữ duyệt. Fallback gợi ý: requested locale reviewed/current → vi reviewed/current → không publish field; không trả key raw hay dịch tự động giá/điều khoản pháp lý. Quy tắc fallback per-field hay per-document, minimum locale coverage để publish và stale bản dịch là S-10 chưa duyệt.

Brand/CommercialName/EditionLabel là identity thương mại không tự Google-translate; DisplayName/description/SEO/alt nằm ở translation. Status/enum/spec codes map qua resource keys ở frontend/admin về sau, không lưu chuỗi trạng thái sáu lần.

Gate public **đề xuất**: category/brand/model active, category/model published; có vi reviewed/current và thông số/ảnh được duyệt khi hiển thị chúng. Nếu UI hiển thị giá thì phải có các version pricing hợp lệ đã chốt semantics; không bịa giá khi thiếu. Đề xuất có thể publish trang thông tin chỉ inquiry khi chưa có giá, nhưng có cho phép như vậy và có bắt buộc ảnh/spec đủ trước publish là S-03, chưa coi approved. IsFeatured không vượt gate. Public DTO tương lai chỉ whitelist model, không serialize EF unit/registration navigations.

## 11. Availability, concurrency và lịch sử

**APPROVED invariant:** xe vận hành Ready không chứng minh có sẵn ở lịch khách muốn; stock không phải số rows Published hoặc COUNT(Ready). Public selection chỉ ModelId. Booking confirmation còn cần verified payment và allocate thành công; schema này không thay quy tắc đó.

**PROPOSED boundary**: query availability sau này cần model/candidate unit, unit lifecycle/status, khoảng maintenance, inspection/actual-ready, buffer version và các hold/allocation/gia hạn tương lai. Không tạo AvailableQuantity cache làm nguồn sự thật trong Catalog. Bộ bảng hiện tại chỉ cung cấp dữ liệu đầu vào, chưa đủ tính availability hoặc confirm booking.

Các rủi ro và guard cần triển khai sau review:

1. Hai cập nhật cùng unit/model: rowversion chống lost update, trả conflict thay silent overwrite. Child edit phải bump root rowversion/content revision; rowversion chỉ trên parent mà child đổi riêng vẫn tạo race publish.
2. Hai lần publish giá/buffer cùng owner: unique version number không ngăn overlap; khóa scope owner trong transaction, kiểm interval dưới isolation phù hợp, xử lý deadlock/retry có kiểm soát. Chưa chốt SQL lock protocol; không viết code allocation.
3. Allocation đụng maintenance/retire/lock: các workflow tương lai phải thống nhất inventory coordination key/transaction. Chỉ khóa hàng Unit ở một phía và không phía còn lại sẽ thất bại; unique interval index thường không đủ, rowversion không chống phantom.
4. Future hold theo model: query số unit hiện tại không trừ hết overlapping holds/allocations. Chưa tạo hold entity/capacity formula để tránh thiết kế lại nghiệp vụ ở lượt này.
5. Maintenance kết thúc dự kiến, actual received và ready-at khác nhau. Không tự ready theo clock; chưa verification thì không đưa xe tìm lại/bảo trì xong vào capacity. Không tự cancel các đơn tương lai khi xe mất (BR-33); conflict offline BR-46 vẫn theo module sau.
6. Retirement chỉ ngừng cho thuê mới sau khi xử lý conflict hợp lệ; không hủy booking đã có hoặc rewrite model lịch sử. Model/UnitId không tái sử dụng. Đề xuất không hard delete cả draft; BR-28 bắt buộc tối thiểu với lịch sử thuê, mở rộng đến draft cần S-12.
7. Financial history: booking snapshot tương lai phải sao chép model description/spec/giá/discount/rental/buffer version áp dụng, không chỉ join “giá hiện tại”. Không rewrite confirmed transaction, không tính refund từ Catalog. Catalog audit không thay snapshot tài chính.
8. Registration/image history: giữ row và file được tham chiếu; thay plate không làm biên bản cũ hiện biển mới. Không trộn file chứng cứ đã ký với gallery có thể sửa. Không tự ấn định retention giấy tờ theo tuổi model.
9. Rate/discount/rental/buffer có version riêng không có nghĩa cùng VersionNumber là cùng bộ policy. Quote tương lai phải chọn một bộ nhất quán theo effective instant đã chốt, đọc dưới transaction/snapshot phù hợp và lưu từng version ID cùng resolved values. Chưa có policy bundle/snapshot contract được duyệt; không tự thêm bảng quote/booking để che khoảng trống này.

## 12. Clean Architecture / EF Core consistency review

| Lớp | Hướng thiết kế sau khi duyệt | Trạng thái hiện tại |
|---|---|---|
| Domain | Entities/value objects/invariants thuần C#, không DbContext/DataAnnotations EF bắt buộc; aggregate Model, PhysicalVehicle, policy/category roots có owner rõ | Chưa tạo class |
| Application | Use cases, validation, trusted actor, publication/verification guards, transaction boundaries, concurrency conflict, public DTO whitelist | Chưa tạo use case/API |
| Infrastructure | IEntityTypeConfiguration cho types/keys/FKs/check/index/IsRowVersion; DbContext giữ schema sangrental; storage/provider implementations | Chưa tạo configuration/migration |
| API | Composition root giữ nguyên Phase 2; authorization và HTTP contracts khóa ở lượt sau | Không sửa Program/config/endpoint |

Shared-PK Specification là 1–0..1; translations/gallery là children; policy version không nằm trong eager-loaded model graph vô hạn. Không yêu cầu EF navigation loading trả toàn unit/audit ra public. Actor FK đến User được hoãn có chủ đích vì identity chưa chốt, không tự tạo auth tables. SQL type precision/column length/check explicit, không phụ thuộc provider default string(max)/decimal. EF configuration và generated SQL cần kiểm sau, bản vẽ chưa chứng minh migration compile/apply thành công.

## 13. Thiếu rõ ràng, quyết định và gate tiếp theo

Chi tiết S-01…S-13 ở [decision log](PHASE_3_SCHEMA_DECISION_LOG.md). Các blocker thiết kế chính: granularity model/variant (S-04), status/inspection/maintenance (S-07), scope biển/VIN/history (S-06), pricing/version/rounding (S-08/09), buffer semantics (S-11), actor/audit enforcement (S-02/12). Brand, typed specs, publication/i18n là đề xuất cần review chứ không phải mâu thuẫn đã giải quyết.

Không có mâu thuẫn buộc sửa quy tắc BR đã duyệt. Có khoảng trống: enum nguồn là minh họa; plate unique chưa chốt scope; giá tháng/tuần không xác định công thức; không có retention tài sản/giấy tờ đã duyệt; đa ngôn ngữ chưa chốt publication gate. Nhãn category 4/7/16 không đủ kết luận số ghế bao gồm tài xế. Không dùng Stitch để lấp các khoảng trống.

**Khuyến nghị: chưa chuyển sang tạo EF entity configuration/migration ở lượt này.** Có thể duyệt Catalog/Fleet lõi thành một slice riêng và hoãn pricing/buffer nếu chủ dự án chọn, nhưng phải xác nhận danh sách entity/cột được duyệt và không quảng bá slice đó là availability/pricing hoàn chỉnh. Không cần chốt OPEN-01/05/07 hoặc CR-01…04 để review catalog lõi; các module liên quan vẫn bị chặn riêng, không tự đóng OPEN.

## 14. Kế hoạch kiểm chứng sau phê duyệt — CHƯA CHẠY

| ID | Case cần kiểm SQL Server thật / application |
|---|---|
| CF-01 | Model list/selection không lộ UnitId/plate/VIN; inquiry không tạo hold/payment |
| CF-02 | Unique code/slug/asset/plate current/VIN, null/Unicode normalization, conflict concurrent inserts |
| CF-03 | Range/check/FK/restrict-delete, retired rows còn đọc được từ lịch sử; không cascade graph |
| CF-04 | Tối đa một primary image khi hai admin publish; blob cũ không overwrite/delete khi có reference |
| CF-05 | Spec/translation child đổi đồng thời publish: tăng root version, stale source/review bị từ chối |
| CF-06 | Rowversion conflict; mutation + status/audit append rollback cùng nhau |
| CF-07 | Hai interval version cùng owner, edge end=start, null end, timezone UTC, concurrent version number |
| CF-08 | Plate transfer/correction không overlap lịch sử và không đổi ảnh/biên bản cũ |
| CF-09 | Lost/recovered/inspection/maintenance không auto Ready; dispute tiền không buộc xe đã kiểm tra bị khóa |
| CF-10 | Pricing bounds >0/0..100/(0,100], snapshot không đổi sau version mới; chưa policy rounding không publish quote |
| CF-11 | Availability race maintenance/retire vs allocation và buffer — dependency module sau, AT-01/10/11 chưa chạy |
| CF-12 | Sáu locale/fallback/stale translation/review/admin override, public text không chứa PII/secret |

Phiên này chỉ review tĩnh tài liệu/ERD/links và phạm vi file. Không build/test runtime/SQL/migration; kết quả kiểm tài liệu sẽ ghi trong PROGRESS_LOG. Không báo các case trên PASS khi chưa triển khai.
