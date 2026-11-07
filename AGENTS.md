# AGENTS.md — SangRental Repository Instructions

> Áp dụng cho mọi tác vụ Codex/AI agent trong repository SangRental. Đặt file này tại **thư mục gốc `SangRental/AGENTS.md`**. Ngôn ngữ trao đổi và tài liệu: **tiếng Việt**; tên code, API, class, biến: **tiếng Anh**.

## 1. Bắt đầu mỗi phiên làm việc

1. Kiểm tra thư mục làm việc, cấu trúc repository, `git status`, project đã tồn tại và công cụ có sẵn. Không giả định scaffolding đã hoàn tất.
2. **Đọc toàn bộ** các tài liệu sau trong `docs/` trước khi lên kế hoạch hoặc sửa code:
   - `docs/SangRental_Codex_Master_Prompt_v2_7.md`
   - `docs/SangRental_DacTa_ChiTiet_v2_7.md`
   - `docs/SangRental_UIUX_SweetAlert2_FontAwesome_Standard.md`
   - `docs/desktop.html`, `docs/mobile.html` (nếu có; tham chiếu giao diện Google Stitch).
   - Các tài liệu quyết định, OPEN questions, changelog, kiến trúc và kế hoạch mới hơn nếu đã có.
3. Nếu tên file khác đôi chút, tìm file tương ứng trong `docs/`; nếu thiếu, báo rõ file thiếu và không tuyên bố đã đọc. Không tự tạo lại nội dung tài liệu nguồn từ suy đoán.
4. Xác định phạm vi công việc, trạng thái pha hiện tại, phụ thuộc, rủi ro và các quyết định còn OPEN. Thực hiện thay đổi nhỏ, có thể kiểm tra được.

## 2. Thứ tự ưu tiên và xử lý xung đột

- Quy tắc an toàn, bảo mật, pháp lý và quyết định nghiệp vụ **đã được phê duyệt** trong tài liệu v2.7 là bắt buộc; không được tự sửa hay suy diễn chính sách.
- Tài liệu `SangRental_UIUX_SweetAlert2_FontAwesome_Standard.md` là **quy chuẩn UI/UX bổ sung đã được chủ dự án yêu cầu**, ưu tiên áp dụng cho **icon và thông báo** khi tài liệu v2.7 chỉ nêu nguyên tắc chung.
- Các phần khác của Master Prompt và đặc tả v2.7 vẫn giữ nguyên. Nếu có mâu thuẫn thực sự, ghi lại vị trí, tác động và đề xuất; không âm thầm chọn một phương án làm thay đổi nghiệp vụ.
- Không coi `REVIEW DRAFT`, `OPEN`, giả định, ví dụ hoặc đề xuất là quyết định cuối cùng. Đặc biệt giữ nguyên các vấn đề pháp lý, retention giấy tờ, chính sách hoàn tiền và tính khả dụng thực tế của nhà cung cấp chưa được xác minh.
- Không ghi đè tài liệu nguồn v2.7 nếu không được yêu cầu; cập nhật tài liệu dẫn xuất, changelog hoặc tạo bản mới có đánh dấu thay đổi.

## 3. Công nghệ và cấu trúc

- `frontend/`: Next.js App Router, React, TypeScript, Tailwind CSS, ưu tiên shadcn/ui cho các component giao diện thông thường; responsive, SEO, accessibility.
- `admin/`: Blazor Web App trên .NET 10, xác thực và phân quyền thực thi ở backend.
- `backend/`: ASP.NET Core Web API .NET 10, Clean Architecture: `SangRental.Domain`, `SangRental.Application`, `SangRental.Infrastructure`, `SangRental.Api`.
- Database: **SQL Server + EF Core 10**, migrations, giao dịch và concurrency phù hợp. **DBeaver chỉ là công cụ quản trị DB, không thay SQL Server Database Engine**.
- `docs/`: đặc tả, quy chuẩn, quyết định, thiết kế Stitch, kế hoạch và nhật ký triển khai.
- Tôn trọng cấu trúc thực tế đã tạo; không tạo solution/project trùng lặp, không đổi tên hoặc di chuyển hàng loạt khi chưa cần.
- Chỉ cài package cần thiết, tương thích phiên bản thực tế; không tự thêm Redis, JWT, AutoMapper, BCrypt hoặc thư viện khác nếu chưa có lý do kiến trúc.

## 4. Quy chuẩn UI/UX bắt buộc

### 4.1. Font Awesome

- **Font Awesome là thư viện icon chính** cho Next.js Frontend và Blazor Admin.
- Chỉ import các icon sử dụng thực tế; ưu tiên SVG, icon miễn phí có giấy phép phù hợp; không nạp cả bộ icon.
- Đồng nhất kích thước, màu, khoảng cách, trạng thái và accessible name; icon trang trí phải ẩn với screen reader.
- Icon trạng thái mặc định của SweetAlert2 được phép giữ nguyên.

### 4.2. SweetAlert2

