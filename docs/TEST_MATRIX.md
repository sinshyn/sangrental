# Ma trận kiểm thử nền tảng

Đối chiếu UI/UX bổ sung: `scripts/ui-policy.test.mjs` kiểm dependency được phép, không Swal.fire/browser popup trong component, không whole-pack import, dictionary sáu locale đầy đủ và assets/CSS admin đồng bộ. Unit core bổ sung unknown key, loading promise reject và stale failure không mở khóa tác vụ mới. Chạy cả hai bằng `node --test scripts/notifications.test.mjs scripts/ui-policy.test.mjs`. Policy tests chỉ là guard mã nguồn có phạm vi, không thay code review hoặc kiểm tra runtime/browser.

Notification core: `node --test scripts/notifications.test.mjs` kiểm confirm/cancel, sáu locale/fallback/text, khóa loading và ownership, success/error/warning/info/toast, double-submit và khôi phục khi loader lỗi. Đây là kiểm thử logic bằng popup engine giả, chưa phải trình duyệt thực. Chưa xác minh keyboard/focus/screen reader/responsive/SweetAlert DOM/Blazor circuit disconnect bằng browser; cần làm khi triển khai màn hình tương tác đầu tiên. Chưa có payment/refund message nên không phát thông báo tài chính giả. Smoke bổ sung kiểm các assets JS và locale admin phục vụ được cho anonymous.

| ID | Given/When | Expected |

|---|---|---|

| SC-01 | Restore/build solution | 5 project, 0 warning/error |

| SC-02 | TS/build frontend | Typecheck và production build pass |

| SC-03 | API thiếu config DB | live 200, ready 503 |

| SC-04 | API Development | OpenAPI 200 JSON hợp lệ |

| SC-05 | GET missing API | 404 ProblemDetails |

| SC-06 | Admin anonymous | login 200, dashboard 302, auth/session 401 |

| SC-07 | Frontend startup production | / 200, lang vi, thông báo chưa nhận booking |

| SC-08 | EF tooling | Sinh script và không pending model changes, chưa apply DB |

HTTP assertions: scripts/smoke.mjs khởi động ứng dụng tạm và dừng sau kiểm tra, kết quả artifacts/smoke-results.json. Chưa SQL Server nên chưa readiness happy-path/migration apply. Chưa account nên chưa kiểm authenticated role/revocation. Chưa browser visual QA/E2E.

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
