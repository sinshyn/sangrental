# Identity và Catalog/Fleet — lát triển khai tiếp theo

Ngày 08/11/2026. Chủ dự án xác nhận SQL Server đang chạy, hai migration và 16 bảng đúng; đồng ý ASP.NET Core Identity + SQL Server + cookie authentication.

Cập nhật 10/11: các giới hạn "không tự tạo test DB" dưới đây là phạm vi ban đầu; DECISIONS ngày 08/11 đã ghi nhận phê duyệt SangRentalTestDb và bằng chứng test. Identity Development vẫn chờ review. Hợp đồng chi tiết hiện hành ở API_CONTRACTS; hướng dẫn ở TESTING_IDENTITY_CATALOG. Lần kiểm SQL mới 10/11 thất bại kết nối, xem PROGRESS_LOG; không suy kết quả PASS hôm nay từ bằng chứng cũ.

## Phạm vi

Triển khai Identity dùng chung cho API và Blazor Admin; đăng nhập nội bộ, không đăng ký công khai, không tạo tài khoản mặc định. Cookie `__Host-SangRental.Admin`, HTTPS, HttpOnly, SameSite Strict, TTL 30 phút. Hai host cùng tên máy dùng chung Identity scheme và Data Protection application name. Production cần chung key ring và HTTPS reverse proxy đã cấu hình; chưa triển khai production.

Nguồn quyền là database Identity tại mỗi request; cookie hết hiệu lực khi security stamp thay đổi, tài khoản bị khóa/vô hiệu hóa. Role Admin được phép Catalog/Fleet; Manager chỉ có capability khi được cấp claim permission cụ thể; Staff không mặc nhiên được quyền ghi. Không tạo API tự cấp quyền. Blazor revalidation kiểm lại phiên trong circuit, mọi mutation vẫn kiểm quyền backend.

Bootstrap Admin đầu tiên chỉ bằng lệnh CLI tương tác, mật khẩu nhập ẩn, không tham số command line/seed/config mặc định. Không tạo tài khoản trong startup. Có khóa transaction chống hai bootstrap đồng thời. MFA chưa có quy trình enrollment và vẫn là gate trước production/tác vụ tài chính.

Schema Identity bổ sung vào schema `identity`, giữ nguyên 16 bảng `sangrental`. Sinh migration và SQL để review; không tự áp dụng vào SangRentalDb, không tự tạo test DB. API cần migration Identity trước khi đăng nhập thật. Guard SQL Catalog/Fleet có thể kiểm bằng transaction rollback trên schema đã có; kiểm Identity end-to-end cần DB test được chọn và migration được áp dụng.

## Hợp đồng HTTP của lát đầu

- `GET /auth/csrf`: cấp request token, cookie antiforgery HttpOnly; response `Cache-Control: no-store`.
- `POST /auth/login`: JSON username/password, antiforgery header `X-CSRF-TOKEN`, chỉ HTTPS. Không remember-me. Sai tài khoản/mật khẩu/khóa/vô hiệu hóa trả chung `401 auth.invalid_credentials`. Giới hạn tần suất và Identity lockout.
- `POST /auth/logout`: cookie + antiforgery, hủy cookie; không có logout GET.
- `GET /auth/session`: cookie, trả ID/role/permission hiện thời, không token/secrets.
- Swagger UI và OpenAPI chỉ Development; Swagger tự lấy antiforgery request token cho request có side effect. Đăng nhập tại API hoặc Admin HTTPS cùng hostname; cookie được dùng cho Try it out.
- `/api/admin/catalog/*`, `/api/admin/fleet/*`: yêu cầu phiên và quyền backend, DTO whitelist; không serialize entity hoặc nhận actor/status/audit từ client. Lát đầu hỗ trợ tạo catalog nháp, đọc danh sách nội bộ, tạo xe Ready/nháp và bảo trì/checklist. Public catalog, upload/duyệt ảnh, cập nhật toàn bộ, booking/giá/availability nằm ở lát sau.
- Command tạo mới không tự retry khi mất phản hồi. DB business key unique chặn trùng mã. Command hoàn tất bảo trì chống replay bằng trạng thái; cập nhật checklist dùng rowversion tại HTTP boundary. Các command chưa có ledger Idempotency-Key không được tuyên bố exactly-once.

## Kiểm chứng bắt buộc

Build/restore locked mode, model/migration SQL review, notification tests; HTTP anonymous/CSRF/HTTPS/rate limit/Swagger; SQL guard rollback và concurrency trên SQL thật khi kết nối được. Đăng nhập thành công, lockout, logout, revoked role/security stamp và CLI bootstrap cần kiểm trên DB test sau migration; không dùng kết quả unit test thay bằng chứng SQL.

Không thay OPEN-01/05/07, CR-01…04 hoặc các quyết định Q1–Q39. Phạm vi này không triển khai thanh toán, giấy tờ khách hoặc booking.
