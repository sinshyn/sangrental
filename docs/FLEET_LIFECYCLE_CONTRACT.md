# Hợp đồng vòng đời xe — lát 12/11/2026

Phạm vi CF-01/CF-03: sửa hồ sơ/gán mẫu, đổi biển giữ lịch sử, kích hoạt hồ sơ nháp, ngừng khai thác, khôi phục sau kiểm tra. Theo Q1–7, Q17, Q34–36 và BR-28; không triển khai booking hoặc xử lý xe mất/tai nạn.

| POST `/api/admin/fleet/vehicles/{id}/…` | Từ → đến | Điều kiện |
|---|---|---|
| `activate` | Draft/PendingInspection → Active/Ready | Admin, xác nhận kiểm tra đạt, mẫu đang Active |
| `retire` | Active → Retired | Admin; không còn đợt Planned/InProgress; giữ nguyên operational status và mọi lịch sử, xóa mốc Ready hiện hành |
| `restore` | Retired → Active/Ready | Admin, kiểm tra đạt, mẫu Active; không LostLocked/AccidentLocked; không có bảo trì chưa hoàn tất đã đến giờ |

Request whitelist `{reason,confirmInspection}`; reason bắt buộc 1–1000 ký tự sau trim; activate/restore bắt buộc confirmInspection=true. Không nhận actor/status từ client. Mọi POST cần HTTPS/cookie/CSRF và If-Match Base64 rowversion xe 8 byte bọc nháy kép. GET `/api/admin/fleet/vehicles/{id}` trả hồ sơ + rowVersion hiện hành cho Admin hoặc Manager có fleet.manage. Không tìm thấy: 404; lỗi nghiệp vụ: 409; thiếu version: 428; version sai định dạng: 400; cũ: 412; mutation thành công: 204.

Transaction Serializable + khóa cùng resource bảo trì theo xe trước đọc/reload/version check, để retire/restore/activate không cạnh tranh sai với schedule/release. Thay đổi hồ sơ, kiểm tra Passed (InitialReadiness/Reactivation), history đổi operational status (nếu có), audit actor/lý do cùng commit. Không tự retry; replay với version cũ bị chặn; không có ledger Idempotency-Key.

Guard kỹ thuật retire chặn mọi đợt bảo trì còn mở, kể cả Planned: tránh kẹt hồ sơ vì complete maintenance đòi xe Active và restore không được vượt bảo trì đã đến giờ. Xử lý xong bảo trì trước khi ngừng khai thác; không tự hủy đợt hoặc xóa checklist.

Admin SSR POST dùng cùng service/guards, version từ form cũ, không thay bằng version vừa đọc. Xác nhận SweetAlert2 trước submit, loading/chống gửi lặp qua adapter hiện có; validation inline; success sau commit. Kích hoạt/khôi phục cần checkbox kiểm tra, lý do và confirmation. Xe Retired vẫn xem được bảo trì/lịch sử; Ready không chứng minh lịch thuê khả dụng.

## Hồ sơ và biển số

- PUT `/vehicles/{id}/profile`: `{modelId,colorName?,manufacturingYear?,vin?,reason,duplicateVinReason?}`. Model phải Active, năm nếu nhập là số nguyên dương trong smallint; không coi là xác minh pháp lý. Mã tài sản bất biến. VIN tùy chọn; đổi thành VIN trùng cần lý do riêng. Khóa VIN toàn fleet trước khóa xe, chung khóa create. Audit ProfileUpdated/lý do, ModelReassigned ghi ID cũ→mới, VinOverrideAcknowledged ghi lý do override; giữ mọi inspection/maintenance/history. Không có rental snapshot trong scope này; module booking sau phải lưu snapshot độc lập.
- GET `/vehicles/{id}/registrations?page=1`: lịch sử mới nhất trước, 50/trang, quyền đọc Fleet. POST cùng path: `{jurisdictionCode,plate,reason}` với version của **xe**. Mã quốc gia hai chữ ASCII hoa; plate tối đa 32 ký tự, chỉ chữ ASCII/số/khoảng trắng/chấm/gạch ngang. So sánh bỏ ba dấu phân cách và uppercase; đây là chuẩn kỹ thuật so trùng, không xác nhận định dạng đăng ký hợp pháp. Không nhận normalizedPlate hoặc thời điểm hiệu lực từ client.
- Đổi biển hiệu lực ngay theo UTC server. Khóa xe rồi khóa registration toàn fleet, kiểm unique `(quốc gia,biển chuẩn hóa)` hiện hành. Đóng row cũ (ValidTo/ClosedAt/actor/reason), save rồi thêm row mới và bump version xe cùng transaction. Không sửa biển/ValidFrom cũ, không backdate, không tự gỡ biển xe khác/xe retired. Cùng biển hiện hành: 409 registration_unchanged; trùng xe khác: 409 plate_in_use; khoảng thời gian không dương: 409 registration_time_conflict. Hai xe tranh một biển chỉ một xe thành công; stale version 412. Không có API xóa registration.
- DbContext chỉ cho phép đóng interval hiện hành đầy đủ (end>start, ClosedAt/actor/reason); chặn sửa identity/plate/start/creation và chặn sửa/xóa row đã đóng. Đây là guard EF, không thay SQL least privilege trước production.
- SSR `/fleet/vehicles/{id}` có form profile/registration và lịch sử biển phân trang độc lập; initial profile chỉ điền từ DB trên GET, POST giữ nguyên input/version người dùng. Các mutation vẫn cần Admin/HTTPS/antiforgery/confirmation, lý do, version và transaction như lifecycle.

Giới hạn: chưa retire/restore model, worker Planned hoặc inventory. Trước module allocation/booking phải phối hợp khóa/guard retirement và sửa gán model với các phân bổ có hiệu lực; hiện chưa có các bảng này. Xe LostLocked/AccidentLocked không được mở khóa qua restore; dành cho module incident đã xác minh.
