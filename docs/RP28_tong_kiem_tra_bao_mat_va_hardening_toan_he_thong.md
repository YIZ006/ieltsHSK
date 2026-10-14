# BÁO CÁO TỔNG KIỂM TRA BẢO MẬT, KHẮC PHỤC LỖ HỔNG & HARDENING TOÀN HỆ THỐNG
> **Mã tài liệu:** RP28  
> **Dự án:** ieltsHSK  
> **Thời gian thực hiện:** Tháng 10/2026  
> **Phạm vi:** Backend .NET 9 API, Frontend Blazor WASM, Docker Redis, Quản lý Secret & CI/CD Scripts  

---

## 1. BẢNG TỔNG HỢP XỬ LÝ 10 ĐIỂM NÓNG BẢO MẬT

| STT | Mức độ | Vị trí tệp | Lỗ hổng phát hiện | Biện pháp khắc phục đã triển khai | Trạng thái |
| :---: | :---: | :--- | :--- | :--- | :---: |
| **1** | 🔴 CRITICAL | `appsettings*.json` | JWT key, OAuth secret, R2 credential nằm trong repo | Đã thu hồi/thay thế bằng placeholder chuẩn (`YOUR_*`). Chuyển toàn bộ sang nạp qua Environment Variables / Secret Manager. | ✅ ĐÃ KHẮC PHỤC |
| **2** | 🔴 CRITICAL | `DependencyInjection.cs`, `RP23` | Startup seed mật khẩu admin cố định và ghi đè mỗi khi chạy lại | Đã xóa logic ghi đè mật khẩu của admin đã tồn tại. Nếu tạo mới admin lần đầu, nạp từ biến môi trường `ADMIN_INITIAL_PASSWORD` hoặc sinh ngẫu nhiên 16 ký tự an toàn. | ✅ ĐÃ KHẮC PHỤC |
| **3** | 🔴 CRITICAL | `GrammarEndpoints.cs` | Các route CRUD/import/export grammar dưới `/api/admin/*` không có auth | Đã bổ sung `[Authorize(Roles = "admin")]` cho toàn bộ các endpoint tạo, sửa, xóa, bulk-delete, import-multiple và export grammar. | ✅ ĐÃ KHẮC PHỤC |
| **4** | 🔴 CRITICAL | `ExamSubmissionEndpoints.cs` | Danh sách và cập nhật điểm bài nộp admin không có phân quyền | Đã bổ sung `[Authorize(Roles = "admin")]` cho các route `/api/admin/test-submissions`, chi tiết bài nộp và chấm điểm. | ✅ ĐÃ KHẮC PHỤC |
| **5** | 🟠 HIGH | `IeltsEndpoints.cs` | API audio-shadowing catalog & lesson R2 cho phép public ghi đè | Đã khóa toàn bộ endpoint `/api/ielts/audio-shadowing/save` và `upload-lesson` bằng `[Authorize(Roles = "admin")]`. | ✅ ĐÃ KHẮC PHỤC |
| **6** | 🟠 HIGH | `IeltsEndpoints.cs` | Stream private audio speaking không kiểm tra quyền sở hữu | Đã bổ sung kiểm tra: Chỉ Admin hoặc học viên sở hữu bản ghi (khớp `userId` hoặc `sessionId`) mới được phép stream audio. | ✅ ĐÃ KHẮC PHỤC |
| **7** | 🟠 HIGH | `ExamSubmissionEndpoints.cs` | API sync/latest/r2-json cho phép query tùy tiện dữ liệu học viên | Đã siết chặt logic: User đã đăng nhập chỉ xem bài của mình; Khách vãng lai bắt buộc phải khớp đúng `sessionId`; cấm truy vấn lộ email/feedback. | ✅ ĐÃ KHẮC PHỤC |
| **8** | 🟠 HIGH | `AuthEndpoints.cs` | Login bỏ qua CAPTCHA nếu `CaptchaToken` rỗng | Bắt buộc gọi kiểm tra Cloudflare Turnstile CAPTCHA cho cả login học viên và admin; từ chối mọi request thiếu CAPTCHA token. | ✅ ĐÃ KHẮC PHỤC |
| **9** | 🟠 HIGH | `docker-compose.redis.yml` | Redis & Commander mở port `0.0.0.0` không auth, chưa pin version | Đã bind về `127.0.0.1`, bắt buộc `--requirepass`, cấu hình HTTP Basic Auth cho Commander, pin image `rediscommander:v0.8.0`. | ✅ ĐÃ KHẮC PHỤC |
| **10** | 🟡 MEDIUM | `Program.cs` | Rate limit và audit IP tin trực tiếp header `X-Forwarded-For` | Đã nâng cấp `ResolveClientIp`: Chỉ tin `X-Forwarded-For` từ loopback/local proxy, ưu tiên `CF-Connecting-IP` từ Cloudflare Edge và socket IP thực tế. | ✅ ĐÃ KHẮC PHỤC |

