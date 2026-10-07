# BÁO CÁO KỸ THUẬT

- **Ngày thực hiện**: 07/10/2026
- **Tên báo cáo**: BÁO CÁO NÂNG CẤP BẢO MẬT: RATE LIMITING PHÂN VÙNG THEO IP & TÍCH HỢP CLOUDFLARE TURNSTILE CAPTCHA TOÀN DIỆN
- **Người thực hiện**: Antigravity AI Assistant
- **Tài liệu lưu trữ**: `docs/bao_cao_nang_cap_rate_limiting_va_cloudflare_turnstile_captcha.md`
- **Trạng thái**: Đã hoàn thành, kiểm thử thành công & sẵn sàng triển khai

---

## 1. TỔNG QUAN HẠNG MỤC (EXECUTIVE SUMMARY)

Nhằm ngăn chặn triệt để nguy cơ tấn công brute-force mật khẩu, spam đăng ký tài khoản rác tự động từ bot/tool và bảo vệ tài nguyên hệ thống, đợt nâng cấp kỹ thuật này tập trung vào hai cơ chế phòng thủ trọng yếu:

1. **Rate Limiting phân vùng độc lập theo Client IP (`PartitionedRateLimiter`):**
   - Loại bỏ việc chia sẻ chung bộ đếm request trên toàn server.
   - Gán nhãn và phân vùng riêng biệt theo địa chỉ Client IP thực tế.
   - Áp dụng ngưỡng bảo vệ nghiêm ngặt: **tối đa 5 yêu cầu / phút / IP**, từ request thứ 6 trả về ngay mã `HTTP 429 Too Many Requests`.
2. **Tích hợp Cloudflare Turnstile CAPTCHA ("Tôi không phải người máy"):**
   - Triển khai giải pháp CAPTCHA thế hệ mới của Cloudflare, thân thiện với người dùng (không cần chọn hình ảnh phức tạp, tự động xác thực bằng chứng chỉ an toàn).
   - Tích hợp toàn diện trên cả hai luồng **Đăng nhập (Sign In)** và **Đăng ký (Register)**.
   - Xác thực 2 lớp: Frontend kiểm tra token trước khi submit; Backend gọi trực tiếp API Cloudflare `siteverify` trước khi xử lý nghiệp vụ.
3. **Tối ưu hóa UI/UX & Khắc phục lỗi vòng đời Blazor:**
   - Xử lý triệt để lỗi Turnstile widget bị xoay vô tận do vòng lặp re-render của Blazor WebAssembly.
   - Hoàn thiện validation trực quan (viền đỏ `invalid`, cảnh báo số ký tự tối thiểu).
   - Khống chế chiều cao `42px` để ẩn hoàn toàn thanh cảnh báo đỏ nội bộ của Test Key khi thử nghiệm local.

---

## 2. NỘI DUNG CHI TIẾT CÁC THAY ĐỔI

### 2.1. Nâng cấp Rate Limiting phân vùng theo Client IP (`Program.cs`)

- **Vấn đề trước đây:** Bộ đếm rate limiting của ASP.NET Core được tính chung trong phiên làm việc của server hoặc không phân biệt theo IP. Khi nhiều người dùng cùng truy cập hoặc một bên spam, bộ đếm có thể ảnh hưởng chéo hoặc không chặn được kẻ tấn công dùng công cụ tự động.
- **Giải pháp áp dụng:**
  - Viết hàm trích xuất Client IP thực tế qua các HTTP Headers:
    - `X-Forwarded-For` (qua Reverse Proxy / Nginx / Cloudflare).
    - `CF-Connecting-IP` (khi đi qua Cloudflare CDN).
    - `RemoteIpAddress` (kết nối trực tiếp).
  - Sử dụng `PartitionedRateLimiter.Create<HttpContext, string>` với chính sách `"auth"`:
    - **Thuật toán:** Fixed Window Limiter.
    - **Thời gian cửa sổ:** 1 phút (`TimeSpan.FromMinutes(1)`).
    - **Ngưỡng cho phép:** 5 lượt (`PermitLimit = 5`).
    - **Hàng đợi:** 0 (`QueueLimit = 0` - từ chối ngay lập tức khi vượt ngưỡng).
  - Tùy biến `OnRejected`: Trả về `HTTP 429 Too Many Requests`, kèm header `Retry-After: 60` và body JSON:
    ```json
    {
      "error": "Quá nhiều yêu cầu từ địa chỉ IP của bạn. Vui lòng chờ ít phút trước khi thử lại.",
      "retryAfterSeconds": 60
    }
    ```

---

### 2.2. Kiến trúc Cloudflare Turnstile CAPTCHA (Backend)

Tuân thủ nghiêm ngặt quy tắc kiến trúc Clean Architecture và quy định repository:

1. **Application Core Layer:**
   - Tạo Interface `ICaptchaService.cs` (`backend/src/Backend.Application/Abstractions/ICaptchaService.cs`):
     ```csharp
     public interface ICaptchaService
     {
         Task<bool> VerifyCaptchaAsync(string? token, string? clientIp = null, CancellationToken cancellationToken = default);
     }
     ```
   - Cập nhật DTO `RegisterRequest.cs` và `LoginRequest.cs` bổ sung trường `CaptchaToken`.

2. **Infrastructure Layer:**
   - Tạo `CloudflareTurnstileService.cs` (`backend/src/Backend.Infrastructure/Services/CloudflareTurnstileService.cs`):
     - Gọi POST tới `https://challenges.cloudflare.com/turnstile/v0/siteverify`.
     - Hỗ trợ biến cấu hình `Turnstile:SecretKey` và cờ bật/tắt `Turnstile:Enabled`.
     - Đăng ký `HttpClient` có cấu hình Timeout 10s trong `DependencyInjection.cs`.

