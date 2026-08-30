# HƯỚNG DẪN TRIỂN KHAI VÀ VẬN HÀNH REDIS CACHE (DEPLOYMENT GUIDE)

Tài liệu này hướng dẫn chi tiết cách cài đặt, cấu hình, triển khai và giám sát hệ thống Caching phân tán sử dụng **Redis Server** kết hợp cơ chế **Resilience Fallback** (tự động chuyển sang `IMemoryCache` nếu Redis offline) cho hệ thống backend `ieltsHSK`.

---

## 1. TỔNG QUAN KIẾN TRÚC CACHING

```
                          ┌──────────────────────────┐
                          │   Frontend Blazor WASM   │
                          └─────────────┬────────────┘
                                        │ HTTP Request
                                        ▼
                          ┌──────────────────────────┐
                          │    Backend ASP.NET Core  │
                          │      (Minimal APIs)      │
                          └──────┬────────────┬──────┘
                   (Redis Up)    │            │ (Redis Down / Fallback)
                                 ▼            ▼
                    ┌────────────────┐    ┌─────────────────┐
                    │  Redis Server  │    │  IMemoryCache   │
                    │ (Distributed)  │    │ (Local Process) │
                    └────────────────┘    └─────────────────┘
                                 │            │ (Cache Miss)
                                 └──────┬─────┘
                                        ▼
                          ┌──────────────────────────┐
                          │  PostgreSQL (Supabase)   │
                          └──────────────────────────┘
```

### Điểm nổi bật của kiến trúc:
1. **Resilience Fallback (Không lo gián đoạn)**:
   - Cấu hình `AbortOnConnectFail = false`.
   - Lớp `RedisCacheService` bọc toàn bộ tương tác Redis trong `try/catch`.
   - Nếu Redis chưa bật hoặc gặp sự cố, hệ thống **tự động chuyển sang In-Memory Cache cục bộ**, API vẫn phản hồi bình thường mà không gây lỗi 500.
2. **Cache-Aside Pattern**:
   - Khi có request `GET`: Đọc từ Cache trước. Nếu có (Cache Hit), trả về ngay lập tức (< 5ms). Nếu không có (Cache Miss), truy vấn PostgreSQL, ghi vào Cache với TTL, sau đó trả về dữ liệu.
3. **Chủ động Invalidation (Xóa cache khi dữ liệu thay đổi)**:
   - Khi Admin thêm, sửa, xóa, duyệt video, hoặc Import Excel, hệ thống gọi `RemoveAsync` hoặc `RemoveByPrefixAsync` để xóa cache cũ ngay lập tức, đảm bảo người dùng luôn thấy dữ liệu mới nhất.

---

## 2. BẢNG DANH MỤC CACHE KEYS VÀ TTL

| API Endpoint | Chức năng | Cache Key | TTL Mặc định | Cơ chế Invalidation |
| :--- | :--- | :--- | :--- | :--- |
| `GET /api/ielts/courses` | Danh sách khóa học | `ielts:courses` | 2 giờ | Tự hết hạn / Xóa khi admin cập nhật |
| `GET /api/ielts/websites` | Danh sách website tham khảo | `ielts:websites` | 2 giờ | Tự hết hạn / Xóa khi admin cập nhật |
| `GET /api/ielts/sections` | Cấu hình chuyên mục học | `ielts:sections` | 2 giờ | Tự hết hạn |
| `GET /api/listen-videos` | Video chép chính tả đã duyệt | `listen-videos:approved` | 30 phút | Tự động xóa khi Duyệt, Sửa, Xóa, hoặc Import Excel |
| `GET /api/ielts/exams` | Danh sách đề thi IELTS | `ielts:exams:all` | 1 giờ | Tự động xóa khi Admin tạo đề thi mới |
| `GET /api/ielts/vocab` | Từ vựng IELTS (theo topic/level) | `ielts:vocab:all` | 2 giờ | Tự động xóa prefix `ielts:vocab:` khi CRUD, Import Excel, Auto-classify CEFR |
| `GET /api/hsk/vocab` | Từ vựng HSK (toàn bộ hoặc theo cấp) | `hsk:vocab:all`<br>`hsk:vocab:hsk1` ... | 2 giờ | Tự động xóa prefix `hsk:vocab:` khi CRUD, Import Excel, Xóa toàn bộ |

---

## 3. TRIỂN KHAI CỤC BỘ (LOCAL DEVELOPMENT)

