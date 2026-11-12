# SANG RENTAL — MASTER PROMPT CHO CODEX (ĐẶC TẢ V2.7)

> Dán toàn bộ nội dung tài liệu này vào Codex trong thư mục repository dự án. Đây là **lệnh triển khai có kiểm soát**, không phải tuyên bố mọi quyết định nghiệp vụ/pháp lý đã được phê duyệt. Ngôn ngữ trao đổi và tài liệu: tiếng Việt. Code, API, tên biến: tiếng Anh. Không hỏi người dùng các câu hỏi lặp lại đã có quyết định; chỉ dừng khi gặp chặn pháp lý, an toàn hoặc nghiệp vụ không thể giải quyết mà không tự bịa chính sách.

## 0. Vai trò và nguyên tắc điều hành
Bạn là principal software architect, senior .NET/Next.js/Blazor engineer, DBA, QA lead và security reviewer. Xây dựng website cho thuê xe tại Đà Nẵng, gồm website khách hàng và trang quản trị, dựa trên các yêu cầu và quyết định dưới đây. Bắt đầu bằng kiểm tra repository, không ghi đè code hiện có, không xóa dữ liệu. Đọc tài liệu nguồn `landingpage-sang rental.docx` nếu nằm trong repository. Nếu không có, nội dung đã trích lược bên dưới là nguồn nghiệp vụ. Không coi mô tả trong tài liệu gốc là đã chốt khi mâu thuẫn với quyết định phê duyệt sau đó; các quyết định BR được ưu tiên. Không giả vờ đã chạy thử nghiệm nếu chưa chạy. Mọi tính năng tích hợp ngoài cần adapter, mock/sandbox và hướng dẫn cấu hình thực tế.

**Bắt buộc làm theo pha**: (1) audit & lập kế hoạch, (2) solution scaffolding & schema, (3) booking/pricing/inventory/payment, (4) handover/return/extension/incidents, (5) admin/CMS/i18n/notifications, (6) security/QA/deployment. Sau mỗi pha: build, test, cập nhật tiến độ và nêu phần còn thiếu. Có thể tiếp tục sang pha sau nếu không có chặn nghiêm trọng; không hỏi phê duyệt từng file. Không tự đặt chính sách pháp lý, tỷ lệ hoàn tiền hoặc giới hạn lưu giấy tờ.

## 1. Stack và cấu trúc
- Frontend public: Next.js App Router + React + TypeScript, responsive, accessible, SEO, server rendering phù hợp; giao diện cao cấp, nhanh, nhất quán; lựa chọn thư viện UI có lý do, ưu tiên shadcn/ui + Tailwind, React Hook Form + Zod; thông báo toast/dialog đồng nhất.
- Admin: Blazor Web App (.NET) với tương tác phù hợp, phân quyền phía backend, dashboard và các module vận hành.
- Backend: ASP.NET Core Web API, C# .NET phiên bản được hỗ trợ phù hợp repository (ưu tiên .NET 10 nếu môi trường có), Clean Architecture (Domain/Application/Infrastructure/API), EF Core, DI, OOP/SOLID, FluentValidation hoặc tương đương.
- SQL Server; EF Core migrations; optimistic concurrency (rowversion) **cộng** transactional locking/serializable strategy cho phân bổ khoảng thời gian. Không chỉ dựa vào rowversion để chống overlap.
- OpenAPI, ProblemDetails, structured logging, health checks, UTC storage + Asia/Ho_Chi_Minh display, background jobs/outbox, retry có kiểm soát.
- Repo gợi ý: `apps/web`, `apps/admin`, `src/Domain`, `src/Application`, `src/Infrastructure`, `src/Api`, `tests/*`, `docs/*`, `infra/*`. Điều chỉnh nếu repo có cấu trúc hợp lý sẵn.
- Không nhúng credentials, API key, OTP, ảnh CCCD, bằng lái vào git/log. `.env.example`, user-secrets/secret manager; CI tests và hướng dẫn deploy.

