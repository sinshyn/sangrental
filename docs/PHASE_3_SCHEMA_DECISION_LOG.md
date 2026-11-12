# Phase 3 — Schema decision log

## Bổ sung 08/11/2026 — chuẩn hóa `Code`

Chủ dự án yêu cầu CHECK `[Code] = LOWER([Code])` và không chứa khoảng trắng, hoặc chốt chuẩn hóa ở application. Chọn CHECK ở SQL Server vì chưa có luồng ghi ứng dụng. Bốn entity dùng cột đúng tên `Code`: Category, Brand, Model, MaintenanceTaskTemplate. CHECK cũng chặn chuỗi rỗng và ASCII whitespace; unique BIN2 giữ nguyên. `AssetCode` là mã tài sản riêng và không áp quy tắc chữ thường. Đây là thay đổi migration review đã duyệt theo yêu cầu trực tiếp, **chưa áp dụng database**.

**DESIGN REVIEW — NOT APPROVED**  
Ngày: 08/11/2026. Revision 6. Chủ dự án đã trả lời Q1–Q38 về hành vi/phạm vi; **chưa phê duyệt toàn bộ schema vật lý**. Mã S-* chỉ là mã review schema, không phải BR mới hoặc thay OPEN nguồn. Các bảng revision cũ phía dưới là lịch sử và được thay thế ở những mục nêu sau.

## Revision 6 — Q37–Q38

| Câu | Trạng thái | Quyết định / tác động |
|---|---|---|
| Q37–Q38 | APPROVED — hành vi | ABS, dung tích động cơ và số ghế cần nguồn xác minh khi công khai; các thông số khác không bắt buộc nguồn. Người có quyền Lưu thay đổi được hiển thị ngay theo Q19 |

Mapping nguồn theo từng trường, độ dài cột và DB/application guard vẫn PROPOSED; không coi nguồn chung bắt buộc của Revision 1 là còn hiệu lực.

## Revision 5 — Q36

| Câu | Trạng thái | Quyết định / tác động |
|---|---|---|
| Q36 | APPROVED — hành vi | Admin tạo xe và chọn Ready: tạo `VehicleInspection` Passed, có actor/thời điểm, cùng transaction với kích hoạt hồ sơ và đổi trạng thái |

Chưa phê duyệt mã `InitialReadiness` hoặc toàn bộ enum inspection/status; đó là mapping PROPOSED. Không thêm entity hoặc triển khai workflow ở lượt review.

## Revision 4 — Q34–Q35

| Câu | Trạng thái | Quyết định / tác động |
|---|---|---|
| Q34 | APPROVED — hành vi | Xe vật lý có hồ sơ Nháp riêng; Chờ kiểm tra là hồ sơ đã lưu, không mặc nhiên được xóa hẳn |
| Q35 | APPROVED — hành vi | Admin hoàn tất hồ sơ, chọn Sẵn sàng khi tạo và xác nhận kiểm tra: Lưu chuyển hồ sơ sang Đang khai thác |

Mapping `RecordLifecycle Draft/Active/Retired` là PROPOSED; không coi `PendingInspection` là Nháp. Điều kiện xóa bản nháp Q33 chỉ áp dụng khi không có reference/history. Chưa tạo EF mapping hoặc migration.

## Revision 3 — Q29–Q33

| Câu | Trạng thái | Quyết định / tác động |
|---|---|---|
| Q29 | APPROVED — phạm vi | Migration đầu gồm Catalog/Fleet/checklist; hoãn giá và buffer. Lát đầu 16 entity đề xuất, chưa duyệt dictionary vật lý |
| Q30 | APPROVED — hành vi | Category phẳng cho xe số, tay ga, xe côn, ô tô 4/7/16 chỗ; mỗi category thuộc Motorcycle hoặc Car |
| Q31 | APPROVED — hành vi | Unit có thể Ready khi chưa có biển; registration optional |
| Q32 | APPROVED — hành vi | Có thể phân bổ tạm unit chưa biển; phải bổ sung trước bàn giao; không triển khai booking/handover ở lát schema này |
| Q33 | APPROVED — hành vi | Admin được hard delete model/unit nháp khi chưa có bất kỳ dữ liệu liên quan nào; BR-28 vẫn bảo vệ bản ghi có lịch sử thuê |

