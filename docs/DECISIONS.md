# Sổ quyết định

## Quyết định chủ dự án 13/11/2026 — ảnh và định danh file

- Đã duyệt: ảnh nội dung chuyển sang WebP/JPG; mỗi upload thành công luôn là file mới, kể cả trùng tên/nội dung. GUID v7 sinh bằng .NET, giữ ID bigint/schema cũ; không dedupe hash hoặc overwrite.
- CCCD **và GPLX, giấy tờ, bằng chứng bàn giao** giữ nguyên định dạng/raw bytes/metadata, private; không nén/resize/chuyển đổi. Primitive lưu được mã hóa, giải mã khôi phục byte gốc; chưa mở module hồ sơ khách hoặc chốt retention OPEN-07.
- Cache dài hạn cho URL bất biến của ảnh công khai; preview nháp/private no-store. Đổi ảnh tạo URL mới; không promise browser giữ cache mãi hoặc thu hồi ảnh đã cache sau hide. Pipeline content và private tách theo purpose server.
- Lát đầu dùng schema VehicleModelImages hiện có. Chi tiết implementation/file/test/rollback tại [Changes](../Changes/2026-11-13-upload-anh-guid-v7-cache.md). Không thay các quyết định nghiệp vụ giá/xe/thanh toán.

## Quyết định hiện hành 08/11/2026 — Identity và test database

- Chủ dự án xác nhận SQL Server chạy và đúng hai migration/16 bảng; duyệt ASP.NET Core Identity + SQL Server + cookie authentication. Sau đó cho phép tạo SangRentalTestDb để migration và kiểm thử. Không coi đây là phê duyệt migration Identity lên SangRentalDb.
- Giữ một DbContext/migration chain, thêm 8 bảng schema identity; không thay 16 bảng Catalog/Fleet. API/Admin cùng Identity cookie scheme và Data Protection app name; không JWT, không registration public/default password.
- Admin đầu tiên qua CLI nhập ẩn; SQL applock chống hai bootstrap đồng thời. Chế độ test cho tạo thêm Admin trong đúng DB test local để chủ dự án tự thử; các tài khoản automation được disable cuối test.
- Mỗi request đọc lại security stamp/active/role/permission; logout revoke mọi phiên. Blazor revalidate 1 phút. MFA, password recovery, user/permission management UI và production key management còn TODO.
- Lát API đầu có tạo/đọc catalog nháp, tạo/đọc xe, bảo trì/checklist. Không mở publish ảnh/thông số, đổi biển/xóa, booking/pricing/availability trước khi hoàn thiện guard liên quan. SQL integration và HTTP concurrency được kiểm trên DB test. Chi tiết ở IDENTITY_CATALOG_IMPLEMENTATION.md.

Các mục ngày 07/11 dưới đây là lịch sử scaffold; mô tả chưa có entity/identity/migration đã được thay thế bởi trạng thái trên.

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

Nội dung nguyên văn BR-24…47 được giữ tại [đặc tả mục 4](important/SangRental_DacTa_ChiTiet_v2_7.md#4-sổ-quyết-định-đã-chốt-br-24-đến-br-47). Nhóm trước BR-24 ở mục 3; không tự ánh xạ số BR-01…23. Ma trận truy vết và bản sao quyết định nằm trong REQUIREMENTS.md.

BR-46 chỉ áp dụng xung đột offline đã xác minh; BR-33 không tự hủy đơn tương lai vì xe mất/tai nạn. BR-47 chặn retry refund unknown. Không triển khai các nghiệp vụ này trong scaffold.

OPEN-01/05/07 và CR-01…04 vẫn chưa phê duyệt. State/ERD tương lai là đề xuất, chưa schema thực tế.
