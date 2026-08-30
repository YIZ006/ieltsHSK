# Kế hoạch Triển Khai Redis Cache Server cho Backend & Tài Liệu Deploy

Tài liệu này mô tả chi tiết phương án tích hợp **Redis Distributed Cache** vào tầng Backend Web API (.NET 10 & PostgreSQL), thiết lập cơ chế **Cache-Aside**, cơ chế **Resilient Fallback** (tự động fallback về In-Memory / Direct DB nếu Redis offline) và hướng dẫn triển khai thực tế khi deploy.

---

## 1. Mục Tiêu & Phạm Vi

1. **Hiệu năng & Tải DB**:
   - Cache các tài nguyên đọc nhiều nhưng ít thay đổi: Danh sách từ vựng (IELTS, HSK, TOEIC), Video luyện nghe (Listen Videos), Đề thi (Exams Catalog), Danh mục học tập (Learning Sections/Websites).
   - Tốc độ phản hồi API giảm từ ~50-150ms (PostgreSQL query) xuống < 5ms (Redis in-memory).
2. **Cơ chế Resilience (An toàn)**:
   - Nếu Redis local chưa bật hoặc kết nối thất bại, ứng dụng vẫn hoạt động bình thường nhờ tự động fallback sang `IMemoryCache` (In-Memory) hoặc truy vấn DB trực tiếp.
3. **Cơ chế Invalidation (Làm mới dữ liệu)**:
   - Xóa cache tự động khi Admin thêm, sửa, xóa, hoặc import Excel từ vựng / video / đề thi.
4. **Tài liệu Deploy**:
   - Hướng dẫn chạy Redis cục bộ (Docker / Docker Compose / Windows Native / WSL).
   - Hướng dẫn cấu hình Production (Upstash Redis, Redis Cloud, Docker Swarm / VPS).

---

## 2. Kiến Trúc & Thiết Kế Kỹ Thuật

```text
┌───────────────────────────────────────────────────────────┐
│                      Client Request                       │
└─────────────────────────────┬─────────────────────────────┘
                              │
                              ▼
┌───────────────────────────────────────────────────────────┐
│                      Minimal API                          │
└─────────────────────────────┬─────────────────────────────┘
                              │
                              ▼
┌───────────────────────────────────────────────────────────┐
│                       ICacheService                       │
│ ┌─────────────────────────┐     ┌───────────────────────┐ │
│ │    RedisCacheService    │ ──> │ Connection Multiplexer│ │
│ │ (StackExchange.Redis)   │     │ (localhost:6379)      │ │
│ └───────────┬─────────────┘     └───────────────────────┘ │
│             │ (Fallback if Redis offline)                 │
│             ▼                                             │
│ ┌─────────────────────────┐                               │
│ │   MemoryCacheFallback   │                               │
│ └─────────────────────────┘                               │
└─────────────────────────────┬─────────────────────────────┘
                              │ (Cache Miss)
                              ▼
┌───────────────────────────────────────────────────────────┐
│              AppDbContext (PostgreSQL / Supabase)         │
└───────────────────────────────────────────────────────────┘
```

---

## 3. Proposed Changes

### Backend Application Layer

#### [NEW] [`ICacheService.cs`](file:///e:/tailieu/Dự án/ielstHSK-PostgeSQL/backend/src/Backend.Application/Abstractions/ICacheService.cs)
- Định nghĩa các phương thức chuẩn:
  - `Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)`
  - `Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)`
  - `Task RemoveAsync(string key, CancellationToken cancellationToken = default)`
  - `Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)`

---

### Backend Infrastructure Layer

#### [MODIFY] [`Backend.Infrastructure.csproj`](file:///e:/tailieu/Dự án/ielstHSK-PostgeSQL/backend/src/Backend.Infrastructure/Backend.Infrastructure.csproj)
- Thêm package `StackExchange.Redis` (version 2.8+).

#### [NEW] [`RedisCacheService.cs`](file:///e:/tailieu/Dự án/ielstHSK-PostgeSQL/backend/src/Backend.Infrastructure/Services/RedisCacheService.cs)
- Triển khai `ICacheService` dùng `IConnectionMultiplexer` từ `StackExchange.Redis`.
- Bọc các lệnh Redis trong `try/catch`: Nếu Redis mất kết nối, tự động fallback sang `IMemoryCache` và ghi log cảnh báo mà không ném lỗi ra ngoài làm crash request.
- Hỗ trợ serialization/deserialization JSON nhanh bằng `System.Text.Json`.
- Hỗ trợ xóa cache theo prefix (dùng Redis `SCAN` hoặc `Server.Keys` qua pattern) để invalidate toàn bộ namespace khi có thay đổi dữ liệu (vd: `ielts:vocab:*`).

