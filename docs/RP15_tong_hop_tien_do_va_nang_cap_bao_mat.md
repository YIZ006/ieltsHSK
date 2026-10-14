# BÁO CÁO KỸ THUẬT

- **Ngày thực hiện**: 14/09/2026
- **Tên báo cáo**: BÁO CÁO TỔNG HỢP TIẾN ĐỘ CÔNG VIỆC VÀ NÂNG CẤP BẢO MẬT TOÀN DIỆN HỆ THỐNG IELTS/HSK
- **Người viết**: Antigravity AI Assistant
- **Tài liệu lưu trữ**: `docs/bao_cao_tong_hop_tien_do_va_nang_cap_bao_mat.md`
- **Trạng thái**: Đã hoàn thành & kiểm chứng thành công

---

## 1. TỔNG QUAN THỰC HIỆN (EXECUTIVE SUMMARY)

Trong phiên làm việc vừa qua, hệ thống đã được nâng cấp, tối ưu hóa và hoàn thiện trên **5 nhóm nghiệp vụ cốt lõi**:

1. **Học liệu & Dữ liệu đề thi mới:** Cào dữ liệu ngữ pháp tiếng Anh từ Langmaster (từ A–Z); phân tích, bóc tách và tích hợp trọn bộ đề **IELTS Listening Actual Tests VOL 2** và **VOL 3** (kèm file audio, câu hỏi và đáp án chuẩn).
2. **Trải nghiệm Học viên & Khách vãng lai:** Xử lý triệt để sự cố lặp modal hỏi chọn trình độ của tài khoản khách; phân lập luồng lưu kết quả bài thi của khách (chỉ lưu session tạm thời, không đưa vào danh sách ưu tiên ôn tập, giải phóng khi thoát).
3. **Cải tiến Giao diện (UI/UX) Danh sách đề:** Khắc phục lỗi hiển thị gộp chung bộ đề do CSS/HTML tĩnh; bổ sung bảng dashboard thống kê số đề và lượt làm bài; chuyển đổi bố cục chống cuộn chuột dài bất tiện.
4. **Chẩn đoán Kỹ thuật & Hệ thống:** Phân tích nguyên nhân loa nhận nhưng không nhận diện được chữ trong bài thi Speaking; làm rõ nguồn gốc an toàn của tiến trình `Bun` (`omp.exe`) trong Task Manager.
5. **Tăng cường Bảo mật Hệ thống (Security Hardening):** Triển khai đồng loạt 10 giải pháp bảo mật theo tiêu chuẩn công nghiệp (CORS, Rate Limiting, Access Token 30m, Hashed Refresh Tokens SHA-256, Account Lockout, HTTP Security Headers, Password Policy, XSS Sanitizer).

---

## 2. NỘI DUNG CHI TIẾT CÁC HẠNG MỤC ĐÃ THỰC HIỆN

### 2.1. Cào dữ liệu Ngữ pháp Langmaster & Nhập bộ đề IELTS Listening Actual Tests VOL 2 & 3

#### A. Cào dữ liệu Ngữ pháp Langmaster
- **Nguồn:** `https://langmaster.edu.vn/sieu-hot-tong-hop-ngu-phap-tieng-anh-co-ban-tu-a-z`
- **Thực hiện:**
  - Trích xuất toàn bộ các chủ điểm ngữ pháp căn bản (Thì tiếng Anh, Từ loại, Câu điều kiện, Mệnh đề quan hệ, Câu bị động, Cấu trúc so sánh, v.v.).
  - Cấu trúc hóa dữ liệu dạng JSON/Entities để nạp vào CSDL phục vụ học viên tra cứu và ôn tập.

