# SỔ TAY TOÀN TẬP: CÁC LỆNH VẬN HÀNH, TRIỂN KHAI & FIX LỖI HỆ THỐNG
> **Dự án:** ieltsHSK  
> **Cập nhật:** Tháng 10/2026  
> **Tài liệu tổng hợp:** Dành cho Quản trị viên và Lập trình viên để thao tác nhanh bằng dòng lệnh (Terminal / PowerShell).

---

## 1. TRIỂN KHAI FRONTEND (CLOUDFLARE PAGES)

### 🚀 Lệnh Deploy Siêu Tốc (1 Lệnh Ăn Ngay)
Chạy tổ hợp 3 lệnh này từ thư mục gốc của dự án để tự động build, dọn dẹp và đẩy thẳng lên Cloudflare Pages mà không cần kéo thả chuột:

```powershell
# 1. Biên dịch gói Release & tự động CacheBust (không sinh file .br/.gz thừa)
dotnet publish frontend/src/Frontend.App/Frontend.App.csproj -c Release -o frontend_dist

# 2. Xóa bỏ file âm thanh mẫu > 25MB (nếu có) để tránh lỗi trần file của Cloudflare
Remove-Item -Path "frontend_dist/wwwroot/sample-data/audio/ielts-actual-vol1-test1.mp3" -Force -ErrorAction SilentlyContinue

# 3. Đẩy trực tiếp lên Cloudflare Pages qua Wrangler CLI (mất 5 - 10 giây)
npx wrangler pages deploy frontend_dist/wwwroot --project-name=ieltshsk
```

### 🔍 Lệnh Kiểm tra Gói Deploy Trước Khi Đẩy
```powershell
# Kiểm tra xem có file nào vượt quá 25MB không (phải bằng 0)
Get-ChildItem -Path "frontend_dist/wwwroot" -Recurse -File | Where-Object { $_.Length -gt 25000000 } | Select-Object FullName, Length

# Kiểm tra xem có file nén thừa .br hoặc .gz không (phải bằng 0)
(Get-ChildItem -Path "frontend_dist/wwwroot" -Recurse -Filter "*.gz").Count + (Get-ChildItem -Path "frontend_dist/wwwroot" -Recurse -Filter "*.br").Count

# Đếm tổng số lượng file trong gói deploy
(Get-ChildItem -Path "frontend_dist/wwwroot" -Recurse -File).Count
```

### 💻 Lệnh Chạy Frontend Cục bộ (Local Development)
```powershell
# Khởi chạy Frontend trên máy (cổng mặc định https://localhost:7102 hoặc http://localhost:5102)
dotnet run --project frontend/src/Frontend.App/Frontend.App.csproj
```

---

## 2. VẬN HÀNH & BIÊN DỊCH BACKEND (.NET 9 WEB API)

### 💻 Chạy Backend Cục bộ (Local Dev)
```powershell
# Chạy Backend API (cổng mặc định https://localhost:7101 hoặc http://localhost:5101)
dotnet run --project backend/src/Backend.Api/Backend.Api.csproj
```

### 🛠️ Build & Kiểm tra Lỗi Backend (.NET Solution & Project)
```powershell
# Build toàn bộ Solution Backend (bao gồm Domain, Application, Infrastructure, Api)
dotnet build backend/Backend.sln

# Hoặc build riêng dự án Backend.Api
dotnet build backend/src/Backend.Api/Backend.Api.csproj
```

### 📦 Xuất bản Gói Backend để Đưa Lên Server (MonsterASP / VPS)
```powershell
dotnet publish backend/src/Backend.Api/Backend.Api.csproj -c Release -o backend_dist
```

### 🌐 Kiểm tra Sức khỏe API (HealthCheck & Ping)
```powershell
# Kiểm tra máy chủ có phản hồi không
curl -I https://ieltshsk.runasp.net/swagger/index.html

# Kiểm tra API danh mục Audio Shadowing
curl https://ieltshsk.runasp.net/api/ielts/audio-shadowing

# Kiểm tra API Từ vựng HSK
curl "https://ieltshsk.runasp.net/api/hsk/vocab?level=HSK1"
```

