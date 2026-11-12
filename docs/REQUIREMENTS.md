# Yêu cầu và truy vết

Nguồn: Master Prompt và đặc tả chi tiết v2.7 cùng thư mục. Các bản sao dưới đây giữ nội dung nguồn; không coi độ hoàn thiện mô tả trong bảng nguồn là tính năng đã triển khai.

## Phạm vi hiện tại

Clean Architecture .NET10, 16 entity Catalog/Fleet, Identity + cookie đã duyệt, API tạo/đọc catalog nháp và xe/bảo trì/checklist. Identity migration và SQL/API tests áp dụng trên SangRentalTestDb; Development giữ hai migration đầu. Frontend vẫn preview; booking/payment/live customer data chưa triển khai. Các đoạn nghiệp vụ nguồn dưới đây là yêu cầu, không phải mô tả tính năng đã hoàn tất.

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


## Trạng thái thực hiện

Tất cả dòng nghiệp vụ trong ma trận là kế hoạch, chưa triển khai và chưa chạy AT. Chỉ các kiểm tra SC trong TEST_MATRIX thuộc giai đoạn khởi tạo. Không tạo mapping BR-01…23. Trước module phải khóa hợp đồng chi tiết, state/guard/schema/index/API và negative/concurrency cases.
