# SANG RENTAL — BỘ ĐẶC TẢ NGHIỆP VỤ & KỸ THUẬT v2.7

**Trạng thái:** REVIEW DRAFT — chưa phê duyệt tổng thể. **Mục đích:** tài liệu nguồn cho Master Prompt Codex v2.7, không phải lệnh triển khai. **Cơ sở:** `landingpage-sang rental.docx` và các quyết định được người dùng xác nhận trong chuỗi rà soát v1.x–v2.7. **Quy tắc ưu tiên:** quyết định phê duyệt sau cùng > quyết định trước > đề xuất kỹ thuật > yêu cầu gốc khi mâu thuẫn. Không suy diễn chính sách chưa duyệt.

## 1. Phạm vi, vai trò và nguyên tắc

**Sản phẩm:** website thuê xe tại Đà Nẵng gồm website công khai, quy trình đặt xe, quản lý thuê và trang quản trị. Khách chọn **mẫu xe**; hệ thống phân bổ **xe vật lý/biển số** nội bộ. Hai nút trên card: **Liên hệ** chỉ tạo yêu cầu tư vấn, không thu tiền/giữ xe; **Đặt ngay** tạo quy trình giữ chỗ/thanh toán. **Không có tiền thế chân bảo đảm riêng**: khoản trả trước là phần của tiền thuê.

**Vai trò:** khách, nhân viên giao nhận, quản lý (quyền chi tiết được cấp), Admin; hệ thống tự động xử lý webhook/hàng đợi. Phân quyền luôn xác minh tại backend, lưu audit. Các thao tác nhạy cảm phải lưu actor, thời điểm, lý do, bằng chứng, phiên bản dữ liệu.

**Stack đã chọn:** Next.js + React + TypeScript (public), Blazor (.NET admin), ASP.NET Core Web API C# + Clean Architecture + EF Core, SQL Server; SePay (cần kiểm thử), Brevo email, Google Cloud Translation (cache/override). Ưu tiên .NET 10 khi môi trường tương thích. Phân tách domain/application/infrastructure/API, DI/OOP/SOLID.

## 2. Yêu cầu website công khai (nguồn Word)

- Danh mục xe máy: xe số, tay ga, xe côn; ô tô 4/7/16 chỗ. Card xe, ảnh, bảng giá, mô tả, liên hệ/đặt ngay, form thay đổi theo loại xe, ngày giờ và địa điểm giao/trả.
- Landing: giới thiệu, ưu đãi thuê 3/7/15/30 ngày, ưu đãi online mặc định 10% cấu hình được, Google Maps và **5 đánh giá thật** nếu có nguồn xác minh; không tự tạo đánh giá; FAQ, bài viết/điểm du lịch địa phương, thông tin cửa hàng.
- Dịch vụ dự kiến: xe sạch và được kiểm tra phanh/lốp/đèn/còi, mũ bảo hiểm và lót mũ, 1–2 lít xăng, giá điện thoại, baga, áo mưa, sạc USB nếu có, giao sân bay/khách sạn/ga/bến xe theo khu vực miễn phí/phụ thu, hỗ trợ đường bộ, tư vấn địa phương, ghi hình bàn giao, lưu hành lý và trả trễ linh hoạt. **Chỉ công bố cam kết nếu doanh nghiệp xác nhận khả năng cung cấp thực tế.**
- Mobile: thanh liên hệ cố định dưới cùng (Gọi/Zalo/Đặt xe); desktop: các nút dọc góc dưới phải, hover label.
- Đa ngôn ngữ: VI là nguồn; EN,TH,KO, ZH, JA; dịch bằng Google Cloud Translation, cache DB, retry/fallback, admin override, không dịch nhầm ID/giá hoặc văn bản pháp lý cần duyệt.
- Chỉ số phù hợp tuyến đường/hành lý/tiêu thụ/vận hành: tính từ thông số đã xác minh và quy tắc minh bạch, admin duyệt trước khi công bố; không tự bịa điểm an toàn.
- SEO, responsive, accessibility, tốc độ tải, hình tối ưu, trạng thái loading/error/empty, bảo vệ dữ liệu cá nhân.

## 3. Quy tắc nghiệp vụ đã phê duyệt trước BR-24 (nhóm tổng hợp)

**Lưu ý truy vết:** bản ghi hiện có chỉ cho phép xác nhận nội dung nhóm quy tắc trước BR-24, không có mapping đầy đủ đáng tin cậy cho từng mã BR-01…BR-23; **không gán lại số mã giả**.