#### B. Nhập bộ đề IELTS Listening Actual Tests VOL 2 & VOL 3
- **Nguồn:** `C:\Users\Chien\Downloads\IELTS Listening Actual Tests_ VOL 2` & `VOL 3`.
- **Thực hiện:**
  - Quét và trích xuất cấu trúc đề thi, bóc tách câu hỏi từng Part (Section 1 đến 4).
  - Khớp file audio tương ứng của từng bài nghe.
  - Phân tích và biên soạn bảng answer keys (đáp án chuẩn) cho từng đề.
  - Xuất các file dữ liệu chuẩn định dạng JSON:
    - `listening-actual-vol2-test1.json` đến `test6.json` (kèm `.answers.json`)
    - `listening-actual-vol3-test1.json` đến `test6.json` (kèm `.answers.json`)
  - Toàn bộ dữ liệu được lưu đúng thư mục ứng dụng theo quy tắc repository:  
    `frontend/src/Frontend.App/wwwroot/sample-data/`

---

### 2.2. Khắc phục lỗi lặp Modal chọn trình độ & Tối ưu tài khoản khách vãng lai

#### A. Khắc phục lỗi Modal chọn trình độ liên tục hiển thị
- **Hiện tượng:** Khách vãng lai sau khi chọn trình độ học (A1, A2, B1...) thì mỗi lần chuyển trang hoặc thao tác lại bị modal tiếp tục bật lên hỏi lại.
- **Nguyên nhân:** Trạng thái lưu trữ chỉ kiểm tra tạm thời trên biến bộ nhớ runtime, khi chuyển đổi trang/component bị re-render thì cờ này bị đặt lại giá trị mặc định.
- **Giải pháp:** Bổ sung cơ chế lưu cờ trạng thái `user_guest_level_selected` vào `localStorage` / `sessionStorage`. Khi khách đã hoàn tất chọn trình độ một lần, hệ thống ghi nhớ và không mở lại modal trong suốt phiên làm việc.

#### B. Tách biệt kết quả làm bài của Khách vãng lai
- **Yêu cầu:** Khách làm bài không lưu vĩnh viễn vào CSDL, không đưa vào danh sách "Ưu tiên ôn tập" (Priority Review), đóng trình duyệt là xóa.
- **Giải pháp:**
  - Luồng tài khoản đăng nhập: Gửi request lưu vào database qua API `/api/test-submissions` và tính điểm vào Streak/LMS.
  - Luồng tài khoản khách: Chỉ lưu kết quả tạm trong bộ nhớ Session của trình duyệt. Không ghi đè hay làm loãng dữ liệu thống kê của học viên chính thức.

---

### 2.3. Tái cấu trúc Giao diện Danh sách Bộ đề & Báo cáo Thống kê

#### A. Sửa lỗi gộp chung bộ đề do CSS/HTML cứng
- **Hiện tượng:** Mặc dù tên bộ đề trong database khác nhau, giao diện hiển thị vẫn bị gom chung vào một khối container duy nhất.
- **Giải pháp:** Cập nhật template hiển thị động theo danh mục thực tế từ backend; phân tách rõ ràng từng bộ sưu tập (Collection / Volume).

#### B. Bổ sung Dashboard Thống kê & Chống cuộn trang dài
- **Thống kê tổng quan:** Hiển thị thẻ chỉ số ở đầu trang gồm tổng số lượng đề thi, số lượt đã hoàn thành, điểm số gần nhất.
- **Tối ưu trải nghiệm:** Chuyển sang bố cục dạng lưới thẻ (Card Grid) kèm phân nhóm trực quan, loại bỏ tình trạng phải vuốt chuột qua danh sách đề dài hàng mét để tiếp cận các đề thi bên dưới cùng.

---

### 2.4. Chẩn đoán Kỹ thuật Microphone Speaking & Phân tích Tiến trình Bun

#### A. Chẩn đoán Micro & Nhận diện Giọng nói (IELTS Speaking)
- **Hiện tượng:** Thiết bị âm thanh (loa/micro) đã được hệ điều hành nhận nhưng Web Speech API không ghi nhận được chữ người dùng phát âm.
- **Nguyên nhân & Khắc phục:**
  - Kiểm tra quyền truy cập microphone trên trình duyệt và chính sách `Permissions-Policy`.
  - Bổ sung cấu hình header `Permissions-Policy: microphone=(self)` trên backend để tránh bị trình duyệt chặn ngầm quyền ghi âm.
  - Hướng dẫn kiểm tra cấu hình default input device và kiểm tra engine nhận diện Web Speech API (yêu cầu kết nối mạng để Google Speech Recognition hoạt động trên trình duyệt Chromium).

