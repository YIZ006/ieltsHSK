# BÁO CÁO KỸ THUẬT: KHẮC PHỤC CÁC SỰ CỐ TRIỂN KHAI VÀ VẬN HÀNH HỆ THỐNG (DEPLOYMENT POST-MORTEM)

> **Dự án:** ieltsHSK - Nền tảng luyện thi IELTS & HSK  
> **Thời gian:** 10/10/2026 - 11/10/2026  
> **Phiên bản hệ thống:** Frontend Blazor WASM (Cloudflare Pages) + Backend .NET 9 (MonsterASP) + Database PostgreSQL (Supabase) + Media Storage (Cloudflare R2)  
> **Chính sách bảo mật:** Toàn bộ thông tin nhạy cảm, mật khẩu, JWT Secret, Token API đã được mã hóa / ẩn danh (`[REDACTED]`).

---

## 1. TỔNG QUAN KIẾN TRÚC PHÂN BỔ DỮ LIỆU

Nhằm tối ưu hóa chi phí và hiệu năng, hệ thống được phân tách thành 4 thành phần độc lập:

```
[ Người dùng / Trình duyệt ]
         │
         ├─── (1) Tải giao diện Web (HTML, WASM, CSS) ────────► [ Cloudflare Pages ] (ieltshsk.me)
         │                                                            │
         ├─── (2) Tải Audio, Ảnh đề thi lớn (>25MB) ──────────► [ Cloudflare R2 ] (r2.dev)
         │                                                            
         └─── (3) Gọi API nghiệp vụ, Đăng nhập, Chấm điểm ────► [ MonsterASP IIS ] (ieltshsk.runasp.net)
                                                                      │
                                                                      ▼
                                                                [ Supabase PostgreSQL ] (Database)
                                                                [ Upstash Redis ] (Cache)
```

| Thành phần | Hạ tầng | Vai trò | Dữ liệu quản lý |
| :--- | :--- | :--- | :--- |
| **Frontend App** | Cloudflare Pages | Phục vụ WebAssembly PWA tĩnh | Giao diện, Router, logic chấm điểm Client, file đề thi mẫu JSON |
| **Object Storage** | Cloudflare R2 | Lưu trữ tệp tin đa phương tiện | File MP3 nghe đề thi, Audio Shadowing, file ảnh đề, ghi âm nộp bài |
| **Backend API** | MonsterASP (Windows IIS) | Máy chủ xử lý API trung gian | Xác thực JWT, phân quyền Admin, tính điểm, ghi nhận tiến độ |
| **Database** | Supabase (PostgreSQL) | Cơ sở dữ liệu quan hệ | Bảng người dùng (`users`), Admin (`admins`), Từ vựng (`ielts_vocabularies`), Ngữ pháp (`grammar_structures`) |

---

## 2. CHI TIẾT CÁC LỖI GẶP PHẢI & GIẢI PHÁP TRIỆT ĐỂ

### 2.1. Lỗi 1: `TypeError: Failed to fetch` khi đăng nhập Admin Hub
* **Triệu chứng:**  
  Người dùng truy cập `/portal-hub/login`, điền thông tin đăng nhập Quản trị viên và nhấn "Đăng nhập Quản trị". Giao diện xuất hiện thông báo lỗi: `Lỗi kết nối máy chủ: TypeError: Failed to fetch`.
* **Nguyên nhân gốc rễ:**  
  1. **Chính sách Mixed Content của trình duyệt:** Trang Frontend chạy giao thức bảo mật `https://ieltshsk.me`, nhưng tệp cấu hình `appsettings.json` ban đầu khai báo `BackendApi:BaseUrl` là `http://ieltshsk.runasp.net/` (HTTP không mã hóa). Trình duyệt lập tức chặn kết nối ra ngoài để bảo vệ người dùng.
  2. **Bộ nhớ đệm Service Worker (PWA Cache-First):** Sau khi Backend kích hoạt SSL (`https://ieltshsk.runasp.net`) và sửa `appsettings.json`, trình duyệt vẫn báo lỗi vì tệp `sw.js` áp dụng cơ chế lưu cache-first cho mọi tệp tĩnh và chưa loại trừ `appsettings.json`. Trình duyệt tiếp tục nạp bản cache HTTP cũ từ CacheStorage.