### 3.1. Đặt xe, giá, khuyến mãi

- Inquiry không giữ chỗ/thu tiền. Booking tạo hold **15 phút theo mẫu xe**; xác nhận sau khi backend xác minh khoản thanh toán online bắt buộc và phân bổ xe vật lý thành công; không xác nhận chỉ dựa vào frontend redirect, ảnh chuyển tiền hay email.
- Theo từng mẫu xe, Admin chọn trả trước **100%** hoặc **% tiền thuê sau giảm** (>0 đến 100%). Phần còn lại trả lúc bàn giao bằng tiền mặt hoặc chuyển khoản. Không có khoản thế chân bảo đảm độc lập.
- Giảm theo thời lượng và giảm online 10% mặc định được cấu hình; được áp dụng kết hợp. Giá thuê >0, phí >=0, giảm 0–100%, tổng >=0, giờ trả > giờ nhận; snapshot giá, quy tắc, phụ phí, điều khoản khi xác nhận. **Thứ tự làm tròn và thứ tự áp giảm cần định nghĩa nhất quán trước production, không tự áp chính sách tài chính trái ý chủ sở hữu.**
- Hold hết hạn và webhook đến trễ không tự xác nhận. Sai/thừa/thiếu/trùng tiền, thanh toán nhưng hết xe => đối soát/nhân sự xử lý. Phí giao nhận và trả trễ cấu hình theo vùng/loại xe; chưa chốt mức phí thực tế.

### 3.2. Xe, bàn giao, trả xe, gia hạn

- Khách không chọn biển số; backend phân bổ xe vật lý nội bộ; bảo vệ đồng thời, lịch tương lai, bảo trì, thời gian đệm cấu hình theo loại xe; xe chỉ được cho thuê lại khi đã nhận, kiểm tra và đạt điều kiện. Lưu thời điểm trả dự kiến, nhận thực tế, sẵn sàng thực tế riêng.
- Có thể đặt và thanh toán trước khi upload CCCD/GPLX, nhưng **không bàn giao khi chưa kiểm tra giấy tờ hợp lệ**. Khách có thể xuất trình lúc nhận. Giấy tờ riêng tư/mã hóa/phân quyền/audit; không tự ấn định thời hạn lưu.
- Biên bản giao/nhận và trả xe kèm ảnh/video, hai bên xác nhận bằng chữ ký trực tiếp hoặc OTP; OTP không nhận được thì dùng chữ ký. Không giả mạo xác nhận, không sửa âm thầm bằng chứng đã ký.
- Gia hạn chỉ khi không xung đột lịch, hold 15 phút, **thanh toán thành công trước khi có hiệu lực**. Đổi địa điểm trả: Admin duyệt và phụ phí nếu có. No-show: nhân viên liên hệ và xử lý thủ công, không tự phạt/hủy.
- Trước bàn giao xe hỏng/thiếu mẫu: đề xuất mẫu tương đương, **Admin và khách cùng đồng ý**. Mẫu đắt hơn: Admin miễn chênh hoặc yêu cầu thanh toán thêm, lưu lý do. Mẫu rẻ hơn: xử lý chênh minh bạch, không bịa tỷ lệ hoàn.
- Hỏng giữa chuyến: ưu tiên đổi xe tương đương, biên bản thu hồi xe cũ và giao xe mới; giữ toàn bộ lịch sử. Nếu không do lỗi khách mà không có xe thay: Admin liên hệ thương lượng lịch/giải pháp, không tự động hứa hoàn tiền.
- Hủy trước bàn giao giải phóng lịch phân bổ độc lập với hoàn tiền. Trả sớm sau bàn giao phải có nhận xe/kiểm tra/quyết toán. Tranh chấp tài chính không ngăn cho thuê lại xe đã kiểm tra đạt điều kiện.

## 4. Sổ quyết định đã chốt BR-24 đến BR-47

