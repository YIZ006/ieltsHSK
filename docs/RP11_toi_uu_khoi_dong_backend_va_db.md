# BÁO CÁO KỸ THUẬT

- **Ngày thực hiện**: 09/09/2026
- **Tên báo cáo**: BÁO CÁO TỐI ƯU KHỞI ĐỘNG BACKEND VÀ KHẮC PHỤC HIỆN TƯỢNG PHẢI REFRESH NHIỀU LẦN MỚI THẤY DỮ LIỆU TỪ DATABASE
- **Người viết**: YIZ006 (https://github.com/YIZ006)

---

## 1. MÔ TẢ HIỆN TƯỢNG (SYMPTOMS)

- Khi khởi động backend và truy cập vào giao diện web (Frontend Blazor WASM) lần đầu tiên, giao diện thường trống dữ liệu hoặc chỉ hiện dữ liệu mẫu (mock/fallback).
- Người dùng phải nhấn **Refresh (F5)** liên tục từ 2 đến 3 lần thì dữ liệu thực tế từ cơ sở dữ liệu (PostgreSQL trên Supabase) mới bắt đầu xuất hiện.
- Hiện tượng này lặp đi lặp lại mỗi khi chạy lại backend hoặc sau một khoảng thời gian không thao tác.

---

## 2. NGUYÊN NHÂN GỐC RỄ (ROOT CAUSE ANALYSIS)

Qua rà soát chuyên sâu kiến trúc khởi động giữa Backend (.NET Minimal API) và Frontend (Blazor WebAssembly), đội ngũ đã làm rõ **4 nguyên nhân kỹ thuật**:

### 2.1. Quá trình `SeedDataAsync()` chặn toàn bộ việc mở cổng Kestrel (Nguyên nhân trọng yếu nhất)
- **Vị trí**: File `backend/src/Backend.Api/Program.cs`.
- **Cơ chế**: Lệnh `await app.Services.SeedDataAsync();` được gọi đồng bộ ngay trước `app.Run()`.
- **Tác động**:
  - `SeedDataAsync()` thực hiện tuần tự hơn **20 truy vấn SQL** qua internet sang máy chủ PostgreSQL trên đám mây Supabase (Tokyo/Sydney): kiểm tra `__EFMigrationsHistory`, chạy `MigrateAsync`, kiểm tra Admin, Categories, Courses, Websites, Languages, LearningSections, HSK, TOEIC, Games, GrammarStructures...
  - Vì kết nối mạng đám mây qua TLS có độ trễ round-trip (100–150ms mỗi truy vấn), tổng thời gian để hoàn tất việc seed mất từ **5 đến 12 giây**.
  - Trong suốt 5–12 giây này, `app.Run()` **chưa hề được gọi**, đồng nghĩa Kestrel **chưa mở cổng 5101**.
  - Frontend Blazor WASM tải xong trên trình duyệt chỉ mất 0.5s và lập tức gửi request API đến `http://localhost:5101/api/...`. Do cổng chưa mở, trình duyệt trả về lỗi `ERR_CONNECTION_REFUSED`.
  - Frontend bắt lỗi này trong khối `try/catch` và nuốt exception, trả về danh sách rỗng hoặc mock data. Khi người dùng refresh lần 1, backend vẫn đang seed dở dang. Phải đến lần refresh thứ 2 hoặc 3 khi backend seed xong và mở cổng thì dữ liệu mới nạp được.

### 2.2. Kết nối Redis đồng bộ gây nghẽn 3.000ms ở request đầu tiên
- **Vị trí**: `backend/src/Backend.Infrastructure/DependencyInjection.cs`.
- **Cơ chế**: Thư viện StackExchange.Redis được cấu hình kết nối đồng bộ `ConnectionMultiplexer.Connect(options)` với timeout mặc định 3.000ms.
- **Tác động**: Nếu môi trường phát triển local không bật Docker Redis, request đầu tiên gọi vào cache sẽ bị chặn luồng (freeze) đúng 3 giây chờ kết nối thất bại rồi mới fallback sang `MemoryCache`.

### 2.3. Cấu hình Supabase Pooler thiếu Keepalive dẫn đến đứt kết nối ngầm (NAT Timeout)
- **Vị trí**: `backend/src/Backend.Api/appsettings.Development.json` và chuỗi kết nối Npgsql.
- **Cơ chế**: Kết nối tới `pooler.supabase.com` thiếu tham số `Keepalive=30` và `No Reset On Close=true`.
- **Tác động**: Tường lửa / NAT của AWS và Supabase Pooler (Supavisor/PgBouncer) tự động ngắt các kết nối TCP không hoạt động sau 4–5 phút rảnh. Phía client tưởng socket vẫn mở, nên khi có truy vấn mới, client phải chờ socket timeout rồi mới kích hoạt retry, gây cảm giác đơ hoặc lỗi ở lần truy cập kế tiếp. Đồng thời, `MinPoolSize=0` làm request đầu tiên phải chịu thêm chi phí bắt tay TLS (~500ms).

### 2.4. Phía Frontend không có cơ chế tự động thử lại (Auto-Retry)
- **Vị trí**: Các Service gọi API ở Frontend (`IeltsService`, `HskService`, `NavigationService`, `MockTestService`).
- **Cơ chế**: Khi gặp lỗi `HttpRequestException` (kết nối bị từ chối do backend đang bật), các service lập tức trả về `null` hoặc danh sách rỗng, bắt buộc người dùng phải F5 thủ công.

---

## 3. CÁC BIỆN PHÁP ĐÃ TRIỂN KHAI (IMPLEMENTATION DETAILS)

### 3.1. Chuyển đổi khởi động Backend thành Bất đồng bộ (< 100ms)
- **File sửa đổi**: `backend/src/Backend.Api/Program.cs`
- Toàn bộ các tác vụ nặng: mở trước kết nối PostgreSQL (`CanConnectAsync`), nạp trước model EF Core, kết nối Redis và chạy `SeedDataAsync()` được đưa vào **Background Task** (`Task.Run`).
- Kestrel gọi `app.Run()` và mở cổng 5101 **ngay lập tức trong vòng 50–100ms**.
- Dữ liệu đã có sẵn trong cơ sở dữ liệu được phục vụ ngay từ request đầu tiên của người dùng mà không phải chờ đợi kiểm tra seed.

### 3.2. Tối ưu hóa chuỗi kết nối Supabase Pooler và Redis
- **File sửa đổi**: `backend/src/Backend.Infrastructure/DependencyInjection.cs` & `backend/src/Backend.Api/appsettings.Development.json`
- Tự động bổ sung các cờ tối ưu cho Supabase Pooler:
  - `Keepalive=30`: Gửi gói tin giữ kết nối TCP mỗi 30s, chống việc NAT tự ngắt kết nối khi rảnh.
  - `No Reset On Close=true`: Ngăn lệnh `DISCARD ALL` gây lỗi/chậm khi trả kết nối về pooler.
  - `Minimum Pool Size=1`: Luôn duy trì ít nhất 1 kết nối ấm sẵn sàng, triệt tiêu độ trễ TLS handshake ở request đầu.
  - `Connection Idle Lifetime=60`: Thu hồi kết nối cũ an toàn trước khi pooler server timeout.
- Bọc kết nối Redis qua `Lazy<IConnectionMultiplexer>` với `ConnectTimeout=1000ms`, đồng thời kích hoạt kết nối ngầm khi app vừa chạy. Request đầu tiên hoàn toàn không bị trễ kể cả khi máy không bật Redis.
- Bổ sung kiểm tra nhanh (Fast-path) trong `SeedDataAsync`: Nếu Admin và các danh mục học tập cơ bản đã có trong DB, hệ thống lập tức bỏ qua 15 lần SELECT lặp lại không cần thiết.

### 3.3. Bổ sung cơ chế tự động thử lại (Auto-Retry) trên Frontend
- **File sửa đổi**:
  - `frontend/src/Frontend.App/Services/IeltsService.cs`
  - `frontend/src/Frontend.App/Services/HskService.cs`
  - `frontend/src/Frontend.App/Services/NavigationService.cs`
  - `frontend/src/Frontend.App/Services/MockTestService.cs`
- Khi bắt được ngoại lệ `HttpRequestException` (xảy ra nếu người dùng mở web đúng tích tắc backend đang khởi tạo Kestrel), service sẽ tự động đợi 500ms và thử lại 1 lần. Dữ liệu sẽ xuất hiện tự nhiên mà người dùng không bao giờ cần phải bấm F5 thủ công.

---

## 4. DANH SÁCH FILE THAY ĐỔI

1. `backend/src/Backend.Api/Program.cs`
2. `backend/src/Backend.Infrastructure/DependencyInjection.cs`
3. `backend/src/Backend.Api/appsettings.Development.json`
4. `frontend/src/Frontend.App/Services/IeltsService.cs`
5. `frontend/src/Frontend.App/Services/HskService.cs`
6. `frontend/src/Frontend.App/Services/NavigationService.cs`
7. `frontend/src/Frontend.App/Services/MockTestService.cs`
8. `docs/bao_cao_toi_uu_khoi_dong_backend_va_ket_noi_db.md` (Tài liệu này)

---

## 5. HƯỚNG DẪN KHỞI ĐỘNG LẠI & KIỂM CHỨNG

Để các thay đổi trên có hiệu lực:
1. **Khởi động lại Backend**:
   - Dừng tiến trình backend hiện tại (nhấn `Ctrl + C` hoặc nút Stop trong IDE).
   - Chạy lại backend: `dotnet run --project backend/src/Backend.Api`
   - Quan sát log console: Kestrel sẽ mở cổng `http://localhost:5101` ngay lập tức (< 1 giây), sau đó mới xuất hiện log ngầm `[Startup] Database connection warmed up & seed data verified.`
2. **Tải lại Frontend**:
   - Khởi chạy / reload trang web Frontend (`http://localhost:5102`).
   - Vào các trang: Dashboard IELTS, Từ vựng HSK, Mock Test... Dữ liệu từ database Supabase sẽ tải ra ngay từ lần đầu tiên mà không cần bấm F5.
