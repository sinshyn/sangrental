# Phụ lục UI/UX cho Master Prompt và đặc tả v2.7

Ngày 2026-11-07. Đã đọc toàn bộ quy chuẩn mới, gồm mục 1–8 và chỉ dẫn cuối. Phụ lục này ghi rõ thay đổi, không ghi đè hai tài liệu nguồn v2.7.

## Đối chiếu

| Nguồn/mục | Nội dung trước | Bổ sung hiện tại |
|---|---|---|
| Master Prompt §1 | Ưu tiên shadcn/ui + Tailwind; thông báo toast/dialog nhất quán | Giữ UI thông thường; chọn SweetAlert2 duy nhất cho thông báo, Font Awesome chính cho icon |
| Đặc tả §2, §14 | Responsive/accessibility/i18n | Tokens Coastal Luxury chung, sáu locale notification và fallback VI; browser QA vẫn là gate riêng |
| Master §6; đặc tả §8, §10 | Authz, idempotency, payment verified/refund reconciliation | Confirm không thực thi nghiệp vụ; popup success chỉ phản ánh trạng thái tin cậy; chưa có module tài chính |
| Master §9; đặc tả §13 | OPEN-01/05/07, CR-01…04 | Không thay đổi, không tự chốt |
| Quy chuẩn mới §1 | SweetAlert2/Font Awesome, inline validation | Hai service hiện có đáp ứng scaffold; không thêm thư viện khác, không sửa bản Stitch lưu tham chiếu |
| Quy chuẩn mới §2–3 | Service tập trung, ownership, safe text/confirmation | Shared core, khóa popup/loading, loading token và xử lý reject; không Swal.fire trong component |
| Quy chuẩn mới §4 | Ocean/Turquoise/Sand/Navy/Ivory | CSS nguồn chung, map semantic tokens và theme popup; ghi trong DESIGN_SYSTEM |
| Quy chuẩn mới §5 | Sáu ngôn ngữ | Dictionary vi/en/ko/zh-CN/ja/th; chưa toàn website i18n |
| Quy chuẩn mới §6 | Bảo mật thông báo và trạng thái tài chính | Chỉ translation keys, không raw HTML/PII, không payment/refund success giả |
| Quy chuẩn mới §7–8 | Docs và kiểm thử | Phụ lục, design system, policy tests, unit tests, build/smoke; browser/a11y/bundle còn TODO |

Không có mâu thuẫn nghiệp vụ/pháp lý cần chọn chính sách mới. Master/đặc tả chỉ nêu UI library ưu tiên và nguyên tắc tổng quát; quy chuẩn bổ sung cụ thể hóa thư viện thông báo/icon. Design tokens mới ưu tiên cho thương hiệu; surface/layout Stitch vẫn là tham chiếu.

## Dependency hiện tại

Đã kiểm tra package.json, lockfile và `npm ls --depth=0`: SweetAlert2 11.26.25; Font Awesome React 3.5.0, SVG core/free solid icons 7.3.1. Các package đã có từ lần scaffold trước; phiên đối chiếu không cần cài mới/nâng version. Admin dùng assets tự host từ chính dependency npm đã pin, không thêm wrapper NuGet hoặc CDN khác. Tailwind 4.3.5 và cấu hình shadcn/ui được giữ.

## Tài liệu và kiến trúc áp dụng

Đọc phụ lục này cùng Master Prompt/đặc tả gốc trước module UI mới. [DESIGN_SYSTEM](DESIGN_SYSTEM.md) quy định tokens/API/ownership/i18n; [PROJECT_RULES](PROJECT_RULES.md) là coding conventions; [ARCHITECTURE](ARCHITECTURE.md) mô tả hai adapter; [TASK_BOARD](TASK_BOARD.md) và [TEST_MATRIX](TEST_MATRIX.md) theo dõi gate còn thiếu. Không tạo lại project, không đổi schema/booking/payment hoặc chính sách OPEN.