| ID | Nội dung phê duyệt |
|---|---|
| BR-24 | Chỉ quản lý/Admin xác nhận đã thu tiền mặt; nhân viên không được tự xác nhận. |
| BR-25 | Admin và quản lý được cấp quyền duyệt hoàn tiền thủ công, lưu lý do/bằng chứng. |
| BR-26 | Trả xe trễ ảnh hưởng khách sau: cảnh báo Admin, ưu tiên tìm xe thay thế và liên hệ khách sau. |
| BR-27 | Mất xe/tai nạn nghiêm trọng: khóa lịch khả dụng, lập hồ sơ sự cố, Admin xử lý thủ công. |
| BR-28 | Không xóa vĩnh viễn mẫu xe/xe vật lý đã phát sinh đơn; chỉ ngừng khai thác, giữ lịch sử. |
| BR-29 | Nhân viên giao tại sân bay gửi yêu cầu thu tiền mặt; quản lý/Admin xác nhận từ xa **trước** giao. |
| BR-30 | Được hoàn tất **phần sử dụng xe** khi còn tranh chấp phụ phí; quyết toán vẫn tranh chấp. |
| BR-31 | Quá hạn không liên hệ được: tự cảnh báo Admin, đánh dấu quá hạn; nhân viên liên hệ và xử lý thủ công. |
| BR-32 | Admin điều chỉnh tiền thuê/phụ phí bằng bản ghi điều chỉnh riêng, lý do/audit; không sửa giao dịch gốc. |
| BR-33 | Mất xe/tai nạn: khóa, cảnh báo, ưu tiên xe thay; **không tự hủy** đơn tương lai chỉ vì sự cố. |
| BR-34 | Đã nhận tiền nhưng không xác nhận được xe: không tự confirm booking; ghi tiền, cảnh báo Admin bố trí xe/hoàn. |
| BR-35 | Quản lý từ chối xác nhận tiền mặt: ghi lý do, nhân viên kiểm tra lại; chưa giao xe. |
| BR-36 | Khách từ chối ký biên bản trả: lưu ảnh/video/bằng chứng, quản lý xử lý; không giả lập xác nhận. |
| BR-37 | Xe báo mất được tìm lại vẫn khóa cho đến khi kiểm tra kỹ thuật và Admin cho phép khai thác. |
| BR-38 | Admin tăng tiền thuê/phụ phí: adjustment riêng, thông báo khách, chỉ thu theo căn cứ/quy trình phù hợp. |
| BR-39 | Cho phép bàn giao **ngoại tuyến đặc biệt** nếu quản lý/Admin phê duyệt, đồng bộ sau. |
| BR-40 | Admin hoặc quản lý được cấp quyền thực hiện chuyển hoàn, phải lưu bằng chứng. |
| BR-41 | Người duyệt và người chuyển hoàn **không bắt buộc** khác nhau; vẫn phải có nhật ký/bằng chứng. |
| BR-42 | Đã giao thực tế nhưng hệ thống chưa cập nhật: khóa thao tác xung đột, tạo sự cố đối soát, quản lý xác minh cập nhật. |
| BR-43 | Chuyển hoàn thất bại: giữ trạng thái thất bại/chờ xử lý, thử lại có kiểm soát. |
| BR-44 | Mất Internet hoàn toàn: quản lý/Admin phê duyệt bàn giao qua **cuộc gọi xác minh**, bắt buộc bổ sung xác nhận và nhật ký trên hệ thống. |
| BR-45 | Tiền còn lại chuyển khoản nhưng không có Internet đối soát: **Admin** được duyệt ngoại lệ sau khi kiểm tra **chứng từ ngân hàng độc lập**. |
| BR-46 | Khi đồng bộ bàn giao ngoại tuyến phát hiện xung đột phân bổ: **ưu tiên đơn đã giao xe thực tế và tự hủy đơn còn lại**. |
| BR-47 | Lệnh hoàn tiền chưa rõ kết quả ngân hàng: **bắt buộc đối soát trước khi thử lại**. |

**Phạm vi BR-46:** ngoại lệ riêng cho xung đột đã được xác minh từ bàn giao ngoại tuyến; không thay BR-33 cho mọi sự cố xe. Trước khi tự hủy cần xác minh chính xác xe, thời gian và sự kiện giao thực tế; không tin mù quáng dữ liệu offline. Nếu đơn bị hủy đã thanh toán, phải giữ lịch sử tiền và mở xử lý hoàn tiền thủ công, không tự đánh dấu đã hoàn. Chi tiết thứ tự kiểm tra xe thay trước hay sau hủy chưa được phê duyệt; mặc định tôn trọng nghĩa đen BR-46 và ghi rõ rủi ro.

## 5. State machine — mô hình kỹ thuật ĐỀ XUẤT (chưa khóa tên enum)

### 5.1. Booking lifecycle