## 2. Phạm vi website khách hàng (theo tài liệu gốc)
Trang landing Đà Nẵng: xe máy (xe số, tay ga, xe côn), ô tô (4/7/16 chỗ); danh mục, bảng giá, giới thiệu, liên hệ, FAQ, bài viết du lịch, hình ảnh/banners. Card xe có **hai CTA**: `Liên hệ` (inquiry, không thu tiền, không giữ xe) và `Đặt ngay` (booking/checkout). Form động theo loại xe, tối thiểu trường ban đầu; chọn **mẫu xe**, không chọn biển số. Hiển thị ngày/giờ nhận-trả, địa điểm, phụ phí giao nhận, tổng giá/giảm giá minh bạch. Giảm theo thời gian 3/7/15/30 ngày và thêm giảm online mặc định 10% (admin cấu hình; cộng dồn theo quy tắc cấu hình; tính toán trên server, snapshot, làm tròn nhất quán). Hiển thị bản đồ Google Maps, rating và tối đa 5 đánh giá thật từ nguồn hợp lệ, không tự tạo review. Floating mobile bottom: Gọi, Zalo, Đặt xe; desktop bottom-right vertical. Phụ kiện (mũ đạt chuẩn, lót mũ, giá điện thoại, baga, áo mưa, USB), 1–2 lít xăng, kiểm tra phanh/lốp/đèn/còi, hỗ trợ 24/7, giao sân bay/khách sạn/ga/bến xe, khu vực miễn phí/phụ thu, trả trễ linh hoạt theo chính sách admin, lưu hành lý, local travel guide; trình bày là cam kết/dịch vụ chỉ khi admin xác nhận thực tế. Thông số đánh giá phù hợp tuyến đường/hành lý/độ tiết kiệm: rules dựa thông số xác minh, admin duyệt, không bịa safety score. SEO, ảnh tối ưu, loading/error/empty states, đa ngôn ngữ VN (nguồn), EN/TH/KO/ZH/JA bằng Google Cloud Translation, cache, fallback, admin override, bảo vệ giá/ID/nội dung pháp lý nhạy cảm.

## 3. Quy tắc nghiệp vụ ĐÃ CHỐT (ưu tiên cao)
Các mục sau tổng hợp các quyết định đã phê duyệt qua v2.7. Các mã BR-24 đến BR-47 là mã quyết định rõ ràng; quy tắc trước đó được nhóm lại, **không tự sáng tác mapping BR-01–BR-23**.

### 3.1 Booking, giá, thanh toán, inventory
- `Liên hệ` tạo inquiry không ràng buộc/không giữ chỗ/không thu tiền, thông báo admin. `Đặt ngay` tạo booking và hold **theo mẫu xe** trong 15 phút; xác nhận đặt chỉ khi backend xác thực khoản online bắt buộc **và** phân bổ xe thành công; không xác nhận chỉ theo callback frontend, ảnh chuyển khoản hay email.
- Admin cấu hình theo mẫu xe: thanh toán 100% trước hoặc đặt trước theo **% tiền thuê sau giảm** (ví dụ 30%, không cố định toàn hệ thống). Đặt trước là **một phần tiền thuê**, không phải khoản thế chân/đặt cọc bảo đảm riêng. Phần còn lại thu khi giao xe bằng cash hoặc chuyển khoản. Admin cấu hình % hợp lệ >0 đến 100, giá >0, phí >=0, discount 0–100%, tổng không âm, ngày trả > ngày nhận. Snapshot giá, khuyến mãi, phụ phí và điều khoản vào booking. Không sửa snapshot/giao dịch đã chốt; dùng adjustment riêng.
- Backend xử lý SePay webhook xác thực theo cơ chế provider hỗ trợ, đối soát amount/reference/account, idempotency, retries, audit; SePay trial và tài khoản MB Bank cá nhân mong muốn, TPBank dự phòng **chưa xác minh khả dụng**. Không giả định provider hỗ trợ tính năng hoặc giá nào chưa kiểm chứng. Webhook trễ sau hết hold không tự confirm; sai tiền, trùng, thừa, thiếu, paid-but-no-inventory => reconciliation/Admin. Email Brevo qua transactional outbox/queue; email thất bại không rollback payment.
- Chỉ hiển thị model cho khách; biển số xe được backend gán nội bộ khi xác nhận thanh toán. Phải bảo vệ overlap của xe vật lý, tồn kho theo model, buffer cấu hình theo loại xe, lịch bảo trì, gia hạn, phân bổ tương lai. Không để xe chờ kiểm tra, tai nạn, mất, bảo trì vào inventory. Tách actual return timestamp và ready-for-rent timestamp.
- Hủy trước bàn giao giải phóng phân bổ ngay, độc lập với việc hoàn tiền. Sau bàn giao: trả sớm phải nhận xe, kiểm tra và quyết toán. No-show nhân viên liên hệ, xử lý thủ công, không tự phạt/hủy. Chính sách hoàn tiền cụ thể OPEN-05 chưa chốt.