**Blocker schema lát đầu:** cần chốt “nháp” của PhysicalVehicle là trạng thái nào trước khi EF mapping quy tắc Q33. Không được coi `PendingInspection` là nháp mặc nhiên. 16 entity, tên cột, index/check/audit/concurrency và cách bảo vệ hard delete vẫn PROPOSED. S-08/09/11 hoãn khỏi migration đầu, không được đánh dấu đã giải quyết. Câu Q28 về hiệu lực giá sẽ áp dụng khi thiết kế lát giá sau.

## Revision 2 — sổ câu trả lời và ranh giới phê duyệt

| Câu | Trạng thái | Quyết định đã ghi nhận |
|---|---|---|
| Q1–3 | APPROVED — hành vi | Mỗi cấu hình khác biệt là model riêng; màu chỉ nội bộ; Admin sửa gán nhầm model có lý do/audit, giữ snapshot lịch sử |
| Q4–5 | APPROVED — hành vi | Đổi biển giữ mã xe và lịch sử; biển không trùng trên toàn SangRental trong cùng quốc gia tại cùng thời điểm |
| Q6–7 | APPROVED — hành vi | VIN tùy chọn cả khi khai thác; trùng VIN chỉ cảnh báo, Admin override phải có lý do |
| Q8–10 | APPROVED — hành vi | Admin có thể cho Ready ngay khi tạo cùng xác nhận kiểm tra; sau bảo trì Admin/quản lý được quyền mở; hết giờ dự kiến chưa xác nhận vẫn khóa và cảnh báo |
| Q11–12 | APPROVED — hành vi | Gộp việc trong một đợt bảo trì; checklist chọn việc mẫu hoặc việc riêng, từng mục có trạng thái và ghi chú |
| Q13–16 | APPROVED — hành vi | Cho bỏ qua mục checklist, lý do tùy chọn; không cho hai đợt cùng xe trùng thời gian; kết thúc sớm khi đủ checklist và kiểm tra đạt; bảo trì xung đột đơn vẫn được ghi và cảnh báo, không tự hủy đơn |
| Q17–19 | APPROVED — hành vi | Xe ngừng khai thác chỉ Admin khôi phục sau kiểm tra đạt; Admin/quản lý có quyền duyệt thông số công khai; người có quyền sửa và Lưu thì thông số mới hiện ngay, cần lịch sử/audit |
| Q20–22 | APPROVED — hành vi | Model Nháp/Công khai/Ẩn, mặc định Nháp; thiếu giá vẫn công khai để Liên hệ nhưng không Đặt ngay; cần ít nhất một ảnh hợp lệ có quyền sử dụng trước công khai |
| Q23–24 | APPROVED — hành vi | Công khai khi đủ tiếng Việt, thêm các locale sau; thiếu bản dịch thì fallback tiếng Việt |
| Q25 sửa, Q26 mới | APPROVED — hành vi | Không có xe Ready hoặc không còn xe trống trong khoảng khách chọn: chỉ Liên hệ, không Đặt ngay. Câu Q25 cũ và Q26 cũ đã hủy |
| Q27 | APPROVED — hành vi | ScheduledEnd bảo trì tùy chọn; thiếu end không tự giải phóng xe |
| Q28 | APPROVED — hành vi | Thay giá có hiệu lực ngay khi Lưu, giữ phiên bản giá cũ |

**S-04/06/07/10 đã được trả lời một phần đáng kể**, nhưng các mapping vật lý vẫn PROPOSED. S-06 phải bỏ VIN unique; S-07 phải thay MaintenanceWindow bằng MaintenanceEpisode + MaintenanceTaskTemplate + MaintenanceChecklistItem; S-08/09 phải ghi thay giá có hiệu lực ngay. Xem [proposal Revision 2](PHASE_3_CATALOG_FLEET_SCHEMA_PROPOSAL.md) và [ERD Revision 2](PHASE_3_CATALOG_FLEET_ERD.md). Không đưa những constraint Revision 1 đã bị thay thế vào migration.

### Các quyết định còn cần trước EF mapping/migration của từng lát