| Từ | Sự kiện/điều kiện (guard) | Đến | Hệ quả / từ chối |
|---|---|---|---|
| PendingPayment | Thanh toán online được xác minh + hold còn hiệu lực + allocate thành công | Confirmed | Snapshot, allocation, outbox thông báo |
| PendingPayment | Hold hết hạn | HoldExpired | Giải phóng giữ chỗ; tiền đến muộn sang reconciliation |
| PendingPayment | Tiền đã nhận nhưng không còn xe/allocate lỗi | PaymentAllocationException | Giữ payment, không confirm; báo Admin |
| Confirmed | Đến thời điểm chuẩn bị giao | AwaitingHandover | Kiểm tra giấy tờ, tiền đến hạn, xe |
| AwaitingHandover | Biên bản/giấy tờ/tiền hợp lệ hoặc ngoại lệ đã phê duyệt, có bàn giao thực tế | Active | Ghi actual handover; không giả payment confirmed |
| AwaitingHandover | Giao offline chưa đối soát | OfflineReconciliationPending (trạng thái phụ đề xuất) | Chặn thao tác xung đột, đợi xác minh |
| Active | Nhận xe thực tế | ReturnInspection | Ghi actual return; không đồng nghĩa rentable |
| ReturnInspection | Kiểm tra và biên bản hợp lệ hoặc hồ sơ ngoại lệ được xử lý | UsageCompleted | Quyết toán có thể vẫn disputed |
| Confirmed/AwaitingHandover | Hủy hợp lệ trước bàn giao | Cancelled | Giải phóng allocation ngay; hoàn tiền riêng |
| Active | Quá giờ trả | Active + Overdue flag | Cảnh báo; không tự xác nhận trả hoặc hủy đơn sau |

**Invariants:** `UsageCompleted` không ép `FinancialSettled`; `Overdue` không tự biến thành `Lost`; `PaymentExceptionApproved` không phải `PaymentConfirmed`. Các trạng thái cancellation/incident/return refusal phải có reason và evidence. `OfflineReconciliationPending` chỉ là đề xuất cách biểu diễn, có thể tách aggregate thay vì enum.

### 5.2. Payment/Refund/Vehicle/Extension

- **PaymentObligation:** Pending → PartiallyPaid/Settled; Exception/UnderReconciliation độc lập với booking. Tiền mặt: CashRequestPending → Approved/Rejected chỉ bởi quản lý/Admin. Chuyển khoản: backend đối soát webhook thực, ngoại lệ BR-45 lưu riêng và không giả nhận tiền.
- **Refund:** Requested → Approved/Rejected → TransferPending → Unknown/VerifiedFailed/Completed. `Unknown` **không được retry**. Một request có thể có nhiều attempts, nhưng tổng thực trả không vượt khoản duyệt; cùng người duyệt và thực hiện được phép theo BR-41.
- **PhysicalVehicle:** Ready, Handover, Rented, ReturnInspection, Maintenance, Suspended, Lost/AccidentLocked; trạng thái hiện tại **không thay thế** lịch allocation tương lai. Xe tìm lại phải được kiểm tra và Admin mở khóa.
- **Extension:** Requested → HoldActive (15 phút) → AwaitingPayment → Effective / Expired / Rejected. Không có hiệu lực nếu tiền chưa xác minh hoặc lịch bị xung đột.
- **Dispute:** Open → UnderReview → Resolved; xe đã được kiểm tra vẫn có thể ready khi tranh chấp còn mở.
- **OfflineSync:** Captured → Submitted → Verified/Conflict/Rejected; dữ liệu ngoại tuyến là input chưa tin cậy, không được tự ghi đè lịch server.

## 6. Ma trận phân quyền (phần không được phê duyệt cụ thể là ĐỀ XUẤT)

| Thao tác | Nhân viên | Quản lý được cấp quyền | Admin |
|---|---|---|---|
| Chuẩn bị giao, ảnh/video, lập biên bản | Có | Có | Có |
| Yêu cầu xác nhận tiền mặt | Có | Có | Có |
| Xác nhận thu tiền mặt | **Không** | Có | Có |
| Phê duyệt bàn giao ngoại tuyến | Không | Có | Có |
| Ngoại lệ chứng từ chuyển khoản BR-45 | Không | Không | **Có** |
| Phê duyệt hoàn tiền | Không | Có | Có |
| Thực hiện chuyển khoản hoàn tiền | Không | Có | Có |
| Xử lý sự cố mất xe/tai nạn nghiêm trọng | Hỗ trợ | Hỗ trợ | Quyết định |
| Xử lý tranh chấp phụ phí | Không tự quyết | Theo quyền | Có |
| Cấu hình giá, phân quyền, chính sách | Không | Theo quyền cụ thể (đề xuất) | Có |

