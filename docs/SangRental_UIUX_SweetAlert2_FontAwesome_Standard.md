# SANGRENTAL — GLOBAL UI/UX STANDARD: SWEETALERT2 & FONT AWESOME

> **Trạng thái:** Quy chuẩn bắt buộc để tích hợp vào SangRental Master Prompt và tài liệu đặc tả.  
> **Phạm vi:** Next.js / React Frontend và Blazor Admin.  
> **Nguyên tắc:** Một hệ thống thông báo thống nhất, một thư viện icon chính, giao diện Coastal Luxury, an toàn nghiệp vụ.

## 1. Thư viện và phạm vi sử dụng

### 1.1. SweetAlert2 — thư viện thông báo duy nhất

Sử dụng **SweetAlert2** làm thư viện duy nhất cho tất cả **thông báo ứng dụng** trên:

- Next.js / React Frontend.
- Blazor Admin.

**Không sử dụng** cho mục đích thông báo ứng dụng:

- JavaScript `alert()`, `confirm()`, `prompt()`.
- Sonner, React Hot Toast, React Toastify.
- `shadcn/ui Toast` hoặc các thư viện toast/popup thông báo khác.

**Phân định phạm vi:** SweetAlert2 chịu trách nhiệm cho thông báo, xác nhận, toast và trạng thái đang xử lý. Validation theo từng trường phải hiển thị **inline**; form, date picker, tooltip, menu và các hộp thoại nội dung phức tạp vẫn dùng component UI phù hợp (ví dụ shadcn/ui trên Next.js). Không thay mọi component giao diện bằng SweetAlert2.

### 1.2. Font Awesome — thư viện icon chính

- Sử dụng **Font Awesome** làm thư viện icon chính trên Next.js Frontend và Blazor Admin.
- Dùng một bộ icon và quy ước phong cách nhất quán; ưu tiên icon dạng **SVG** phù hợp với từng nền tảng.
- Chỉ import các icon thực sự dùng; **không** import toàn bộ icon pack hoặc tải cả bộ icon nếu không cần.
- Ưu tiên icon miễn phí có giấy phép sử dụng phù hợp; không phụ thuộc Font Awesome Pro nếu chưa được cấp phép.
- Thiết lập kích thước, màu, khoảng cách và trạng thái hover/disabled thống nhất với design system.
- Icon trang trí phải ẩn khỏi công nghệ hỗ trợ đọc màn hình; icon mang ý nghĩa hoặc nút chỉ có icon phải có nhãn truy cập (`aria-label`, accessible name hoặc tương đương).
- Không dùng icon đơn lẻ để truyền đạt lỗi/trạng thái: kết hợp văn bản rõ ràng.
- SweetAlert2 sử dụng icon trạng thái tích hợp (`success`, `error`, `warning`, `info`) hoặc tùy chỉnh có kiểm soát; **không bắt buộc thay icon trạng thái nội bộ của SweetAlert2 bằng Font Awesome**.

## 2. NotificationService thống nhất

Không gọi `Swal.fire()` trực tiếp rải rác trong các page/component. Tập trung việc gọi SweetAlert2 vào một adapter/wrapper dùng chung.

API logic thống nhất:

- `success(message)`
- `error(message)`
- `warning(message)`
- `info(message)`
- `confirm(options)` — trả về kết quả xác nhận/hủy rõ ràng, không tự thực thi nghiệp vụ.
- `loading(message)`
- `closeLoading()`
- `toast(message, type)`

Yêu cầu triển khai:

- **Next.js:** TypeScript service, gọi SweetAlert2 ở **client-side** phù hợp App Router; tránh lỗi SSR/hydration.
- **Blazor Admin:** C# service gọi SweetAlert2 qua **JavaScript Interop**, có quản lý vòng đời và xử lý lỗi interop phù hợp.
- Có cấu hình mặc định tập trung cho giao diện, nút bấm, thời gian hiển thị, vị trí toast, hành vi đóng popup.
- Chống thông báo lặp và tránh xung đột giữa loading, toast và confirmation; không tự động đóng nhầm popup của tác vụ khác.
- Không đưa HTML không tin cậy vào popup; ưu tiên truyền nội dung dưới dạng text, tránh XSS.

## 3. Quy tắc hiển thị thông báo

| Tình huống | Cách hiển thị |
|---|---|
| Thành công | SweetAlert2 `success` hoặc toast |
| Thất bại | SweetAlert2 `error` |
| Cảnh báo | SweetAlert2 `warning` |
| Thông tin | SweetAlert2 `info` |
| Xóa dữ liệu, hủy đơn, hoàn tiền, điều chỉnh giá | SweetAlert2 confirmation |
| Đang gửi yêu cầu | SweetAlert2 loading |
| Thao tác nhanh, ít quan trọng | SweetAlert2 toast |
| Lỗi validation của một trường | Hiển thị inline cạnh trường; không bật popup cho từng lỗi nhỏ |

- Các thao tác tài chính hoặc không thể đảo ngược phải xác nhận rõ ràng, kiểm tra quyền ở backend và có cơ chế chống gửi yêu cầu trùng lặp (idempotency khi phù hợp).
- Nút hủy, nút đóng và hành vi nhấn ngoài popup phải được định nghĩa theo mức độ rủi ro; không cho phép vô tình đóng popup đang xử lý tác vụ quan trọng.
- Việc người dùng đóng popup **không** được hiểu là đã phê duyệt hành động.
- Không hiển thị thông báo thành công dựa riêng vào thao tác click hoặc kết quả phía client.

