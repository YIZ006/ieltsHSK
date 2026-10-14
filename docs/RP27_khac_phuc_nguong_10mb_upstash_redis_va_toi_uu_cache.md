# BÁO CÁO KỸ THUẬT: XỬ LÝ CẢNH BÁO MAX REQUEST SIZE (10MB) UPSTASH REDIS & TỐI ƯU HÓA BỘ NHỚ ĐỆM
> **Mã tài liệu:** RP27  
> **Dự án:** ieltsHSK  
> **Dịch vụ liên quan:** Upstash Serverless Redis (`ieltshsk-redis` - Gói Free), ASP.NET Core 9 Web API  
> **Thời gian cập nhật:** Tháng 10/2026  

---

## 1. TỔNG QUAN HIỆN TƯỢNG & NGUYÊN NHÂN GỐC RỄ (ROOT CAUSE)

### 📧 Hiện tượng:
Hệ thống Upstash Redis gửi email cảnh báo:
> *"Your requests to your database 'ieltshsk-redis' has hit Max Request Size limit of 10MB at least 1 times in the last 15 minutes. This database is currently on Free plan. Operations exceeding this limit will return an error."*

### 🔍 Nguyên nhân kỹ thuật:
1. **Gom cụm dữ liệu quá lớn vào 1 key duy nhất (Monolithic Cache Key):**
   - Trước đây tại endpoint `GET /api/ielts/vocab`, hệ thống thực hiện truy vấn toàn bộ bảng `ielts_vocabularies` và lưu vào 1 key duy nhất `ielts:vocab:all`.
   - Khi số lượng từ vựng tăng lên hàng nghìn từ (kèm phiên âm, ví dụ, bản dịch câu), chuỗi JSON tuần tự hóa (serialization) vượt ngưỡng kích thước an toàn, tiến sát hoặc vượt trần **10MB** của 1 request duy nhất tới Upstash Redis.
2. **Thiếu chốt chặn kiểm soát kích thước payload (Size Guard):**
   - `RedisCacheService` trước đây thực hiện `JsonSerializer.Serialize` rồi trực tiếp gọi `db.StringSetAsync(fullKey, json, ttl)` mà không đo đạc dung lượng byte UTF-8 của payload trước khi gửi qua mạng.
3. **Thao tác xóa hàng loạt (Bulk Invalidation) không chia chunk:**
   - Trong `RemoveByPrefixAsync`, việc gửi toàn bộ mảng keys trong 1 lệnh duy nhất nếu keyspace lớn có thể làm phình to gói tin pipeline gửi qua socket TLS.

---

## 2. PHÂN TÍCH TÁC ĐỘNG HỆ THỐNG

| Thành phần | Mức độ ảnh hưởng | Đánh giá an toàn |
| :--- | :--- | :--- |
| **Cơ sở dữ liệu chính (PostgreSQL)** | Không ảnh hưởng (0%) | Database Supabase vẫn lưu trữ an toàn, độc lập hoàn toàn với Upstash Redis. |
| **Dung lượng Database Redis** | Không bị khóa | Upstash chỉ từ chối các request vượt quá 10MB, các request đọc/ghi key nhỏ bình thường (< 10MB) vẫn thông suốt. |
| **Ứng dụng Backend** | Đã có sẵn Try-Catch & Fallback | `RedisCacheService` đã bọc `try-catch` và tự động fallback sang `IMemoryCache` (RAM), do đó người dùng cuối không bị crash web. Tuy nhiên, việc để lỗi xảy ra ở Upstash sẽ làm tốn băng thông TLS và kích hoạt email cảnh báo của nhà cung cấp. |

---

## 3. GIẢI PHÁP PHÒNG THỦ ĐA TẦNG ĐÃ TRIỂN KHAI

### 🛡️ Tầng 1: Payload Size Guard tại Hạ tầng (`RedisCacheService.cs`)
* **Thiết lập ngưỡng an toàn `MaxItemSizeBytes`:** Mặc định **512 KB** (hoặc cấu hình tùy chỉnh qua `appsettings.json`).
  * *Lý do:* Theo best practice của Redis, một key chỉ nên có kích thước từ 10KB - 256KB để tối ưu Single-Threaded Event Loop. Giới hạn 512KB đảm bảo không bao giờ một request có thể tiệm cận con số 10MB của Upstash.