Kiểm tra quyền **tại thời điểm xử lý** (kể cả khi quyền vừa bị thu hồi). Audit không được sửa. Cần giới hạn quyền theo cửa hàng/đơn được phân công nếu sau này có nhiều chi nhánh.

## 7. Database — mô hình logic đề xuất

| Aggregate | Bảng/thực thể dự kiến | Khóa và ràng buộc quan trọng |
|---|---|---|
| Catalog/Fleet | VehicleCategory, VehicleModel, PhysicalVehicle, VehicleSpecification, Inspection, MaintenanceWindow | Plate unique theo phạm vi kinh doanh; ngừng khai thác thay hard delete nếu có lịch sử |
| Pricing | PricingRule, DiscountRule, DeliveryZone, BookingPriceSnapshot | Money decimal chính xác, snapshot bất biến, version policy |
| Booking/Inventory | Booking, BookingHold, VehicleAllocation | Hold TTL, FK model/vehicle, khoảng `[start,end)`, chống overlap dưới transaction |
| Customers/Identity | Customer, IdentityDocumentReference, IdentityVerification | File private, encryption, audit, retention OPEN-07 |
| Payments | PaymentObligation, PaymentTransaction, PaymentReconciliation, CashCollectionRequest | Unique theo provider+transaction ID, amount/reference/account; không sửa confirmed transaction |
| Handover | HandoverRecord, ReturnRecord, EvidenceAsset, SignatureConfirmation | Evidence hash/version, actor, timestamps, không giả chữ ký |
| Offline | OfflineApproval, OfflineHandoverSync, SyncConflict | Event ID, approval reference, actual time, conflict status, idempotency |
| Operations | RentalExtension, VehicleReplacement, IncidentReport, Dispute | Giữ lịch sử xe cũ/mới, điều kiện gia hạn, chứng cứ |
| Refund/Adjustment | FinancialAdjustment, RefundRequest, RefundApproval, RefundAttempt | Mỗi attempt và bank reference, unknown blocks retry, tổng hoàn <= duyệt |
| Admin/Content | User, Role, Permission, AuditLog, NotificationOutbox, TranslationCache, CmsContent | RBAC, outbox retries, review provenance |

**Inventory invariant:** Không hai allocation có hiệu lực chồng lấn cùng xe vật lý; hold theo mẫu xe không vượt capacity. SQL Server unique index thông thường **không đủ** để chống overlap theo khoảng thời gian; cần transaction và khóa phù hợp/serializable hoặc chiến lược tương đương, cùng test cạnh tranh thật. `rowversion` hỗ trợ phát hiện ghi đè, không thay khóa lịch.

**Tiền:** Phân biệt tiền thuê trả trước, tiền còn lại, phí gia hạn, adjustment, refund, tranh chấp. Không coi tiền thuê trả trước là security deposit. Không ghi trùng khoản thu do webhook retry. Không cho refund retry khi ngân hàng chưa rõ kết quả. Quy tắc rounding/order áp dụng giảm phải được tài liệu hóa và kiểm thử trước go-live.

**Thời gian:** Lưu UTC, hiển thị Asia/Ho_Chi_Minh; tách scheduled return, actual received, ready-at; buffer theo loại xe, tránh cộng đôi công việc kiểm tra.

## 8. API — hợp đồng chức năng đề xuất, CHƯA khóa DTO/routes

| Resource gợi ý | Operations | Guard/role | Tests |
|---|---|---|---|
| `/api/catalog`, `/api/quotes` | List model, quote theo thời gian/địa điểm | Giá từ backend, snapshot khi booking | Giá âm, timezone, discount |
| `/api/inquiries` | Create inquiry | Không tạo hold/payment | Inquiry không chiếm xe |
| `/api/booking-holds`, `/api/bookings` | Hold, confirm, view, cancel | 15 phút, allocation + verified payment | AT-01,02,04 |
| `/api/payments/webhooks/sepay` | Receive/verify/reconcile | Xác thực theo provider thực, idempotent | AT-03 |
| `/api/cash-collections` | Request, approve/reject | Staff request; manager/Admin confirm | AT-05,06,07 |
| `/api/handovers`, `/api/returns` | Draft, evidence, sign, finalize | Giấy tờ, tiền, hai bên; ngoại lệ riêng | AT-12,13 |
| `/api/offline-approvals`, `/api/offline-sync` | Approve, submit, reconcile | BR-39,42,44,45,46; untrusted sync | AT-15,16,21,22,23,24 |
| `/api/vehicles`, `/api/allocations` | Manage, lock, assign, replace | Không overlap, không xe khóa | AT-01,10,11 |
| `/api/extensions` | Quote, hold, pay, confirm | Không trùng, 15 phút, paid | AT-08,09 |
| `/api/incidents`, `/api/disputes` | Report, review, resolve | Admin/manager role | AT-10,13 |
| `/api/adjustments`, `/api/refunds` | Adjust, request, approve, attempt, reconcile | Immutable ledger, permissions, no double refund | AT-14,17,18,19 |
| `/api/cms`, `/api/translations`, `/api/admin/dashboard` | Content, i18n, metrics | RBAC, real reviews only | Role/content tests |