| Nhóm | Trạng thái | Tác động |
|---|---|---|
| S-01/S-02 | PROPOSED | Kiểu key, audit actor đáng tin, quyền ghi audit và cấu trúc correction; chưa có identity thực, không fake Admin/User FK |
| S-03/S-05 | PROPOSED | Cột tối thiểu của model/spec/ảnh, kiểm chứng quyền sử dụng ảnh, xử lý sửa thông số đã công khai và dữ liệu lịch sử |
| S-06 | PROPOSED | Chuẩn hóa biển/quốc gia, trường hợp xe chưa biển, đóng khoảng lịch sử và xử lý trùng VIN dưới cạnh tranh; quyết định hành vi Q4–7 đã có |
| S-07 | PROPOSED | Mã operational status, trạng thái checklist, guard kết thúc/hủy bảo trì và inspection khi cho Ready; quyết định hành vi Q8–17/Q27 đã có |
| S-08/S-09 | OPEN cho pricing engine | Phạm vi cột giá ngày/tuần/tháng, discount composition, rounding, version giá với policy khác, thời điểm quote/hold; Q28 chỉ chốt hiệu lực giá ngay khi lưu. Có thể hoãn các bảng pricing khỏi migration lõi |
| S-10 | PROPOSED | Fallback tiếng Việt và public gate đã chốt; slug/redirect/revision khi đổi nội dung và metadata dịch vẫn cần review kỹ thuật |
| S-11 | OPEN cho availability | Buffer trước/sau, thời điểm chọn policy, transaction chống overlap với booking/maintenance; không suy từ Q25/26 rằng đã có thuật toán availability |
| S-12/S-13 | OPEN về phạm vi triển khai | Giữ/xóa bản nháp chưa có lịch sử và chọn migration lõi hay cả pricing/buffer; không áp dụng DB trước SQL review và quyết định phạm vi |

**Gate hiện tại:** cập nhật thiết kế cho review; chưa tạo entity/configuration/migration, chưa chạy database update. Đề xuất lát đầu chỉ Catalog/Fleet và checklist; hoãn pricing/buffer còn OPEN. Sau khi duyệt dictionary lát đó: tạo EF mappings, build/test, sinh migration và SQL để review. Việc áp dụng SQL Server là quyết định riêng sau khi xem SQL, không mặc định từ việc duyệt schema.

Đọc cùng [proposal](PHASE_3_CATALOG_FLEET_SCHEMA_PROPOSAL.md) và [ERD](PHASE_3_CATALOG_FLEET_ERD.md). Chưa tạo code/configuration/migration/schema. Chủ trì review nghiệp vụ là chủ dự án; DBA/kỹ thuật chịu trách nhiệm tính đúng constraint và concurrency; pháp lý chỉ cần ở phạm vi pháp lý thực sự liên quan.

## A. Quy tắc nguồn cần giữ — APPROVED về hành vi

| ID review | Quy tắc đã có căn cứ | Nguồn / tác động |
|---|---|---|
| A-01 | Khách chọn model, backend phân bổ xe/biển số nội bộ | Master §3.1, đặc tả §1/§3.2; không public unit selection |
| A-02 | Inquiry không giữ xe/thu tiền; hold booking 15 phút theo model | Master §3.1; không cột hold/payment trong catalog |
| A-03 | Chỉ confirm sau payment được backend xác minh và allocation thành công | Master §3.1/§4; catalog/schema không phát success giả |
| A-04 | Bảo vệ overlap, buffer, maintenance, xe khóa/chưa kiểm tra | Master §3.1/§4; rowversion/unique thường không đủ cho interval |
| A-05 | Không hard delete model/unit có lịch sử thuê | BR-28; giữ FK/history, retire |
| A-06 | Mất/tai nạn khóa xe, không tự hủy đơn tương lai; tìm lại cần kiểm tra và Admin mở | BR-27/33/37; không Ready theo thời gian hoặc tự recover |
| A-07 | Hoàn tất sử dụng/xe kiểm tra đạt không bị phụ thuộc tài chính đang tranh chấp | BR-30, đặc tả §3.2; tách usage/finance |
| A-08 | Giá >0, giảm 0..100, trả trước >0..100% tiền thuê sau giảm; không security deposit riêng | Master §3.1/đặc tả §3.1; không seed tiền/tỷ lệ không duyệt |
| A-09 | Snapshot/giao dịch chốt không sửa âm thầm; adjustment riêng | Master §3.1/BR-32/38; catalog version không thay ledger |
| A-10 | Sáu locale, VI nguồn, fallback/cache/admin override; thông số/ảnh/service claims được xác minh | Master §2, đặc tả §2, quy chuẩn UI/UX §5; không bịa dịch/ảnh/score/giá |

