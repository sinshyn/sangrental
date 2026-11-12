# Quy tắc dự án

Phạm vi hiện hành: nền tảng, Identity và lát Catalog/Fleet theo TASK_BOARD; giới hạn chỉ khởi tạo ngày 07/11 là lịch sử. Không triển khai thanh toán thật, dữ liệu khách thật hoặc phát hành production khi chưa đủ các gate được duyệt.

Nguồn v2.7, quy chuẩn UI/UX và hai bản Stitch hiện ở `docs/important/`. Giữ nguyên nội dung nguồn; ghi quyết định và tiến độ trong tài liệu dẫn xuất.

- Đầu mỗi phiên đọc tài liệu quản lý và nguồn v2.7, kiểm tra git status.
- Ưu tiên yêu cầu trực tiếp, rồi quyết định đã duyệt; tách đề xuất và OPEN. Không tự gán BR-01…23.
- Không ghi đè tài liệu/code có sẵn, không xóa dữ liệu hoặc commit credentials. Chỉnh các tệp scaffold mới để hoàn thiện được phép.
- Tài liệu tiếng Việt; code/API/biến tiếng Anh; UTF-8.
- Domain thuần C#; Application phụ thuộc Domain; Infrastructure phụ thuộc Application; API là composition root.
- Không thêm package thiếu mục đích. Không Redis/JWT/AutoMapper/BCrypt ở giai đoạn này.
- Chạy kiểm tra phù hợp sau thay đổi, ghi kết quả thật vào PROGRESS_LOG và cập nhật TASK_BOARD.
- Không tự chốt OPEN/CR hoặc chuyển sang pha nghiệp vụ ngoài yêu cầu.
- Đọc AGENTS.md và SangRental_UIUX_SweetAlert2_FontAwesome_Standard.md bổ sung: chỉ SweetAlert2 cho thông báo, Font Awesome cho icon. Thông báo qua service tập trung, i18n keys, validation inline. Không window.alert/confirm/prompt hoặc thư viện toast khác.
- Đọc UIUX_V27_ADDENDUM.md và DESIGN_SYSTEM.md cùng nguồn v2.7; không sửa nguồn để hợp thức hóa thay đổi UI. Chỉ sửa CSS/core/dictionary nguồn, chạy prepare-ui-assets.mjs để cập nhật bản sinh admin.
