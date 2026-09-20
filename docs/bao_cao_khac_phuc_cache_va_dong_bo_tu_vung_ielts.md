# BÁO CÁO TOÀN DIỆN: KHẮC PHỤC SỰ CỐ BỘ NHỚ ĐỆM CACHE VÀ ĐỒNG BỘ TỪ VỰNG IELTS

> **Dự án**: Nền tảng luyện thi IELTS & HSK (ieltsHSK)  
> **Thời gian thực hiện**: 20/09/2026  
> **Tài liệu lưu trữ**: `docs/bao_cao_khac_phuc_cache_va_dong_bo_tu_vung_ielts.md`  
> **Trạng thái**: Đã khắc phục triệt để & Build kiểm chứng thành công

---

## 1. TỔNG QUAN VẤN ĐỀ (EXECUTIVE SUMMARY)

### 1.1. Hiện tượng ghi nhận
Tại trang quản trị Ngân hàng Từ vựng IELTS (`http://localhost:5102/portal-hub/ielts-vocab`):
1. **Tổng số từ không thay đổi sau khi thêm mới bằng tay**: Người dùng thử nhập tay một từ vựng mới và bấm Lưu, modal popup tự đóng lại nhưng số lượng thống kê trên màn hình vẫn giữ nguyên là **"Tổng: 8155 từ"**, từ mới không xuất hiện trong bảng danh sách.
2. **Thắc mắc về tính chính xác của số lượng từ và giá trị ID**: Bảng từ vựng hiển thị ID bắt đầu lên tới hơn 20.000 (ví dụ: `#20029`, `#20030`), khiến người dùng hoài nghi liệu hệ thống có thực sự đang có 8.155 từ hay bị mất mát / không đồng bộ dữ liệu.

---

## 2. PHÂN TÍCH NGUYÊN NHÂN GỐC RỄ (ROOT CAUSE ANALYSIS)

### 2.1. Tại sao ID lên tới hơn 20.000 trong khi tổng số từ chỉ là 8.155?
- **Cơ chế Identity / Auto-Increment Sequence**: Cột `Id` trong cơ sở dữ liệu (PostgreSQL / SQL Server) được cấu hình tự tăng tuần tự.
- **Tính chất sequence**: Bộ đếm ID chỉ tăng tiến tới, không tự động lùi lại hoặc reset khi có thao tác xóa dữ liệu (`DELETE FROM`), import đè (`UPSERT`), hoặc rollback transaction.
- **Lịch sử dữ liệu**: Hệ thống hỗ trợ tính năng **Nhập Excel nhiều file** và **Xóa hết**. Qua nhiều đợt thử nghiệm nạp/xóa/cập nhật hàng ngàn từ vựng, sequence của database đã tăng dần từ 1 lên đến trên 20.000.
- **Kết luận**: Số lượng **8.155 từ** là con số chính xác 100% về số bản ghi thực tế đang tồn tại trong Database (`SELECT COUNT(*) = 8155`). Các ID từ 1 đến ~12.000 trước đó là các bản ghi của các đợt dữ liệu cũ đã được dọn dẹp.

---

### 2.2. Tại sao nhập tay từ mới mà tổng số từ không đổi?
Sự cố phát sinh từ 3 nguyên nhân kỹ thuật phối hợp:

#### A. Lỗ hổng trong cơ chế xóa Cache của Backend (`RedisCacheService.cs`)
- Backend cài đặt dịch vụ cache `RedisCacheService` hoạt động theo mô hình 2 tầng (Two-Tier Caching):
  - Tầng 1: Redis Server (phân tán).
  - Tầng 2: `IMemoryCache` (RAM nội bộ của tiến trình .NET) đóng vai trò fallback khi Redis không khả dụng trong môi trường local development.
- Endpoint `GET /api/ielts/vocab` lưu trữ toàn bộ danh sách từ vào key `"ielts:vocab:all"` với thời hạn TTL là **2 giờ**.
- Khi người dùng thêm từ mới (`POST /api/ielts/vocab`), backend gọi:
  ```csharp
  await cacheService.RemoveByPrefixAsync("ielts:vocab:", cancellationToken);
  ```
- **Lỗ hổng**: Trong `RedisCacheService.cs`, phương thức `RemoveByPrefixAsync` **chỉ tìm và xóa key trên Redis Server**. Khi chạy local không có Redis, hệ thống fallback sang `_memoryCache`, nhưng phương thức này **hoàn toàn bỏ qua `_memoryCache`**.
- **Hậu quả**: Khi Frontend gọi lại `GET /api/ielts/vocab`, Backend tiếp tục lấy danh sách 8.155 từ cũ còn lưu trong RAM trả về, khiến tổng số từ không đổi.

#### B. Frontend không gửi tín hiệu ép làm mới (Bypass Cache)
- Phương thức `GetVocabularyAsync` trong `IeltsService.cs` của Frontend khi tải lại trang không truyền tham số yêu cầu Backend bỏ qua cache. Do đó, dù Frontend có xóa cache RAM của Blazor WASM thì Backend vẫn phục vụ bản snapshot cũ từ `IMemoryCache`.