Các tên bảng, enum minh họa trong đặc tả §5/§7 vẫn là đề xuất kỹ thuật. Không gán mapping giả BR-01…23. Phạm vi loại xe nguồn gồm xe số/tay ga/xe côn và ô tô 4/7/16 chỗ; thiết kế danh mục thể hiện yêu cầu đó, không seed dữ liệu thật.

## B. Các lựa chọn cần review — PROPOSED

| ID / chủ trì | Đề xuất cụ thể | Lý do / phương án khác | Ảnh hưởng nếu chưa duyệt |
|---|---|---|---|
| S-01 / kỹ thuật + chủ dự án | Schema sangrental, bigint identity roots, composite translation PK, shared-PK spec, Unicode text, bounded codes/BIN2, precision explicit; toàn bộ dictionary/constraints/index proposal | Join gọn, không GUID phân tán khi chưa cần; GUID hoặc surrogate translation Id là lựa chọn khác, chưa có yêu cầu bắt buộc | Chưa tạo EF mappings/migration từ dictionary |
| S-02 / chủ dự án + kỹ thuật | ActorReference opaque, audit fields M/A; CatalogFleetAuditEvent append-only cùng transaction, không User FK giả | Identity chưa chốt; không hard-code actor/email/System để giả dữ liệu vận hành đã duyệt | Chặn write/admin thực; có thể cấu hình schema actor sau duyệt nhưng không fake auth |
| S-03 / chủ dự án | Category phẳng; Motorcycle/Car + category Code; lifecycle Active/Retired tách Draft/Published/Hidden; IsFeatured không ảnh hưởng inventory | Tránh trạng thái tổng hợp; hierarchy/collection chỉ thêm khi thật cần | Chưa chốt publication gate hoặc category enum để migrate |
| S-04 / chủ dự án + vận hành | VehicleBrand riêng; mỗi cấu hình có thể cho thuê đồng nhất là VehicleModel; EditionLabel/ModelYear tùy chọn, chưa VehicleVariant/ModelFamily/Manufacturer | ABS/động cơ/giá khác không nên bị coi interchangeable chỉ vì chung tên thương mại. Nếu chọn Variant, phải chỉ rõ khách chọn entity nào và bảo toàn model invariant | **Blocker catalog/fleet identity** trước entity configuration; cần quyết định model-level fungibility |
| S-05 / chủ dự án + vận hành | Typed spec 1–0..1 và verification; model gallery + translation/provenance, immutable asset key; chưa bảng ảnh unit/evidence | Có kiểu/check/filter tốt hơn EAV tự do; EAV extensibility có thể thêm sau nếu chứng minh nhu cầu | Chốt trường/đơn vị/ảnh tối thiểu; không public dữ liệu chưa verified |
| S-06 / chủ dự án + vận hành + kỹ thuật | AssetCode lifetime unique; VIN optional normalized unique; VehicleRegistration theo thời gian, unique current jurisdiction+plate và current/unit | Không khóa plate global lifetime khi có thể đổi/tái cấp; plate-in-unit đơn giản hơn nhưng mất lịch sử | **Blocker fleet keys/history**: scope, normalization và trường hợp tái cấp/đổi biển cần xác nhận |
| S-07 / chủ dự án + vận hành | Tách ServiceLifecycle/OperationalStatus khỏi usage/allocation; PendingInspection default; status history/inspection/maintenance; release không tự Ready | Nguồn chỉ minh họa enum; không dùng số đơn/finance làm operational state. Chưa checklist/chính sách reactivation | **Blocker operational mappings**; phải chốt status codes và inspection/release guards, không tự thiết kế lại booking |
| S-08 / chủ dự án + kỹ thuật | Model rate/discount/rental và category buffer có version/effective interval; published payload bất biến, đóng end một lần có audit; không cascade delete | Giá scalar trên model đơn giản nhưng dễ mất policy history; SQL temporal chưa được chọn vì không thay semantic version/snapshot | Chặn pricing/buffer migration nếu version lifecycle/overlap protocol chưa review |
| S-09 / chủ dự án | decimal(19,2), percent(5,2), VND scope đầu; DailyRate required, weekly/monthly optional; online + duration tier lưu riêng; PrepaymentPercent duy nhất | Không float/money, không bool full-payment mâu thuẫn percent. VND integer/đa currency/phí thuế/quote units cần chốt | **Chưa có pricing engine**. Có thể hoãn toàn bộ pricing slice nếu duyệt lõi trước |
| S-10 / chủ dự án + nội dung | Translation child theo sáu locale, revision/review/stale handling, localized unique slug; brand/trade identity không tự dịch | Tránh NameVi/NameEn hoặc auto publish dịch; per-field và per-document fallback có trade-off | Chốt gate public/fallback/slug redirect; chưa claim toàn website i18n hoàn chỉnh |
| S-11 / chủ dự án + vận hành + kỹ thuật | CategoryBufferPolicyVersion, số phút >=0, không default 0; áp policy theo version/instant rõ ràng ở module availability tương lai | Dữ liệu buffer cần tách scheduled end/actual ready, không double-count inspection | **Chưa chốt availability contract**: trước/sau/both, policy change và maintenance blocking phải review |
| S-12 / chủ dự án + kỹ thuật | Retire thay hard delete cho cả draft; NO ACTION mọi FK; history/images/version không ghi đè; application role không xóa audit | Rộng hơn tối thiểu BR-28 (chỉ bắt buộc với lịch sử thuê); soft-delete boolean đơn lẻ dễ mất visibility lịch sử | Chốt mức giữ draft/correction, operational audit enforcement/cleanup asset; không tự đặt retention pháp lý |
| S-13 / chủ dự án | Chọn phạm vi implementation slice sau review: core catalog/fleet hay cả 19 entity | Proposal đầy đủ không có nghĩa cần migrate toàn bộ cùng một lần. Có thể hoãn price/buffer/audit integration chưa đủ quyết định | Chưa được chuyển sang entity/migration hoặc Phase 4 khi chưa có phạm vi đã duyệt |