### Cách 1: Chạy Redis qua Docker CLI (Khuyên dùng - Nhanh nhất)
Nếu máy bạn đã cài Docker Desktop, chạy lệnh sau trong terminal:

```bash
docker run -d --name ieltshsk-redis -p 6379:6379 --restart unless-stopped redis:7-alpine
```

Kiểm tra trạng thái Redis:
```bash
docker ps
docker exec -it ieltshsk-redis redis-cli ping
# Kết quả trả về: PONG
```

---

### Cách 2: Chạy qua Docker Compose (Kèm Giao diện quản lý Web UI)
Tạo file `docker-compose.redis.yml` ở thư mục gốc hoặc dùng nội dung sau:

```yaml
version: '3.8'

services:
  redis:
    image: redis:7-alpine
    container_name: ieltshsk-redis
    restart: always
    ports:
      - "6379:6379"
    volumes:
      - redis-data:/data
    command: redis-server --appendonly yes

  redis-commander:
    image: rediscommander/redis-commander:latest
    container_name: ieltshsk-redis-commander
    restart: always
    environment:
      - REDIS_HOSTS=local:redis:6379
    ports:
      - "8081:8081"
    depends_on:
      - redis

volumes:
  redis-data:
```

**Khởi chạy:**
```bash
docker compose -f docker-compose.redis.yml up -d
```
- **Redis Server**: `localhost:6379`
- **Redis Commander UI (Web trực quan)**: Mở trình duyệt truy cập `http://localhost:8081` để xem, tìm kiếm và xóa cache keys bằng giao diện đồ họa.

---

### Cách 3: Chạy Redis Native trên Windows / WSL2
Nếu không dùng Docker, bạn có thể:
1. Sử dụng WSL2 (Ubuntu):
   ```bash
   sudo apt update
   sudo apt install redis-server -y
   sudo service redis-server start
   ```
2. Hoặc tải bản Redis Windows Port (.msi / .zip) từ GitHub (Memurai hoặc Redis-Windows).

---

## 4. CẤU HÌNH ỨNG DỤNG (.NET APPSETTINGS)

Trong `backend/src/Backend.Api/appsettings.json` và `appsettings.Development.json`:

```json
{
  "Redis": {
    "ConnectionString": "localhost:6379",
    "InstancePrefix": "ieltsHSK:dev:",
    "Enabled": true,
    "DefaultTtlMinutes": 60
  }
}
```

- `ConnectionString`: Địa chỉ Redis (hỗ trợ cả dạng `localhost:6379` hoặc URL đầy đủ `redis://user:password@host:port`).
- `InstancePrefix`: Tiền tố key được thêm tự động cho mọi entry (giúp phân biệt môi trường `dev` và `prod` khi dùng chung 1 cụm Redis).
- `Enabled`: Bật (`true`) hoặc tắt (`false`) Redis. Nếu `false`, hệ thống dùng In-Memory Cache.

---

## 5. TRIỂN KHAI PRODUCTION (CLOUD DEPLOYMENT)

Khi đưa ứng dụng lên máy chủ Production hoặc Cloud, bạn có các lựa chọn Redis chất lượng cao:

