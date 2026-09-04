Hiện hệ thống chưa dùng Redis cho từ vựng: API /api/hsk/vocab đang đọc PostgreSQL trực tiếp, còn frontend chỉ lưu một số trạng thái học vào localStorage. Kế hoạch phù hợp nhất là triển khai Redis theo mô hình cache-aside ở backend, kết hợp tối ưu truy vấn PostgreSQL và giảm số lần frontend gọi API.
Các vị trí cần tác động:
- [Program.cs (line 3079)](/E:/tailieu/Dự án/ielstHSK-PostgeSQL/backend/src/Backend.Api/Program.cs:3079): API đọc/thêm/sửa/xóa/import từ vựng.
- [DependencyInjection.cs](/E:/tailieu/Dự án/ielstHSK-PostgeSQL/backend/src/Backend.Infrastructure/DependencyInjection.cs): nơi phù hợp để đăng ký dịch vụ Redis/cache.
- [HskService.cs (line 1)](/E:/tailieu/Dự án/ielstHSK-PostgeSQL/frontend/src/Frontend.App/Services/HskService.cs:1): frontend gọi API từ vựng.
- [HskVocabulary.razor (line 1)](/E:/tailieu/Dự án/ielstHSK-PostgeSQL/frontend/src/Frontend.App/Pages/Hsk/HskVocabulary.razor:1): nơi tải và hiển thị danh sách từ.
- [index.html (line 1)](/E:/tailieu/Dự án/ielstHSK-PostgeSQL/frontend/src/Frontend.App/wwwroot/index.html:1): đang dùng query version cho CSS.
Kiến trúc đề xuất
Blazor frontend
   |
   | GET /api/hsk/vocab?level=HSK1
   v
ASP.NET API
   |
   | 1. Đọc Redis: hsk:vocab:v1:HSK1
   |    - Có cache: trả về ngay
   |    - Không có cache: truy vấn PostgreSQL
   v
PostgreSQL
   |
   v
API ghi kết quả vào Redis, rồi trả frontend

Khi admin thêm/sửa/xóa/import từ:
PostgreSQL cập nhật thành công
   |
   v
Xóa key Redis tương ứng
   |
   v
Lần đọc kế tiếp tự tạo cache mới
Kế hoạch triển khai cho AI khác
Giai đoạn 1: Đo hiệu năng trước khi sửa
1. Đo thời gian API /api/hsk/vocab theo từng cấp HSK.
2. Ghi nhận:
   - Số lượng từ mỗi cấp.
   - Kích thước JSON trả về.
   - Thời gian PostgreSQL query.
   - Thời gian API serialize JSON.
   - Thời gian frontend render danh sách.
3. Chạy EXPLAIN ANALYZE cho truy vấn lọc theo HskLevel và sắp xếp DisplayOrder.
4. Xác định chậm do DB, network hay render UI. Redis chỉ giải quyết phần đọc DB; nó không tự xử lý render hàng nghìn dòng trên trình duyệt.
Tiêu chí: có số liệu baseline để so sánh sau triển khai.
Giai đoạn 2: Tối ưu PostgreSQL trước
1. Đổi truy vấn đọc từ vựng sang AsNoTracking().
2. Chỉ Select các cột cần hiển thị thay vì tải entity dư thừa.
3. Tạo composite index phù hợp với truy vấn chính:
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_hsk_vocabularies_level_display_order
ON "HskVocabularies" ("HskLevel", "DisplayOrder");
4. Nếu danh sách quá lớn, API cần phân trang:
GET /api/hsk/vocab?level=HSK1&page=1&pageSize=100
5. Không nên tải toàn bộ mọi cấp HSK ngay từ đầu. Chỉ tải cấp người dùng đang chọn.
Tiêu chí: API vẫn ổn định khi Redis tắt; Redis là lớp tăng tốc, không phải điểm phụ thuộc bắt buộc.
Giai đoạn 3: Cài Redis và cấu hình môi trường
1. Thêm package:
Microsoft.Extensions.Caching.StackExchangeRedis
2. Thêm cấu hình theo biến môi trường, không hard-code mật khẩu:
{
  "Redis": {
    "ConnectionString": "localhost:6379",
    "InstanceName": "ielts-hsk:"
  }
}
3. Cấu hình production dùng managed Redis hoặc Redis container riêng.
4. Nếu Redis không kết nối được:
   - Ghi log cảnh báo.
   - API tự động fallback về PostgreSQL.
   - Không trả lỗi 500 chỉ vì cache hỏng.
Tiêu chí: môi trường local, staging và production dùng cấu hình khác nhau qua environment variables/secrets.
Giai đoạn 4: Viết lớp cache tập trung
Tạo abstraction, ví dụ IHskVocabularyCache và HskVocabularyCache, thay vì viết Redis trực tiếp trong từng endpoint.
Các hàm cần có:
Task<IReadOnlyList<HskVocabularyDto>?> GetAsync(string level, CancellationToken ct);
Task SetAsync(string level, IReadOnlyList<HskVocabularyDto> items, CancellationToken ct);
Task InvalidateAsync(string? level, CancellationToken ct);
Quy ước key:
hsk:vocab:v1:HSK1
hsk:vocab:v1:HSK2
hsk:vocab:v1:HSK3
Dùng version v1 để có thể đổi cấu trúc dữ liệu sau này mà không cần xóa toàn bộ Redis thủ công.
TTL đề xuất:
Dữ liệu	TTL	Cách làm mới
Danh sách từ vựng theo cấp	30-60 phút	Xóa ngay khi admin sửa dữ liệu
Danh sách section HSK	1-6 giờ	Xóa khi dữ liệu section thay đổi
Dữ liệu tĩnh, ít đổi	6-24 giờ	Xóa khi deploy hoặc cập nhật
Tiến độ học cá nhân	Không cache giai đoạn đầu	Đọc PostgreSQL trực tiếp