3. **API Endpoints Layer:**
   - Trong `AuthEndpoints.cs` (`backend/src/Backend.Api/Endpoints/AuthEndpoints.cs`):
     - Endpoint `/api/auth/register`: Bắt buộc token hợp lệ, từ chối bot ngay từ bước tiếp nhận.
     - Endpoint `/api/auth/login`: Xác thực token CAPTCHA trước khi gọi logic kiểm tra tài khoản.

---

### 2.3. Tích hợp & Sửa lỗi Frontend Blazor WebAssembly

1. **Khắc phục lỗi vòng lặp xoay vô tận (Infinite Spinner):**
   - *Nguyên nhân:* Trong Blazor, mỗi khi người dùng gõ phím vào ô nhập liệu hoặc đổi focus, `StateHasChanged()` sẽ gọi `OnAfterRenderAsync()`. Ban đầu, helper JS liên tục gọi `innerHTML = ''` và khởi tạo lại `turnstile.render()`, khiến iframe bị hủy và tạo mới liên tục trong vài mili-giây.
   - *Khắc phục:* Bổ sung cơ chế quản lý trạng thái bằng cờ `data-rendered="true"` và `data-widget-id`. Khi Blazor render lại, helper JS kiểm tra cờ này và bỏ qua, giữ nguyên trạng thái xác thực của Cloudflare.

2. **Quản lý 2 widget độc lập cho Đăng nhập và Đăng ký:**
   - Màn hình Đăng nhập: `<div id="turnstile-login-widget"></div>`
   - Màn hình Đăng ký: `<div id="turnstile-register-widget"></div>`
   - Khi chuyển đổi tab (SwitchMode), tự động reset widget tương ứng và xóa sạch token cũ.

3. **Validation trực quan & Phản hồi Rate Limiting:**
   - Ràng buộc độ dài: Username/Email tối thiểu 3 ký tự, Mật khẩu tối thiểu 6 ký tự.
   - Hiển thị viền đỏ `form-control invalid` khi dữ liệu không hợp lệ.
   - Trong `AuthService.LoginAsync` và `RegisterAsync`, bắt trực tiếp mã `HTTP 429` để hiển thị rõ ràng cho người dùng:
     > *"Bạn đã thao tác quá nhiều lần liên tiếp (vượt quá 5 lượt/phút). Vui lòng chờ 1 phút trước khi thử lại."*

4. **Xử lý dòng chữ cảnh báo của Test Key:**
   - Key thử nghiệm mặc định của Cloudflare (`1x00000000000000000000AA`) tự động đính kèm thanh thông báo màu đỏ *"For testing only. If seen, report to site owner"* ở đáy widget (từ pixel 43 đến 65).
   - Thiết lập chuẩn chiều cao `height: 42px; max-height: 42px; overflow: hidden; border-radius: 4px;` để che khuất hoàn toàn dòng chữ đỏ này, giữ lại trọn vẹn dấu tích xanh `Success!` và nhãn bảo mật.

---

## 3. KẾT QUẢ KIỂM THỬ THỰC TẾ

| Kịch bản kiểm thử | Hành động thực hiện | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|---|---|---|---|---|
| **1. Rate Limiting (1-5 reqs)** | Gửi 5 request liên tiếp trong 10s tới `/api/auth/login` | Server xử lý bình thường | HTTP 400 (Sai thông tin đăng nhập) | ✅ Đạt |
| **2. Rate Limiting (Req thứ 6+)** | Gửi request thứ 6 và thứ 7 từ cùng IP | Bị chặn ngay lập tức | HTTP 429 Too Many Requests (`Retry-After: 60`) | ✅ Đạt |
| **3. Đăng ký thiếu CAPTCHA** | Gửi POST `/api/auth/register` không có token | Bị từ chối ngay lập tức | HTTP 400: *"Xác thực CAPTCHA không thành công..."* | ✅ Đạt |
| **4. Xác thực CAPTCHA hợp lệ** | Hoàn thành widget Turnstile trên UI rồi submit | Server xác nhận token qua Cloudflare API | Tiếp nhận và hoàn tất đăng ký thành công | ✅ Đạt |
| **5. Nhập liệu ngắn (ví dụ: `ád`)** | Gõ `ád` vào ô Tên đăng nhập rồi submit | Hiện validate viền đỏ | Viền đỏ cảnh báo *"Tối thiểu 3 ký tự trở lên"* | ✅ Đạt |
| **6. Giao diện CAPTCHA** | Mở form Login / Register | Không bị xoay vô tận, không lộ dòng chữ đỏ | Hiển thị tích xanh gọn gàng trong khung 42px | ✅ Đạt |

---

## 4. HƯỚNG DẪN CẤU HÌNH CHO MÔI TRƯỜNG PRODUCTION

Khi triển khai ứng dụng lên máy chủ và tên miền thực tế:

1. Truy cập [Cloudflare Dashboard](https://dash.cloudflare.com/) -> **Turnstile** (Miễn phí).
2. Tạo Widget mới:
   - **Domains**: Nhập domain của website (ví dụ: `yourdomain.com`).
   - **Widget Mode**: Chọn `Managed` (khuyên dùng) hoặc `Non-interactive`.
3. Lấy `Site Key` và `Secret Key` chính thức.
4. Cập nhật vào cấu hình hệ thống:
   - **Backend** (`appsettings.Production.json` hoặc Environment Variable):
     ```json
     "Turnstile": {
       "Enabled": true,
       "SecretKey": "0x4AAAAAA..."
     }
     ```
   - **Frontend** (`appsettings.Production.json`):
     ```json
     "Turnstile": {
       "SiteKey": "0x4AAAAAA..."
     }
     ```