**API conventions:** OpenAPI; DTO validation; ProblemDetails + stable error code; pagination; `Idempotency-Key` cho thao tác có side effect; `ETag`/`If-Match` hoặc concurrency token cho cập nhật phù hợp; backend permission checks; audit; trace/correlation ID; không lộ giấy tờ trong logs. Endpoint names là ví dụ, Codex phải khóa hợp đồng trong `API_CONTRACTS.md` trước implementation từng module.

## 9. Quy trình giao ngoại tuyến — chi tiết và rủi ro

1. Nhân viên tại điểm giao mất Internet, gọi **quản lý/Admin** theo kênh đã đăng ký để xác minh và xin phê duyệt (BR-44).
2. Kiểm tra giấy tờ, xe, chứng cứ thanh toán. Nếu chuyển khoản không đối soát được, **chỉ Admin** được duyệt ngoại lệ sau khi kiểm tra chứng từ ngân hàng **độc lập** (BR-45); không đánh dấu đã nhận tiền nếu chưa xác minh thực nhận.
3. Ghi thời điểm giao thực tế, biên bản, ảnh/video, xác nhận hai bên; lưu phê duyệt và bằng chứng, đồng bộ khi có Internet (BR-39).
4. Nếu backend chưa nhận sự kiện giao, khóa thao tác xung đột và tạo hồ sơ đối soát, quản lý xác minh (BR-42).
5. Nếu phát hiện hai allocation xung đột, chỉ áp dụng BR-46 **sau khi xác minh giao xe thực tế và đúng xe**, ưu tiên đơn đã giao và tự hủy đơn còn lại. Ghi audit, thông báo, khoản tiền cần xử lý; không tự giả hoàn tiền.

**CR-01 chưa chốt:** cách chứng minh cuộc gọi/phê duyệt thực sự từ người có quyền (mã một lần, ghi âm, callback, chữ ký hậu kiểm). **CR-02 chưa chốt:** khi Admin cho giao dựa chứng từ độc lập nhưng ngân hàng sau đó không ghi nhận tiền. **CR-03 chưa chốt:** kiểm tra xe thay trước/sau tự hủy BR-46. Để triển khai an toàn, yêu cầu hệ thống đánh dấu `REQUIRES_MANUAL_RECONCILIATION` ở điểm không thể chứng minh, không tự bịa một quyết định đã phê duyệt.

## 10. Quy trình hoàn tiền — chi tiết và rủi ro

- Yêu cầu hoàn tiền phải liên kết nghĩa vụ tài chính, căn cứ, lý do, người tạo. Chỉ Admin/quản lý có quyền duyệt (BR-25); Admin/quản lý có quyền thực hiện chuyển tiền và lưu chứng từ (BR-40). Có thể cùng một người duyệt và chuyển (BR-41).
- Tách `RefundRequest`, `RefundApproval`, `RefundAttempt`; duyệt không có nghĩa đã chuyển. Giao dịch gốc bất biến; refund và adjustment là bản ghi riêng.
- Mỗi attempt có mã tham chiếu, số tiền, thời điểm, actor, trạng thái, chứng từ. Nếu ngân hàng trả kết quả **chưa rõ** => không thử lại (BR-47). Chỉ thử lại có kiểm soát khi xác minh lần trước thất bại (BR-43). Không được đánh dấu hoàn tất khi mới upload ảnh chứng từ.
- **CR-04 chưa chốt:** loại bằng chứng ngân hàng đủ để xác nhận thất bại. **OPEN-05:** tỷ lệ và điều kiện hoàn tiền chi tiết chưa được phê duyệt; không tự tính mức hoàn theo chính sách tưởng tượng.

## 11. Acceptance tests — ma trận nền

