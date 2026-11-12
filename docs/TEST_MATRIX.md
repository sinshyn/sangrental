# Ma trận kiểm thử nền tảng

Checkpoint **13/11 ảnh**: `--media-only` **33 PASS** (native codec/EXIF/alpha/JPEG, GUID v7 unique concurrent writes, preserved/encrypted private bytes, malformed/oversize/extension/path/disabled); SQL runner **246 PASS** (bao gồm upload/list/preview/rights/primary race/rowversion/private gate/immutable/ETag 304/DB failure cleanup/native SSR/oversized inline và role guards); Catalog guard **36 PASS**; JS **15 PASS**; HTTP smoke **18 PASS**. .NET build 0 warning/error; restore locked PASS, EF không pending changes, NuGet audit Infrastructure không vulnerable package. CI thêm media-only, remote chưa chạy. Không thay browser/manual/native Linux tests bằng TestServer; frontend không đổi, Next build/lint/typecheck không chạy lại. Xem [lệnh và bằng chứng](../Changes/2026-11-13-upload-anh-guid-v7-cache.md#kiểm-thử-thực-tế-và-cách-chạy).

Checkpoint **12/11/2026**: **200 Identity/API/SSR SQL checks + 36 guards PASS**, **15 notification/policy/form + 16 HTTP smoke PASS**. `dotnet build SangRental.slnx --no-restore` PASS 0 warning/error; `dotnet ef migrations has-pending-model-changes ... --no-build` xác nhận không đổi model. Test mới ở LifecycleTests.cs/ProfileRegistrationTests.cs: lifecycle versions/replay/permission, hai retire đồng thời, retire vs maintenance, Passed inspection/audit; profile validation/VIN override/reassign, hai xe tranh cùng biển chuẩn hóa, đổi/nhả/tái cấp biển theo quốc gia, interval/history, chặn sửa/xóa lịch sử, SSR validation/post/persistence/stale. Manager có fleet.manage vẫn không được ghi lifecycle/profile/registration. GuardSmoke thêm lost/accident locks, model retired, Planned/InProgress chặn retire. Chỉ SangRentalTestDb được ghi/migrate; Development vẫn 16 Catalog, 0 Identity, 2 migrations. Không thay browser E2E/a11y/mobile hoặc acceptance booking; chứng chỉ localhost chưa trusted. Các checkpoint dưới là lịch sử.

Checkpoint Admin Fleet 11/11: **112 Identity/API/SSR SQL checks + 22 guards PASS**, **15 notification/policy/form tests + 16 HTTP smoke PASS**; .NET build 0 warning/error, lint/typecheck PASS. Bốn ca JS mới dùng DOM giả: cancel không submit, click/Enter lặp, adapter lỗi chặn submit và retry, confirm→loading→submit. Các ca SQL mới xác minh Draft/Ready, audit/inspection, timezone, overlap, checklist/version/confirm/release và Staff. Đây không thay browser E2E/a11y; chứng chỉ localhost vẫn chưa trusted. Kết quả số lượng cũ dưới đây là lịch sử.

Checkpoint mới sau Docker ngày 11/11: **84 Identity/API/SSR SQL checks + 22 Catalog guards PASS**, Development giữ nguyên ranh giới migrations. Clean npm ci, lint/typecheck/build, .NET build, notification/policy 11/11, smoke 14/14 PASS. Audit online 0 sau minimatch override; lint negative case vẫn phát hiện sai liên kết. Admin HTTPS localhost chưa trusted; browser visual/authenticated/a11y và CI remote vẫn chưa xác minh. Các kết quả FAIL/9 moderate ở checkpoint trước bên dưới là lịch sử đã được thay thế.

Checkpoint 11/11/2026: build .NET PASS 0 warning/error, typecheck/build Next PASS, notification/policy 11/11 và HTTP smoke 14/14 PASS. Lint `eslint . --max-warnings=0` PASS với ESLint 10.14.0 và Next config 16.4.5. Hai SQL runners FAIL SqlException; local TCP 1436 không có listener. CI remote và browser vẫn NOT VERIFIED. Audit online: 9 moderate trong dev tooling, 0 high/critical; kết quả offline báo 0 không được dùng thay audit online.

Cập nhật 10/11/2026: solution có **7 project**, gồm 2 console test runners. Smoke hiện 14 assertions, có hồi quy Admin live 200/ready 503 khi thiếu DB. Build .NET, typecheck, 11 notification/policy tests và HTTP smoke PASS trong phiên này. SQL Identity/Guard runners đã thử nhưng FAIL kết nối; chưa xác minh lại SQL/concurrency. Không dùng `dotnet test` thay hai runners. Bằng chứng chi tiết: PROGRESS_LOG.

| Nhóm | Lệnh / kiểm tra | Phạm vi và trạng thái 10/11 |
|---|---|---|
| SQL Identity/API | dotnet run --project tests/SangRental.Identity.Integration --no-build --no-restore | Có code: login/logout/CSRF/HTTPS/lockout/rate-limit/shared cookie/role revocation, tạo catalog, If-Match và concurrency bảo trì; lần chạy mới FAIL tại kết nối/migrate test DB |
| SQL Catalog guards | dotnet run --project tests/SangRental.CatalogFleet.GuardSmoke --no-build --no-restore | Có code: publication gate, quyền, Ready inspection, VIN, draft deletion, checklist/release, audit, rollback và SQL lock; FAIL kết nối mới |
| Browser | keyboard/focus/screen reader/responsive/hydration/disconnect | NOT VERIFIED; không coi HTTP hoặc fake popup engine thay trình duyệt |

Đối chiếu UI/UX bổ sung: `scripts/ui-policy.test.mjs` kiểm dependency được phép, không Swal.fire/browser popup trong component, không whole-pack import, dictionary sáu locale đầy đủ và assets/CSS admin đồng bộ. Unit core bổ sung unknown key, loading promise reject và stale failure không mở khóa tác vụ mới. Chạy cả hai bằng `node --test scripts/notifications.test.mjs scripts/ui-policy.test.mjs`. Policy tests chỉ là guard mã nguồn có phạm vi, không thay code review hoặc kiểm tra runtime/browser.

Notification core: `node --test scripts/notifications.test.mjs` kiểm confirm/cancel, sáu locale/fallback/text, khóa loading và ownership, success/error/warning/info/toast, double-submit và khôi phục khi loader lỗi. Đây là kiểm thử logic bằng popup engine giả, chưa phải trình duyệt thực. Chưa xác minh keyboard/focus/screen reader/responsive/SweetAlert DOM/Blazor circuit disconnect bằng browser; cần làm khi triển khai màn hình tương tác đầu tiên. Chưa có payment/refund message nên không phát thông báo tài chính giả. Smoke bổ sung kiểm các assets JS và locale admin phục vụ được cho anonymous.

| ID | Given/When | Expected |

|---|---|---|

| SC-01 | Restore/build solution | 7 project, 0 warning/error; restore riêng phải ghi kết quả riêng |

| SC-02 | TS/build frontend | Typecheck và production build pass |

| SC-03 | API thiếu config DB | live 200, ready 503 |

| SC-03A | Admin thiếu config DB | live 200 Healthy, ready 503 Unhealthy; không re-execute thành trang 404 |

| SC-04 | API Development | OpenAPI 200 JSON hợp lệ |

| SC-05 | GET missing API | 404 ProblemDetails |

| SC-06 | Admin anonymous | login 200, dashboard 302, auth/session 401 |

| SC-07 | Frontend startup production | / 200, lang vi, thông báo chưa nhận booking |

| SC-08 | EF tooling | Không pending model changes; không apply schema chỉ để kiểm tra. Catalog local applied theo lịch sử; Identity Development chờ review |

HTTP assertions: scripts/smoke.mjs khởi động ứng dụng tạm và dừng sau kiểm tra, kết quả artifacts/smoke-results.json. SQL connection/migration và authenticated flow có bằng chứng lịch sử 07–08/11 trong tài liệu; chưa xác minh lại thành công ngày 10/11. Browser visual QA/E2E vẫn chưa kiểm. CI remote chưa chạy xác minh; workflow hiện không chạy SQL runners.

## Nghiệp vụ tương lai — TẤT CẢ CHƯA TRIỂN KHAI / CHƯA CHẠY

## 11. Acceptance tests — ma trận nền

| ID | Given / When | Then / DB assertion |
|---|---|---|
| AT-01 | Hai khách cùng giữ xe cuối | Không oversell/overlap allocation |
| AT-02 | Thanh toán sau hold hết hạn | Không auto-confirm; payment đối soát |
| AT-03 | Webhook cùng transaction gửi nhiều lần | Một transaction logical, một tác động ledger |
| AT-04 | Nhận tiền nhưng allocate thất bại | Không confirm booking; tiền giữ lịch sử, cảnh báo |
| AT-05 | Staff gọi API confirm cash | 403, không đổi payment |
| AT-06 | Manager/Admin confirm cash lặp | Một xác nhận hợp lệ |
| AT-07 | Cash request bị từ chối | Không cho bàn giao thường |
| AT-08 | Thanh toán gia hạn sau hold hết hạn | Không auto-extend |
| AT-09 | Gia hạn trùng booking sau | Không mở rộng allocation |
| AT-10 | Xe bị tai nạn/mất | Không phân bổ mới |
| AT-11 | Xe tìm lại chưa kiểm tra | Không tự ready |
| AT-12 | Khách từ chối ký trả | Evidence + ngoại lệ, không chữ ký giả |
| AT-13 | Đã trả và kiểm tra nhưng phụ phí disputed | UsageCompleted, finance Disputed |
| AT-14 | Admin tăng/giảm phí | Adjustment mới; transaction gốc bất biến |
| AT-15 | Sync offline event hai lần | Không hai lần handover |
| AT-16 | Offline handover xung đột được xác minh | BR-46, audit cancel, không giả refund |
| AT-17 | Refund attempt unknown | Retry bị chặn |
| AT-18 | Refund verified failed | Cho retry kiểm soát, không vượt approved |
| AT-19 | Hai người duyệt/chuyển refund đồng thời | Không double settlement |
| AT-20 | Brevo gửi thất bại | Payment/booking vẫn đúng; outbox retry |
| AT-21 | Cuộc gọi phê duyệt ngoại tuyến bị giả mạo | Không auto chấp nhận; chờ xác minh |
| AT-22 | Chứng từ chuyển khoản sai/chưa xác minh | Không giả webhook confirmed |
| AT-23 | Hai thiết bị cùng báo đã giao xe | Kiểm tra evidence, không hủy chỉ dựa input chưa tin |
| AT-24 | Đơn đã trả tiền bị hủy do BR-46 | Lưu tiền, thông báo, mở xử lý hoàn; không đánh dấu đã hoàn |

**Bổ sung bắt buộc khi viết test chi tiết:** mỗi case có preconditions, actor/permission, request, expected HTTP/ProblemDetails, DB state, outbox/audit, retry/concurrency/negative case. Test SQL Server thực (không chỉ in-memory), kiểm tra timestamp/buffer, rollback, crash/restart, role revoked, bảo vệ file CCCD, chính sách không xóa lịch sử.


UAT bổ sung: desktop/mobile, keyboard/screen reader, loading/error/empty, múi giờ, quyền vừa thu hồi, private documents, payment sandbox, crash/restart, outbox/retry/offline evidence. Không đánh dấu AT-01…24 pass trong pha này.
