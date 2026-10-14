# BÁO CÁO TOÀN DIỆN VÀ HƯỚNG DẪN TRIỂN KHAI FRONTEND LÊN CLOUDFLARE PAGES
**Dự án:** ieltsHSK  
**Thời gian thực hiện:** Ngày 10/10/2026  
**Trạng thái:** Website đã chính thức hoạt động hoàn tất trên internet với tên miền riêng và chứng chỉ SSL HTTPS.

---

## I. CÁC ĐƯỜNG LINK HỆ THỐNG QUAN TRỌNG

| Hạng mục | Đường dẫn (URL) | Trạng thái |
| :--- | :--- | :--- |
| **Website chính thức** | [https://ieltshsk.me](https://ieltshsk.me) | **Hoạt động (Active)** - SSL HTTPS |
| **Link CDN dự phòng** | [https://ieltshsk.pages.dev](https://ieltshsk.pages.dev) | **Hoạt động (Active)** |
| **Backend API (Health)** | [https://ieltshsk.runasp.net/health/ready](https://ieltshsk.runasp.net/health/ready) | **Hoạt động (Active)** - HTTPS |
| **Cloudflare Pages Dashboard** | [dash.cloudflare.com/.../pages/view/ieltshsk](https://dash.cloudflare.com/0a3da3f1052ab03fdd2013ddf4b463d6/pages/view/ieltshsk) | Quản lý bản build & Domain |
| **Namecheap Domain Manager** | [ap.www.namecheap.com](https://ap.www.namecheap.com/Domains/DomainControlPanel/ieltshsk.me/domain/) | Quản lý Nameservers |
| **Google Cloud Console (OAuth)** | [console.cloud.google.com/auth/clients](https://console.cloud.google.com/auth/clients) | Cấu hình đăng nhập Google |

---

## II. CHI TIẾT TỪNG BƯỚC ĐÃ THỰC HIỆN VÀ KHẮC PHỤC LỖI

### Bước 1: Điều chỉnh Mã nguồn Frontend kết nối Production
1. **Sửa logic nhận diện URL API trong `Program.cs`:**
   - Ban đầu mã nguồn ép cổng `:5101` đối với mọi domain không phải localhost (dành cho test qua IP LAN).
   - Đã cập nhật lại: Chỉ ép cổng khi truy cập bằng IP LAN, còn khi chạy bằng tên miền online (`ieltshsk.me`, `ieltshsk.pages.dev`) sẽ đọc chính xác `BaseUrl` từ file cấu hình.
2. **Tách biệt môi trường cấu hình:**
   - `appsettings.Development.json`: Giữ `https://localhost:7101/` cho việc chạy thử nghiệm offline trên máy.
   - `appsettings.json`: Cấu hình trỏ về Backend API chính thức `https://ieltshsk.runasp.net/` (bắt buộc HTTPS chống Mixed Content).
3. **Mở khóa CORS trên Backend (MonsterASP):**
   - Đã nạp cấu hình `AllowedOrigins` cho phép `https://ieltshsk.me`, `https://www.ieltshsk.me`, `https://ieltshsk.pages.dev` gọi API an toàn mà không bị trình duyệt chặn.

---

### Bước 2: Biên dịch và Tối ưu hóa Gói triển khai (Build & Optimize)
Thực hiện lệnh biên dịch:
```bash
dotnet publish frontend/src/Frontend.App/Frontend.App.csproj -c Release -o frontend_dist
```
Trong quá trình này, toàn bộ các rào cản kỹ thuật của Cloudflare Pages đã được giải quyết triệt để:

1. **Xử lý giới hạn 1.000 file của Cloudflare Pages:**
   - Ban đầu có 1.301 file (vượt giới hạn 1.000 file).
   - Giải pháp: Xóa bỏ 444 file nén `.br` và `.gz` do .NET sinh thừa (Cloudflare có hệ thống Edge nén Brotli tự động nên không cần các file này). Số file giảm xuống 857.
2. **Xử lý giới hạn kích thước file 25 MB:**
   - File `ielts-actual-vol1-test1.mp3` nặng 35.5 MB bị Cloudflare từ chối.
   - Giải pháp: Kiểm tra thấy file âm thanh này đã được tải sẵn lên Cloudflare R2 (`https://pub-91655bd1442d498b9788d1f8f8575587.r2.dev/audio/ielts-actual-vol1-test1.mp3`), do đó xóa bản nháp thừa cục bộ trong gói publish.
3. **Khắc phục lỗi "133 are unknown" khi kéo thả:**
   - Nguyên nhân: Các thư mục đề thi `sample-data/ETS 2024 Test` có chứa thư mục đặt tên tiếng Việt có dấu `Ảnh` khiến trình duyệt bị lỗi mã hóa đường dẫn (encoding).
   - Giải pháp: Loại bỏ các thư mục `ETS` nặng hơn 300 MB này khỏi gói `frontend_dist`.
   - Kết quả: Thư mục deploy rút gọn từ **425 MB xuống chỉ còn ~60 MB** với **270 file** siêu nhẹ và sạch.
4. **Cấu hình chống lỗi 404 khi tải lại trang (SPA Fallback):**
   - Bổ sung file `_redirects` với quy tắc `/* /index.html 200`.
   - Tạo file `404.html` đồng bộ với `index.html` để đảm bảo học viên nhấn F5 ở bất kỳ trang bài học nào cũng không bị lỗi trắng trang.

---

### Bước 3: Đưa lên Cloudflare Pages (Direct Upload)
1. Đăng nhập Cloudflare $\rightarrow$ Vào **Workers & Pages** $\rightarrow$ Tab **Pages** $\rightarrow$ **Upload assets**.
2. Đặt tên dự án: `ieltshsk`.
3. Tải toàn bộ thư mục `D:\project\ieltsHSK\frontend_dist\wwwroot` lên hệ thống.
4. Kích hoạt thành công dịch vụ tại địa chỉ: **`https://ieltshsk.pages.dev`**.

---

### Bước 4: Trỏ Tên miền chính thức `ieltshsk.me`
1. **Phát hiện nguyên nhân xung đột GitHub Pages cũ:**
   - Khi đăng ký tên miền bằng GitHub Student Pack, Namecheap mặc định trỏ về `YIZ006.github.io`.
2. **Chuyển quyền quản lý sang Cloudflare Nameservers:**
   - Vào Namecheap $\rightarrow$ Quản lý domain `ieltshsk.me` $\rightarrow$ Đổi mục **NAMESERVERS** sang **Custom DNS**.
   - Cập nhật 2 địa chỉ Nameservers của Cloudflare:
     * `dana.ns.cloudflare.com`
     * `troy.ns.cloudflare.com`
3. **Kích hoạt Custom Domain trên Cloudflare Pages:**
   - Vào dự án Pages `ieltshsk` $\rightarrow$ Tab **Custom domains** $\rightarrow$ Thêm `ieltshsk.me`.
   - Trạng thái chuyển sang: **`Active`** cùng chứng chỉ bảo mật **SSL enabled**.
4. Website chính thức mở tại: **`https://ieltshsk.me`**.

---

### Bước 5: Nâng cấp Cloudflare Turnstile CAPTCHA & Google OAuth
1. **Xóa bỏ cảnh báo "For testing only":**
   - Khởi tạo widget Turnstile chính thức trên Cloudflare Dashboard:
     * **Site Key:** `0x4AAAAAAFTD3Yv***` (Đã nạp vào Frontend)
     * **Secret Key:** `0x4AAAAAAFTD3W***` (Đã nạp bảo mật vào Backend)
   - Cập nhật Site Key vào `frontend_dist\wwwroot\appsettings.json`.
   - Nạp Secret Key lên Backend MonsterASP (`appsettings.Production.json`).
   - Widget Captcha chuyển sang chế độ bảo vệ chính thức, xóa bỏ hoàn toàn dòng chữ thử nghiệm.
2. **Cấu hình đăng nhập Google OAuth:**
   - Trên Google Cloud Console, thêm tên miền vào danh sách nguồn được phép:
     * **Authorized JavaScript origins:** `https://ieltshsk.me`, `https://www.ieltshsk.me`, `https://ieltshsk.pages.dev`.
     * **Authorized redirect URIs:** `https://ieltshsk.me/authentication/login-callback`.

---

## III. QUY TRÌNH CẬP NHẬT CODE (RE-DEPLOY) SIÊU TỐC

Hiện tại hệ thống đã được tối ưu hóa toàn diện với công cụ tự động hóa **Wrangler CLI** và **CacheBuster**. Bạn có 2 lựa chọn triển khai:

### Phương pháp 1: Deploy Tự Động 100% bằng Lệnh CLI (Khuyến nghị - Mất 10 giây)
Không cần mở trình duyệt web, không cần kéo thả thủ công! Chỉ cần mở PowerShell tại thư mục gốc của dự án:

```powershell
# Bước 1: Biên dịch gói Release & tự động CacheBust (không nén thừa)
dotnet publish frontend/src/Frontend.App/Frontend.App.csproj -c Release -o frontend_dist

# Bước 2: Đảm bảo file audio mẫu > 25MB không bị sót lại
Remove-Item -Path "frontend_dist/wwwroot/sample-data/audio/ielts-actual-vol1-test1.mp3" -Force -ErrorAction SilentlyContinue

# Bước 3: Đẩy trực tiếp lên Cloudflare Pages qua Wrangler CLI
npx wrangler pages deploy frontend_dist/wwwroot --project-name=ieltshsk
```

Sau khi chạy lệnh trên, Cloudflare Pages sẽ trả về ngay link preview và production: `https://ieltshsk.me` cập nhật hoàn tất!

---

### Phương pháp 2: Kéo thả thủ công trên Cloudflare Web Dashboard
1. Biên dịch và dọn dẹp thư mục:
   ```powershell
   dotnet publish frontend/src/Frontend.App/Frontend.App.csproj -c Release -o frontend_dist
   Remove-Item -Path "frontend_dist/wwwroot/sample-data/audio/ielts-actual-vol1-test1.mp3" -Force -ErrorAction SilentlyContinue
   ```
2. Vào Dashboard: [Cloudflare Pages - ieltshsk](https://dash.cloudflare.com/0a3da3f1052ab03fdd2013ddf4b463d6/pages/view/ieltshsk).
3. Bấm **`Create deployment`** ở góc trên bên phải.
4. Kéo thả trực tiếp thư mục: **`D:\project\ieltsHSK\frontend_dist\wwwroot`** (Gói đã được rút gọn chỉ còn 301 file sạch, không còn lỗi `117 are unknown`).
5. Bấm **`Save and Deploy`**.

---

## IV. BÍ QUYẾT XÓA CACHE ĐỂ WEB CẬP NHẬT NGAY LẬP TỨC (0 GIÂY)

Khi vừa deploy xong nhưng vào web vẫn thấy bản cũ, đó là do cơ chế lưu bộ nhớ đệm (Cache & Service Worker) của trình duyệt. Hãy xử lý ngay bằng các cách sau:

1. **Xóa Cache nhanh trên trình duyệt:**
   - Bấm tổ hợp phím: **`Ctrl + Shift + R`** (hoặc **`Ctrl + F5`**).
   - Hoặc mở bằng Cửa sổ ẩn danh: **`Ctrl + Shift + N`**.
2. **Xóa toàn bộ Cache CDN Cloudflare Edge (Purge Everything):**
   - Vào Dashboard Cloudflare $\rightarrow$ Chọn tên miền **`ieltshsk.me`**.
   - Menu bên trái: Chọn **Caching** $\rightarrow$ **Configuration**.
   - Bấm nút **Purge Everything** $\rightarrow$ Xác nhận. Trong vòng 500 mili-giây, toàn bộ máy chủ Cloudflare trên khắp thế giới sẽ xóa sạch cache cũ và phục vụ bản mới nhất!
3. **Cơ chế tự động đã tích hợp trong mã nguồn:**
   - Đã đặt `{ updateViaCache: 'none' }` và `reg.update()` trong `index.html`.
   - Mỗi lần deploy, Service Worker được bump version (`app-v10-native-mp3`), trình duyệt của học viên sẽ tự động xóa sạch dữ liệu cũ trong `CacheStorage` và nạp bản mới.