Đề xuất kỹ thuật có chủ đích khác ví dụ nguồn: không dùng trạng thái Rented/Handover trong OperationalStatus, mà để usage/allocation tương lai thể hiện. Đây **không phải thay đổi quyết định đã duyệt** vì enum nguồn còn ĐỀ XUẤT; vẫn phải được S-07 duyệt trước khi code. Nếu chủ dự án muốn giữ enum nguồn, chỉnh proposal với ranh giới trạng thái rõ ràng, không âm thầm chọn.

## C. Câu hỏi còn OPEN trong review mới

Những mã Q-* dưới đây chỉ ghi nhận khoảng trống để review; không thay OPEN_QUESTIONS nguồn hoặc tự tạo quyết định nghiệp vụ mới.

| ID | Câu hỏi cần chốt | Khuyến nghị để review / không coi là quyết định |
|---|---|---|
| Q-01 → S-04 | “Mẫu xe” có phân biệt trim/ABS/dung tích/đời/động cơ không? Màu có được khách chọn? Unit có được chuyển model khi chỉnh dữ liệu? | Giữ một rentable model/cấu hình đồng nhất; sửa căn cước model/unit có lịch sử phải correction riêng hoặc model mới; chưa Variant table |
| Q-02 → S-03/05 | Category 4/7/16 có bao gồm tài xế? Xe có tài/tự lái có phải offering khác? Hãng có localized name/logo không? | Chưa dùng category label làm CHECK SeatCount; chưa tạo Chauffeur/Driver business |
| Q-03 → S-06 | Plate unique trong một cửa hàng, quốc gia hay toàn doanh nghiệp? Tái cấp/chuyển biển, xe chưa biển, VIN xe máy chuẩn hóa thế nào? | Scope jurisdiction + current-only là phương án review; không tự chọn global lifetime uniqueness |
| Q-04 → S-07 | Checklist/loại kiểm tra, thời hạn và role được kiểm? Maintenance overlap có được phép? Một inspection được release nhiều window hay chỉ một? Guard reactivation/retire undo? | Bám BR-37 cần Admin sau kiểm tra; chưa tự thêm lịch bảo dưỡng/hạn đăng kiểm hoặc quyền mới |
| Q-05 → S-09 | Đơn vị ngày/tuần/tháng; discount tier selection; cộng dồn/cộng phần trăm hay nối tiếp; VAT/phí; rounding bước nào/bao nhiêu? Có đa currency không? | Không auto quote/publish pricing dùng thật; 3/7/15/30 và online default 10 là phạm vi nguồn, không đủ công thức |
| Q-06 → S-08/11 | Effective policy tính theo giờ quote, giờ tạo hold hay lịch thuê? Booking đã giữ xử lý buffer/price version thay đổi ra sao? | Snapshot contract với booking ở pha sau; không thay booking transitions, không reprice giao dịch cũ |
| Q-07 → S-10 | Publish cần đủ 6 locale hay vi trước? Fallback theo field/document? Slug đổi có redirect và uniqueness lịch sử không? | vi reviewed/current làm baseline đề xuất; không tự giữ link SEO bằng xóa lịch sử |
| Q-08 → S-02/12 | Trusted actor identity, immutable audit permissions, correction records, retention asset/audit/draft thế nào? | Không fake FK User/auth; không đặt retention giấy tờ hay tự xóa file có reference |

