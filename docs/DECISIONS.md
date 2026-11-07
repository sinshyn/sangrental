# Sổ quyết định

## Quyết định nền tảng 2026-11-07

- Phase 2: giữ DbContext/migrations/dependencies hiện có; connection SangRental vào SangRentalDb Docker localhost:1436. Credentials trong User Secrets Development. TrustServerCertificate=True chỉ local theo yêu cầu; production dùng cert tin cậy. Do localhost timeout 258 trong khi tcp:127.0.0.1 hoạt động, opt-in Development UseIPv4Loopback chỉ ánh xạ endpoint local này; không đổi Docker/password/schema. Không retry SQL tự động. Xem PHASE_2_DATABASE_CONNECTION_REPORT.md.

- Audit readiness: API/Admin dùng ClearProviders + AddJsonConsole để logging không phụ thuộc quyền Windows EventLog. Đã tái hiện readiness 500/ngắt stream do EventLog Access denied; sau sửa HTTP ready thiếu DB trả 503 và smoke 12/12. Không thay quyết định nghiệp vụ, xem PHASE_1_READINESS_REPORT.md.

- Giữ cấu trúc thư mục hiện có, solution SangRental.slnx tại root; SDK pin 10.0.203.
- Package Microsoft cần thiết và dotnet-ef pin 10.0.13. Direct reference Pkcs 10.0.13 để xử lý cảnh báo bảo mật transitive; không tắt audit.
- Chưa tạo aggregate. Migration InitialFoundation rỗng, không apply DB hoặc seed startup.
- Thiếu connection vẫn khởi động: live 200, ready 503. Không dùng retry SQL tự động trước khi thiết kế transaction/idempotency.
- Cookie và AdminAccess policy chuẩn bị auth. Login khóa, chưa có identity store/signin/account bootstrap; không chia sẻ cookie với API.
- shadcn/ui mới chuẩn bị cấu trúc/config/CSS tokens; Button là component nội bộ. Chưa cần form library/validation library.
- Dùng system font để build không cần Google Fonts. Plus Jakarta Sans/Inter trong Stitch sẽ được xem xét tự host ở pha giao diện.
- AGENTS.md và quy chuẩn UI/UX xuất hiện trong quá trình khởi tạo: đã đọc, áp dụng SweetAlert2 11.26.25 và Font Awesome (core/icons 7.3.1, React 3.5.0). Notification core TypeScript dùng chung; admin dùng bản JS sinh bằng script, assets tự host có license. Chỉ chọn compass/gauge; không nạp toàn pack runtime.
- Các thông báo chỉ nhận translation key, sáu locale và fallback VI. Loading có ownership token để close không đóng nhầm tác vụ; confirm chỉ trả bool. Chưa có notification nghiệp vụ hoặc thông báo payment/refund thành công.

## Quyết định nghiệp vụ đã duyệt

Đối chiếu UI/UX bổ sung ngày 2026-11-07: xem [UIUX_V27_ADDENDUM.md](UIUX_V27_ADDENDUM.md). Không cần thay dependency đã pin. CSS Coastal Luxury là nguồn chung, copy sang admin bằng script; không thêm icon/notification library. `closeLoading(token)` giữ ownership thay cho close không điều kiện. Các gate browser/a11y/bundle chưa hoàn thành; không thay quy tắc nghiệp vụ bên dưới.

Nội dung nguyên văn BR-24…47 được giữ tại [đặc tả mục 4](SangRental_DacTa_ChiTiet_v2_7.md#4-sổ-quyết-định-đã-chốt-br-24-đến-br-47). Nhóm trước BR-24 ở mục 3; không tự ánh xạ số BR-01…23. Ma trận truy vết và bản sao quyết định nằm trong REQUIREMENTS.md.

BR-46 chỉ áp dụng xung đột offline đã xác minh; BR-33 không tự hủy đơn tương lai vì xe mất/tai nạn. BR-47 chặn retry refund unknown. Không triển khai các nghiệp vụ này trong scaffold.

OPEN-01/05/07 và CR-01…04 vẫn chưa phê duyệt. State/ERD tương lai là đề xuất, chưa schema thực tế.