* **Cơ chế chốt chặn tự động:**
  ```csharp
  var json = JsonSerializer.Serialize(value, _jsonOptions);
  var byteCount = System.Text.Encoding.UTF8.GetByteCount(json);

  if (byteCount > _maxItemSizeBytes)
  {
      _logger.LogWarning(
          "[Redis Protection] Key {Key} payload size ({SizeKB:N1} KB) exceeds safe Redis threshold ({LimitKB:N1} KB). Skipping Redis to prevent Upstash 10MB limit violation. Storing in local MemoryCache.",
          fullKey, byteCount / 1024.0, _maxItemSizeBytes / 1024.0);
  }
  else
  {
      await db.StringSetAsync(fullKey, json, ttl);
  }
  ```
* **Fallback liền mạch sang `IMemoryCache` (RAM):**
  Dữ liệu lớn (như toàn bộ danh mục từ vựng) vẫn được lưu tại RAM của server máy chủ Web API. Nhờ đó, người dùng vẫn nhận được tốc độ phản hồi cực nhanh (< 5ms) mà **100% không gửi bất kỳ byte vượt ngưỡng nào lên Upstash Redis**.

### 🧩 Tầng 2: Phân mảnh Key Caching (Fine-grained Topic Chunking)
* Tại `GET /api/ielts/vocab` trong `IeltsEndpoints.cs`:
  * Khi người dùng học theo Topic (trường hợp thực tế của 95% phiên học): Hệ thống tách và lưu cache riêng theo từng Topic:
    `ielts:vocab:topic:{cleanTopic}`
  * Mỗi topic chỉ chứa 20 - 50 từ $\rightarrow$ Payload chỉ từ **10KB - 30KB**, cực kỳ nhẹ và an toàn khi lưu trên Upstash Redis.
  * Khi lấy toàn bộ bảng từ vựng (không lọc theo Topic), `RedisCacheService` sẽ tự động kích hoạt Payload Guard để lưu tại RAM thay vì đẩy chuỗi JSON khổng lồ lên Upstash.

### 📦 Tầng 3: Phân đoạn xóa Batch (Chunking Deletion)
* Trong phương thức `RemoveByPrefixAsync`:
  Thay vì gọi `db.KeyDeleteAsync(keys)` với một mảng hàng nghìn key trong một payload duy nhất, hệ thống tự động chia nhỏ thành từng batch 100 keys/lần:
  ```csharp
  const int batchSize = 100;
  for (int i = 0; i < keys.Length; i += batchSize)
  {
      var chunk = keys.Skip(i).Take(batchSize).ToArray();
      await db.KeyDeleteAsync(chunk);
  }
  ```

---

## 4. BẢNG CẤU HÌNH VÀ VẬN HÀNH UPSTASH REDIS BỀN VỮNG

Cấu hình tại `backend/src/Backend.Api/appsettings.Production.json`:
```json
"Redis": {
  "ConnectionString": "<UPSTASH_REDIS_CONNECTION_STRING>",
  "InstancePrefix": "ieltsHSK:",
  "Enabled": true,
  "DefaultTtlMinutes": 60,
  "MaxItemSizeBytes": 524288
}
```

### 📋 Khuyến nghị thực tiễn cho Gói Upstash Free:
1. **Giới hạn 10,000 commands/ngày:**
   * Các key tĩnh như `navigation:all`, `ielts:sections` được đặt TTL từ **1 - 2 giờ** để giảm tần suất đọc/ghi Redis.
2. **Giới hạn 256MB tổng dung lượng:**
   * Tuyệt đối không lưu file media (audio, ảnh, video, PDF) hay chuỗi base64 vào Redis.
   * File media luôn lưu tại **Cloudflare R2 Object Storage**, database/cache chỉ lưu đường link URL hoặc R2 Key.
3. **Giới hạn 10MB/request:**
   * Hoàn toàn được bảo vệ tự động bởi `Payload Size Guard` (512KB) ở tầng `RedisCacheService`.
