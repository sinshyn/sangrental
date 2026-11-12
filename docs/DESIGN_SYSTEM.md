# Design system Coastal Luxury — bổ sung ngày 2026-11-07

Nguồn bắt buộc: [quy chuẩn SweetAlert2 và Font Awesome](important/SangRental_UIUX_SweetAlert2_FontAwesome_Standard.md). Bổ sung cho Master Prompt/đặc tả v2.7, không thay nội dung nguồn hoặc chính sách nghiệp vụ.

## Màu và bố cục

| Token CSS | Màu | Sử dụng |
|---|---|---|
| --coastal-ocean | #087EA4 | CTA, focus ring, nút xác nhận |
| --coastal-turquoise | #16B8B0 | Điểm nhấn phụ; không mặc định dùng làm màu chữ nhỏ |
| --coastal-sand | #EBCB83 | Accent, nhãn trên nền navy |
| --coastal-navy | #123C50 | Chữ, nút hủy, sidebar |
| --coastal-ivory | #FFF9EE | Nền và popup |

Nguồn CSS dùng chung: `frontend/src/design/coastal.css`. Script `scripts/prepare-ui-assets.mjs` sao chép nguyên bản sang admin vendor cùng notification core và sáu locale. Không sửa trực tiếp bản sinh trong vendor. Tailwind/shadcn semantic tokens map sang bộ màu này; các surface xanh từ Stitch vẫn được giữ làm màu hỗ trợ cho hero/card.

Giữ bố cục responsive đã scaffold, font system hiện tại và card bo góc. Popup bo góc 16px, nút cao tối thiểu 44px, focus ring rõ, nội dung dài tự ngắt dòng, mobile có padding; animation tắt để tránh chuyển động không cần thiết. Việc có CSS hỗ trợ không thay thế kiểm tra accessibility bằng trình duyệt.

## Icon

Font Awesome free SVG là nguồn chính. Next import tên icon thực dùng (`faCompass`), không import namespace/whole pack hoặc `library.add(fas)`. Admin hiện chỉ xuất `faGauge` thành SVG tự host. Kích thước chuẩn 16px ở navigation; màu theo text/surface, icon trang trí `aria-hidden`, nút chỉ icon phải có accessible name. Không dùng riêng icon hoặc màu để thông báo lỗi. License được giữ trong vendor. Icon trạng thái tích hợp của SweetAlert2 được phép dùng.

## Thông báo và UI component

SweetAlert2 là lớp duy nhất cho success/error/warning/info/confirmation/loading/toast. Không Sonner, React Hot Toast, React Toastify, shadcn Toast hoặc browser alert/confirm/prompt trong mã ứng dụng. Hai HTML Stitch là tài liệu tham chiếu lưu nguyên trạng, không được phục vụ như mã ứng dụng.

shadcn/ui và Tailwind tiếp tục dành cho form, menu, tooltip, date picker, card và modal nội dung phức tạp. Validation theo trường hiển thị inline; không bật popup từng lỗi field. Error boundary/trang lỗi hệ thống có thể giữ nội dung inline để hoạt động khi JavaScript/circuit không hoạt động; đây không phải một hệ thống toast thứ hai.

## NotificationService

- Next: `src/lib/notification-service.ts`, client-only, dynamic import SweetAlert2 khi dùng. Không truy cập browser lúc import module SSR.
- Blazor: `Services/NotificationService.cs`, DI scoped, JS module lazy import; chỉ gọi sau interactive render. Không gọi lúc prerender. Dispose module khi hết circuit, xử lý JSDisconnectedException lúc dispose; lỗi invoke được truyền cho caller xử lý.
- Shared core: `src/lib/notification-core.ts`. Chỉ nhận translation key; không nhận HTML, dữ liệu khách, exception hoặc raw server response. Key không tồn tại bị từ chối, không hiển thị nội dung đầu vào.
- API: success/error/warning/info(key), confirm({message:key}) ở TS; Confirm(key) ở C#; toast(key,type), loading(key), closeLoading(token). C# dùng PascalCase theo conventions .NET.
- Loading trả token sở hữu; closeLoading(token) là dạng cụ thể hóa API logic closeLoading() để không đóng nhầm tác vụ. Chỉ token đúng được đóng loading hiện hành. Không gọi closeLoading không điều kiện trong component khác.
- Khi popup/loading đang mở, lời gọi chồng bị bỏ qua; confirm trả false, loading trả null. Chưa có hàng đợi thông báo. Caller vẫn phải có pending state và idempotency cho tác vụ backend.
- Confirm chỉ trả kết quả, đóng/Escape/cancel không phê duyệt nghiệp vụ. Nút hủy được focus mặc định, không đóng bằng click ngoài. Loading không cho Escape/click ngoài.
- Lỗi tải engine được truyền về caller và nhả khóa. Promise hiển thị loading bị reject sẽ nhả đúng khóa sở hữu, không tạo unhandled rejection hoặc mở khóa tác vụ mới.

## Ngôn ngữ và an toàn nghiệp vụ

`src/messages/notifications.json`: vi mặc định; en, ko, zh-CN, ja, th. Nhãn OK/cancel và toàn bộ nội dung thông báo lấy từ dictionary, fallback VI. Chỉ hỗ trợ thông báo nền tảng hiện tại; chưa tuyên bố toàn website đã đa ngôn ngữ hoặc bản dịch đã được người bản ngữ duyệt.

Không có thông báo payment/refund success trong scaffold. Khi làm module phải phân biệt lỗi mạng/validation/nghiệp vụ/quyền/hệ thống bằng key riêng, và chỉ phát success sau trạng thái backend đáng tin cậy. Popup không thay authentication, authorization, transaction, reconciliation, audit hoặc idempotency. OPEN/CR không đổi.

## Gate kiểm tra tiếp theo

Lát Admin Fleet dùng form SSR + HTML validation và thông báo qua NotificationService. Module `fleet-forms.js` confirm trước Ready/Skipped/release, loading và khóa submit lặp; hủy không gửi. Lỗi adapter có inline boundary hoạt động khi popup không tải được. Các trường/label/checkbox có tên truy cập, bảng overflow và form stack trên mobile. Native POST vẫn có server validation/CSRF/authorization, không dựa vào JavaScript để bảo vệ nghiệp vụ. 4 tests logic form đã có, browser DOM/focus/screen reader vẫn cần kiểm thật.

Đã có unit test logic và HTTP smoke/assets. Trước màn hình tương tác đầu tiên: browser confirm/cancel/loading/toast, SSR/hydration, keyboard, focus return, screen reader, desktop/mobile, double-submit, offline/timeout, Blazor disconnect và đo bundle/tree shaking. Chưa đánh dấu các gate browser này hoàn thành.
