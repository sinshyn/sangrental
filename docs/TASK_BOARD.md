# Bảng công việc

Cập nhật **13/11/2026**. Bảng hiện hành dưới đây thay thế trạng thái scaffold cũ ở cuối file. DONE chỉ áp dụng đúng phạm vi ticket, không có nghĩa production-ready. Số ticket không phải phần trăm hoàn thành project.

## Đợt ưu tiên hiện tại

Checkpoint ảnh 13/11: đã có upload/duyệt/ảnh chính/ẩn/retire mẫu xe trên API/Admin; WebP/JPG + GUID v7, cache public immutable/ETag, draft private. Primitive giấy tờ giữ raw bytes/EXIF và mã hóa private, không mở module khách/handover. PASS 33 media, 246 SQL integration, 36 guards, 15 JS, 18 smoke; build/locked restore/EF model checks đạt, NuGet audit không advisory. Chưa browser/CI remote; chưa UI publish mẫu/category, spec/translation. Không đổi DB hoặc frontend. Xem Changes/2026-11-13-upload-anh-guid-v7-cache.md (từ root).

Checkpoint Fleet lifecycle 12/11: SQL PASS **200 Identity/API/SSR + 36 guards**, **15 notification/policy/form + 16 smoke**; .NET build 0 warning/error, EF không pending model changes. Admin chi tiết xe có sửa hồ sơ/gán mẫu, đổi biển giữ lịch sử, activate/retire/restore. Lint/typecheck/audit frontend là bằng chứng 11/11, không chạy lại vì không đổi frontend/dependencies. Baseline chưa commit/push; browser QA vẫn chờ trust cert, CI remote chưa xác minh. Xem FLEET_LIFECYCLE_CONTRACT.

| Ticket | Ưu tiên | Trạng thái | Công việc / điều kiện hoàn tất |
|---|---|---|---|
| P0-01 Health và HTTP smoke | P0 | DONE | Admin live không phụ thuộc DB; ready giữ 503; smoke cô lập SQL/key ring. Build PASS, smoke 14/14 ngày 10/11; xem PROGRESS_LOG |
| P0-02 Đồng bộ tài liệu | P0 | DONE | Sửa trạng thái Identity/Catalog, API contracts, test matrix, security/deployment; tạo TESTING_IDENTITY_CATALOG, giữ lịch sử và OPEN |
| P0-03 Baseline Git | P0 | REVIEWED, CHƯA COMMIT | Review diff/untracked và chuyển nguồn vào docs/important; không đưa secrets/artifacts vào Git. Chưa commit/push |
| P1-01 SQL verification | P1 | DONE | SangRentalTestDb PASS 84 Identity/API/SSR và 22 guards ngày 11/11; Development vẫn 2 migration, chưa Identity |
| P1-02 Lint frontend/CI | P1 | DONE LOCAL, CI REMOTE CHƯA KIỂM | Lint phù hợp phiên bản thực tế và đưa vào CI; CI remote chưa được xác minh |

## Nền tảng hiện hành

| Ticket cũ | Trạng thái | Phạm vi / giới hạn |
|---|---|---|
| INIT-01…08 | DONE nền tảng | Audit, 4 backend projects, EF/SQL, Next preview, Blazor shell, SweetAlert2/6 locale thông báo/Font Awesome. Browser QA chưa làm |
| AUDIT-01 | DONE có điều kiện | Phase 1 là lịch sử; hồi quy health/smoke đã sửa ở P0-01 |
| AUDIT-02 | TÁCH TICKET | Theo P0-03 và P1-02 |
| DB-01 | DONE theo bằng chứng 07–08/11 | User Secrets/readiness local; P1-01 xác minh lại runtime hôm nay |
| DESIGN-03 / NEXT-02 | SCHEMA + MỘT PHẦN GUARDS/API ĐÃ CÓ | 16 entity; create/list catalog/unit, bảo trì/checklist, audit/locking/If-Match. Chưa đầy đủ CRUD/publication/availability |
| NEXT-01 Identity | ĐÃ TRIỂN KHAI LÁT ĐẦU | Identity SQL + cookie đã duyệt; CLI/revocation/CSRF/lockout có code; migration Identity Development chưa duyệt apply; kiểm mới ở P1-01 |

## Backlog theo thứ tự phụ thuộc

