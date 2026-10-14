# BÁO CÁO TRIỂN KHAI BACKEND VÀ ĐỊNH HƯỚNG TRIỂN KHAI FRONTEND
**Dự án:** ieltsHSK  
**Thời gian thực hiện:** Ngày 09/10/2026  
**Trạng thái hệ thống:** Backend đã triển khai thành công lên Production, Frontend sẵn sàng triển khai.

---

## PHẦN I: NHỮNG VIỆC ĐÃ HOÀN THÀNH THÀNH CÔNG HÔM NAY

### 1. Đăng ký Tên miền Quốc tế (Domain)
- **Tên miền:** `ieltshsk.me`
- **Nhà cung cấp:** Namecheap (thông qua chương trình giáo dục `nc.me`).
- **Chi phí:** **0 VNĐ** (Miễn phí 1 năm thông qua liên kết đặc quyền **GitHub Student Developer Pack** với thời hạn tài khoản sinh viên đến tháng 10/2027).
- **Trạng thái:** Tên miền đã kích hoạt (Active), sẵn sàng kết nối DNS với hệ thống web.

---

### 2. Thiết lập Cơ sở dữ liệu Đám mây (Supabase PostgreSQL)
- **Hạ tầng:** Supabase Cloud Database (PostgreSQL).
- **Mã định danh dự án:** `<SUPABASE_PROJECT_REF>` (Khu vực: `<SUPABASE_REGION>`).
- **Giải pháp kết nối:** 
  - Khắc phục triệt để rào cản mạng IPv6 của kết nối trực tiếp (Direct Connection) trên các hosting thông thường bằng cách chuyển hướng sang **Session Pooler (IPv4)**:
    `Host=<SUPABASE_HOST>;Port=5432;Database=postgres;Username=<SUPABASE_USER>;SSL Mode=Require;No Reset On Close=true;Maximum Pool Size=20`
  - *(Lưu ý bảo mật: Thông tin kết nối thực tế được cấu hình qua Biến môi trường / Secret Manager của hosting, tuyệt đối không commit thông tin nhạy cảm vào Git repo)*.
- **Đồng bộ lược đồ cơ sở dữ liệu (Database Migrations):**
  - Thực thi thành công toàn bộ **8 phiên bản EF Core Migrations** lên database:
    1. `20260826062942_InitialPostgreSQL`
    2. `20260826102953_InitialCreate_PostgreSQL`
    3. `20260826164733_AddFullNameToUser`
    4. `20260827111448_AddHskMockTestEntity`
    5. `20260828100248_AddIeltsVocabularyImport`
    6. `20260828111500_AddSubmissionDetailsAndR2Key`
    7. `20260829160132_AddCefrLevelToVocabulary`
    8. `20260901123728_AddRefreshTokens`
  - Đã tạo hoàn chỉnh các bảng dữ liệu: `users`, `refresh_tokens`, `ielts_vocabularies`, `grammar_structures`, `listen_videos`, `test_submissions`,...

---

### 3. Thiết lập Bộ nhớ đệm Tốc độ cao (Upstash Redis Cloud)
- **Dịch vụ:** Upstash Serverless Redis.
- **Cụm máy chủ:** `<UPSTASH_REDIS_ENDPOINT>:<PORT>` (Khu vực Singapore, độ trễ thấp).
- **Chi phí:** Miễn phí vĩnh viễn (gói 10,000 commands/ngày, 250MB data).
- **Cấu hình:** Kết nối an toàn với giao thức TLS/SSL, có tích hợp cơ chế tự động fallback sang `IMemoryCache` (RAM) trong mã nguồn dự án.

---

### 4. Triển khai Máy chủ Backend (.NET 10 Web API)
- **Nền tảng lưu trữ:** MonsterASP.NET.
- **Địa chỉ API Production:** `https://ieltshsk.runasp.net`
- **Quy trình triển khai:**
  - Biên dịch toàn bộ solution backend chế độ Release (`dotnet publish -c Release`).
  - Xây dựng script PowerShell tự động hóa upload FTP đẩy các tệp tin binaries, thư viện DLL và tài nguyên vào thư mục `/wwwroot`.
  - Cấu hình file `web.config` kích hoạt In-Process hosting.
