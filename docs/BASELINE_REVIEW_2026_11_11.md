# Rà soát baseline ngày 11/11/2026

## Phạm vi đã kiểm

- Working tree có sẵn nhiều thay đổi Identity/Catalog, migrations, test runners và tài liệu từ phiên trước; không reset, stage, commit hoặc push trong đợt này.
- Ba tài liệu Markdown nguồn trong `docs/important/` trùng từng byte với phiên bản `HEAD:docs/`. Hai file Stitch có nội dung giống sau chuẩn hóa CRLF/LF. Không sửa nội dung nguồn.
- Git đang biểu diễn nguồn cũ là deleted và thư mục mới là untracked; cần đưa cả hai phía vào cùng commit khi chốt baseline để Git nhận diện rename. Không coi trạng thái này là mất nguồn.
- `.env`, artifacts và node_modules được `git check-ignore` xác nhận loại khỏi Git. Chỉ các `.env.example` và `next-env.d.ts` liên quan env đang tracked.
- Quét có giới hạn trên file code/config/script tracked và untracked không bị ignore: hai cảnh báo trong AdminBootstrapCommand.cs và set-database-secret.ps1 đều là biến nhận mật khẩu lúc chạy, không phải credential literal. Đây không phải chứng nhận không có secret trong toàn bộ lịch sử Git.
- `next-env.d.ts` được production build tự đổi tham chiếu từ `.next/dev/types` sang `.next/types`; giữ bản sinh phù hợp build, không chỉnh tay.

## Chưa xác minh

- SQL local TCP 1436 không có listener tại thời điểm kiểm tra. Identity runner FAIL tại bước migrate test DB; Catalog guard runner FAIL SqlException. Không đổi credential/TLS, không apply Identity vào SangRentalDb.
- Docker CLI không tìm thấy trong PATH hoặc đường dẫn cài đặt chuẩn đã kiểm; chưa xác định được cách khởi động SQL trên máy này.
- CI remote, browser QA, review bảo mật toàn diện và các acceptance nghiệp vụ chưa được chạy.

## Khi chốt baseline

Review diff code/migration/lockfiles và danh sách untracked trước khi commit. Giữ artifacts, secrets, dữ liệu thử và key ring ngoài Git. Không gộp migration Identity Development hoặc production deployment vào thao tác baseline.