| ID | Given / When | Then / DB assertion |
|---|---|---|
| AT-01 | Hai khách cùng giữ xe cuối | Không oversell/overlap allocation |
| AT-02 | Thanh toán sau hold hết hạn | Không auto-confirm; payment đối soát |
| AT-03 | Webhook cùng transaction gửi nhiều lần | Một transaction logical, một tác động ledger |
| AT-04 | Nhận tiền nhưng allocate thất bại | Không confirm booking; tiền giữ lịch sử, cảnh báo |
| AT-05 | Staff gọi API confirm cash | 403, không đổi payment |
| AT-06 | Manager/Admin confirm cash lặp | Một xác nhận hợp lệ |
| AT-07 | Cash request bị từ chối | Không cho bàn giao thường |
| AT-08 | Thanh toán gia hạn sau hold hết hạn | Không auto-extend |
| AT-09 | Gia hạn trùng booking sau | Không mở rộng allocation |
| AT-10 | Xe bị tai nạn/mất | Không phân bổ mới |
| AT-11 | Xe tìm lại chưa kiểm tra | Không tự ready |
| AT-12 | Khách từ chối ký trả | Evidence + ngoại lệ, không chữ ký giả |
| AT-13 | Đã trả và kiểm tra nhưng phụ phí disputed | UsageCompleted, finance Disputed |
| AT-14 | Admin tăng/giảm phí | Adjustment mới; transaction gốc bất biến |
| AT-15 | Sync offline event hai lần | Không hai lần handover |
| AT-16 | Offline handover xung đột được xác minh | BR-46, audit cancel, không giả refund |
| AT-17 | Refund attempt unknown | Retry bị chặn |
| AT-18 | Refund verified failed | Cho retry kiểm soát, không vượt approved |
| AT-19 | Hai người duyệt/chuyển refund đồng thời | Không double settlement |
| AT-20 | Brevo gửi thất bại | Payment/booking vẫn đúng; outbox retry |
| AT-21 | Cuộc gọi phê duyệt ngoại tuyến bị giả mạo | Không auto chấp nhận; chờ xác minh |
| AT-22 | Chứng từ chuyển khoản sai/chưa xác minh | Không giả webhook confirmed |
| AT-23 | Hai thiết bị cùng báo đã giao xe | Kiểm tra evidence, không hủy chỉ dựa input chưa tin |
| AT-24 | Đơn đã trả tiền bị hủy do BR-46 | Lưu tiền, thông báo, mở xử lý hoàn; không đánh dấu đã hoàn |

**Bổ sung bắt buộc khi viết test chi tiết:** mỗi case có preconditions, actor/permission, request, expected HTTP/ProblemDetails, DB state, outbox/audit, retry/concurrency/negative case. Test SQL Server thực (không chỉ in-memory), kiểm tra timestamp/buffer, rollback, crash/restart, role revoked, bảo vệ file CCCD, chính sách không xóa lịch sử.

## 12. Ma trận truy vết kiểm tra chéo

| Quy tắc | State/guard | DB | API | Test | Mức hoàn thiện |
|---|---|---|---|---|---|
| Hold 15 phút + paid booking | PendingPayment→Confirmed, TTL + capacity | BookingHold, Allocation, Payment | booking-holds, bookings, webhook | AT-01..04 | Đủ logic; chưa khóa DTO |
| BR-24/29/35 | CashPending→Approved/Rejected | CashCollectionRequest, AuditLog | cash-collections | AT-05..07 | Nhất quán |
| Biên bản hai bên | Handover/Return confirmation | HandoverRecord, Evidence, Signature | handovers, returns | AT-12 | Còn cơ sở pháp lý chữ ký |
| BR-30/32/38 | UsageCompleted + Dispute, adjustment | Dispute, FinancialAdjustment | disputes, adjustments | AT-13,14 | Nhất quán; căn cứ thu phí cần chính sách |
| BR-27/33/37 | Locked→Inspection→Ready | Incident, VehicleInspection | incidents, vehicles | AT-10,11 | Nhất quán |
| BR-39/42/44/45 | Offline pending/reconcile | OfflineApproval, SyncConflict | offline-approvals/sync | AT-15,21..23 | CR-01/02 mở |
| BR-46 | Verified conflict→cancel other booking | Allocation, Booking, Audit, Payment | offline-sync, bookings | AT-16,24 | CR-03 mở; hủy có rủi ro cao |
| BR-25/40/41/43/47 | Refund requested→approved→attempt→settled | RefundRequest, RefundAttempt | refunds | AT-17..19 | CR-04, OPEN-05 mở |
| Gia hạn paid-before-effective | Hold→Paid→Effective | RentalExtension, Allocation, Payment | extensions | AT-08,09 | Nhất quán nguyên tắc |
| Email Brevo | Outbox pending→sent/retry | NotificationOutbox | notifications worker | AT-20 | Chưa xác minh provider |

