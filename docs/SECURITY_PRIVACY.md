# Bảo mật và dữ liệu cá nhân

## Nền tảng hiện có

Phase 2: credential connection SQL local đã lưu ngoài repo bằng .NET User Secrets của API, không in ra log/báo cáo. Local Docker dùng Encrypt=True, TrustServerCertificate=True theo yêu cầu; đây không phải cấu hình TLS production. sa chỉ dùng thiết lập/kiểm tra local, chưa tạo application login ít quyền. Health chỉ Healthy/Unhealthy. Xem PHASE_2_DATABASE_CONNECTION_REPORT.md.

- Không secret/dữ liệu khách; connection string từ user-secrets/environment, ví dụ để trống.
- Không default admin, đăng ký công khai hoặc cookie giả. HttpOnly/Secure Always/Strict, TTL 30 phút là cấu hình kỹ thuật; chưa identity store/session revocation/MFA.
- Admin middleware auth/authz/antiforgery, policy server. HTTP local chỉ kiểm trang khóa; cookie yêu cầu HTTPS.
- API ProblemDetails; không sensitive-data logging; health không lộ exception SQL; OpenAPI Development only.
- Lockfiles, không vô hiệu hóa NuGet audit; pin Pkcs sửa dependency transitive.
- Frontend noindex, không thu thập thông tin hoặc tải Google scripts/fonts/ảnh ngoài.

## Trước module thật/production

Chốt identity provider/store, provisioning riêng, MFA, revocation, Data Protection keys lưu bền và mã hóa, backend permission policy, CSRF cho cookie mutation, rate limit/lockout. Chưa có mutation nên chưa thêm limiter nghiệp vụ giả.

Giấy tờ phải private storage, mã hóa, signed short-lived access và audit. Không log CCCD/GPLX/passport/OTP/token/API key/raw webhook. Retention OPEN-07 chưa chốt; không tự đặt ngày.

Payment cần provider verification + amount/reference/account + idempotency, không dựa ảnh/redirect. OPEN-01/05/07 và CR-01…04 chặn chức năng liên quan trước go-live.

Host .NET runtime 10.0.7; package 10.0.13. Chủ hạ tầng cần cập nhật runtime được hỗ trợ trước deployment. Phiên này không sửa cài đặt hệ thống.
