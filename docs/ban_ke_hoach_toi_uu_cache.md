# Bản Kế Hoạch & Hướng Dẫn Tối Ưu Cache (Cache Busting & Static Asset Strategy)

Tài liệu này tổng hợp thiết kế, giải pháp kỹ thuật, cơ chế tự động hóa và hướng dẫn vận hành tối ưu hóa bộ nhớ đệm (Cache Busting & HTTP Caching) cho ứng dụng Blazor WebAssembly trong hệ thống ieltsHSK.

---

## 1. Hiện Trạng & Vấn Đề Trước Khi Tối Ưu

- **Vấn đề**: Khi deploy phiên bản giao diện (CSS) hoặc JavaScript mới, người dùng vẫn thấy giao diện cũ do trình duyệt (browser) và các tầng mạng trung gian (CDN/Proxy) lưu cache file `app.css`, `Frontend.App.styles.css`, hoặc các file JS.
- **Nguyên nhân**: 
  - File `index.html` trước đây dùng query string thủ công `?v=20260828`. Nếu nội dung CSS thay đổi nhưng số phiên bản không đổi (hoặc CDN cấu hình bỏ qua query string), trình duyệt không tải file mới.
  - Scoped CSS của Blazor (`Frontend.App.styles.css`) được sinh tự động khi build, nếu không có cơ chế băm nội dung (content hashing) tự động thì dễ bị bỏ sót.

---

## 2. Giải Pháp Triển Khai: Tự Động Content Hashing (Content Hash Cache Busting)

Hệ thống triển khai công cụ băm nội dung tự động tích hợp trực tiếp vào quy trình build/publish của .NET MSBuild:

### 2.1. Cơ chế hoạt động

1. **Khi chạy `dotnet publish`**:
   - .NET SDK biên dịch toàn bộ Razor Components và sinh ra scoped CSS `Frontend.App.styles.css` cùng các static assets trong thư mục publish `wwwroot/`.
   - MSBuild Target `ApplyContentHashCacheBusting` kích hoạt công cụ `frontend/tools/CacheBuster`.
2. **Công cụ CacheBuster thực hiện**:
   - Quét file `index.html` trong output publish để tìm các liên kết tài nguyên cục bộ (`<link rel="stylesheet">`, `<script src="...">`).
   - Đọc nội dung từng file CSS/JS cục bộ, tính mã băm SHA256 (lấy 8 ký tự hex đặc trưng, ví dụ: `d2c7fdda`).
   - Sinh bản sao có gắn mã băm:
     - `css/app.css` ➔ `css/app.<hash>.css`
     - `Frontend.App.styles.css` ➔ `Frontend.App.styles.<hash>.css`
     - `js/<file>.js` ➔ `js/<file>.<hash>.js`
   - Tự động nén sẵn cả 2 định dạng **GZip (`.gz`)** và **Brotli (`.br`)** cho tất cả các file đã băm hash.
   - Cập nhật các đường dẫn trong `index.html` sang tên file băm mới.
   - Nén lại `index.html.gz` và `index.html.br` tương ứng.
   - Tự động kiểm thử tính toàn vẹn (Self-Verification): xác nhận 100% tài nguyên tham chiếu trong `index.html` tồn tại thực tế trên đĩa cứng và có đầy đủ file nén.
3. **An toàn & Tương thích ngược**:
   - Source code `index.html` gốc không bị sửa đổi hardcode mã băm.
   - Các file gốc không băm (`app.css`, `Frontend.App.styles.css`, ...) vẫn được giữ lại song song trong thư mục publish để tránh lỗi 404 cho các client/bookmark cũ chưa tải lại trang.
   - File bootstrap `_framework/blazor.webassembly.js` và thư mục `_framework/` được giữ nguyên vẹn để Blazor WASM nạp runtime chuẩn xác.

---

## 3. Chính Sách HTTP Header & Cache-Control Khuyến Nghị

Khi đưa lên hosting (Static Web Hosting, Cloudflare Pages, S3/R2 + CDN, Nginx, IIS):

| Loại tài nguyên | Header Cache-Control khuyến nghị | Mục đích / Giải thích |
| :--- | :--- | :--- |
| `index.html` | `Cache-Control: no-cache, max-age=0, must-revalidate` | Trình duyệt luôn kiểm tra origin/CDN để lấy ngay file HTML mới nhất khi có bản release. |
| CSS & JS có hash (`*.<hash>.css`, `*.<hash>.js`) | `Cache-Control: public, max-age=31536000, immutable` | URL đã thay đổi theo nội dung. Cache tối đa 1 năm giúp tải tức thì, giảm tải server. |
| Ảnh, Favicon, Font có hash | `Cache-Control: public, max-age=31536000, immutable` | Cache vĩnh viễn, tối ưu tốc độ render. |
| Asset chưa có hash (fallback) | `Cache-Control: no-cache` hoặc `max-age=3600` | Tránh giữ file cũ khi đang trong chu kỳ release. |

> [!IMPORTANT]
> Tuyệt đối không cấu hình `immutable` hoặc `max-age` dài hạn cho `index.html`. `index.html` là điểm mấu chốt để phân phối các URL asset băm mới đến người dùng.

---

## 4. Hướng Dẫn Vận Hành & Build

### 4.1. Lệnh Publish Tạo Bản Release Tự Động Cache Busting
```bash
dotnet publish frontend/src/Frontend.App/Frontend.App.csproj -c Release -o bin/Publish/Frontend
```

### 4.2. Lệnh Kiểm Tra / Verify Độc Lập Build Artifact
```bash
dotnet run --project frontend/tools/CacheBuster/CacheBuster.csproj -- bin/Publish/Frontend/wwwroot --verify-only
```

---

## 5. Kế Hoạch Rollback Khi Gặp Sự Cố

1. **Trường hợp lỗi asset mới tại production**:
   - Nếu đã deploy bản build mới bị lỗi giao diện, chỉ cần rollback file `index.html` về bản phát hành trước đó.
   - Do các phiên bản asset cũ và mới đều có tên file chứa mã băm riêng biệt (`.<hash>.css`), các client tải `index.html` cũ sẽ trỏ ngay về đúng asset cũ mà không bị xung đột hay thiếu file.
2. **Trường hợp CDN vẫn lưu cache file `index.html` cũ**:
   - Thực hiện lệnh **Purge Cache** (Single File Purge) riêng cho URL `https://<domain>/index.html` và `https://<domain>/` trên CDN (Cloudflare/CloudFront/Fastly).
   - Không cần purge toàn bộ cache các file `*.<hash>.*` vì mỗi URL băm là duy nhất (immutable).

---

## 6. Phân Biệt Static Asset Cache vs Redis Data Cache

```text
Static Asset Caching (CSS/JS/Fonts):
  ➔ Content Hashing + Cache-Control Headers + index.html no-cache

Dynamic Data Caching (Từ vựng, đề thi, user profile):
  ➔ PostgreSQL + Redis Cache-Aside Pattern + Invalidation khi update
```
Hai giải pháp này giải quyết hai bài toán độc lập và hỗ trợ lẫn nhau để mang lại hiệu năng toàn diện cho ứng dụng.