#### B. Phân tích Tiến trình "Bun" trong Windows Task Manager
- **Bản chất:** Tiến trình hiển thị `Bun` (chiếm ~160MB RAM) thực chất là file nhị phân `C:\Users\Chien\AppData\Local\omp\omp.exe` được đóng gói trên nền tảng Bun JavaScript Runtime v1.3.14.
- **Mục đích:** Đang thực thi script extension `orca-agent-status.ts` để đồng bộ trạng thái giữa môi trường IDE terminal và trợ lý AI Antigravity.
- **Kết luận:** An toàn tuyệt đối, thuộc công cụ phát triển hiện tại và tự giải phóng khi đóng phiên làm việc.

---

### 2.5. Nâng cấp Bảo mật Toàn diện (Security Hardening)

Triển khai đầy đủ 10 biện pháp bảo mật cốt lõi theo khuyến nghị ưu tiên:

1. **CORS Policy an toàn:** Loại bỏ wildcard `_ => true` khi có `AllowCredentials()`. Ở Development cho phép toàn bộ cổng trên `localhost` và `127.0.0.1`; ở Production chỉ cho phép domain đã định danh cụ thể từ cấu hình.
2. **Rate Limiting chống brute-force:** Tích hợp `Microsoft.AspNetCore.RateLimiting` giới hạn 15 request/phút cho các endpoint xác thực (`/api/auth/*`).
3. **Rút ngắn thời hạn Token:**
   - Access token học viên: Giảm từ **1 ngày** xuống **30 phút** (kết hợp `TokenRefreshService` silent refresh tự động trên Blazor WASM).
   - Access token quản trị: Giảm từ **30 ngày** xuống **12 giờ**.
   - Cửa sổ ân hạn tái sử dụng Refresh Token (`ReuseGraceSeconds`): Giảm từ **90s** xuống **10s**.
4. **Băm SHA-256 Refresh Token:** CSDL chỉ lưu mã băm hex 64 ký tự. Nếu database bị rò rỉ, token không thể bị giải mã để chiếm quyền. Vẫn hỗ trợ fallback token cũ đang chạy.
5. **Khóa tài khoản tạm thời (Account Lockout):** Nhập sai mật khẩu liên tiếp **5 lần** -> Khóa tài khoản tạm thời **15 phút** (lưu trên `IMemoryCache`). Đăng nhập đúng tự động xóa bộ đếm thất bại.
6. **HTTP Security Headers:** Bổ sung middleware trả về:
   - `X-Content-Type-Options: nosniff`
   - `X-Frame-Options: SAMEORIGIN`
   - `Referrer-Policy: strict-origin-when-cross-origin`
   - `X-XSS-Protection: 1; mode=block`
   - `Permissions-Policy: microphone=(self), camera=(), geolocation=()`
7. **Production Hardening:** Kích hoạt `UseHsts()`, `UseHttpsRedirection()`, ẩn Swagger UI trong môi trường Production.
8. **Độ an toàn Mật khẩu:** Yêu cầu mật khẩu đăng ký tối thiểu 8 ký tự, có cả chữ cái và số; endpoint reset mật khẩu của admin chuyển sang hàm sinh mật khẩu ngẫu nhiên an toàn cao `RandomNumberGenerator` (16 ký tự đa dạng).
9. **Nâng cấp Bộ vệ sinh đầu vào (`SecuritySanitizer`):** Mở rộng danh sách thẻ HTML nguy hiểm (`video`, `audio`, `details`, `dialog`, `template`, `source`), bổ sung regex chặn mọi inline event handlers (như `<img onerror=...>`, `<details ontoggle=...>`), và cơ chế giải mã đa tầng chống bypass encoding.
10. **Giới hạn Kích thước Body:** Thiết lập `MultipartBodyLengthLimit = 50MB` để chống tấn công DoS tràn bộ nhớ khi upload audio/ảnh/file.

---

## 3. DANH SÁCH FILE NGUỒN ĐÃ THAY ĐỔI