#### [MODIFY] [`DependencyInjection.cs`](file:///e:/tailieu/Dự án/ielstHSK-PostgeSQL/backend/src/Backend.Infrastructure/DependencyInjection.cs)
- Đăng ký `IConnectionMultiplexer` (Lazy / Singleton với cấu hình `AbortOnConnectFail=false` để không crash app nếu Redis chưa khởi động).
- Đăng ký `ICacheService` (`RedisCacheService`).

---

### Backend API Layer & Endpoints

#### [MODIFY] [`appsettings.json`](file:///e:/tailieu/Dự án/ielstHSK-PostgeSQL/backend/src/Backend.Api/appsettings.json) & [`appsettings.Development.json`](file:///e:/tailieu/Dự án/ielstHSK-PostgeSQL/backend/src/Backend.Api/appsettings.Development.json)
- Thêm section cấu hình Redis:
  ```json
  "Redis": {
    "ConnectionString": "localhost:6379",
    "InstancePrefix": "ieltsHSK:",
    "Enabled": true,
    "DefaultTtlMinutes": 60
  }
  ```

#### [MODIFY] [`Program.cs`](file:///e:/tailieu/Dự án/ielstHSK-PostgeSQL/backend/src/Backend.Api/Program.cs)
- Tích hợp Cache-Aside và Invalidation cho các endpoints chính:
  1. **IELTS Vocabulary**:
     - `GET /api/ielts/vocab` ➔ Cache theo topic/filter (TTL 2 giờ).
     - `POST/PUT/DELETE /api/ielts/vocab/*`, import Excel ➔ `RemoveByPrefixAsync("ielts:vocab:")`.
  2. **HSK Vocabulary**:
     - `GET /api/hsk/vocab` ➔ Cache theo level (TTL 2 giờ).
     - `POST/PUT/DELETE /api/hsk/vocab/*` ➔ `RemoveByPrefixAsync("hsk:vocab:")`.
  3. **TOEIC Vocabulary**:
     - `GET /api/toeic/vocab` ➔ Cache theo topic (TTL 2 giờ).
     - `POST/PUT/DELETE /api/toeic/vocab/*` ➔ `RemoveByPrefixAsync("toeic:vocab:")`.
  4. **Listen Videos**:
     - `GET /api/listen-videos` ➔ Cache danh sách video đã duyệt (TTL 30 phút).
     - `PUT /api/admin/listen-videos/*`, `POST /api/admin/listen-videos/import-excel` ➔ Invalidate `listen-videos:list`.
  5. **Exams / Catalog**:
     - `GET /api/ielts/exams`, `GET /api/ielts/audio-shadowing` ➔ Cache (TTL 1 giờ).
     - `POST/PUT /api/ielts/exams/*` ➔ Invalidate tương ứng.

---

### Documentation Layer

#### [NEW] [`docs/redis_cache_deployment_guide.md`](file:///e:/tailieu/Dự án/ielstHSK-PostgeSQL/docs/redis_cache_deployment_guide.md)
- Hướng dẫn chi tiết:
  1. Chạy Redis cục bộ bằng Docker (`docker run -d -p 6379:6379 --name ieltshsk-redis redis:7-alpine`) hoặc Memurai / WSL.
  2. File mẫu `docker-compose.yml` cho Redis + Redis Commander (giao diện web quản lý cache).
  3. Hướng dẫn cấu hình Redis Cloud miễn phí (Upstash Redis, Redis Cloud) cho môi trường staging/production.
  4. Cấu hình biến môi trường production (`REDIS__CONNECTIONSTRING`).
  5. Các lệnh kiểm tra và debug Redis CLI (`redis-cli ping`, `redis-cli monitor`, `redis-cli keys "ieltsHSK:*"`).

#### [MODIFY] [`docs/ban_ke_hoach_toi_uu_cache.md`](file:///e:/tailieu/Dự án/ielstHSK-PostgeSQL/docs/ban_ke_hoach_toi_uu_cache.md)
- Cập nhật mục 6 về kiến trúc phân tầng kết hợp: Frontend Static Cache + Backend Redis Cache.

---

## 4. Verification Plan

### Automated Build & Tests
- Chạy `dotnet build backend/src/Backend.Api/Backend.Api.csproj` để đảm bảo code biên dịch sạch 0 warning/error.
- Chạy test API endpoints với Redis offline: Xác nhận app khởi động và trả về dữ liệu bình thường từ DB / In-Memory (resilience test).
- Test cache hit / miss logging và invalidation khi cập nhật dữ liệu.

### Manual Verification
- Kiểm tra tính đúng đắn của Redis keys và TTL khi có request.
- Kiểm tra xóa cache khi admin chỉnh sửa hoặc import file.