| Ticket | Ưu tiên | Trạng thái | Phần còn lại / phụ thuộc |
|---|---|---|---|
| CF-01 Fleet lifecycle | P2 | DONE LÁT HIỆN TẠI | API + SSR sửa hồ sơ/gán model, đổi biển giữ lịch sử, activate/retire/restore; version/SQL locks/audit/inspection và kiểm quyền, concurrency đã test. Không có booking/allocation; phải phối hợp guard trước module đó. Browser QA theo UI-QA-01 |
| CF-02 Catalog publication | P2 | IN PROGRESS | Đã xong lát ảnh: upload/metadata/quyền/approve/primary/hide/retire và HTTP cache, giữ nguyên schema. Còn spec, translation/revision, sửa catalog và API/UI publish/hide model/category |
| CF-03 Admin Catalog/Fleet | P2 | IN PROGRESS | SSR catalog + xe + bảo trì/checklist + sửa hồ sơ/biển/lifecycle + quản lý ảnh đã có. Tiếp theo phần spec/bản dịch/publication của CF-02; chưa edit catalog đầy đủ. Browser QA chờ trust cert |
| UI-QA-01 Browser QA | P2 | TODO | Keyboard/focus/screen reader, responsive, SSR/hydration, disconnect/offline/timeout và bundle |
| PRICE-01 Hợp đồng giá/buffer | P3 | CHỜ CHỐT CHI TIẾT | Đơn vị thuê, kết hợp giảm/rounding, chọn policy, buffer; không tự chọn chính sách tài chính |
| PRICE-02 Pricing/quote | P3 | TODO | Version giá/discount/phí/rental policy, quote/snapshot; sau PRICE-01 |
| INV-01 Availability/allocation | P3 | TODO | Capacity theo model, buffer, bảo trì, lịch [start,end), transaction/lock + SQL concurrency tests |
| BOOK-01 Inquiry/hold/booking | P4 | TODO | Inquiry không giữ xe/thu tiền; hold 15 phút, hết hạn/hủy; sau PRICE/INV và hợp đồng module |
| PAY-01 Payment sandbox | P4 | TODO | Verified webhook/reconciliation, idempotency, paid-but-no-inventory, cash approval; live chờ OPEN-01 |
| MSG-01 Outbox/Brevo | P4 | TODO | Transactional outbox, retry/backoff/dead-letter; gửi thất bại không rollback booking/payment |
| OPS-01 Handover/return | P5 | TODO | Giấy tờ private, biên bản/evidence/chữ ký/OTP, thu tiền và kiểm tra trả; pháp lý OPEN-07 |
| OPS-02 Extension/replacement/incidents | P5 | TODO | Gia hạn trả trước, thay xe, overdue, xe khóa/khôi phục sau kiểm tra |
| FIN-01 Adjustment/dispute/refund | P5 | TODO / CHỜ POLICY | Ledger bất biến, quyền/audit, unknown không retry; OPEN-05/CR-04 |
| OFF-01 Offline reconciliation | P5 | TODO / CHỜ QUYẾT ĐỊNH | BR-39/42/44/45/46 và evidence; CR-01…03 vẫn mở |
| WEB-01 Website khách | P6 | TODO | Giao diện Stitch + API thật, CTA Liên hệ/Đặt ngay, giá/lịch minh bạch; hiện chỉ preview |
| CMS-01 CMS/i18n/integrations | P6 | TODO | FAQ/bài viết/banner, Translation/cache/override, Maps/review thật, dữ liệu OPS |
| SEC-01 Identity production | P7 | TODO | MFA/recovery/user/quyền, protected shared key ring, SQL ít quyền; không hỏi lại provider |
| QA-01 Acceptance/E2E | P7 | TODO | AT-01…24, browser/security/load/concurrency; tests hiện có không thay kiểm module chưa viết |
| DEPLOY-01 Staging/go-live | P7 | TODO | TLS/proxy/secrets/backup-restore/monitoring/rollback và các OPEN liên quan |

## Tiếp tục phiên sau

Đọc mục mới nhất trong PROGRESS_LOG, kiểm git status; hoàn tất ticket IN PROGRESS trước. Sau mỗi lát ghi file/package/lệnh/kết quả/giới hạn/ticket tiếp theo và cập nhật bảng này. Không tự apply Identity migration vào SangRentalDb hoặc bật tiền thật.

## Bảng scaffold lịch sử — được thay thế bởi trạng thái hiện hành phía trên

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
| DESIGN-03 Catalog/Fleet | MIGRATION APPLIED LOCAL — GUARDS PENDING | Chủ dự án áp dụng InitialFoundation + CatalogFleetInitial vào SangRentalDb Development; EF migrations list xác nhận applied. 16 entity schema có, chưa API/guard/concurrency test. Xem PHASE_3_EF_MIGRATION_REVIEW_REPORT.md |
| UI-QA-01 | TODO trước màn hình tương tác | Browser keyboard/focus/screen reader, responsive, SSR/hydration, Blazor disconnect, offline/timeout và đo bundle |
| NEXT-01 Identity | TODO | Chốt provider/provisioning/MFA/revocation trước admin thật |
| NEXT-02 Schema/catalog | SCHEMA LOCAL APPLIED, CHỜ GUARDS | Migration đã áp dụng local; tiếp theo thiết kế/triển khai write guards, authz/audit và kiểm thử concurrency trước API ghi hoặc khả dụng xe |
| NEXT-03 Booking/pricing/inventory/payment | OUT OF SCOPE | Sau hợp đồng module, rounding và SQL concurrency tests |
| NEXT-04 Handover/return/extension/incidents | OUT OF SCOPE | Sau NEXT-03, giữ OPEN/CR |
| NEXT-05 Admin/CMS/i18n/notifications | OUT OF SCOPE | Backend permission, provider adapters/outbox |
| NEXT-06 Security/QA/deployment | OUT OF SCOPE | AT-01…24 + pháp lý, sandbox, backup/rollback |

Giới hạn phạm vi trong bảng lịch sử chỉ phản ánh yêu cầu lúc scaffold; yêu cầu tiếp tục ngày 10/11 được theo dõi bằng bảng hiện hành phía trên.