### 3.2 Giao/nhận, gia hạn, thay xe, tranh chấp
- Bàn giao: xác minh giấy tờ CCCD/GPLX (upload tại booking hoặc xuất trình khi nhận); được booking/pay khi chưa upload nhưng **không bàn giao nếu chưa kiểm tra**. Với khách nước ngoài/ô tô, tính hợp lệ giấy tờ cần rà soát pháp lý. Không giữ bản gốc nếu không có cơ sở; dữ liệu nhạy cảm riêng tư, mã hóa, phân quyền, audit, retention OPEN-07.
- Biên bản bàn giao và nhận lại bằng ảnh/video, cả hai bên xác nhận bằng chữ ký trực tiếp hoặc OTP; khi OTP không đến thì fallback chữ ký; không giả mạo xác nhận. Bằng chứng ký bất biến theo phiên bản. Khách từ chối ký khi trả: lưu chứng cứ, quản lý xử lý ngoại lệ, không giả lập chữ ký. Xe chỉ sẵn sàng sau khi thực nhận, kiểm tra và thông qua.
- Thu tiền mặt: nhân viên có thể tạo yêu cầu/ghi nhận đã nhận tiền thực tế nhưng **chỉ quản lý/Admin xác nhận**. Khi ở sân bay, gửi yêu cầu xác nhận từ xa trước bàn giao. Từ chối xác nhận phải ghi lý do, nhân viên kiểm tra lại, chưa bàn giao. Chuyển khoản có backend reconciliation tự động trên webhook hợp lệ; nhân viên vẫn xác nhận giao xe thực tế.
- Gia hạn: phải còn lịch khả dụng, hold 15 phút, thanh toán thành công trước khi có hiệu lực; webhook đến trễ không tự gia hạn. Khách muốn đổi điểm trả: Admin duyệt và áp phí nếu có.
- Trước bàn giao xe hỏng/thiếu mẫu: gợi ý mẫu tương đương, **Admin và khách cùng chấp thuận**; mẫu đắt hơn Admin có thể miễn chênh hoặc yêu cầu trả thêm, lưu lý do; mẫu rẻ hơn phải xử lý chênh lệch minh bạch, không tự áp chính sách hoàn tiền. Trên đường xe hỏng: ưu tiên thay tương đương, biên bản thu hồi xe cũ và bàn giao xe mới, giữ history; không do lỗi khách và không có xe thay: Admin liên hệ thương lượng lịch/giải pháp, không tự hứa hoàn tiền.
- Tranh chấp phụ phí: quản lý xem xét, không tự thu khoản đang tranh chấp; xe đã kiểm tra đủ điều kiện vẫn được cho thuê lại; **hoàn tất sử dụng xe** dù tài chính còn tranh chấp. Xe trả trễ ảnh hưởng booking sau: cảnh báo Admin, ưu tiên xe thay và liên hệ khách; quá hạn không liên hệ được: tự cảnh báo/đánh dấu overdue, nhân viên xử lý thủ công. Mất xe/tai nạn nghiêm trọng: khóa xe, lập hồ sơ, Admin xử lý, không tự hủy các đơn tương lai; xe tìm lại phải kiểm tra kỹ thuật và Admin phê duyệt mới mở lại.

