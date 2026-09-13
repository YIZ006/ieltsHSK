# BÁO CÁO TRIỂN KHAI PWA

**Dự án:** ielstHSK-PostgeSQL — Frontend.App (Blazor WebAssembly)
**Ngày:** 2026-09-12
**Phạm vi:** Cấu hình Progressive Web App — chạy offline, cài đặt icon Desktop Windows

---

## 1. Mục tiêu

| # | Yêu cầu | Trạng thái |
|---|---|---|
| 1 | Viết `manifest.json` đầy đủ (tên, short_name, start_url, display standalone, icons) | Hoàn thành |
| 2 | Viết `sw.js` cache file tĩnh, phục vụ offline | Hoàn thành |
| 3 | Mã JS đăng ký Service Worker | Hoàn thành |
| 4 | Hướng dẫn kiểm tra trên Chrome DevTools | Hoàn thành |
| 5 | Icon PNG 192/512/maskable | **Chưa làm** (cần ảnh) |

---

## 2. File đã tạo / sửa

### 2.1. Tạo mới — `frontend/src/Frontend.App/wwwroot/manifest.json`

Khai báo PWA metadata: `name`, `short_name`, `description`, `lang: vi`, `start_url: /`, `scope: /`, `display: standalone`, `orientation`, `background_color`, `theme_color`, và 3 icon (192, 512, maskable 512).

### 2.2. Tạo mới — `frontend/src/Frontend.App/wwwroot/sw.js`

Chiến lược cache:

| Giai đoạn | Hành vi |
|---|---|
| `install` | Precache `./`, `index.html`, `manifest.json`, `favicon.png`; dùng `Promise.allSettled` để 1 file lỗi không chặn cài đặt |
| `activate` | Xóa cache phiên bản cũ (khác `app-v1`), gọi `clients.claim()` |
| `fetch` | Cache-first cho GET same-origin; miss thì fetch và lưu vào cache; offline navigation fallback về `index.html` |

Xử lý đặc thù Blazor WASM:
- Bỏ qua request `method !== 'GET'`.
- Bỏ qua cross-origin (`url.origin !== location.origin`) — CDN, Google Fonts, GSI, YouTube đi thẳng network.
- File `_framework/*`, `_bin/*` (hash đổi mỗi build) **không** precache mà tự động vào cache ở lần fetch đầu tiên → tránh lỗi 404 khi precache.

### 2.3. Sửa — `frontend/src/Frontend.App/wwwroot/index.html`

Hai thay đổi:

**a) Trong `<head>`, sau `<link rel="icon">`:**
```html
<link rel="manifest" href="manifest.json" />
<meta name="theme-color" content="#1f2937" />
```

**b) Cuối `<body>`, trước `</body>`:**
```html
<script>
    // Đăng ký Service Worker cho PWA (offline + cài đặt Desktop)
    if ('serviceWorker' in navigator) {
        window.addEventListener('load', function () {
            navigator.serviceWorker.register('sw.js').catch(function (err) {
                console.warn('Service Worker registration failed:', err);
            });
        });
    }
</script>
```

---

## 3. Phần còn thiếu

### 3.1. Icon — BẮT BUỘC trước khi test

`manifest.json` trỏ tới 3 file chưa tồn tại (thư mục `wwwroot/icons/` chưa có):

```
wwwroot/icons/icon-192.png
wwwroot/icons/icon-512.png
wwwroot/icons/icon-maskable-512.png
```

Không có icon → Chrome **không hiện nút Install**.

Cách tạo: dùng `favicon.png` hiện có, resize thành PNG 192×192 và 512×512. Bản maskable cần chừa padding ~20% mỗi cạnh để icon không bị cắt khi Android/Windows bo tròn.

### 3.2. Chính sách offline cho tính năng phụ thuộc mạng

Đã phân loại:

| Tính năng | Nguồn | Offline? | Đề xuất |
|---|---|---|---|
| Bootstrap, bootstrap-icons, Google Fonts | CDN | Không | Self-host nếu muốn offline hoàn hảo |
| Google GSI (đăng nhập) | accounts.google.com | Không | Disable nút khi offline |
| YouTube player | iframe | Không | Disable, báo "cần mạng" |
| Backend API (PostgreSQL) | server | Không | Banner + disable nút gọi API |
| `speechSynthesis` (Web Speech API) | local | Có | Chạy bình thường |
| Web Audio SFX (`WordleSfx`) | local | Có | Chạy bình thường |
| Blazor WASM + file tĩnh | SW cache | Có | Chạy bình thường |

**Nguyên tắc:** không khóa cứng toàn app. Chỉ disable ở nút cụ thể cần server. `navigator.onLine` không đáng tin (WiFi mất route vẫn báo `true`) — chỉ dùng làm gợi ý UI, không dùng làm guard.

Helper đề xuất (chưa triển khai):
```js
window.AppOnline = {
    is: () => navigator.onLine,
    onChange: (cb) => {
        window.addEventListener('online',  () => cb(true));
        window.addEventListener('offline', () => cb(false));
    }
};
```

---

## 4. Kiểm tra trên Chrome DevTools

**Điều kiện:** chạy qua `https://` hoặc `http://localhost` — Service Worker không hoạt động trên HTTP thường (trừ localhost).

| Bước | Thao tác | Kết quả mong đợi |
|---|---|---|
| 1 | `F12` → tab **Application** → **Manifest** | Hiện tên app, icon, `display: standalone`. Warning vàng = thiếu trường |
| 2 | **Application** → **Service Workers** | `sw.js` status **activated and is running**. Tick "Update on reload" khi dev |
| 3 | **Application** → **Cache Storage** → `app-v1` | Thấy `index.html`, `_framework/*`, `_bin/*` sau lần load đầu |
| 4 | Tab **Network** → tick **Offline** → `Ctrl+Shift+R` | Trang load được từ cache |
| 5 | **Lighthouse** → chọn "Progressive Web App" → Run | Pass = PWA đủ chuẩn |
| 6 | Icon install trên address bar, hoặc **Application → Manifest → Install** | Tạo shortcut Desktop Windows |

---

## 5. Lưu ý vận hành

1. **Bump cache version mỗi deploy:** đổi `const CACHE = 'app-v1'` → `'app-v2'` trong `sw.js`. Không đổi → user dính cache cũ.
2. **`index.html` đang cache-first** → user phải hard-reload mới thấy bản mới. Nếu muốn tự động cập nhật, đổi SW sang network-first cho `index.html` (chưa làm).
3. **SW phải nằm ở gốc `wwwroot/`** — đã đúng. Nếu đặt trong thư mục con, scope sẽ bị giới hạn, không cover được `_framework/`.

---

## 6. Việc tiếp theo

| Ưu tiên | Việc | Ghi chú |
|---|---|---|
| Cao | Tạo 3 file icon PNG | Chặn tính năng Install |
| Trung bình | Thêm banner offline + disable nút cần mạng | Cần helper `AppOnline` |
| Thấp | Self-host Bootstrap + Google Fonts | Offline hoàn hảo, tăng dung lượng build |
| Thấp | Đổi `index.html` sang network-first | Tránh user kẹt cache cũ |

---

## 7. Kết luận

Cấu hình PWA cốt lõi đã xong: manifest, service worker, đăng ký SW. App có thể chạy offline cho phần Blazor WASM + tài nguyên tĩnh sau lần truy cập đầu. Chức năng Install trên Desktop Windows **chưa hoạt động** do thiếu file icon — cần bổ sung 3 file PNG trước khi nghiệm thu.