### Phương án A: Upstash Redis Serverless (Khuyên dùng cho Cloud / Tiết kiệm chi phí)
- **Đặc điểm**: Miễn phí 10,000 requests/ngày, Serverless không cần quản trị máy chủ, hỗ trợ TLS/SSL.
- **Cách cấu hình**:
  1. Đăng ký tài khoản tại [upstash.com](https://upstash.com).
  2. Tạo Redis Database (chọn region gần người dùng nhất, ví dụ Singapore `ap-southeast-1`).
  3. Lấy chuỗi **`ioredis`** hoặc **`Node.js`** connection string, định dạng:
     ```
     rediss://default:AbCdEfGh123456@ap1-fluent-salmon-12345.upstash.io:6379
     ```
  4. Đặt biến môi trường trên server/hosting:
     ```bash
     Redis__ConnectionString="rediss://default:AbCdEfGh123456@ap1-fluent-salmon-12345.upstash.io:6379"
     Redis__InstancePrefix="ieltsHSK:prod:"
     Redis__Enabled=true
     ```

---

### Phương án B: Redis Cloud (Redis.com Free Tier 30MB)
1. Đăng ký tại [redis.com/try-free](https://redis.com/try-free/).
2. Tạo database, lấy endpoint dạng: `redis-12345.c1.ap-southeast-1-1.ec2.cloud.redislabs.com:12345` cùng `password`.
3. Cấu hình biến môi trường:
   ```bash
   Redis__ConnectionString="redis-12345.c1.ap-southeast-1-1.ec2.cloud.redislabs.com:12345,password=YOUR_PASSWORD,ssl=False"
   ```

---

### Phương án C: Triển khai Docker trên Linux VPS (Ubuntu Server)
Nếu bạn có VPS riêng (DigitalOcean, Linode, AWS EC2, Contabo, Hetzner):

1. File `docker-compose.prod.yml`:
   ```yaml
   version: '3.8'
   services:
     redis:
       image: redis:7-alpine
       container_name: ieltshsk-redis-prod
       restart: always
       command: >
         redis-server 
         --requirepass "YOUR_STRONG_PASSWORD_HERE"
         --maxmemory 256mb
         --maxmemory-policy allkeys-lru
         --appendonly yes
       ports:
         - "127.0.0.1:6379:6379" # Chỉ mở nội bộ VPS, không mở ra public internet
       volumes:
         - /var/lib/redis/data:/data
   ```
2. Cấu hình `appsettings.Production.json` hoặc Environment Variable:
   ```bash
   Redis__ConnectionString="localhost:6379,password=YOUR_STRONG_PASSWORD_HERE"
   Redis__InstancePrefix="ieltsHSK:prod:"
   Redis__Enabled=true
   ```

---

## 6. GIÁM SÁT VÀ LỆNH THAO TÁC REDIS-CLI THƯỜNG DÙNG

### 1. Kiểm tra kết nối
```bash
redis-cli -h localhost -p 6379 ping
# Trả về: PONG
```

### 2. Xem toàn bộ keys đang có trong hệ thống
```bash
redis-cli -h localhost -p 6379 keys "ieltsHSK:*"
```

### 3. Xem giá trị và thời gian sống (TTL) của 1 key
```bash
# Kiểm tra TTL còn lại bao nhiêu giây (-1: vĩnh viễn, -2: đã hết hạn/không tồn tại)
redis-cli -h localhost -p 6379 ttl "ieltsHSK:dev:ielts:vocab:all"

# Đọc nội dung JSON đã cache
redis-cli -h localhost -p 6379 get "ieltsHSK:dev:ielts:vocab:all"
```

### 4. Giám sát thời gian thực mọi hoạt động đọc/ghi (Realtime Monitor)
```bash
redis-cli -h localhost -p 6379 monitor
```
*Lệnh này sẽ in ra màn hình mỗi khi Backend thực hiện `GET`, `SET`, hoặc `DEL` cache.*

### 5. Kiểm tra dung lượng RAM đang sử dụng
```bash
redis-cli -h localhost -p 6379 info memory
```

### 6. Xóa thủ công toàn bộ cache khi cần bảo trì
```bash
# Xóa toàn bộ database hiện tại
redis-cli -h localhost -p 6379 flushdb

# Hoặc xóa toàn bộ tất cả database trên Redis server
redis-cli -h localhost -p 6379 flushall
```

---

## 7. KIỂM THỬ VÀ NGHIỆM THU

1. **Khởi động Redis Server** (Docker hoặc local).
2. **Khởi động Backend API**: `dotnet run --project backend/src/Backend.Api`.
3. **Kiểm tra Logs Backend**:
   - Khi khởi động: `[RedisCacheService] Redis Cache connected successfully: localhost:6379`.
4. **Gọi API `GET /api/ielts/vocab`**:
   - Lần 1: Cache Miss -> Đọc DB -> Ghi Cache Redis (thời gian ~100-200ms).
   - Lần 2: Cache Hit -> Đọc trực tiếp từ Redis (thời gian ~2-5ms).
5. **Kiểm tra Invalidation**:
   - Thực hiện thao tác Thêm từ vựng mới qua `POST /api/ielts/vocab`.
   - Log ghi nhận: `[RedisCacheService] Removed 1 keys matching pattern: ieltsHSK:dev:ielts:vocab:*`.
   - Lần gọi `GET /api/ielts/vocab` tiếp theo tự động tải lại dữ liệu mới nhất từ PostgreSQL.
6. **Kiểm tra Resilience Fallback**:
   - Tắt container Redis (`docker stop ieltshsk-redis`).
   - Gọi lại API: API vẫn phản hồi bình thường nhờ fallback sang In-Memory Cache, không có ngoại lệ làm gián đoạn hệ thống.