---

## 3. BÍ QUYẾT XÓA CACHE ĐỂ WEB ĐỔI NGAY LẬP TỨC (0 GIÂY)

### ⌨️ Phím tắt trên Trình duyệt
* **Hard Refresh (Xóa cache trang hiện tại):** Nhấn `Ctrl + Shift + R` (hoặc `Ctrl + F5`).
* **Mở Cửa sổ Ẩn danh (Bỏ qua mọi cache):** Nhấn `Ctrl + Shift + N`.
* **Xóa Service Worker trong F12:** 
  1. Nhấn `F12` $\rightarrow$ Chọn tab **Application** (hoặc **Ứng dụng**).
  2. Bên trái chọn **Service workers** $\rightarrow$ Bấm nút **Unregister**.
  3. Bấm sang tab **Storage** $\rightarrow$ Bấm **Clear site data**.

### ☁️ Xóa Toàn Cầu trên Cloudflare Dashboard (Purge Everything)
1. Truy cập [Cloudflare Dashboard](https://dash.cloudflare.com/) $\rightarrow$ Chọn tên miền **`ieltshsk.me`**.
2. Chọn menu **Caching** $\rightarrow$ **Configuration**.
3. Bấm **Purge Everything** $\rightarrow$ Xác nhận. Trong 500ms, toàn bộ cache trên máy chủ CDN thế giới được làm mới.

---

## 4. QUẢN LÝ CLOUDFLARE R2 OBJECT STORAGE (CLI)

### 📂 Thao tác với Bucket R2 bằng Wrangler
```powershell
# Liệt kê toàn bộ tệp trong bucket ielts-hsk-data
npx wrangler r2 object list ielts-hsk-data

# Tải 1 file âm thanh lên R2
npx wrangler r2 object put ielts-hsk-data/audio/bai-nghe-1.mp3 --file="./bai-nghe-1.mp3"

# Tải 1 file từ R2 về máy
npx wrangler r2 object get ielts-hsk-data/audio/bai-nghe-1.mp3 --file="./bai-nghe-1.mp3"

# Xóa 1 file trên R2
npx wrangler r2 object delete ielts-hsk-data/audio/bai-nghe-1.mp3
```

---

## 5. QUẢN TRỊ CƠ SỞ DỮ LIỆU & REDIS CACHE

### ⚡ Quản lý Redis Cache
```bash
# Kiểm tra kết nối Redis
redis-cli ping
# Trả về: PONG

# Quét an toàn các key cache của dự án bằng SCAN (không làm nghẽn/treo server như KEYS)
redis-cli --scan --pattern "ieltsHSK:*"

# Hoặc quan sát thông tin keyspace / số lượng key mà không duyệt qua toàn bộ database
redis-cli info keyspace

# Xóa sạch toàn bộ cache khi cập nhật dữ liệu lớn
redis-cli flushdb
```

> [!WARNING] **CẢNH BÁO VẬN HÀNH REDIS:**  
> Tuyệt đối **KHÔNG** sử dụng lệnh `KEYS *` hoặc `KEYS "pattern*"` trên môi trường Production! Lệnh `KEYS` có độ phức tạp thời gian là $O(N)$ và chạy đơn luồng (single-threaded), sẽ chặn (block) toàn bộ các request khác khiến server Redis bị đơ hoặc timeout. Luôn sử dụng `SCAN` hoặc `redis-cli --scan` để quét an toàn từng batch.

### 🗄️ Sao lưu & Phục hồi PostgreSQL (Supabase)

> [!IMPORTANT] **NGUYÊN TẮC BẢO MẬT CREDENTIAL:**  
> - **Tuyệt đối không commit** mật khẩu (`PGPASSWORD`), Service Role Key hoặc Connection String thực tế vào kho mã nguồn (Git repo).  
> - Luôn nạp thông tin kết nối từ **Biến môi trường (Environment Variables)** hoặc **Secret Manager / Key Vault**.  
> - Thiết lập biến môi trường trước khi thực thi lệnh:  
>   - Trên Windows PowerShell:  
>     `$env:PGPASSWORD="<SUPABASE_PASSWORD>"`  
>     `$env:SUPABASE_CONNECTION_STRING="Host=<SUPABASE_HOST>;Port=<SUPABASE_PORT>;Database=<SUPABASE_DB_NAME>;Username=<SUPABASE_USER>;SSL Mode=Require"`  
>   - Trên Linux/macOS:  
>     `export PGPASSWORD="<SUPABASE_PASSWORD>"`  
>     `export SUPABASE_CONNECTION_STRING="Host=<SUPABASE_HOST>;Port=<SUPABASE_PORT>;Database=<SUPABASE_DB_NAME>;Username=<SUPABASE_USER>;SSL Mode=Require"`

```bash
# 1. Sao lưu dữ liệu ra file SQL (khuyến nghị dùng biến môi trường):
pg_dump "$SUPABASE_CONNECTION_STRING" -f backup_ieltshsk.sql

# Hoặc truyền placeholder:
pg_dump "Host=<SUPABASE_HOST>;Port=6543;Database=postgres;Username=<SUPABASE_USER>;SSL Mode=Require" -f backup_ieltshsk.sql

# 2. Phục hồi dữ liệu từ file SQL (khuyến nghị dùng biến môi trường):
psql "$SUPABASE_CONNECTION_STRING" -f backup_ieltshsk.sql

# Hoặc truyền placeholder:
psql "Host=<SUPABASE_HOST>;Port=6543;Database=postgres;Username=<SUPABASE_USER>;SSL Mode=Require" -f backup_ieltshsk.sql
```

---

## 6. KIỂM SOÁT MÃ NGUỒN VÀ GIT AN TOÀN (REPO RULES)

Trước khi commit code, luôn chạy các lệnh sau để đảm bảo không đưa file rác hay secret lên Git:

```powershell
# 1. Kiểm tra trạng thái gọn gàng
git status --short

# 2. Đảm bảo KHÔNG stage các file nháp ở thư mục gốc:
# - fix_json.js, fix_json.py, output.json, part*.html
# - Các file *.json nháp ở thư mục gốc

# 3. Xem trước các thay đổi cụ thể
git diff --stat

# 4. Commit an toàn
git add <các_file_nguồn_cần_thiết>
git commit -m "feat: mo ta thay doi"
```

---

## 7. FILE SCRIPT 1-CLICK TIỆN LỢI (`scripts/deploy.ps1`)

File script tự động hóa được lưu trữ tại `scripts/deploy.ps1`. Từ thư mục gốc của dự án, bạn chỉ cần chạy lệnh sau để tự động hóa toàn bộ 3 bước (biên dịch, dọn dẹp và đẩy lên Cloudflare Pages):

```powershell
.\scripts\deploy.ps1
```

Nội dung chi tiết của script `scripts/deploy.ps1`:

```powershell
Write-Host "=== BẮT ĐẦU QUÁ TRÌNH BIÊN DỊCH VÀ DEPLOY IELTSHSK ===" -ForegroundColor Cyan

# 1. Build & Publish Release
Write-Host "[1/3] Biên dịch gói Release..." -ForegroundColor Yellow
dotnet publish frontend/src/Frontend.App/Frontend.App.csproj -c Release -o frontend_dist
if ($LASTEXITCODE -ne 0) {
    Write-Host "Lỗi build Frontend!" -ForegroundColor Red
    exit 1
}

# 2. Dọn dẹp file lớn vượt trần Cloudflare Pages (> 25MB)
Write-Host "[2/3] Dọn dẹp file lớn > 25MB..." -ForegroundColor Yellow
Remove-Item -Path "frontend_dist/wwwroot/sample-data/audio/ielts-actual-vol1-test1.mp3" -Force -ErrorAction SilentlyContinue

# 3. Deploy lên Cloudflare Pages
Write-Host "[3/3] Đang tải lên Cloudflare Pages..." -ForegroundColor Yellow
npx wrangler pages deploy frontend_dist/wwwroot --project-name=ieltshsk

Write-Host "=== HOÀN TẤT! WEBSITE ĐÃ CẬP NHẬT TẠI HTTPS://IELTSHSK.ME ===" -ForegroundColor Green
```