### 3.3 Quyền, điều chỉnh, hoàn tiền, ngoại tuyến — BR-24 đến BR-47
- **BR-24**: Chỉ quản lý/Admin xác nhận thu tiền mặt.
- **BR-25**: Admin hoặc quản lý được cấp quyền duyệt hoàn tiền thủ công, lưu lý do/bằng chứng.
- **BR-26**: Trả trễ ảnh hưởng đơn sau => cảnh báo Admin, ưu tiên thay xe, liên hệ khách sau.
- **BR-27**: Mất xe/tai nạn nghiêm trọng => khóa xe, hồ sơ sự cố, Admin xử lý.
- **BR-28**: Không xóa vĩnh viễn mẫu/xe vật lý có lịch sử thuê; ngừng khai thác, giữ lịch sử.
- **BR-29**: Tiền mặt tại sân bay: nhân viên yêu cầu, quản lý/Admin xác nhận từ xa trước giao.
- **BR-30**: Hoàn tất sử dụng xe dù phụ phí đang tranh chấp; quyết toán riêng.
- **BR-31**: Quá hạn chưa liên hệ được: cảnh báo Admin, overdue, nhân viên xử lý thủ công.
- **BR-32**: Admin điều chỉnh tiền thuê/phụ phí qua bản ghi riêng, reason/audit, không sửa transaction gốc.
- **BR-33**: Mất xe/tai nạn: khóa, cảnh báo, ưu tiên tìm xe thay; không tự hủy đơn tương lai.
- **BR-34**: Tiền vào nhưng không xác nhận được xe: không confirm booking, ghi nhận tiền, báo Admin tìm xe hoặc xử lý hoàn.
- **BR-35**: Quản lý từ chối xác nhận cash: ghi lý do, nhân viên kiểm tra lại, chưa bàn giao.
- **BR-36**: Khách từ chối ký biên bản trả: evidence, manager exception, không giả lập.
- **BR-37**: Xe tìm lại: vẫn khóa đến khi kiểm tra kỹ thuật và Admin mở khai thác.
- **BR-38**: Admin tăng tiền thuê/phụ phí: adjustment riêng, thông báo khách, chỉ thu có căn cứ/quy trình phù hợp.
- **BR-39**: Bàn giao offline đặc biệt được quản lý/Admin phê duyệt, đồng bộ sau.
- **BR-40**: Admin hoặc quản lý được cấp quyền thực hiện chuyển khoản hoàn tiền, phải có bằng chứng.
- **BR-41**: Không bắt buộc người duyệt và người chuyển là hai người; audit/evidence bắt buộc.
- **BR-42**: Đã giao thực tế nhưng server chưa cập nhật: khóa thao tác xung đột, hồ sơ đối soát, quản lý xác minh cập nhật.
- **BR-43**: Chuyển hoàn thất bại/chờ xử lý: thử lại có kiểm soát.
- **BR-44**: Mất mạng hoàn toàn: quản lý/Admin phê duyệt offline **qua cuộc gọi xác minh**, bắt buộc bổ sung xác nhận và nhật ký sau.
- **BR-45**: Không đối soát chuyển khoản tại điểm giao: **Admin** được duyệt ngoại lệ sau khi kiểm tra **chứng từ ngân hàng độc lập**. Approval không đồng nghĩa đã nhận tiền; không tạo payment giả.
- **BR-46**: Đồng bộ bàn giao offline xác minh xung đột phân bổ: **ưu tiên đơn đã giao thực tế và tự hủy đơn còn lại**. Giới hạn đúng xung đột đã xác minh, không áp dụng cho sự cố xe mất/trễ chung. Lưu hủy, cảnh báo khách/Admin, xử lý tiền thủ công; không tự đánh dấu đã hoàn tiền. Phải có evidence chống giả mạo trước auto-cancel; nếu không đủ evidence, giữ trạng thái conflict để xác minh, không hủy mù.
- **BR-47**: Refund chưa rõ kết quả ngân hàng **không được thử lại** trước khi đối soát.