## 13. OPEN / chưa được phê duyệt — không tự bịa

| Mã | Vấn đề | Ảnh hưởng và hướng xử lý tạm |
|---|---|---|
| OPEN-01 | SePay trial, tương thích tài khoản MB cá nhân/TPBank, webhook thực, giá dịch vụ | Adapter/mock/sandbox; chưa bật tiền thật nếu chưa thử nghiệm |
| OPEN-05 | Tỷ lệ hoàn khi hủy/trả sớm, mốc thời gian, phụ phí thiệt hại, bồi thường | Chưa tự động tính/chi hoàn theo tỷ lệ không được duyệt; lưu policy placeholder |
| OPEN-07 | Thời hạn lưu CCCD/GPLX/passport, căn cứ pháp lý, dữ liệu xuyên biên giới, GPLX khách nước ngoài, hiệu lực xác nhận điện tử | Hạn chế truy cập, mã hóa/audit, chờ rà soát pháp lý; không bịa số ngày |
| CR-01 | Xác thực phê duyệt ngoại tuyến qua cuộc gọi | Không coi log do nhân viên tự khai là bằng chứng duy nhất |
| CR-02 | Hậu quả khi ngoại lệ chứng từ chuyển khoản không có tiền thực nhận | Ghi khoản cần đối soát; chuyển Admin xử lý, không giả settled |
| CR-03 | Xe thay trước/sau hủy tự động BR-46 | Tôn trọng BR-46; ghi rõ thứ tự chưa phê duyệt |
| CR-04 | Bằng chứng xác minh refund failed | Không retry khi chưa xác minh |
| OPS | Địa chỉ cửa hàng, số liên hệ/Zalo OA, Google Maps place ID, giá/fee thật, khu vực giao xe, email domain, review thật, phụ kiện, SLA cứu hộ, chính sách bảo hiểm | Cấu hình placeholder rõ ràng, không công bố thông tin bịa |

## 14. Yêu cầu phi chức năng, bảo mật, vận hành

- Admin auth bảo mật (HttpOnly/Secure cookie khi phù hợp, CSRF protection, RBAC/permissions), rate limiting, kiểm tra session/revocation, MFA khuyến nghị cho tác vụ nhạy cảm.
- SQL Server transaction/idempotency/optimistic concurrency + locking cho allocation; audit bất biến; transactional outbox và retry có backoff, dead-letter/alert; tracing/health checks.
- Ảnh giấy tờ private storage, signed short-lived access, mã hóa, phân quyền và audit; không log ảnh CCCD/GPLX, OTP, token, API keys. Chính sách retention phải được chốt trước go-live.
- Admin CMS: dashboard số xe/đơn/doanh thu; xe vật lý, lịch bảo trì, giá/khuyến mãi, giao nhận, tranh chấp, refund, nhân sự/role, thông báo, nội dung/SEO/translation, cấu hình.
- CI chạy lint/build/unit/integration/concurrency/E2E; migration/backup/restore, staging sandbox, checklist deploy và rollback. Không tuyên bố đã kiểm thử nếu chưa chạy.

## 15. Điều kiện khóa đặc tả và sẵn sàng production

**Đủ để bắt đầu triển khai có kiểm soát:** các quy tắc đã duyệt, các guard quan trọng, mô hình dữ liệu/API đề xuất, test matrix và OPEN được ghi nhận. Codex phải tạo hợp đồng chi tiết trước từng module và kiểm thử theo giai đoạn.

**Chưa đủ để tuyên bố production-ready:** OPEN-01/05/07, CR-01..04, dữ liệu vận hành thực, kiểm thử end-to-end thanh toán thật, rà soát pháp lý/bảo mật, chính sách phí và bồi thường. Các phần chưa chốt phải bị khóa hoặc dùng chế độ thử nghiệm; không suy diễn phê duyệt.

**Tài liệu phối hợp với Master Prompt:** `SangRental_Codex_Master_Prompt_v2_7.md`. Trong repository Codex cần duy trì `PROJECT_RULES.md`, `REQUIREMENTS.md`, `ARCHITECTURE.md`, `DECISIONS.md`, `OPEN_QUESTIONS.md`, `API_CONTRACTS.md`, `TEST_MATRIX.md`, `SECURITY_PRIVACY.md`, `TASK_BOARD.md`, `PROGRESS_LOG.md`, `DEPLOYMENT.md` và `README.md`.