| File | Module | Nội dung thay đổi chính |
| :--- | :--- | :--- |
| `backend/src/Backend.Api/Program.cs` | Backend API | Tích hợp Rate Limiting, CORS whitelist, Security Headers middleware, HSTS, HTTPS redirection, gate Swagger, Multipart upload limit, Secure reset password. |
| `backend/src/Backend.Infrastructure/Services/AuthService.cs` | Backend Service | Thời hạn token 30m / 12h, grace window 10s, băm SHA-256 refresh token, Account lockout 5 lần / 15 phút, password complexity validation, hàm sinh mật khẩu an toàn. |
| `backend/src/Backend.Application/Common/SecuritySanitizer.cs` | Backend Common | Bổ sung thẻ HTML nguy hiểm, regex inline event handler, multi-pass decoding chống bypass XSS. |
| `frontend/src/Frontend.App/Services/AuthService.cs` | Frontend Service | Cập nhật `RegisterModel` validation mật khẩu tối thiểu 8 ký tự đồng bộ với backend. |
| `frontend/src/Frontend.App/Components/AuthModal.razor` | Frontend Component | Cập nhật thanh đo độ mạnh mật khẩu (Password Strength) theo mốc tối thiểu 8 ký tự. |
| `frontend/src/Frontend.App/wwwroot/sample-data/*` | Frontend Data | Bộ đề thi và đáp án chuẩn của IELTS Listening Actual Tests VOL 2 và VOL 3 (Test 1 đến 6). |

---

## 4. KẾT QUẢ KIỂM THỬ THỰC TẾ (VERIFICATION)

Toàn bộ các bài kiểm thử tự động trên môi trường thực tế đã được thực thi và xác nhận:

| Kịch bản kiểm thử | Kỳ vọng | Kết quả thực tế | Trạng thái |
| :--- | :--- | :--- | :--- |
| **Biên dịch Backend** | `dotnet build Backend.Api.csproj` | Succeeded (0 Warning, 0 Error) | ✅ PASS |
| **Biên dịch Frontend** | `dotnet build Frontend.App.csproj` | Succeeded (0 Error) | ✅ PASS |
| **Security Headers** | `GET /` trả về đầy đủ các header bảo mật | Có đủ `nosniff`, `SAMEORIGIN`, `strict-origin`, `Permissions-Policy` | ✅ PASS |
| **CORS Origin Check** | Cho phép localhost:5102, chặn malicious-site.com | Origin localhost có header; Origin lạ bị từ chối cấp quyền | ✅ PASS |
| **Password Policy** | Đăng ký với mật khẩu ngắn hoặc không có số | Bị từ chối với mã HTTP 400 kèm thông báo rõ ràng | ✅ PASS |
| **Account Lockout** | Đăng nhập sai 5 lần liên tiếp | Lần 5: Thông báo khóa 15 phút; Lần 6+: Lập tức chặn request | ✅ PASS |
| **Rate Limiting** | Gửi dồn dập >15 request/phút | Kích hoạt mã HTTP 429 Too Many Requests | ✅ PASS |
| **Kiểm tra File Rác** | `git status --short` | Chỉ sửa đổi các file mã nguồn hợp lệ, không có file nháp ở root | ✅ PASS |

---

## 5. HƯỚNG DẪN VẬN HÀNH & TRIỂN KHAI (DEPLOYMENT NOTES)

1. **Biến môi trường Production:**
   - Cần cung cấp biến môi trường `Jwt__Key` (chuỗi ngẫu nhiên tối thiểu 32 ký tự, sinh bằng `openssl rand -base64 32`).
   - Cung cấp `Cors__AllowedOrigins` hoặc `Frontend__BaseUrl` (ví dụ: `https://your-domain.com`) để CORS cấp quyền đúng cho frontend trên production.
2. **Khả năng tương thích ngược (Backward Compatibility):**
   - Cơ chế băm Refresh Token hỗ trợ song song cả token đã băm và token cũ chưa băm, đảm bảo người dùng đang đăng nhập không bị văng phiên đột ngột sau khi cập nhật hệ thống.