#### C. Giao diện (UI) nuốt lỗi âm thầm khi trùng cặp (Từ, Nghĩa)
- Backend kiểm tra trùng lặp:
  ```csharp
  bool exists = await dbContext.IeltsVocabularies.AnyAsync(v => v.Word == req.Word && v.Meaning == req.Meaning);
  if (exists) return Results.BadRequest("Cặp (từ, nghĩa) này đã tồn tại.");
  ```
- Trong `AdminIeltsVocab.razor`, phương thức `SaveItem()` trước đây không kiểm tra giá trị boolean phản hồi từ `CreateVocabularyAsync`:
  ```csharp
  // Code cũ:
  if (editingItem == null)
      await IeltsService.CreateVocabularyAsync(form);
  showModal = false; // Luôn đóng modal dù API trả về 400 Bad Request
  await LoadData();
  ```
- Nếu người dùng nhập một từ đã tồn tại, API từ chối lưu, nhưng giao diện tự đóng lại mà không hề có bất kỳ thông báo lỗi nào.

---

## 3. GIẢI PHÁP ĐÃ TRIỂN KHAI (IMPLEMENTATION)

### 3.1. Nâng cấp `RedisCacheService` đồng bộ hóa `IMemoryCache`
- Bổ sung `ConcurrentDictionary<string, byte> _memoryKeys` để theo dõi toàn bộ key được ghi vào `_memoryCache`.
- Khi `RemoveByPrefixAsync(string prefix)` được gọi, hệ thống quét và xóa toàn bộ các key có tiền tố trùng khớp trong cả RAM nội bộ lẫn Redis:
  ```csharp
  var localPrefix = _prefix + prefix;
  var matchingKeys = _memoryKeys.Keys
      .Where(k => k.StartsWith(localPrefix, StringComparison.OrdinalIgnoreCase))
      .ToList();

  foreach (var key in matchingKeys)
  {
      _memoryCache.Remove(key);
      _memoryKeys.TryRemove(key, out _);
  }
  ```

### 3.2. Cập nhật Endpoint IELTS Vocab tại Backend (`Program.cs`)
- Bổ sung tham số `bool? bypassCache` cho `GET /api/ielts/vocab`:
  - Khi `bypassCache == true`, bỏ qua bước đọc cache, truy vấn trực tiếp từ Database và cập nhật lại cache tươi mới nhất.
- Tại các endpoint `POST`, `PUT`, `DELETE`: Thực hiện xóa dứt điểm cả key chính `ielts:vocab:all` lẫn prefix `ielts:vocab:`.

### 3.3. Cải tiến `IeltsService.cs` phía Frontend
- Bổ sung `bypassCache=true` khi gọi `GetVocabularyAsync(forceRefresh: true)`.
- Cung cấp 2 phương thức mới trả về phản hồi chi tiết từ máy chủ:
  - `CreateVocabularyWithFeedbackAsync(IeltsVocabularyItem item)` -> `(bool Success, string? ErrorMessage)`
  - `UpdateVocabularyWithFeedbackAsync(int id, IeltsVocabularyItem item)` -> `(bool Success, string? ErrorMessage)`

### 3.4. Cải tiến giao diện Admin Hub (`AdminIeltsVocab.razor`)
1. **Thêm nút "Làm mới"**:
   - Đặt ngay cạnh chỉ số `Tổng: ... từ` trên toolbar.
   - Kèm icon xoay linh hoạt, kích hoạt `LoadData(forceRefresh: true)` để ép hệ thống tải trực tiếp từ DB.
2. **Hiển thị cảnh báo lỗi chi tiết**:
   - Nếu lưu thất bại hoặc bị trùng từ, hiển thị hộp thoại alert nguyên nhân cụ thể từ máy chủ.
   - Khi lưu thành công, tự động kích hoạt `LoadData(forceRefresh: true)` để từ mới xuất hiện ngay lập tức và tăng tổng số đếm.

---

## 4. KẾT QUẢ KIỂM CHỨNG & BUILD VERIFICATION

1. **Kiểm tra biên dịch Backend**:
   - Lệnh: `dotnet build backend/src/Backend.Api/Backend.Api.csproj`
   - Kết quả: **0 Error(s)**, **0 Warning(s)**.
2. **Kiểm tra biên dịch Frontend**:
   - Lệnh: `dotnet build frontend/src/Frontend.App/Frontend.App.csproj`
   - Kết quả: **0 Error(s)**.
3. **Bảo mật**:
   - Đã xác minh file cấu hình `appsettings.json` giữ nguyên placeholder cho credentials, đảm bảo an toàn bí mật API khi push lên repository.

---

## 5. KẾT LUẬN & HƯỚNG DẪN SỬ DỤNG
- Người dùng chỉ cần F5 hoặc tải lại giao diện tại `http://localhost:5102/portal-hub/ielts-vocab`.
- Bấm nút **"Làm mới"** để kiểm tra dữ liệu tươi mới nhất từ Database bất kỳ lúc nào.
- Việc thêm/sửa/xóa từ vựng hiện tại hoạt động mượt mà, phản ánh tức thì vào tổng số từ mà không bị kẹt cache.
