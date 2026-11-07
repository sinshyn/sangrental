# Bảng công việc

| Ticket | Trạng thái | Acceptance / phụ thuộc |
|---|---|---|
| INIT-01 Audit | DONE | Đọc 4 nguồn, kiểm công cụ/git |
| INIT-02 Backend | DONE | 4 project, references/DI, OpenAPI/ProblemDetails/health |
| INIT-03 Persistence | DONE scaffold | EF10 SQL Server, migration rỗng/script, chưa apply DB |
| INIT-04 Frontend | DONE scaffold | App Router/TS/Tailwind/UI structure và preview shell |
| INIT-05 Admin | DONE scaffold | Blazor layout/cookie/policy, login khóa, không default account |
| INIT-06 Verification/docs | DONE trong phạm vi scaffold | Build, HTTP smoke, migration checks và tài liệu; xem PROGRESS_LOG |
| INIT-07 Notification/icon foundation | DONE scaffold | SweetAlert2 services, sáu locale, Font Awesome chọn lọc, unit tests; browser/a11y còn TODO |
| INIT-08 Đối chiếu quy chuẩn UI/UX | DONE scaffold | Phụ lục v2.7, design system, shared CSS, loading reject/unknown key tests |
| AUDIT-01 Phase 1 readiness | DONE có điều kiện | PHASE_1_READINESS_REPORT.md; sửa lỗi EventLog, build sạch, 11 logic/policy + 12 HTTP PASS; SQL/browser/identity còn chưa xác minh |
| AUDIT-02 Baseline Git và lint | TODO | Review scaffold untracked để đưa vào Git có chủ đích; thêm lint trước mở rộng frontend; audit không commit |
| DB-01 SQL connection | DONE | EF10/DI/CanConnectAsync; User Secrets; live/ready 200, thiếu config và DB unreachable 503; chưa apply schema |
| UI-QA-01 | TODO trước màn hình tương tác | Browser keyboard/focus/screen reader, responsive, SSR/hydration, Blazor disconnect, offline/timeout và đo bundle |
| NEXT-01 Identity | TODO | Chốt provider/provisioning/MFA/revocation trước admin thật |
| NEXT-02 Schema/catalog | TODO | Khóa DTO/schema/FK/index, có SQL Server dev/test |
| NEXT-03 Booking/pricing/inventory/payment | OUT OF SCOPE | Sau hợp đồng module, rounding và SQL concurrency tests |
| NEXT-04 Handover/return/extension/incidents | OUT OF SCOPE | Sau NEXT-03, giữ OPEN/CR |
| NEXT-05 Admin/CMS/i18n/notifications | OUT OF SCOPE | Backend permission, provider adapters/outbox |
| NEXT-06 Security/QA/deployment | OUT OF SCOPE | AT-01…24 + pháp lý, sandbox, backup/rollback |

Không tự chuyển sang các ticket nghiệp vụ trong yêu cầu hiện tại.