---

## 2. NÂNG CẤP HẠ TẦNG VẬN HÀNH & DEPLOYMENT

### 🚀 Nâng cấp Script Triển khai (`scripts/deploy.ps1`)
* Hỗ trợ build và đóng gói cả **Backend Solution** (`dotnet build backend/Backend.sln` $\rightarrow$ `backend_dist`).
* Cố định phiên bản CLI: Chuyển sang dùng `npx wrangler@3.111.4` có pinned version thay vì dùng floating latest.
* Tích hợp tự động gọi **Readiness Health Check** sau khi deploy để xác thực backend trực tuyến.

### 🛑 Cơ chế Khởi tạo An toàn & Readiness Gate (`/health/ready`)
* **Loại bỏ fire-and-forget:** Tiến trình nạp migration và kiểm tra dữ liệu khởi tạo không còn thả nổi trong `Task.Run` không kiểm soát, tránh xung đột race-condition khi scale nhiều replica.
* **Cung cấp 2 đầu dò chuẩn Kubernetes/Docker:**
  * **`GET /health/live`**: Xác nhận tiến trình Kestrel đang chạy bình thường.
  * **`GET /health/ready`**: Kiểm tra thực tế kết nối tới Database PostgreSQL và Redis. Nếu database mất kết nối hoặc đang khởi tạo, trả về ngay mã `HTTP 503 Service Unavailable` để các Load Balancer / Reverse Proxy không điều hướng lưu lượng vào instance chưa sẵn sàng.

### 🔒 Đồng nhất Giao thức HTTPS Toàn diện
* Toàn bộ tài liệu và cấu hình backend production được chuẩn hóa sang **`https://ieltshsk.runasp.net`**, loại bỏ hoàn toàn nguy cơ lỗi **Mixed Content** của trình duyệt khi giao tiếp từ Frontend `https://ieltshsk.me`.
* Swagger API documentation trên môi trường Production mặc định bị tắt (`EnableSwagger = false`).

---

## 3. KẾ HOẠCH LOAD TEST & DIỄN TẬP PHỤC HỒI (DRILL PLAN)

### 📊 Kế hoạch Kiểm thử Tải (Load Testing với k6 / Bombardier)
1. **Mục tiêu hiệu năng (SLO/SLA Targets):**
   * **Throughput:** $\ge 500$ requests/giây đối với các API đọc danh mục (Navigation, HSK Vocab, IELTS Sections).
   * **Độ trễ p95:** $< 150\text{ms}$ khi cache hit; $< 400\text{ms}$ khi database query.
   * **Error rate:** $< 0.1\%$ khi chịu tải đồng thời 200 Virtual Users (VUs).
2. **Kịch bản kiểm thử mẫu (k6 script):**
   ```javascript
   import http from 'k6/http';
   import { check, sleep } from 'k6';

   export const options = {
     stages: [
       { duration: '30s', target: 50 },
       { duration: '1m', target: 200 },
       { duration: '30s', target: 0 },
     ],
   };

   export default function () {
     const res = http.get('https://ieltshsk.runasp.net/api/navigation');
     check(res, { 'status is 200': (r) => r.status === 200 });
     sleep(0.5);
   }
   ```

### 🗄️ Diễn tập Sao lưu & Phục hồi (Backup & Restore Drill)
1. **Lập lịch sao lưu tự động:** Sử dụng cron job chạy `pg_dump` mỗi 24 giờ đẩy vào S3/R2 private cold storage.
2. **Quy trình kiểm tra tính toàn vẹn (Dry-run Restore):**
   * Mỗi tháng 1 lần, tải bản backup SQL gần nhất và restore vào một cơ sở dữ liệu staging/test biệt lập.
   * Chạy bộ test tích hợp xác nhận số lượng bảng (30 bảng), số lượng từ vựng, tài khoản admin và đề thi khớp 100% với dữ liệu sản xuất.