- **Kết quả kiểm thử thực tế:**
  - **Health Check & Readiness:** Kiểm tra hoạt động tại [https://ieltshsk.runasp.net/health/ready](https://ieltshsk.runasp.net/health/ready) (Swagger production mặc định tắt để bảo mật, chỉ kích hoạt khi có cờ `EnableSwagger=true`).
  - **Truy vấn Database:** Endpoint `GET /api/auth/check-username?username=testuser` phản hồi thành công:
    ```json
    HTTP 200 OK
    {"isTaken": true}
    ```
    *(Xác nhận backend đã kết nối, truy vấn dữ liệu từ Supabase và trả về kết quả chính xác)*.

---

### 5. Bảo toàn Môi trường Phát triển Cục bộ (Local Isolation)
- File cấu hình gốc `backend/src/Backend.Api/appsettings.json` trên máy tính được giữ nguyên độc lập để phục vụ kiểm thử và phát triển offline.
- File cấu hình trên server được quản lý riêng biệt, đảm bảo không xung đột giữa môi trường local và môi trường production.

---

## PHẦN II: ĐỊNH HƯỚNG VÀ KẾ HOẠCH TRIỂN KHAI FRONTEND

Hệ thống hiện tại chỉ còn duy nhất phần **Frontend (Blazor WebAssembly)** chưa đưa lên internet. Dưới đây là kiến trúc và lộ trình triển khai chi tiết:

### 1. Phân tích Bản chất Kỹ thuật của Frontend
- **Công nghệ:** Blazor WebAssembly (.NET 9.0) — `frontend/src/Frontend.App`.
- **Cơ chế hoạt động:** Frontend của bạn là một ứng dụng SPA (Single Page Application). Khi build ở chế độ Release, trình biên dịch sẽ tạo ra thư mục chứa hoàn toàn các **tệp tin tĩnh** (`index.html`, mã WebAssembly `.wasm`, `css`, `js`, ảnh).
- **Yêu cầu triển khai:** Không cần máy chủ backend để chạy frontend, chỉ cần một nền tảng lưu trữ tĩnh (Static Hosting / CDN) có tốc độ cao và hỗ trợ chứng chỉ HTTPS.

---

### 2. Lựa chọn Nền tảng Triển khai Tối ưu

| Tiêu chí | **Cloudflare Pages** *(Khuyên dùng số 1)* | **Vercel** | **GitHub Pages** |
| :--- | :--- | :--- | :--- |
| **Chi phí** | 0 VNĐ (Miễn phí 100%) | 0 VNĐ | 0 VNĐ |
| **Băng thông** | Không giới hạn (Unlimited) | 100 GB/tháng | 100 GB/tháng |
| **Mạng lưới máy chủ** | Có Datacenter tại **Hà Nội & TP.HCM** | Quốc tế | Quốc tế |
| **Tốc độ load WASM** | Cực nhanh nhờ CDN phân tán | Rất nhanh | Khá |
| **Gắn tên miền riêng** | Hỗ trợ 1-click cho `ieltshsk.me` | Hỗ trợ | Hỗ trợ |
| **Chứng chỉ SSL** | Cấp phát tự động, miễn phí | Cấp phát tự động | Cấp phát tự động |

👉 **Khuyến nghị:** Sử dụng **Cloudflare Pages** để đảm bảo tốc độ tải file WASM nhanh nhất cho học viên tại Việt Nam và kết nối trực tiếp với tên miền `ieltshsk.me`.

---

### 3. Kế hoạch Triển khai Frontend Từng bước (Action Plan)

#### Bước 1: Cập nhật Cấu hình Kết nối API trên Frontend
- Mở file cấu hình của frontend (`frontend/src/Frontend.App/wwwroot/appsettings.json` hoặc trong `Program.cs`).
- Thay đổi địa chỉ `BaseAddress` từ `https://localhost:7102` thành URL backend chính thức:
  ```json
  "ApiBaseUrl": "https://ieltshsk.runasp.net/"
  ```

#### Bước 2: Cập nhật Cấu hình CORS trên Backend
- Thêm tên miền chính thức của Frontend (`https://ieltshsk.me` và domain Cloudflare Pages) vào danh sách cho phép (AllowedOrigins) trên Backend để trình duyệt không chặn cuộc gọi API.

#### Bước 3: Biên dịch và Đóng gói Frontend
- Chạy lệnh build tối ưu hóa:
  ```bash
  dotnet publish frontend/src/Frontend.App/Frontend.App.csproj -c Release -o frontend_publish
  ```
- Thư mục đầu ra `frontend_publish/wwwroot` sẽ chứa toàn bộ sản phẩm hoàn chỉnh để đưa lên web.

#### Bước 4: Đẩy lên Cloudflare Pages & Gắn Tên miền
1. Đăng nhập vào [dash.cloudflare.com](https://dash.cloudflare.com) bằng tài khoản Cloudflare.
2. Vào mục **Workers & Pages** $\rightarrow$ **Create Application** $\rightarrow$ **Pages**.
3. Kết nối với GitHub Repo (hoặc tải trực tiếp thư mục `wwwroot` lên).
4. Tại mục **Custom Domains**, thêm tên miền **`ieltshsk.me`** và **`www.ieltshsk.me`**.
5. Cập nhật bản ghi DNS tại Namecheap trỏ về Cloudflare.

---

## TỔNG KẾT MÔ HÌNH HỆ THỐNG SAU KHI HOÀN THIỆN

```
                                      [ NGƯỜI DÙNG ]
                                            │
                     ┌──────────────────────┴──────────────────────┐
                     ▼                                             ▼
          Tên miền: ieltshsk.me                         API: ieltshsk.runasp.net
       [ FRONTEND - Cloudflare Pages ]                 [ BACKEND - MonsterASP.NET ]
       - Blazor WebAssembly (.NET 9)                   - ASP.NET Core API (.NET 10)
       - CDN Toàn cầu (HN / HCM)                       - Xử lý nghiệp vụ & Bảo mật
                     │                                             │
                     └─────────────────────►───────────────────────┤
                                 (Gọi REST API / JWT)               │
                                                                    ▼
                                              ┌─────────────────────┴─────────────────────┐
                                              ▼                                           ▼
                                    [ Supabase Database ]                         [ Upstash Redis ]
                                   PostgreSQL (Session Pooler)                   Serverless Cache (TLS)
```