- **SweetAlert2 là thư viện duy nhất cho thông báo ứng dụng** trên frontend và admin: success, error, warning, info, confirmation, loading và toast.
- **Không dùng** `window.alert`, `window.confirm`, `window.prompt`, Sonner, React Hot Toast, React Toastify, shadcn/ui Toast hoặc thư viện thông báo khác.
- Tập trung lời gọi SweetAlert2 trong adapter/service, **không gọi `Swal.fire` rải rác trong component**.
- API logic thống nhất: `success(message)`, `error(message)`, `warning(message)`, `info(message)`, `confirm(options)`, `loading(message)`, `closeLoading()`, `toast(message, type)`.
- Next.js: NotificationService TypeScript, chạy client-side phù hợp App Router, tránh SSR/hydration errors.
- Blazor: NotificationService C# qua JS Interop, quản lý vòng đời interop an toàn.
- `confirm()` phải trả kết quả xác nhận/hủy rõ ràng, không tự thực thi hành động nghiệp vụ. Các hành động nhạy cảm cần backend authorization, audit và idempotency/chống double-submit.
- Validation lỗi từng trường hiển thị **inline**; form, tooltip, menu, date picker, hộp thoại nội dung phức tạp vẫn dùng UI component phù hợp. SweetAlert2 **không** thay mọi modal/component.
- Chỉ thông báo thanh toán/hoàn tiền thành công khi trạng thái đã được backend/nguồn có thẩm quyền xác minh; không coi callback client hay đóng popup là xác nhận nghiệp vụ.
- Không đưa CCCD/GPLX, OTP, token, dữ liệu nhạy cảm vào thông báo hoặc log.
- Dùng i18n keys, **không hard-code** chuỗi thông báo; hỗ trợ `vi` (mặc định), `en`, `ko`, `zh-CN`, `ja`, `th`.
- Thiết kế Coastal Luxury: Ocean Blue `#087EA4`, Turquoise `#16B8B0`, Sand Gold `#EBCB83`, Deep Navy `#123C50`, Warm Ivory `#FFF9EE`; responsive, keyboard/accessibility, animation tiết chế.
- Đọc file chuẩn UI/UX trong `docs/` để biết đầy đủ hành vi, ngoại lệ và kiểm thử; mục này là tóm tắt, **không thay thế tài liệu gốc**.

## 5. Các bất biến nghiệp vụ cần bảo vệ

- Khách chọn **mẫu xe**, không chọn xe vật lý/biển số; card có `Liên hệ` (inquiry, không giữ chỗ/thu tiền) và `Đặt ngay` (booking).
- Hold booking 15 phút; chỉ xác nhận khi backend xác minh thanh toán cần thiết **và** phân bổ xe thành công. Không để overlap lịch thuê, buffer hay xe bảo trì.
- Tiền trả trước online là **một phần hoặc toàn bộ tiền thuê sau giảm**, không mặc nhiên là tiền thế chân riêng.
- SePay webhook cần kiểm chứng provider, đối soát và idempotency; Brevo email qua outbox; không giả định tích hợp thật đã hoạt động.
- Quyền xác nhận tiền mặt, hoàn tiền, bàn giao, offline và audit phải tuân thủ các quyết định BR trong đặc tả; không tự suy diễn.
- Không tự đặt tỷ lệ hoàn tiền, thời hạn lưu CCCD/GPLX, cam kết pháp lý, dữ liệu review, giá hay lời hứa dịch vụ.

## 6. Quy trình triển khai và kiểm chứng

1. Theo đúng pha trong Master Prompt: audit/plan → scaffolding/schema → booking/pricing/inventory/payment → handover/return → admin/CMS/i18n/notifications → security/QA/deployment.
2. Nếu đang ở **pha khởi tạo**, chỉ scaffold nền tảng, thiết lập dependencies, DI, cấu hình, NotificationService cơ bản và smoke tests; **chưa** tích hợp SePay live, booking production hay xử lý dữ liệu khách thật.
3. Không ghi đè file đang tồn tại, không xóa dữ liệu, không tự chạy destructive migrations hoặc deploy production.
4. Cấu hình secrets qua user-secrets/env/secret manager; commit `.env.example`, không commit `.env`, keys, passwords, OTP, giấy tờ khách.
5. Sau mỗi thay đổi: chạy những kiểm tra phù hợp (`dotnet restore/build/test`, `npm install`/`npm run build`/lint, migrations validation nếu khả dụng). Ghi rõ lệnh đã chạy và kết quả; không báo thành công khi chưa chạy.
6. Kiểm thử SweetAlert2: confirm/cancel, loading, error, toast, i18n, accessibility, double-submit và trạng thái thanh toán chưa được xác minh. Kiểm thử backend: authz, concurrency, idempotency và luồng tiền/xe theo đặc tả.
7. Cập nhật tài liệu tiến độ, quyết định và OPEN questions. Khi kết thúc, báo cáo **file đã tạo/sửa, package đã thêm, lệnh kiểm thử và kết quả, vấn đề còn OPEN, bước tiếp theo**.

## 7. Chỉ dẫn cho Codex

- **Đọc `AGENTS.md` và các tài liệu `docs/` liên quan trước khi viết code.**
- Ưu tiên tiếp tục công việc đã có thay vì tạo lại từ đầu.
- Không hỏi lại những quyết định đã có trong tài liệu. Chỉ yêu cầu người dùng quyết định khi có blocker thực sự liên quan pháp lý, an toàn hoặc nghiệp vụ chưa chốt.
- Nếu thiếu một tài liệu hoặc công cụ, nêu giới hạn chính xác, tiếp tục các phần độc lập an toàn.
- Luôn phân biệt: **đã triển khai**, **đã kiểm thử**, **mới lên kế hoạch**, **chưa xác minh**.