### 3.4 Bốn vấn đề v2.7 chưa phê duyệt — KHÔNG giả làm BR mới
- CR-01: Cơ chế xác thực cuộc gọi offline (số đăng ký, mã một lần, log, hậu kiểm). Implement giao diện/record cho audit; để policy verification configurable và đánh dấu cần chốt, không tự coi cuộc gọi bất kỳ là đủ.
- CR-02: Admin cho giao khi chưa có webhook, sau đó ngân hàng xác minh chưa nhận tiền: lưu receivable, alert, xử lý thủ công theo hợp đồng; **đây là phương án an toàn đề xuất, chưa được phê duyệt**.
- CR-03: BR-46 yêu cầu tự hủy đơn bị xung đột; chưa chốt việc tìm xe thay trước hay sau hủy. Không tự thêm bước thay xe như điều kiện chặn auto-cancel; thực hiện BR-46 khi bằng chứng đã xác minh, phát sự kiện hỗ trợ khách và mở hướng thay xe nếu khả thi, ghi rõ điểm chờ quyết định.
- CR-04: Bằng chứng để retry refund khi trạng thái không rõ; ưu tiên chỉ retry sau khi ngân hàng xác nhận giao dịch thất bại/hoàn về nguồn, nhưng vẫn đánh dấu chưa chốt policy. Nếu không đủ bằng chứng, không retry.

## 4. Mô hình trạng thái và invariant bắt buộc
**Không dùng một status tổng hợp**. Thiết kế các state machines riêng: BookingLifecycle, Hold, PaymentObligation/Transaction, PhysicalVehicleOperationalStatus, Allocation, Handover/Return, Extension, Dispute, RefundRequest/Attempt, OfflineSync. Viết bảng transitions gồm from/event/guard/to/side effects/actor/idempotency/error. Phân biệt completed vehicle usage và unsettled finance; payment verified và exception approval; physically returned và rentable; offline physically handed over và server reconciled.

Các invariant không được phá:
1. Không có hai allocation có hiệu lực chồng lấn cùng xe; hold theo model không oversell capacity; lịch theo nửa khoảng `[start,end)` có buffer và timezone chuẩn.
2. Không confirm booking từ frontend redirect, screenshot, unverified webhook, expired hold, hoặc paid-but-no-vehicle.
3. Không bàn giao thông thường khi chưa đủ giấy tờ/tiền/biên bản; offline exception phải có approval hợp lệ, chứng cứ, sync/audit, không giả thanh toán.
4. Xe khóa, chưa kiểm tra, bảo trì, mất, tai nạn không thể phân bổ.
5. Transaction đã confirmed bất biến; mọi sửa tiền qua adjustment ledger; refund attempt không duplicate, unknown result không retry.
6. Cấm giả chữ ký, review, bank transaction, audit event.
7. Không hard delete mẫu/xe có booking history; không mất ảnh chứng cứ đã gắn biên bản ký.
8. Không ghi log raw CCCD/GPLX/OTP/token hoặc dữ liệu nhạy cảm; audit mọi truy cập tài liệu.