## 4. Thiết kế Coastal Luxury

Áp dụng cùng design system cho SweetAlert2 và Font Awesome:

| Token | Mã màu | Vai trò |
|---|---|---|
| Primary Ocean Blue | `#087EA4` | Hành động chính, điểm nhấn |
| Tropical Turquoise | `#16B8B0` | Điểm nhấn phụ |
| Sand Gold | `#EBCB83` | Điểm nhấn cao cấp |
| Deep Navy | `#123C50` | Chữ, tiêu đề, độ tương phản |
| Warm Ivory | `#FFF9EE` | Nền sáng ấm |

Yêu cầu:

- Popup sang trọng, tối giản, đồng nhất với website; không lạm dụng animation.
- Responsive trên desktop và mobile; bố cục, bo góc, typography và khoảng cách nhất quán.
- Bảo đảm độ tương phản, focus hiển thị rõ, hỗ trợ bàn phím và công nghệ hỗ trợ.
- Màu trạng thái lỗi/cảnh báo phải dễ phân biệt và dễ đọc; không phụ thuộc chỉ vào màu.
- Font Awesome có quy chuẩn kích thước, độ dày nét, căn chỉnh và trạng thái tương tác.

## 5. Đa ngôn ngữ

Tất cả nội dung thông báo, xác nhận và nhãn nút phải hỗ trợ đủ sáu ngôn ngữ:

- `vi` — Tiếng Việt (mặc định).
- `en` — English.
- `ko` — 한국어.
- `zh-CN` — 简体中文.
- `ja` — 日本語.
- `th` — ภาษาไทย.

Không hard-code chuỗi hiển thị trong NotificationService. Dùng khóa dịch (translation keys) và hệ thống i18n thống nhất; có fallback rõ ràng khi thiếu bản dịch.

## 6. Bảo mật và quy tắc nghiệp vụ

- Chỉ hiển thị **thanh toán thành công** sau khi backend xác minh trạng thái thanh toán đáng tin cậy.
- Chỉ hiển thị **hoàn tiền thành công** khi có trạng thái xác nhận thích hợp từ hệ thống thanh toán/ngân hàng; nếu còn xử lý, hiển thị trạng thái đang xử lý.
- Không đưa CCCD, token, OTP, thông tin thanh toán nhạy cảm hoặc dữ liệu cá nhân không cần thiết vào thông báo.
- Phân biệt lỗi mạng, validation, nghiệp vụ, quyền truy cập và lỗi hệ thống; không tiết lộ stack trace hoặc bí mật hệ thống cho người dùng.
- SweetAlert2 chỉ là lớp giao diện: mọi kiểm tra quyền, giao dịch, đối soát, concurrency và idempotency phải được thực hiện ở backend.

## 7. Quy chuẩn triển khai và kiểm thử

1. Cập nhật **Master Prompt**, **đặc tả kỹ thuật**, **design system** và **coding conventions** trước khi triển khai chức năng mới.
2. Tất cả chức năng mới phải dùng NotificationService; không gọi trực tiếp SweetAlert2 trong business component.
3. Kiểm thử các luồng: `confirm`, `cancel`, `loading`, `closeLoading`, `success`, `error`, `warning`, `info`, `toast`.
4. Kiểm thử i18n đủ sáu ngôn ngữ, bàn phím, focus, screen reader, responsive và SSR/hydration của Next.js.
5. Kiểm thử double-submit, idempotency, thông báo trùng, mất mạng, timeout và các trạng thái thanh toán/hoàn tiền chưa xác minh.
6. Kiểm thử bundle size và bảo đảm Font Awesome không bị import toàn bộ pack; kiểm tra nhãn truy cập của các nút icon-only.
7. Không tự ý bổ sung thư viện thông báo hoặc icon thứ hai khi chưa được phê duyệt.

## 8. Tiêu chí hoàn thành (Acceptance Criteria)

- [ ] Next.js Frontend và Blazor Admin đều dùng SweetAlert2 cho mọi thông báo ứng dụng.
- [ ] Có NotificationService tập trung, không có `alert()`/`confirm()`/`prompt()` mặc định hoặc thư viện thông báo khác.
- [ ] Lỗi validation từng trường vẫn hiển thị inline.
- [ ] Font Awesome là thư viện icon chính; chỉ import icon sử dụng thực tế.
- [ ] SweetAlert2 và icon tuân thủ Coastal Luxury, responsive và accessibility.
- [ ] Nội dung thông báo hỗ trợ `vi`, `en`, `ko`, `zh-CN`, `ja`, `th`.
- [ ] Không có thông báo thanh toán/hoàn tiền thành công trước khi backend xác minh.
- [ ] Các luồng xác nhận nguy hiểm được kiểm tra quyền và chống double-submit ở backend.
- [ ] Có test tự động phù hợp và kết quả build/test được báo cáo.

---

**Chỉ dẫn cho Codex:** Xem tài liệu này là quy chuẩn UI/UX bắt buộc. Khi tích hợp vào Master Prompt, giữ nguyên các quyết định nghiệp vụ đã được phê duyệt và không tự ý chốt các vấn đề đang OPEN/REVIEW. Nếu phát hiện mâu thuẫn, liệt kê rõ và yêu cầu xác nhận trước khi thay đổi quyết định nghiệp vụ.
