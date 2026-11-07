# Hợp đồng nền tảng

| Method/path | Môi trường/quyền | Kết quả |
|---|---|---|
| GET /health/live | API anonymous | 200 Healthy nếu process phục vụ, không kiểm DB |
| GET /health/ready | API anonymous | 200 nếu SQL kết nối được; 503 khi thiếu config/lỗi; không lộ exception |
| GET /openapi/v1.json | API Development | OpenAPI 200; chưa có resource nghiệp vụ |
| GET đường dẫn không có | API | 404 ProblemDetails application/problem+json |
| GET /health/live | Admin anonymous | 200 |
| GET /account/login | Admin anonymous | Trang login chưa bật, không POST signin |
| GET / | AdminAccess | Chưa login: challenge redirect đến login, không render dashboard |
| GET /auth/session | AdminAccess | Anonymous 401; có phiên và role Admin 204; thiếu quyền 403 |

Health trả text; không phải DTO nghiệp vụ. Không CORS wildcard. OpenAPI chưa có business paths là chủ đích.

Hợp đồng tương lai trong đặc tả mục 8 **chưa khóa DTO/routes**. Trước module cần request/response, validation, stable ProblemDetails code, actor/permission, pagination, If-Match/ETag, Idempotency-Key, audit và ví dụ. Không tạo endpoint giả thành công cho booking/payment. Backend phải kiểm quyền khi có API quản trị thật; cookie admin chưa chứng minh auth liên ứng dụng.