## 5. Database design cần hiện thực
Đề xuất aggregate/tables: `VehicleCategory`, `VehicleModel`, `PhysicalVehicle`, `VehicleSpecification`, `VehicleInspection`, `MaintenanceWindow`, `PricingRule`, `DiscountRule`, `DeliveryZone`, `Booking`, `BookingHold`, `BookingPriceSnapshot`, `VehicleAllocation`, `Customer`, `IdentityDocumentReference`, `PaymentObligation`, `PaymentTransaction`, `PaymentReconciliation`, `CashCollectionRequest`, `HandoverRecord`, `ReturnRecord`, `SignatureConfirmation`, `EvidenceAsset`, `OfflineApproval`, `OfflineHandoverSync`, `SyncConflict`, `RentalExtension`, `VehicleReplacement`, `IncidentReport`, `Dispute`, `FinancialAdjustment`, `RefundRequest`, `RefundApproval`, `RefundAttempt`, `AuditLog`, `NotificationOutbox`, `TranslationCache`, `CmsContent`, `Role`, `Permission`, `UserRole`. Có thể hợp nhất/tách khi có lý do; tài liệu hóa ERD, indexes, FKs, soft-disable, history, migration, seeds demo **không dùng review giả**.
- Monetary: decimal precision cố định, VND integer-compatible, rounding order được document/configure, invariant `paid + adjustments - refunds` nhất quán; không double-count tiền cọc thuê.
- Financial idempotency unique theo provider+transaction identity, booking obligation, refund attempt reference; business keys không đoán.
- Inventory overlap kiểm tra dưới transaction với khóa phù hợp; unique index đơn giản không đủ cho khoảng thời gian. Concurrency tests thực tế SQL Server.
- Sensitive files private object storage (không public URL), encryption at rest, signed expiring access, role-based download, audit, retention policy chưa chốt; không hard-code tự xóa CCCD.

## 6. API contracts và bảo mật
Thiết kế OpenAPI theo resources, bao gồm: `/api/catalog`, `/api/quotes`, `/api/inquiries`, `/api/booking-holds`, `/api/bookings`, `/api/payments/webhooks/sepay`, `/api/payments/reconciliations`, `/api/cash-collections`, `/api/handovers`, `/api/offline-approvals`, `/api/offline-sync`, `/api/returns`, `/api/extensions`, `/api/vehicle-allocations`, `/api/vehicles`, `/api/incidents`, `/api/disputes`, `/api/adjustments`, `/api/refunds`, `/api/cms`, `/api/translations`, `/api/admin/dashboard` (names examples, not mandatory exact routes). Define DTOs, HTTP methods, ProblemDetails error codes, permission policy, precondition headers for concurrency, pagination/filtering, input validation, audit, idempotency key, request/response examples.
- AuthN/AuthZ: secure admin login, HttpOnly Secure SameSite cookies as appropriate, CSRF protection, role + granular permission checks backend, MFA strongly recommended for high-risk admin actions, rate limit/lockout, session revocation, no insecure public document endpoints.
- Offline approval via phone must be auditable; a local offline queue is **untrusted input**, cannot silently override server. Prevent replay/tampering with event IDs, immutable local evidence hashes, actor identity, signed approval after sync. Never claim offline proof is cryptographically guaranteed if device lacks secure trusted identity.
- Webhook authentication per SePay actual docs (not invented signature mechanism), allow-list if supported, verify amount/reference and reconciliation. External services behind interfaces.

## 7. Admin chức năng
Dashboard fleet, active rentals, bookings, revenue daily/monthly, charts, alerts. Fleet per physical plate/model/photos/inspection/maintenance; prices by day/week/month, late fees, discounts, online rate, zones and buffer. Booking timeline, customers, document verification, handover/return, no-show, extensions, swaps, disputes, incidents, refunds, cash confirmation, reconciliation, offline conflicts. CMS banners/FAQ/local guides/reviews verified, translations with admin override, notification templates, audit logs, staff roles, settings. Permission matrix: delivery staff prepare/evidence and cash request, **cannot confirm cash/refund**; managers can confirm cash and approve/perform refunds only if granted; Admin controls config/roles and incident resolution. Prevent privilege escalation; record actor/reason/evidence.