Không nên cache chung tiến độ học theo user ở giai đoạn đầu, vì tính cá nhân hóa và tần suất ghi dễ tạo lỗi dữ liệu cũ.
Giai đoạn 5: Áp dụng cache-aside cho endpoint đọc
Luồng cần triển khai trong GET /api/hsk/vocab:
1. Validate level.
2. Đọc Redis theo key của cấp HSK.
3. Nếu cache hit, trả dữ liệu ngay.
4. Nếu cache miss:
   - Đọc PostgreSQL bằng query đã tối ưu.
   - Serialize DTO.
   - Lưu Redis với TTL.
   - Trả dữ liệu.
5. Log cache_hit, cache_miss, thời gian query DB và thời gian tổng.
Nên cache DTO đã được chuẩn bị cho API, không cache entity EF Core.
Giai đoạn 6: Invalidate cache đúng lúc
Sau khi PostgreSQL cập nhật thành công, xóa cache tương ứng tại các endpoint:
- POST /api/hsk/vocab: xóa cache của cấp vừa thêm.
- PUT /api/hsk/vocab/{id}: xóa cache cấp cũ và cấp mới nếu HskLevel có thể thay đổi.
- DELETE /api/hsk/vocab/{id}: xóa cache cấp chứa từ đó.
- DELETE /api/hsk/vocab: xóa toàn bộ cache vocab.
- Import Excel: xóa cache các cấp bị import.
- Nếu có endpoint sửa section, xóa cache section tương ứng.
Nguyên tắc: ghi DB thành công trước, rồi mới xóa cache. Không xóa cache trước khi DB commit thành công.
Giai đoạn 7: Giảm tải ở frontend
1. Trong HskService, giữ cache bộ nhớ theo level trong suốt phiên mở ứng dụng.
2. Khi người dùng đổi tab rồi quay lại cùng cấp HSK, dùng lại dữ liệu đang có thay vì gọi API lần nữa.
3. Khi admin import/sửa dữ liệu, frontend cần có cơ chế refresh rõ ràng.
4. Nếu mỗi cấp có nhiều từ:
   - Render phân trang hoặc virtualized list.
   - Không render tất cả thẻ từ vựng cùng lúc.
5. Debounce tìm kiếm để không lọc/render quá nhiều lần khi người dùng gõ.
Lưu ý: không nên ghi toàn bộ danh sách từ vựng vào localStorage lâu dài, vì dễ đầy storage và khó đồng bộ khi admin thay đổi nội dung.
Giai đoạn 8: HTTP cache và nén phản hồi
Bổ sung thêm, không thay Redis:
- Bật response compression: Brotli/Gzip.
- API trả ETag hoặc Last-Modified cho dữ liệu ít đổi.
- Browser gửi If-None-Match; server có thể trả 304 Not Modified.
- Cache-Control cho API public có thể là:
Cache-Control: private, max-age=300
Redis giúp server nhanh hơn; HTTP cache giúp giảm cả request và băng thông.
Giai đoạn 9: Theo dõi và kiểm thử
Cần có test cho:
- Cache miss trả đúng dữ liệu và ghi Redis.
- Cache hit không gọi PostgreSQL.
- Thêm/sửa/xóa/import làm mất cache đúng cấp.
- Redis bị down vẫn đọc được PostgreSQL.
- Hai request đồng thời khi cache miss không gây quá nhiều query DB.
- Cache không trả dữ liệu cũ sau import.
Metrics nên theo dõi:
hsk_vocab_cache_hit_total
hsk_vocab_cache_miss_total
hsk_vocab_db_query_duration_ms
hsk_vocab_api_duration_ms
redis_connection_failures_total
Mục tiêu hợp lý:
- Cache hit API từ vựng: dưới 50-100 ms trong cùng hạ tầng.
- Cache hit ratio: trên 80% với dữ liệu HSK được truy cập thường xuyên.
- Redis down: chức năng vẫn hoạt động, chỉ chậm hơn.
Prompt giao cho AI triển khai
Hãy triển khai Redis cache-aside cho hệ thống ASP.NET Core + PostgreSQL này.

Phạm vi:
- Cache endpoint GET /api/hsk/vocab theo từng HskLevel.
- Tạo abstraction IHskVocabularyCache, không gọi Redis trực tiếp rải rác trong Program.cs.
- Redis key theo định dạng hsk:vocab:v1:{level}.
- TTL mặc định 60 phút.
- Cache DTO trả API, không cache EF entity.
- Khi POST, PUT, DELETE, DELETE ALL và import Excel từ vựng thành công, invalidate cache đúng cấp; với thao tác toàn bộ thì xóa tất cả key vocab liên quan.
- Redis không hoạt động phải fallback PostgreSQL, chỉ log warning, không làm API lỗi.
- Tối ưu query bằng AsNoTracking và projection về DTO.
- Bổ sung response compression nếu dự án chưa có.
- Thêm test cache hit, cache miss, invalidation và Redis failure fallback.
- Không thay đổi hành vi API hiện có và không commit file scratch ở root.

Các file cần xem trước:
- backend/src/Backend.Api/Program.cs
- backend/src/Backend.Infrastructure/DependencyInjection.cs
- frontend/src/Frontend.App/Services/HskService.cs
- frontend/src/Frontend.App/Pages/Hsk/HskVocabulary.razor