## D. OPEN nguồn giữ nguyên

| Nhóm | Trạng thái / phạm vi bị chặn |
|---|---|
| OPEN-01 | SePay/bank/trial/webhook/cost chưa xác minh; không liên quan để duyệt catalog lõi, vẫn chặn live payment |
| OPEN-05 | Tỷ lệ/điều kiện hoàn, phí/bồi thường chưa duyệt; không RefundRate trong catalog |
| OPEN-07 | Giấy tờ/retention/cơ sở pháp lý/chữ ký/khách nước ngoài/dữ liệu xuyên biên giới; không đưa identity document vào catalog |
| CR-01…04 | Offline approval verification, thiếu tiền thực nhận, thứ tự thay xe/hủy BR-46, refund retry evidence chưa chốt; không giải quyết bằng status fleet |
| OPS | Dữ liệu xe/hãng/giá/ảnh/license/SLA/review/domain thực phải được xác minh; không seed minh họa như thật |
| Identity/production hạ tầng | Provider/provisioning/ít quyền/cert/test DB riêng còn cần xử lý; Phase 2 connection local thành công không giải quyết production readiness |

Không yêu cầu đóng OPEN pháp lý/thanh toán không liên quan chỉ để review Catalog/Fleet. Cần phân tách blocker của slice thay vì giả rằng mọi OPEN phải được phê duyệt trước mọi công việc.

## E. Điều kiện chuyển bước

**Khuyến nghị hiện tại: CHƯA tạo EF entity configuration hoặc migration.** Thiết kế còn DESIGN REVIEW — NOT APPROVED; lượt hiện tại chỉ tài liệu theo yêu cầu.

Sau khi có yêu cầu tiếp theo và phê duyệt cụ thể:

1. Ghi rõ các S-* được chọn, entity/cột/status/key nào được duyệt; giải Q-01/03/04 cho fleet lõi. Nếu hoãn giá/buffer, đánh dấu không nằm trong migration đầu, không để các cột chưa chốt vẫn xuất hiện.
2. Review dictionary + ERD cùng nhau, kiểm chung SQL Server types/check/index và mức enforce DB/application. Chốt policy chống overlap cho version và protocol phối hợp fleet mutations với inventory tương lai; không nhận rowversion là giải pháp đủ.
3. Thiết kế test DB riêng và kế hoạch CF-01…12; identity/audit integration phải có cách dùng trusted actor, không default account/actor giả.
4. Ở lượt triển khai được yêu cầu riêng: tạo entity/configuration trước, build/test model, sinh SQL migration để review, rồi mới xem xét apply theo phạm vi đã cho phép. Chưa thực hiện bất kỳ bước nào trong phiên thiết kế này.

Chưa có phê duyệt mới; không tự chuyển Phase 4. Báo cáo Phase 2 và cấu hình hoạt động được giữ nguyên.