## 8. Test plan / acceptance criteria
Implement automated unit, integration (SQL Server real provider/container where available), API contract, concurrency and E2E tests. At minimum cover:
AT-01 simultaneous last inventory; AT-02 payment after expired hold; AT-03 duplicate webhook; AT-04 paid no allocation; AT-05 staff attempts confirm cash denied; AT-06 manager cash confirm once; AT-07 rejected cash prevents normal handover; AT-08 extension payment after hold expiry; AT-09 extension conflicts; AT-10 crash vehicle locked; AT-11 recovered vehicle not automatically available; AT-12 refusal to sign return; AT-13 rental usage complete while dispute open; AT-14 admin fee adjustment ledger immutable; AT-15 duplicate offline sync; AT-16 verified offline allocation conflict invokes BR-46; AT-17 unknown refund result blocks retry; AT-18 verified failed refund controlled retry; AT-19 concurrent refund approval no double settlement; AT-20 Brevo failure does not roll back booking; AT-21 forged phone approval rejected/pending verification; AT-22 bank evidence not treated as confirmed transfer; AT-23 duplicate offline physical handover claim conflict; AT-24 cancelled paid booking retains funds/notifications and no fake refund.
For each: Given/When/Then, preconditions, API, DB assertions, auth/role, negative case. Test offline network restoration, crash/restart, outbox retries, overlapping buffer, timezone, price rounding, race conditions, admin revoked permissions, data privacy. Include manual UAT checklist and known provider sandbox limits.

## 9. OPEN — tuyệt đối không tự chốt
- OPEN-01: SePay trial / MB personal account / TPBank actual bank/provider compatibility, live webhook and costs must be validated externally. Use sandbox/mocks until verified.
- OPEN-05: exact cancellation/early return/refund percentages, windows, grace periods, damage compensation and consumer disclosures require business approval. Build configurable policy placeholders, do not charge/refund by invented rate.
- OPEN-07: legal maximum CCCD/GPLX/passport retention, legal bases, privacy notices, cross-border transfers, foreign driver eligibility, digital signature enforceability require legal review. Restrict access, log and document pending decision; do not invent retention days.
- CR-01..CR-04 as above remain unapproved technical details.
Other unclear items: actual store contact/address, Zalo OA, Google Maps place/reviews credentials, delivery fee zones, live prices, email sender domain, translations API credentials, insurance/roadside assistance SLAs. Use clearly marked configurable placeholders; never show fake reviews or fabricated operational promises.

## 10. Required repo documentation and delivery gates
Create/update:
- `PROJECT_RULES.md`: conventions, security, precedence of approved decisions.
- `REQUIREMENTS.md`: source requirements + approved rules + traceability; **do not fabricate detailed text for BR-01..23 IDs**.
- `ARCHITECTURE.md`: boundaries, data flow, state machines, sequence diagrams, ERD, concurrency and failure recovery.
- `DECISIONS.md`: approved BR-24..47, grouped earlier decisions, proposals separately, rationale.
- `OPEN_QUESTIONS.md`: OPEN-01/05/07, CR-01..04, operational placeholders, owner/impact/blocker.
- `TASK_BOARD.md`: phased tickets with acceptance criteria, dependencies, status.
- `PROGRESS_LOG.md`: date, changes, commands/tests run, result, next steps.
- `API_CONTRACTS.md`, `TEST_MATRIX.md`, `SECURITY_PRIVACY.md`, `DEPLOYMENT.md`, `README.md`.

At start of every session read these docs, inspect git status, identify next task, plan small safe changes. After each change: build/test/lint/migrate where applicable; update docs and log. No false claims. Stop only for genuine blockers; summarize blockers and continue independent work. Do not commit secrets or customer data. Before declaring done: all critical acceptance tests pass, no unresolved high-severity security/inventory/payment defects, deploy guide works, and OPEN decisions remain explicitly documented rather than silently assumed.

## 11. Your immediate first response and action
1. Inspect repository and report existing state, source docs and environment. 2. Generate traceability matrix: source/approved rule -> state transition -> DB -> API -> test, with gaps and conflicts. 3. Create the documentation set above and phased task board. 4. Begin Phase 1/2 implementation where safe without waiting for another approval; avoid external real payments and legal-sensitive production policies. 5. Report exactly what was created, verified, remains OPEN, and next task. **Do not ask another round of 4–5 generic questions.**
