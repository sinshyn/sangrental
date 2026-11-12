# Bảo mật và dữ liệu cá nhân

## Nền tảng hiện có

13/11: upload ảnh catalog chỉ Admin/HTTPS/CSRF, giới hạn request/file/pixels trước cấp phát native; kiểm codec+đuôi, không tin MIME/tên file. Mỗi ảnh GUID v7/FileMode.CreateNew ngoài wwwroot; input filename không làm path. Image preview/private no-store, public chỉ qua gate approved/visible/model/category publication và cache immutable. Trạng thái ảnh thay đổi yêu cầu rowversion/audit/lock theo mẫu, không tự xuất bản mẫu. Bốn purpose tài liệu giữ bytes/format/EXIF, lưu Data Protection ciphertext, không public route; primitive đọc gốc chỉ dành module hồ sơ tương lai tự kiểm quyền/audit trước gọi. Chưa xử lý dữ liệu khách thật/retention/antivirus; OPEN-07 vẫn mở. Không có API nhận purpose từ client để đổi tài liệu thành public. [Chi tiết](../Changes/2026-11-13-upload-anh-guid-v7-cache.md).

12/11: lifecycle/profile/registration chỉ Admin (Manager có fleet.manage vẫn bị 403), dùng HTTPS/cookie/CSRF, version xe từ client được so với SQL sau khóa, không nhận actor/status/normalized plate/thời điểm từ client. SQL integration mới 200 checks và guard runner 36 PASS; GET Fleet no-store. Inspection/audit append-only; registration chỉ được đóng interval hiện hành, không sửa định danh hoặc row đã đóng/xóa qua DbContext. Chuẩn hóa biển là kỹ thuật so trùng, không xác nhận tính hợp lệ đăng ký pháp lý. Guard này chưa thay quyền SQL trực tiếp ít quyền. Không xử lý giấy tờ/dữ liệu khách thật; OPEN-07 giữ nguyên.

Phase 2: credential connection SQL local đã lưu ngoài repo bằng .NET User Secrets của API, không in ra log/báo cáo. Local Docker dùng Encrypt=True, TrustServerCertificate=True theo yêu cầu; đây không phải cấu hình TLS production. sa chỉ dùng thiết lập/kiểm tra local, chưa tạo application login ít quyền. Health chỉ Healthy/Unhealthy. Xem PHASE_2_DATABASE_CONNECTION_REPORT.md.

- Không secret/dữ liệu khách; connection string từ user-secrets/environment, ví dụ để trống.
- Identity SQL + cookie đã được duyệt và triển khai lát đầu. Không default admin/đăng ký công khai. HttpOnly/Secure Always/Strict, TTL 30 phút, không sliding/remember-me; kiểm security stamp/active/lockout và tái tạo quyền từ DB mỗi request; logout thu hồi mọi phiên. Blazor circuit revalidate mỗi phút, mutation vẫn phải kiểm quyền backend.
- API/Admin mutation có antiforgery; login có rate limit 10 request/phút/IP và Identity lockout sau 5 lần sai trong 15 phút. HTTP chỉ xem thông tin yêu cầu HTTPS, không đăng nhập. Manager cần permission cụ thể; không có API tự cấp quyền.
- Admin đầu tiên bằng CLI nhập ẩn, khóa transaction chống bootstrap đồng thời; không tạo tài khoản startup. Chế độ cấp thêm Admin chỉ allowlist DB test local. Chưa MFA/recovery hoặc UI quản lý user/quyền.
- Audit/inspection/status/security events append-only qua DbContext; chưa thay thế quyền SQL append-only ngoài EF.
- API ProblemDetails; không sensitive-data logging; health không lộ exception SQL; OpenAPI Development only.
- Lockfiles, không vô hiệu hóa NuGet audit; pin Pkcs sửa dependency transitive.
- Frontend noindex, không thu thập thông tin hoặc tải Google scripts/fonts/ảnh ngoài.

## Trước module thật/production

Checkpoint mới 11/11 sau Docker: SQL runners PASS 84 Identity/API và 22 guards. Override có giới hạn minimatch nhánh 10 sang 11.0.6 được kiểm lint/negative case/build/clean install; audit online mới 0 vulnerabilities, thay trạng thái 9 moderate ở checkpoint trước bên dưới. Peer metadata của một số ESLint plugins còn cảnh báo; phải kiểm lại khi nâng công cụ. Chứng chỉ HTTPS localhost chưa trusted trên OS; chưa tự thay trust store. Admin `/catalog` dùng HTTP SSR POST, authorization/CSRF/audit như mô tả API_CONTRACTS; success flash bảo vệ Data Protection.

Checkpoint 11/11: audit npm online sau thêm ESLint báo 9 moderate ở chuỗi dev tooling (minimatch globparts amplification, GHSA-g3hv-75wg-m249), không high/critical. Chưa tự ép major override hoặc tắt audit để làm sạch kết quả. Cần chọn bản sửa tương thích và chạy lại lint/build trước đóng gate dependency security. SQL runners cùng ngày FAIL SqlException, không nâng kết quả lịch sử thành PASS mới.

Không hỏi lại provider/store/provisioning đã duyệt. Còn MFA/recovery, UI quản lý user/quyền, Data Protection shared key ring bảo vệ/lưu bền, SQL login ít quyền và kiểm tra reverse proxy trước production. Windows KeyDirectory dùng DPAPI, không tắt để vượt lỗi môi trường. Smoke có key directory riêng trong artifacts và không kết nối SQL; không dùng smoke để chứng minh phiên production an toàn.

Identity migration chỉ được ghi nhận đã apply trên SangRentalTestDb; Development chờ review riêng. Ngày 10/11, thử lại SQL integration/guards thất bại vì không kết nối được SQL local (TCP 1436 timeout), chưa xác minh lại các ca auth/concurrency. Xem PROGRESS_LOG; không nâng bằng chứng build thành SQL PASS.

Giấy tờ phải private storage, mã hóa, signed short-lived access và audit. Không log CCCD/GPLX/passport/OTP/token/API key/raw webhook. Retention OPEN-07 chưa chốt; không tự đặt ngày.

Payment cần provider verification + amount/reference/account + idempotency, không dựa ảnh/redirect. OPEN-01/05/07 và CR-01…04 chặn chức năng liên quan trước go-live.

Host .NET runtime 10.0.7; package 10.0.13. Chủ hạ tầng cần cập nhật runtime được hỗ trợ trước deployment. Phiên này không sửa cài đặt hệ thống.
