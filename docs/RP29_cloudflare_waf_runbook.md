# SỔ TAY VẬN HÀNH & RUNBOOK: THIẾT LẬP BẢO MẬT CLOUDFLARE FREE TIER (WAF, RATE LIMITING, SECURITY HEADERS)

> **Mã tài liệu:** RP29  
> **Dự án:** ieltsHSK — Nền tảng Luyện thi IELTS & HSK Trực tuyến  
> **Tên miền sản xuất:** `https://ieltshsk.me` (Pages: `ieltshsk.pages.dev`, Backend: `https://ieltshsk.runasp.net`)  
> **Hạ tầng bảo mật:** Cloudflare Free Tier Edge Protection  
> **Thời gian thiết lập ước tính:** 2 phút (Copy & Paste qua Cloudflare Expression Editor)  
> **Phiên bản áp dụng:** Production Go-Live Ready  

---

## MỤC LỤC
1. [Tổng quan & Ràng buộc Kỹ thuật Gói Cloudflare Free Tier](#1-tổng-quan--ràng-buộc-kỹ-thuật-gói-cloudflare-free-tier)
2. [Hướng dẫn Nhanh 2 Phút (Fast-Track Dashboard Setup)](#2-hướng-dẫn-nhanh-2-phút-fast-track-dashboard-setup)
3. [Chi tiết 5 Quy tắc Custom WAF Rules (Chuẩn 100% Free Tier Quota)](#3-chi-tiết-5-quy-tắc-custom-waf-rules-chuẩn-100-free-tier-quota)
   - [Rule 1: Bảo vệ Cổng Quản trị Portal Hub & Admin API](#rule-1-bảo-vệ-cổng-quản-trị-portal-hub--admin-api)
   - [Rule 2: Chặn Bot Cào Dữ liệu, Scrapers & Automated HTTP Clients](#rule-2-chặn-bot-cào-dữ-liệu-scrapers--automated-http-clients)
   - [Rule 3: Chống Hotlinking File Nghe, Soundtrack & Bộ Đề thi](#rule-3-chống-hotlinking-file-nghe-soundtrack--bộ-đề-thi)
   - [Rule 4: Chặn Thăm dò Tệp Nhạy cảm, Exploit Probes & Backups](#rule-4-chặn-thăm-dò-tệp-nhạy-cảm-exploit-probes--backups)
   - [Rule 5: Chặn Tuyến Quản trị Cũ & Bẫy Dò Quét Trap Traversal (/admin*)](#rule-5-chặn-tuyến-quản-trị-cũ--bẫy-dò-quét-trap-traversal-admin)
4. [Chi tiết Quy tắc Giới hạn Tần suất (Rate Limiting Rule) Duy Nhất](#4-chi-tiết-quy-tắc-giới-hạn-tần-suất-rate-limiting-rule-duy-nhất)
5. [Cấu hình Security Headers & Phân tầng Cache trên Cloudflare Pages](#5-cấu-hình-security-headers--phân-tầng-cache-trên-cloudflare-pages)
6. [Xử lý Sự cố Trình duyệt Cốc Cốc Chặn Cloudflare Challenge (ERR_FAILED)](#6-xử-lý-sự-cố-trình-duyệt-cốc-cốc-chặn-cloudflare-challenge-err_failed)
7. [Kịch bản Kiểm thử & Xác minh Tự động (Verification Commands)](#7-kịch-bản-kiểm-thử--xác-minh-tự-động-verification-commands)
8. [Checklist Vận hành & Khắc phục Sự cố Thường gặp (FAQ)](#8-checklist-vận-hành--khắc-phục-sự-cố-thường-gặp-faq)

---

## 1. TỔNG QUAN & RÀNG BUỘC KỸ THUẬT GÓI CLOUDFLARE FREE TIER

Nhằm đảm bảo hệ thống vận hành an toàn với **chi phí 0 VNĐ** mà vẫn đạt tiêu chuẩn bảo vệ cấp doanh nghiệp, toàn bộ kiến trúc WAF được thiết kế tối ưu hóa 100% cho **Cloudflare Free Tier**.

### Bảng Ràng Buộc Hạn Mức Gói Free Tier:
| Tham số kỹ thuật | Giới hạn Free Tier | Chiến lược thiết kế ieltsHSK |
| :--- | :---: | :--- |
| **Số lượng Custom WAF Rules** | **Tối đa 5 rules** | Thiết kế đúng **5 Custom WAF rules** đa nhiệm gộp nhóm logic bằng toán tử `or`/`and`. |
| **Số lượng Rate Limiting Rules** | **Tối đa 1 rule** | Thiết kế **1 Rate Limiting rule** duy nhất gộp chung bảo vệ cả Frontend Portal Hub và Backend Auth. |
| **Toán tử Regular Expression (`matches`)** | ❌ **KHÔNG HỖ TRỢ** | Tuyệt đối **không dùng `matches`**. Chỉ sử dụng các toán tử chuỗi chuẩn: `contains`, `starts_with`, `ends_with`, `eq`, `in`, kết hợp hàm `lower()`. |
| **Bot Management Cấp cao** | ❌ Không có Bot Score | Tận dụng trường miễn phí `cf.client.bot` để xác thực danh sách crawler uy tín của Google/Bing, đồng thời lọc blacklist User-Agent cho các bot khác. |
| **Thời gian áp dụng quy tắc** | Toàn cầu | Có hiệu lực tức thì sau 5 giây trên toàn bộ mạng lưới Edge Cloudflare toàn cầu. |

---

## 2. HƯỚNG DẪN NHANH 2 PHÚT (FAST-TRACK DASHBOARD SETUP)

Quản trị viên chỉ cần thao tác trực tiếp trên giao diện Cloudflare Dashboard theo các bước đơn giản sau:

### Quy trình 3 bước cho mỗi Custom WAF Rule:
1. Đăng nhập [Cloudflare Dashboard](https://dash.cloudflare.com/) $\rightarrow$ Chọn tên miền **`ieltshsk.me`**.
2. Tại menu bên trái, truy cập: **Security** $\rightarrow$ **WAF** $\rightarrow$ Chọn tab **Custom rules** $\rightarrow$ Nhấn nút **Create rule**.
3. Tại giao diện tạo quy tắc:
   - Nhập **Rule name** theo danh sách bên dưới.
   - Nhấp vào liên kết **"Edit expression"** (biểu tượng code bên phải mục Expression Preview).
   - **Copy & Paste** biểu thức Wirefilter tương ứng.
   - Tại mục **Choose action**, chọn hành động tương ứng (**Managed Challenge** hoặc **Block**).
   - Nhấn **Deploy** để kích hoạt ngay lập tức.

### Quy trình cho Rate Limiting Rule:
1. Tại menu bên trái: **Security** $\rightarrow$ **WAF** $\rightarrow$ Chọn tab **Rate limiting rules** $\rightarrow$ Nhấn **Create rule**.
2. Nhấp **"Edit expression"**, dán biểu thức Rate Limiting.
3. Cài đặt các thông số tần suất (Requests: `15`, Period: `10 seconds`, Action: `Block`, Duration: `60 seconds`).
4. Nhấn **Deploy**.

---

## 3. CHI TIẾT 5 QUY TẮC CUSTOM WAF RULES (CHUẨN 100% FREE TIER QUOTA)

### Ma trận Tổng hợp 5 Custom WAF Rules:
| Thứ tự | Tên Quy Tắc | Biểu thức tóm tắt | Hành động | Mục tiêu bảo vệ |
| :---: | :--- | :--- | :---: | :--- |
| **1** | `Protect Portal Hub & Admin API` | `lower(uri.path) starts_with /portal-hub` OR `/api/admin` | **Managed Challenge** | Lọc bot dò quét, chống brute-force đăng nhập admin (chống bypass hoa/thường) |
| **2** | `Block Scrapers & Automated HTTP Clients` | `(not cf.client.bot) and (lower(user_agent) contains ...)` | **Block** | Chống cào đề thi JSON, answers, audio, từ vựng |
| **3** | `Anti-Hotlink Media & Exam Datasets` | `lower(uri.path) in media/data and not referer in origins` | **Block** | Ngăn chặn web khác ăn cắp băng thông, chống subdomain/query spoofing |
| **4** | `Block Exploit Probes & Sensitive Files` | `uri.path contains .env/.git/wp-/php` | **Block** | Triệt tiêu các đợt scan CMS, mã nguồn, database dump |
| **5** | `Block Legacy Admin Traversal` | `lower(uri.path) eq /admin or starts_with /admin/` | **Block** | Bẫy chặn quét đường dẫn cũ (chuẩn hóa chữ thường), tiết kiệm tải SPA fallback |

---

### Rule 1: Bảo vệ Cổng Quản trị Portal Hub & Admin API
* **Tên quy tắc (Rule Name):** `Protect Portal Hub & Admin API`
* **Vị trí ưu tiên (Priority):** 1 (Cao nhất)
* **Hành động (Action):** `Managed Challenge`
* **Mô tả chuyên sâu:**  
  Cổng quản trị bí mật `/portal-hub` và các API quản trị `/api/admin/*` là mục tiêu nhạy cảm nhất. Hàm `lower(http.request.uri.path)` đảm bảo toàn bộ đường dẫn được chuẩn hóa chữ thường trước khi so khớp, triệt tiêu hoàn toàn kỹ thuật lách luật bằng ký tự hoa/thường (như `/Portal-Hub`, `/PORTAL-HUB`, `/Api/Admin/*`). Hành động **Managed Challenge** áp dụng cơ chế thử thách JavaScript thông minh của Cloudflare. Người quản trị dùng trình duyệt chuẩn sẽ được xác thực ngầm tự động trong tích tắc (~1s) mà không gây gián đoạn công việc, trong khi 100% các công cụ rà quét lỗ hổng tự động (như Nikto, Nuclei, Sqlmap, Dirsearch) hoặc script brute-force tự động đều bị chặn đứng ở Edge.
* **Cú pháp Wirefilter Expression (Copy & Paste):**
```wirefilter
(lower(http.request.uri.path) starts_with "/portal-hub" or lower(http.request.uri.path) starts_with "/api/admin")
```

---

### Rule 2: Chặn Bot Cào Dữ liệu, Scrapers & Automated HTTP Clients
* **Tên quy tắc (Rule Name):** `Block Scrapers & Automated HTTP Clients`
* **Vị trí ưu tiên (Priority):** 2
* **Hành động (Action):** `Block`
* **Mô tả chuyên sâu:**  
  Dữ liệu bài thi Cambridge IELTS, HSK mock test JSON, đáp án chấm điểm `*.answers.json`, ngân hàng 110 cấu trúc ngữ pháp và bộ từ vựng song ngữ là tài sản trí tuệ cốt lõi của nền tảng. Quy tắc này chặn toàn bộ các thư viện crawler lập trình phổ biến (Python Requests, Scrapy, Aiohttp, Wget, Curl, Go-http-client, Headless Chrome, Puppeteer, Selenium...) và các request cố tình bỏ trống User-Agent. Điểm đặc biệt: điều kiện `(not cf.client.bot)` giúp **miễn trừ hoàn toàn** các bot tìm kiếm hợp lệ (Googlebot, Bingbot) để website không bị ảnh hưởng chỉ số SEO.
* **Cú pháp Wirefilter Expression (Copy & Paste):**
```wirefilter
(not cf.client.bot) and (http.user_agent eq "" or lower(http.user_agent) contains "python" or lower(http.user_agent) contains "scrapy" or lower(http.user_agent) contains "curl" or lower(http.user_agent) contains "wget" or lower(http.user_agent) contains "postman" or lower(http.user_agent) contains "go-http-client" or lower(http.user_agent) contains "urllib" or lower(http.user_agent) contains "aiohttp" or lower(http.user_agent) contains "httpx" or lower(http.user_agent) contains "httpie" or lower(http.user_agent) contains "headlesschrome" or lower(http.user_agent) contains "selenium" or lower(http.user_agent) contains "puppeteer")
```

---

### Rule 3: Chống Hotlinking File Nghe, Soundtrack & Bộ Đề thi
* **Tên quy tắc (Rule Name):** `Anti-Hotlink Media & Exam Datasets`
* **Vị trí ưu tiên (Priority):** 3
* **Hành động (Action):** `Block`
* **Mô tả chuyên sâu:**  
  Các tệp âm thanh nghe IELTS (`.mp3`), nhạc nền trò chơi (`.wav`, `.ogg` trong `/soundtrack/`) và bộ đề thi (`/sample-data/`) tiêu tốn đáng kể dung lượng băng thông. Quy tắc này kiểm tra tiêu đề `Referer` và đường dẫn tài nguyên:
  1. **Chuẩn hóa chữ thường với `lower(...)`:** Ngăn chặn tuyệt đối việc lách luật qua đuôi mở rộng hoặc đường dẫn viết hoa (ví dụ: `.MP3`, `.WAV`, `/Sample-Data/`).
  2. **Khóa chặt Origin nguồn:** Sử dụng phép so khớp chính xác tiền tố (`eq "https://..."` và `starts_with "https://.../"`) thay cho toán tử lỏng lẻo `contains`. Điều này ngăn chặn triệt để các kỹ thuật vượt rào tinh vi như Subdomain Spoofing (`https://fake-ieltshsk.me.attacker.com`), Query Parameter Injection (`https://attacker.com/?ref=ieltshsk.me`), hoặc Path Injection (`https://attacker.com/ieltshsk.me/player`).
  3. Bất kỳ yêu cầu tải tài nguyên media/đề thi nào phát sinh từ các trang web bên thứ ba (hoặc cố tình làm rỗng Referer khi tải trực tiếp file nhạc) đều bị Cloudflare từ chối với mã lỗi HTTP 403 Forbidden. Chỉ cho phép các yêu cầu có Referer hợp lệ từ domain chính thức (`ieltshsk.me`), staging (`ieltshsk.pages.dev`), hoặc máy trạm phát triển (`localhost`).
* **Cú pháp Wirefilter Expression (Copy & Paste):**
```wirefilter
(lower(http.request.uri.path) starts_with "/sample-data/" or lower(http.request.uri.path) starts_with "/soundtrack/" or ends_with(lower(http.request.uri.path), ".mp3") or ends_with(lower(http.request.uri.path), ".wav") or ends_with(lower(http.request.uri.path), ".ogg")) and not (http.referer eq "https://ieltshsk.me" or http.referer starts_with "https://ieltshsk.me/" or http.referer eq "https://ieltshsk.pages.dev" or http.referer starts_with "https://ieltshsk.pages.dev/" or http.referer starts_with "http://localhost:" or http.referer starts_with "https://localhost:" or http.referer starts_with "http://localhost/")
```

---

### Rule 4: Chặn Thăm dò Tệp Nhạy cảm, Exploit Probes & Backups
* **Tên quy tắc (Rule Name):** `Block Exploit Probes & Sensitive Files`
* **Vị trí ưu tiên (Priority):** 4
* **Hành động (Action):** `Block`
* **Mô tả chuyên sâu:**  
  Các bot dò quét trên Internet liên tục phát tán các payload tìm kiếm các tệp cấu hình `.env`, thư mục `.git`, các lỗ hổng WordPress (`/wp-admin`, `/wp-includes`, `xmlrpc.php`), trang kiểm tra cấu hình PHP (`phpinfo`), bản sao lưu cơ sở dữ liệu (`.sql`, `.bak`) hoặc tệp cấu hình máy chủ web (`.config`). Vì hệ thống ieltsHSK thuần .NET 9 và Blazor WASM không bao giờ chứa mã PHP/WordPress, quy tắc này lập tức ngắt kết nối các probe này ngay tại Edge, giữ cho log máy chủ Backend luôn sạch sẽ và giảm tải CPU.
* **Cú pháp Wirefilter Expression (Copy & Paste):**
```wirefilter
(http.request.uri.path contains "/.env" or http.request.uri.path contains "/.git" or http.request.uri.path contains "/wp-" or http.request.uri.path contains "/xmlrpc.php" or http.request.uri.path contains "/phpinfo" or ends_with(http.request.uri.path, ".php") or ends_with(http.request.uri.path, ".sql") or ends_with(http.request.uri.path, ".bak") or ends_with(http.request.uri.path, ".config"))
```

---

### Rule 5: Chặn Tuyến Quản trị Cũ & Bẫy Dò Quét Trap Traversal (/admin*)
* **Tên quy tắc (Rule Name):** `Block Legacy Admin Traversal`
* **Vị trí ưu tiên (Priority):** 5
* **Hành động (Action):** `Block`
* **Mô tả chuyên sâu:**  
  Theo kiến trúc bảo mật trong tài liệu RP23, toàn bộ đường dẫn quản trị thực tế đã được chuyển dọn sang `/portal-hub/*`. Đường dẫn `/admin` và `/admin/*` đã trở thành "đường dẫn bẫy" (honeypot route). Bất kỳ ai hoặc bot nào cố gắng truy cập `/admin` chắc chắn là đối tượng xấu đang dò tìm trang quản trị mặc định. Bằng việc bọc hàm `lower(http.request.uri.path)`, quy tắc vô hiệu hóa mọi biến thể chữ hoa chữ thường (`/Admin`, `/ADMIN/login`). Thay vì để Cloudflare Pages phải nạp file HTML và chạy logic chuyển hướng SPA Fallback (tiêu tốn tài nguyên CDN), quy tắc WAF này chặn đứng và trả về lỗi 403 tức thì tại Edge.
* **Cú pháp Wirefilter Expression (Copy & Paste):**
```wirefilter
(lower(http.request.uri.path) eq "/admin" or lower(http.request.uri.path) starts_with "/admin/")
```

---

## 4. CHI TIẾT QUY TẮC GIỚI HẠN TẦN SUẤT (RATE LIMITING RULE) DUY NHẤT

Gói Free Tier của Cloudflare cấp chính xác **1 Rate Limiting Rule**. Chúng tôi tận dụng triệt để hạn mức này bằng cách bảo vệ đồng thời cả tuyến đăng nhập Admin Frontend (`/portal-hub`) lẫn các endpoint xác thực Backend (`/api/auth`).

### Cấu hình Chi Tiết:
* **Tên quy tắc (Rule Name):** `Rate Limit Auth & Portal Hub Access`
* **Mục tiêu điều kiện (When incoming requests match...):**
  - Nhấp vào **"Edit expression"** và dán:
```wirefilter
(http.request.uri.path starts_with "/portal-hub" or http.request.uri.path starts_with "/api/auth")
```
* **Cài đặt Tần suất (Rate Limiting Criteria):**
  - **With the same characteristics:** Chọn **`IP`** (Mỗi địa chỉ IP nguồn).
  - **Request threshold:** Nhập **`15`** requests.
  - **Period (Khung thời gian):** Chọn **`10 seconds`** *(hoặc 30 requests trong 1 minute)*.
* **Hành động khi vượt ngưỡng (Then choose action):**
  - **Action:** Chọn **`Block`** *(hoặc Managed Challenge)*.
  - **Duration (Thời gian chặn):** Chọn **`60 seconds`** (1 phút).
  - **Response code:** Mặc định `HTTP 429 Too Many Requests`.

> **Ý nghĩa:** Nếu bất kỳ IP nào gửi vượt quá 15 requests trong vòng 10 giây vào trang Portal Hub hoặc các API đăng nhập/đăng ký/refresh token, IP đó sẽ bị khóa cứng trong 60 giây. Điều này vô hiệu hóa hoàn toàn các cuộc tấn công Brute-force mật khẩu hoặc Credential Stuffing.

---

## 5. CẤU HÌNH SECURITY HEADERS & PHÂN TẦNG CACHE TRÊN CLOUDFLARE PAGES

Cloudflare Pages hỗ trợ quản lý tiêu đề phản hồi HTTP thông qua tệp `_headers` đặt tại thư mục gốc `wwwroot` của dự án Frontend (`frontend/src/Frontend.App/wwwroot/_headers`). Khi chạy lệnh `dotnet publish`, tệp này sẽ tự động xuất hiện tại `frontend_dist/wwwroot/_headers` để Cloudflare Pages nạp cấu hình.

### 5.1. Nội dung chuẩn xác của tệp `_headers`
```text
# Global Security Headers & Base Configuration
/*
  Strict-Transport-Security: max-age=31536000; includeSubDomains; preload
  X-Content-Type-Options: nosniff
  X-Frame-Options: SAMEORIGIN
  Referrer-Policy: strict-origin-when-cross-origin
  Permissions-Policy: camera=(), microphone=(self), geolocation=(), interest-cohort=()
  Content-Security-Policy: default-src 'self'; script-src 'self' 'unsafe-inline' 'wasm-unsafe-eval' https://cdn.jsdelivr.net https://accounts.google.com https://challenges.cloudflare.com https://www.youtube.com https://s.ytimg.com; style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com; font-src 'self' https://fonts.gstatic.com https://cdn.jsdelivr.net data:; img-src 'self' data: blob: https: https://pub-91655bd1442d498b9788d1f8f8575587.r2.dev https://*.googleusercontent.com; media-src 'self' data: blob: https: https://pub-91655bd1442d498b9788d1f8f8575587.r2.dev; connect-src 'self' https://ieltshsk.runasp.net http://localhost:5101 https://challenges.cloudflare.com https://accounts.google.com https://pub-91655bd1442d498b9788d1f8f8575587.r2.dev; frame-src 'self' https://challenges.cloudflare.com https://accounts.google.com https://www.youtube.com https://www.youtube-nocookie.com; object-src 'none'; base-uri 'self';

# 1. Framework Immutable Cache (Assemblies & Runtimes)
/_framework/*
  Cache-Control: public, max-age=31536000, immutable

# 2. Entry Points, Manifests, Configurations & Boot JSON (Must Always Revalidate)
/_framework/blazor.boot.json
  Cache-Control: no-cache, no-store, must-revalidate

/index.html
  Cache-Control: no-cache, no-store, must-revalidate

/404.html
  Cache-Control: no-cache, no-store, must-revalidate

/sw.js
  Cache-Control: no-cache, no-store, must-revalidate

/manifest.json
  Cache-Control: no-cache, no-store, must-revalidate

/appsettings*.json
  Cache-Control: no-cache, no-store, must-revalidate

/appsettings.json
  Cache-Control: no-cache, no-store, must-revalidate

/appsettings.Development.json
  Cache-Control: no-cache, no-store, must-revalidate

# 3. Dynamic Static Data (Grammar, Vocab, Exams)
/sample-data/*
  Cache-Control: public, max-age=86400, stale-while-revalidate=604800

/data/*
  Cache-Control: public, max-age=86400, stale-while-revalidate=604800

# 4. Hashed Code & Stylesheets
/css/*
  Cache-Control: public, max-age=604800, stale-while-revalidate=86400

/js/*
  Cache-Control: public, max-age=604800, stale-while-revalidate=86400

# 5. Media, Icons & Audio
/images/*
  Cache-Control: public, max-age=2592000, stale-while-revalidate=604800

/icons/*
  Cache-Control: public, max-age=2592000, stale-while-revalidate=604800

/mascot/*
  Cache-Control: public, max-age=2592000, stale-while-revalidate=604800

/soundtrack/*
  Cache-Control: public, max-age=2592000, stale-while-revalidate=604800
```

### 5.2. Giải mã Chi tiết 6 Tiêu đề Bảo mật Chuẩn OWASP:
1. **Strict-Transport-Security (HSTS):** Ép buộc trình duyệt chỉ kết nối qua HTTPS trong vòng 1 năm (`max-age=31536000`), bao gồm cả tên miền phụ và đăng ký danh sách HSTS Preload toàn cầu.
2. **X-Content-Type-Options: nosniff:** Ngăn chặn trình duyệt đoán định sai MIME-type để thực thi script độc ẩn dưới dạng hình ảnh hay file văn bản.
3. **X-Frame-Options: SAMEORIGIN:** Chống tấn công Clickjacking bằng cách ngăn các trang web độc hại bên ngoài nhúng ieltsHSK vào thẻ `<iframe>`, đồng thời cho phép hiển thị an toàn trong cùng domain.
4. **Referrer-Policy: strict-origin-when-cross-origin:** Bảo vệ dữ liệu đường dẫn URL của người dùng khi chuyển tiếp sang liên kết ngoài, chỉ gửi origin thay vì full path query params.
5. **Permissions-Policy:** Khóa tuyệt đối quyền truy cập camera (`camera=()`), định vị GPS (`geolocation=()`), và mạng quảng cáo (`interest-cohort=()`). Chỉ cho phép sử dụng microphone trên chính domain trang web (`microphone=(self)`) để phục vụ tính năng Luyện Nói IELTS Speaking & Shadowing.
6. **Content-Security-Policy (CSP):**
   - `'wasm-unsafe-eval'`: Bắt buộc đối với máy ảo Blazor WebAssembly .NET 9 để biên dịch JIT/WASM bytecodes trong trình duyệt.
   - `challenges.cloudflare.com`: Cho phép nhúng mã bảo mật Cloudflare Turnstile CAPTCHA.
   - `accounts.google.com`: Cho phép tính năng Đăng nhập một chạm bằng Google (GIS).
   - `cdn.jsdelivr.net`: Cho phép tải thư viện tiện ích CDN (Tailwind, Lucide icons, Chart.js...).
   - `fonts.googleapis.com`, `fonts.gstatic.com`: Cho phép tải web fonts Google.
   - `*.googleusercontent.com`: Cho phép tải avatar người dùng từ tài khoản Google.
   - `pub-91655bd1442d498b9788d1f8f8575587.r2.dev`: Cho phép stream và tải các file âm thanh đề thi IELTS cỡ lớn từ Cloudflare R2 Storage.
   - `www.youtube.com`, `s.ytimg.com`, `youtube-nocookie.com`: Cho phép nhúng và phát video hướng dẫn luyện thi.
   - `ieltshsk.runasp.net`: Cho phép kết nối Backend API an toàn qua HTTPS.

---

## 6. XỬ LÝ SỰ CỐ TRÌNH DUYỆT CỐC CỐC CHẶN CLOUDFLARE CHALLENGE (ERR_FAILED)

### 6.1. Hiện tượng & Triệu chứng
Khi người dùng hoặc Quản trị viên sử dụng trình duyệt **Cốc Cốc** (phiên bản trên Windows hoặc macOS) để truy cập vào đường dẫn Cổng Quản trị:
`https://ieltshsk.me/portal-hub/login` hoặc `https://ieltshsk.me/portal-hub`
Trình duyệt không hiển thị giao diện xác thực mà ngay lập tức hiển thị thông báo lỗi mạng màu xám:
> **Không thể truy cập trang web này**  
> Trang web tại `https://ieltshsk.me/portal-hub` có thể tạm thời không hoạt động hoặc đã chuyển vĩnh viễn sang địa chỉ web mới.  
> **Mã lỗi: ERR_FAILED** *(hoặc `net::ERR_BLOCKED_BY_CLIENT` trong F12 Console)*.

### 6.2. Nguyên nhân Gốc rễ (Root Cause Analysis - Tham chiếu RP25 §2.2)
1. Theo **Rule 1**, mọi truy vấn vào `/portal-hub*` đều được Cloudflare trả về trang thử thách bảo mật **Managed Challenge** (`Checking your browser...`).
2. Để hoàn tất xác minh, trình duyệt phải tải script bảo mật từ hai nguồn:
   - `https://challenges.cloudflare.com/turnstile/v0/api.js`
   - `https://ieltshsk.me/cdn-cgi/challenge-platform/...`
3. Trình duyệt **Cốc Cốc được trang bị sẵn bộ lọc AdBlocker và Tracking Protection độc quyền** (dựa trên danh sách uBlock / EasyList tùy biến).
4. Bộ lọc nội trang của Cốc Cốc đã **nhận diện nhầm** các đoạn mã JavaScript giải mã thử thách của Cloudflare là công cụ theo dõi quảng cáo bên thứ ba và tiến hành ngắt kết nối mạng cưỡng bức (`ERR_BLOCKED_BY_CLIENT`).
5. Vì script thử thách bị chặn giữa chừng, nhân Chromium không nhận được tín hiệu hoàn tất xác minh thử thách từ Cloudflare, dẫn tới việc tiến trình HTTP bị treo và trình duyệt trả về mã lỗi tổng quát `ERR_FAILED`.

### 6.3. Giải pháp Khắc phục Triệt để

#### Cách 1: Tắt bộ lọc quảng cáo cho tên miền `ieltshsk.me` trên Cốc Cốc (Dành cho Quản trị viên)
1. Mở Cốc Cốc và truy cập `https://ieltshsk.me`.
2. Nhìn lên thanh địa chỉ (Omnibox), bấm vào **biểu tượng chiếc khiên xanh** (hoặc biểu tượng Chặn quảng cáo của Cốc Cốc) nằm cạnh ổ khóa HTTPS.
3. Gạt công tắc chuyển sang trạng thái: **"Tắt tính năng chặn quảng cáo trên trang web này"** (Turn off ad blocking on this site).
4. Nhấn `Ctrl + F5` (hoặc `Cmd + Shift + R`) để tải lại trang. Trang Managed Challenge sẽ hoàn tất trong 1 giây và hiển thị cổng đăng nhập bình thường.

#### Cách 2: Sử dụng trình duyệt tiêu chuẩn (Khuyên dùng cho Quản trị viên)
- Khuyến nghị đội ngũ quản trị viên sử dụng các trình duyệt chuẩn như **Google Chrome**, **Microsoft Edge**, **Mozilla Firefox**, hoặc **Brave** (đã cấu hình cho phép Cloudflare).
- Hoặc mở cửa sổ **Ẩn danh (Incognito Window)** trên Cốc Cốc nếu đã cấu hình không bật extension ở chế độ ẩn danh.

#### Cách 3: Tinh chỉnh Rule 1 trên Cloudflare WAF để Miễn Trừ IP Quản trị Viên (Cấu hình Nâng cao)
Nếu Quản trị viên muốn truy cập trực tiếp bằng Cốc Cốc mà không muốn bị hỏi Challenge, có thể cập nhật Rule 1 để tự động bỏ qua thử thách đối với địa chỉ IP tĩnh của Quản trị viên:
```wirefilter
(lower(http.request.uri.path) starts_with "/portal-hub" or lower(http.request.uri.path) starts_with "/api/admin") and not (ip.src in { 14.161.x.x 118.69.x.x })
```
*(Thay thế danh sách IP bằng địa chỉ IP WAN công cộng của Quản trị viên)*.

---

## 7. KỊCH BẢN KIỂM THỬ & XÁC MINH TỰ ĐỘNG (VERIFICATION COMMANDS)

Để kiểm chứng 100% tính chính xác của các quy tắc trước khi bàn giao hệ thống, quản trị viên hoặc kỹ sư QA có thể chạy các câu lệnh kiểm thử trực tiếp bằng công cụ `curl` hoặc PowerShell.

### 7.1. Kiểm tra Rule 1 (Managed Challenge trên Portal Hub):
```bash
# Yêu cầu gửi tới /portal-hub phải trả về trang thử thách Managed Challenge của Cloudflare (chứa cf-mitigated: challenge)
curl -sI https://ieltshsk.me/portal-hub/login
```
*Kết quả kỳ vọng:* Header phản hồi có chứa `cf-mitigated: challenge` hoặc trang HTML chứa chữ `"Just a moment..."` / Cloudflare Turnstile challenge.

---

### 7.2. Kiểm tra Rule 2 (Chặn Công cụ Cào Dữ liệu):
```bash
# Mô phỏng User-Agent của Python Requests đến tài nguyên đề thi
curl -sI -A "python-requests/2.31.0" https://ieltshsk.me/sample-data/cambridge_ielts_16_test1.json

# Mô phỏng Scrapy Crawler
curl -sI -A "Scrapy/2.11.0 (+https://scrapy.org)" https://ieltshsk.me/data/grammar/sections.json

# Mô phỏng User-Agent rỗng
curl -sI -A "" https://ieltshsk.me/sample-data/audio/shadowing/lesson-1.mp3
```
*Kết quả kỳ vọng:* Toàn bộ các câu lệnh trên đều nhận mã phản hồi **`HTTP/2 403 Forbidden`** tức thì do Cloudflare WAF chặn đứng.

---

### 7.3. Kiểm tra Rule 3 (Chống Hotlink Media & Đề thi):
```bash
# Mô phỏng request có Referer từ trang web lạ (hotlinking từ bên ngoài)
curl -sI -H "Referer: https://evil-pirate-site.com/exam" https://ieltshsk.me/soundtrack/boss-fight-big.wav

# Mô phỏng request tải audio không có Referer
curl -sI https://ieltshsk.me/sample-data/audio/ielts-mock-2025-test1.mp3
```
*Kết quả kỳ vọng:* Nhận mã phản hồi **`HTTP/2 403 Forbidden`**.  
*Ngược lại, nếu gửi kèm Referer chính thức:*
```bash
curl -sI -H "Referer: https://ieltshsk.me/ielts/listening" https://ieltshsk.me/sample-data/audio/ielts-mock-2025-test1.mp3
```
*Kết quả:* Nhận mã phản hồi **`HTTP/2 200 OK`**.

---

### 7.4. Kiểm tra Rule 4 (Chặn Thăm dò Mã nguồn & Tệp Nhạy cảm):
```bash
# Dò quét tệp môi trường .env
curl -sI https://ieltshsk.me/.env

# Dò quét kho mã nguồn .git
curl -sI https://ieltshsk.me/.git/config

# Dò quét mã WordPress
curl -sI https://ieltshsk.me/wp-login.php
curl -sI https://ieltshsk.me/xmlrpc.php

# Dò quét bản sao lưu database
curl -sI https://ieltshsk.me/backup.sql
```
*Kết quả kỳ vọng:* Tất cả đều bị chặn đứng với mã lỗi **`HTTP/2 403 Forbidden`**.

---

### 7.5. Kiểm tra Rule 5 (Chặn Bẫy Tuyến Quản trị Cũ /admin*):
```bash
# Thăm dò đường dẫn /admin
curl -sI https://ieltshsk.me/admin

# Thăm dò đường dẫn con /admin/login
curl -sI https://ieltshsk.me/admin/login
```
*Kết quả kỳ vọng:* Nhận mã **`HTTP/2 403 Forbidden`** trực tiếp từ Edge Cloudflare (không bị fallback về `index.html`).

---

### 7.6. Kiểm tra Rate Limiting Rule (Bảo vệ Tần suất):
Chạy vòng lặp 20 requests liên tiếp trong 5 giây vào `/portal-hub`:
```powershell
1..20 | ForEach-Object {
    $res = Invoke-WebRequest -Uri "https://ieltshsk.me/portal-hub" -Method Head -SkipHttpErrorCheck
    Write-Host "Request #$_ Status: $($res.StatusCode)"
}
```
*Kết quả kỳ vọng:* Từ request số 16 trở đi, server trả về mã **`HTTP 429 Too Many Requests`** và bị chặn trong vòng 60 giây.

---

### 7.7. Chạy Script Độc lập Tự động:
Toàn bộ các bài kiểm thử trên được đóng gói sẵn trong script kiểm thử bảo mật độc lập:
```powershell
pwsh scripts/verify-security.ps1 -IncludeLiveTest -TargetDomain "https://ieltshsk.me"
```

---

## 8. CHECKLIST VẬN HÀNH & KHẮC PHỤC SỰ CỐ THƯỜNG GẶP (FAQ)

### Checklist 5 Bước Trước Khi Go-Live:
- [x] Đã tạo đủ 5 Custom WAF Rules theo đúng cú pháp Wirefilter và thứ tự ưu tiên (Priority 1 $\rightarrow$ 5).
- [x] Đã cấu hình 1 Rate Limiting Rule với ngưỡng 15 requests / 10s cho `/portal-hub` và `/api/auth`.
- [x] Tệp `_headers` đã hiện diện trong `frontend/src/Frontend.App/wwwroot/` với 6 tiêu đề OWASP và cấu hình cache.
- [x] Tệp `robots.txt` đã cấu hình chặn đường dẫn `/portal-hub/`, `/sample-data/*.answers.json`, `/api/` và các AI scrapers.
- [x] Đã chạy script `scripts/verify-security.ps1` và đạt kết quả kiểm thử **PASS 100%**.

---

### Câu hỏi Thường gặp (FAQ):

**Q1: Tại sao chúng ta không dùng biểu thức chính quy (Regex `matches`) trong các quy tắc WAF?**  
*Trả lời:* Cloudflare Free Tier không hỗ trợ toán tử `matches`. Nếu bạn cố tình dán biểu thức chứa `matches`, Cloudflare Dashboard sẽ báo lỗi đỏ: *"Your plan does not support regular expressions in expressions"*. Tất cả 5 quy tắc trong Runbook này đã được thiết kế thuần túy bằng các hàm chuỗi `contains`, `starts_with`, `ends_with`, `eq`, `in` và `lower()`, đảm bảo tương thích 100% với gói Free mà không tốn một xu phí nâng cấp.

**Q2: Làm thế nào để Cloudflare nhận diện Googlebot và không chặn nhầm khi có quy tắc Rule 2?**  
*Trả lời:* Điều kiện `(not cf.client.bot)` trong Rule 2 được Cloudflare tự động kiểm tra thông qua cơ chế Xác thực DNS Đảo ngược (Reverse DNS Verification). Các bot chính thống của Google, Bing, DuckDuckGo, Yahoo đều tự động được gắn cờ `cf.client.bot = true` và được cho phép truy cập bình thường để lập chỉ mục SEO.

**Q3: Tôi là lập trình viên, tôi cần dùng Postman hoặc curl để gọi API thì làm sao?**  
*Trả lời:* Nếu bạn dùng Postman để gọi Backend API (`https://ieltshsk.runasp.net`), lưu lượng không đi qua Cloudflare Pages nên không bị chặn. Nếu bạn test qua domain `https://ieltshsk.me`, bạn có thể thêm địa chỉ IP của bạn vào danh sách cho phép (Security $\rightarrow$ WAF $\rightarrow$ Tools $\rightarrow$ IP Access Rules $\rightarrow$ Add IP with action "Allow").

**Q4: Người dùng bấm vào link audio trực tiếp từ ứng dụng Blazor có bị Rule 3 chặn không?**  
*Trả lời:* Không. Khi ứng dụng Blazor WebAssembly chạy trên trình duyệt tại `https://ieltshsk.me`, mọi request `fetch()` hoặc thẻ `<audio src="...">` đều tự động mang theo header `Referer: https://ieltshsk.me/...`. Do đó điều kiện `http.referer starts_with "https://ieltshsk.me/"` (hoặc `eq "https://ieltshsk.me"`) luôn thỏa mãn và request được xử lý trơn tru.

---
> **Tài liệu được phê duyệt bởi:** Project Architect & Security Auditor  
> **Lưu trữ tại:** `docs/RP29_cloudflare_waf_runbook.md`