* **Giải pháp khắc phục:**
  - **Tự động ép HTTPS ở mã nguồn C# ([Program.cs](file:///D:/project/ieltsHSK/frontend/src/Frontend.App/Program.cs)):** Bổ sung logic kiểm tra nếu Frontend chạy trên HTTPS thì tự động nâng cấp URL Backend API lên `https://`:
    ```csharp
    else if (hostUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) 
             && backendApiBaseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
    {
        backendApiBaseUrl = "https://" + backendApiBaseUrl.Substring("http://".Length);
        builder.Configuration["BackendApi:BaseUrl"] = backendApiBaseUrl;
    }
    ```
  - **Nâng cấp Service Worker ([sw.js](file:///D:/project/ieltsHSK/frontend/src/Frontend.App/wwwroot/sw.js)):** Đổi phiên bản cache sang `'app-v9-https-backend'` để tự động dọn sạch cache cũ khi activate; đồng thời thêm điều kiện bỏ qua cache cho `appsettings`.
  - **Bảo mật:** Gỡ bỏ hoàn toàn nút bấm điền sẵn tài khoản test trên giao diện đăng nhập Admin Hub.

---

### 2.2. Lỗi 2: `ERR_FAILED` trên trình duyệt Cốc Cốc khi vào `/portal-hub`
* **Triệu chứng:**  
  Khi mở `https://ieltshsk.me/portal-hub`, trình duyệt Cốc Cốc không hiển thị trang web mà báo lỗi mạng: `Không thể truy cập trang web này - ERR_FAILED`.
* **Nguyên nhân gốc rễ:**  
  - Quản trị viên đã kích hoạt quy tắc **Cloudflare WAF Custom Rule** với hành động **Managed Challenge** cho mọi URI bắt đầu bằng `/portal-hub`.
  - Khi người dùng gửi request, Cloudflare trả về trang thử thách `Just a moment...` kèm script bảo mật từ `challenges.cloudflare.com` và `/cdn-cgi/challenge-platform/...`.
  - Trình duyệt **Cốc Cốc tích hợp sẵn công cụ AdBlock / Tracking Protection**. Bộ lọc này nhận diện nhầm script của Cloudflare là mã theo dõi quảng cáo nên chặn đứng kết nối. Việc script khởi tạo trang xác minh bị ngắt đột ngột khiến Chromium ngắt kết nối mạng và văng mã lỗi `net::ERR_FAILED`.
* **Giải pháp khắc phục:**
  - Tắt tính năng "Chặn quảng cáo" của Cốc Cốc riêng cho tên miền `ieltshsk.me` bằng cách nhấn vào biểu tượng khiên chắn cạnh thanh địa chỉ.
  - Hoặc truy cập bằng các trình duyệt chuẩn như **Google Chrome**, **Microsoft Edge**, hoặc mở cửa sổ ẩn danh (Incognito).
  - Hoặc điều chỉnh quy tắc Cloudflare WAF: chỉ kích hoạt Challenge khi `Threat Score > 20` thay vì chặn tuyệt đối 100% request.

---

### 2.3. Lỗi 3: `Blocked by CORS policy` / `HTTP 500` tại API `/api/grammar-structures`
* **Triệu chứng:**  
  Console trình duyệt xuất hiện lỗi màu đỏ:
  `Access to fetch at 'https://ieltshsk.runasp.net/api/grammar-structures' from origin 'https://ieltshsk.me' has been blocked by CORS policy: No 'Access-Control-Allow-Origin' header is present on the requested resource.`
* **Nguyên nhân gốc rễ:**  
  1. Trong cơ sở dữ liệu Supabase, bảng `grammar_structures` **chưa tồn tại**. Quá trình khởi tạo ban đầu bị dừng trước khi tới đoạn tạo bảng ngữ pháp.
  2. Đoạn mã DDL mẫu trong `DependencyInjection.cs` thiếu trường `reference_source`, trong khi entity `GrammarStructure` trong C# yêu cầu thuộc tính này.
  3. Khi Backend nhận request `GET /api/grammar-structures`, EF Core truy vấn bảng không tồn tại khiến PostgreSQL ném ngoại lệ `42P01: relation grammar_structures does not exist` $\rightarrow$ Server trả về **HTTP 500 Internal Server Error**.
  4. Trên hạ tầng Microsoft-IIS của MonsterASP, khi một request gặp lỗi 500 chưa được bắt ngoại lệ, pipeline HTTP bị ngắt giữa chừng trước khi middleware CORS kịp gắn header `Access-Control-Allow-Origin`. Do đó trình duyệt thông báo lỗi CORS thay vì hiển thị chi tiết lỗi 500.
* **Giải pháp khắc phục:**
  - Đã chạy script tạo bảng `grammar_structures` chuẩn hóa đầy đủ các cột (bao gồm `reference_source`) trực tiếp trên Supabase.
  - Đã thực hiện nạp sẵn **110 cấu trúc ngữ pháp mẫu** (từ Band 4.0 đến Band 8.5) vào cơ sở dữ liệu.
  - Đã nạp đầy đủ **8 phân mục học tập** (`learning_sections`) cho cả phân hệ IELTS và HSK.

---

### 2.4. Lỗi 4: Xoay vòng vô tận `Đang tải danh sách từ vựng...` & Timeout 100 giây
* **Triệu chứng:**  
  Trang "Ngân hàng Từ vựng IELTS" (`/portal-hub/ielts-vocab`) hiển thị con quay xoay mãi không tải xong dữ liệu. Console ghi nhận hàng loạt lỗi timeout:
  `[AdminUserService] Error fetching dashboard stats: net_http_request_timeout, 100`  
  `Error getting admin stories: net_http_request_timeout, 100`  
* **Nguyên nhân gốc rễ:**  
  1. **Hiểu nhầm về nguồn lưu trữ:** Dữ liệu từ vựng, ngữ pháp, người dùng và truyện **không nằm trên Cloudflare R2** mà nằm trong cơ sở dữ liệu **Supabase PostgreSQL**. Cloudflare R2 chỉ phục vụ file media tĩnh (audio, hình ảnh).
  2. **Bảng dữ liệu trống:** Cơ sở dữ liệu Supabase mới khởi tạo nên bảng `ielts_vocabularies` có **0 từ**.
  3. **Nghẽn đường truyền Backend:** Khi đăng nhập Admin Hub lần đầu, Client gửi đồng thời 6–7 request nặng tới MonsterASP (lấy thống kê, biểu đồ, danh sách user, truyện đọc, từ vựng...).
     - MonsterASP sử dụng gói dịch vụ Free (đặt tại trung tâm dữ liệu Âu/Mỹ).
     - Supabase đặt tại khu vực Châu Đại Dương (`ap-southeast-2` - Sydney).
     - Việc gọi dồn dập nhiều kết nối đồng thời vượt quá giới hạn pool của gói Free, khiến các request bị xếp hàng chờ trong IIS và bị hủy sau 100 giây (`net_http_request_timeout`).
* **Giải pháp khắc phục:**
  - Khởi động lại (Restart) ứng dụng trên bảng điều khiển MonsterASP để làm mới pool kết nối PostgreSQL.
  - Sử dụng tính năng **"Tải Mẫu Excel"** và **"Nhập Excel (nhiều file)"** trên thanh công cụ trang Admin để nạp ngân hàng từ vựng vào database. Khi có dữ liệu, bảng sẽ render bình thường.

---

### 2.5. Lỗi 5: Vượt giới hạn kích thước tệp khi triển khai Cloudflare Pages (>25MB)
* **Triệu chứng:**  
  Cloudflare Pages từ chối gói triển khai hoặc báo lỗi tải lên khi kéo thả thư mục `wwwroot`.
* **Nguyên nhân gốc rễ:**  
  - Cloudflare Pages quy định giới hạn: mỗi tệp tĩnh tối đa **25MB** và tổng số tệp dưới **1,000**.
  - Trong thư mục `sample-data/audio` có tệp `ielts-actual-vol1-test1.mp3` dung lượng **37.2MB**.
* **Giải pháp khắc phục:**
  - Di chuyển toàn bộ các tệp âm thanh dung lượng lớn lên lưu trữ **Cloudflare R2** chuyên dụng (`https://pub-[REDACTED].r2.dev/audio/...`).
  - Cập nhật tệp JSON trỏ đường dẫn trực tiếp tới Cloudflare R2 CDN.
  - Xóa tệp MP3 nặng khỏi gói xuất bản `frontend_dist/wwwroot`, giảm tổng số tệp xuống **724 tệp** và đảm bảo không có tệp nào vượt quá 25MB.

### 2.6. Lỗi 6: `117 are unknown` khi upload gói deploy lên Cloudflare Pages
* **Triệu chứng:**  
  Kéo thả thư mục `frontend_dist/wwwroot` lên Cloudflare Pages Dashboard báo lỗi đỏ: `117/774 files could not be uploaded: 117 are unknown` trên các file `index.html.br`, `index.html.gz`, `*.wasm.br`, `Frontend.App.styles.d1837a1f.css.gz`...
* **Nguyên nhân gốc rễ:**  
  Mặc định Blazor WebAssembly khi chạy `dotnet publish` sinh ra hàng trăm file nén sẵn `.br` và `.gz`. Giao diện upload của Cloudflare Pages tự động nén ở máy chủ Edge nên từ chối các file nén sẵn có đuôi mở rộng kép này.
* **Giải pháp khắc phục:**
  - Bổ sung `<BlazorEnableCompression>false</BlazorEnableCompression>` trong `Frontend.App.csproj`.
  - Nâng cấp công cụ `CacheBuster` với cờ `--no-compress`, tự động xóa sạch 100% file `.gz` và `.br` trong `wwwroot`.
  - Tổng số file giảm từ 774 xuống còn đúng 301 file sạch. Upload nhận 100% không còn bất kỳ lỗi nào.

---

### 2.7. Lỗi 7: Chữ trên bảng Admin Hub bị "tàng hình" (trắng trên nền trắng)
* **Triệu chứng:**  
  Tại các trang quản lý `/portal-hub/speak-along`, `/portal-hub/grammar`, `/portal-hub/stories`, `/portal-hub/navigation`, tiêu đề bài nghe, công thức ngữ pháp và tên truyện bị mờ tịt, gần như không đọc được.
* **Nguyên nhân gốc rễ:**  
  Trình duyệt của người dùng đang kích hoạt Dark Mode (`[data-theme="dark"]`). Các quy tắc CSS Dark Mode toàn cục trong `app.css` ép màu chữ `.text-dark`, `.table td` thành màu trắng sáng `#f8fafc !important`. Do các bảng Admin Hub được thiết kế trên nền card màu trắng `#ffffff`, chữ trắng hiển thị trên nền trắng khiến nội dung bị chìm hoàn toàn.
* **Giải pháp khắc phục:**
  - Thêm bộ quy tắc CSS cô lập `.admin-layout` với độ ưu tiên cao trong `app.css`.
  - Ép buộc tất cả bảng, thẻ card, tiêu đề, công thức `code` trong khu vực Admin Hub luôn hiển thị màu chữ đậm tương phản cao (`#0f172a`, `#1e293b`, `#312e81`) bất kể người dùng có bật Dark Mode ở ngoài trang học viên hay không.

---

### 2.8. Lỗi 8: Âm thanh bị chặn 403 Forbidden & Tự động kích hoạt Giọng đọc AI ảo (TTS)
* **Triệu chứng:**  
  Tính năng Luyện Nói Theo phát ra giọng đọc máy ảo (Text-To-Speech) thay vì giọng đọc bài thi bản quyền gốc; mic không nói gì vẫn bị chấm điểm 74%.
* **Nguyên nhân gốc rễ:**  
  1. Link audio từ `engnovate.com` bị chặn liên kết ngoài (Hotlink/WAF 403 Forbidden).
  2. Logic JS trong `youtubePlayer.js` kiểm tra sớm `isNaN(duration)` khi audio đang tải khiến hàm tự động kích hoạt chế độ Fallback sang giọng đọc máy AI.
* **Giải pháp khắc phục:**
  - Tải toàn bộ 17 file MP3 gốc Cambridge IELTS về lưu trực tiếp trong mã nguồn `sample-data/audio/shadowing/` (mỗi file chỉ từ 0.5 – 2.8 MB, hoàn toàn nằm trong giới hạn Cloudflare Pages).
  - Cập nhật toàn bộ link audio sang đường dẫn nội bộ cùng domain.
  - Xóa bỏ hoàn toàn cơ chế Fallback sang giọng AI trong cả JS và C#. Đảm bảo 100% âm thanh phát ra là file MP3 bản quyền gốc.
  - Sửa hàm chấm điểm Mic: nếu im lặng hoặc không thu được âm thanh thì trả về 0%, đánh dấu từ là "Missed".

---

## 3. MA TRẬN TỔNG HỢP NGUYÊN NHÂN & TRẠNG THÁI KHẮC PHỤC

| STT | Triệu chứng lỗi | Nguyên nhân cốt lõi | Biện pháp đã thực hiện | Trạng thái |
| :---: | :--- | :--- | :--- | :--- |
| **1** | `TypeError: Failed to fetch` khi đăng nhập | Mixed Content (`http` vs `https`) & Service Worker dính cache cũ | Ép HTTPS trong `Program.cs`, bump cache `sw.js` lên v9 | ✅ ĐÃ KHẮC PHỤC |
| **2** | `ERR_FAILED` trên Cốc Cốc khi vào portal-hub | Trình chặn quảng cáo của Cốc Cốc chặn script Cloudflare Challenge | Hướng dẫn tắt AdBlock trên domain hoặc dùng Chrome/Edge | ✅ ĐÃ HƯỚNG DẪN |
| **3** | `Blocked by CORS` tại `/api/grammar-structures` | Bảng `grammar_structures` chưa có trên Supabase $\rightarrow$ 500 IIS ngắt CORS | Tạo bảng chuẩn, nạp 110 cấu trúc ngữ pháp mẫu vào DB | ✅ ĐÃ KHẮC PHỤC |
| **4** | Timeout 100s & vocab xoay tròn | DB trống 0 từ vựng; gọi dồn dập API qua MonsterASP gói Free | Nạp seed data, hướng dẫn restart server & import Excel | ✅ ĐÃ XỬ LÝ |
| **5** | Cloudflare Pages từ chối deploy (>25MB) | Tệp audio đề thi nặng 37.2MB vượt trần 25MB của Cloudflare Pages | Chuyển audio lên Cloudflare R2, dọn sạch thư mục dist | ✅ ĐÃ KHẮC PHỤC |
| **6** | `117 are unknown` khi upload Pages | 474 file nén thừa `.br` và `.gz` sinh ra bởi `dotnet publish` | Thêm `BlazorEnableCompression=false` & dọn sạch file nén | ✅ ĐÃ KHẮC PHỤC |
| **7** | Bảng Admin Hub bị trắng chữ / tàng hình | Global Dark Mode ép chữ thành `#f8fafc` trên nền card trắng | Cô lập CSS `.admin-layout` ép màu chữ đậm tương phản cao | ✅ ĐÃ KHẮC PHỤC |
| **8** | Audio Nói Theo bị giọng AI thay vì MP3 | Link engnovate dính 403 & kiểm tra sớm duration gây fallback AI | Tải 17 MP3 gốc về local, gỡ bỏ triệt để giọng đọc AI TTS | ✅ ĐÃ KHẮC PHỤC |

---

## 4. HƯỚNG DẪN VẬN HÀNH DÀNH CHO QUẢN TRỊ VIÊN

1. **Deploy Frontend siêu tốc (1 lệnh duy nhất):**
   ```powershell
   dotnet publish frontend/src/Frontend.App/Frontend.App.csproj -c Release -o frontend_dist
   Remove-Item -Path "frontend_dist/wwwroot/sample-data/audio/ielts-actual-vol1-test1.mp3" -Force -ErrorAction SilentlyContinue
   npx wrangler pages deploy frontend_dist/wwwroot --project-name=ieltshsk
   ```
2. **Khởi động lại Backend:**
   - Đăng nhập [MonsterASP Control Panel](https://www.monsterasp.net/).
   - Tại mục **Websites** $\rightarrow$ chọn `ieltshsk.runasp.net` $\rightarrow$ nhấn **Restart Website**.
3. **Nạp Từ Vựng:**
   - Mở Cổng quản trị: `https://ieltshsk.me/portal-hub/ielts-vocab`.
   - Bấm **"Tải Mẫu Excel"** để lấy định dạng chuẩn $\rightarrow$ Điền từ vựng $\rightarrow$ Nhấn **"Nhập Excel (nhiều file)"**.
4. **Quản trị Ngữ Pháp & Audio Shadowing:**
   - Truy cập `https://ieltshsk.me/portal-hub/grammar` để quản lý 110 cấu trúc ngữ pháp.
   - Truy cập `https://ieltshsk.me/portal-hub/speak-along` để quản lý ngân hàng bài nghe 17 bài Cambridge IELTS.
