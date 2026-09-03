# BÁO CÁO TOÀN DIỆN: KHẮC PHỤC SỰ CỐ ĐỒNG BỘ TRẠNG THÁI LÀM BÀI VÀ ĐIỂM THI IELTS PHÍA USER

> **Dự án**: Nền tảng luyện thi IELTS & HSK (ieltsHSK)  
> **Thời gian thực hiện**: 03/09/2026  
> **Tài liệu lưu trữ**: `docs/bao_cao_khac_phuc_dong_bo_trang_thai_va_diem_ielts.md`  
> **Trạng thái**: Đã giải quyết triệt để & kiểm chứng thành công

---

## 1. TỔNG QUAN SỰ CỐ (EXECUTIVE SUMMARY)

### 1.1. Hiện tượng ghi nhận
- Học viên làm bài thi IELTS Mock Test (ví dụ: *IELTS Mock Test 2025 December - Practise Test 1*) và bấm nộp bài thành công.
- Trên trang quản trị chấm điểm dành cho Giáo viên / Admin (`http://localhost:5102/portal-hub/grading`), hệ thống hiển thị đầy đủ các bài nộp:
  - Bài nộp **#38**: Kỹ năng *Listening*, trạng thái `Scored`, điểm số `Band 2.5`.
  - Bài nộp **#39**: Kỹ năng *Writing*, trạng thái `Graded` (đã được Admin chấm), điểm số `Band 1.0`.
- **Tuy nhiên, phía giao diện Học viên (User)**:
  1. Trang **Luyện đề IELTS** (`http://localhost:5102/ielts/luyen-de`): Cả 4 ô kỹ năng của Practise Test 1 vẫn hiển thị các nút mặc định "LÀM BÀI", không hiển thị badge trạng thái đã làm, không hiện điểm số Band hay trạng thái chờ chấm.
  2. Trang **Tổng hợp kết quả** (`http://localhost:5102/ielts/mock-test/summary?MockTestId=1&CollectionName=IELTS%20Mock%20Test%202025%20December&Title=Practise%20Test%201`): Hiển thị `Overall Band Score 0,0`, hoàn thành `0/4 kỹ năng`, tất cả 4 kỹ năng đều ghi "Chưa làm".

---

## 2. PHÂN TÍCH NGUYÊN NHÂN GỐC RỄ (ROOT CAUSE ANALYSIS)

Sau khi kiểm tra nhật ký runtime, mô phỏng luồng request kèm token và phân tích mã nguồn giữa Backend (.NET 9 Minimal API) và Frontend (.NET 9 Blazor WASM), đội ngũ đã xác định chính xác **4 nguyên nhân gốc rễ**:

### 2.1. Lỗi vòng lặp đối tượng vô hạn của Entity Framework Core (`HTTP 500 Object Cycle Exception`)
- **Cơ chế phát sinh**:
  1. Khi người dùng đăng nhập tài khoản học viên, HttpClient tự động đính kèm header `Authorization: Bearer <JWT_Token>` qua `AuthHeaderHandler`.
  2. Tại Backend `Program.cs`, middleware JWT xác thực token và kích hoạt sự kiện `OnTokenValidated`. Sự kiện này thực hiện truy vấn:
     ```csharp
     var user = await dbContext.Users.FindAsync(new object[] { userId }, context.HttpContext.RequestAborted);
     ```
     Lúc này, thực thể `User` (#2 - Chiến Phạm) được nạp vào bộ nhớ theo dõi trạng thái của EF Core (`ChangeTracker`).
  3. Khi frontend gọi endpoint đồng bộ `GET /api/test-submissions/sync?userId=2`:
     ```csharp
     var query = dbContext.TestSubmissions.AsQueryable();
     var list = await query.OrderByDescending(...).ToListAsync();
     return Results.Ok(list);
     ```
  4. Do thực thể `User` đã tồn tại sẵn trong `ChangeTracker`, EF Core tự động thực hiện cơ chế **Relationship Fixup**: gán thực thể `user` vào navigation property `submission.User`. Ngược lại, thực thể `User` lại sở hữu tập hợp `user.TestSubmissions` chứa chính các bài nộp đó.
  5. Khi framework tiến hành tuần tự hoá kết quả trả về bằng thư viện `System.Text.Json`, nó rơi vào vòng lặp tham chiếu tuần hoàn vô tận:
     `submission.User -> TestSubmissions[0] -> User -> TestSubmissions[0]...`
  6. Vượt quá giới hạn chiều sâu tối đa (depth 64), `System.Text.Json` ném ngoại lệ:
     ```text
     System.Text.Json.JsonException: A possible object cycle was detected. This can either be due to a cycle or if the object depth is larger than the maximum allowed depth of 64. Path: $.User.TestSubmissions.User.TestSubmissions...
     ```
     và phản hồi mã lỗi **HTTP 500 Internal Server Error**.
  7. Phía Frontend, hàm `ExamSubmissionService.GetAllAsync()` đặt lệnh gọi trong khối `try ... catch` nhưng chỉ nuốt lỗi âm thầm và trả về danh sách trống `local = []`. Do đó toàn bộ dữ liệu bài nộp từ cơ sở dữ liệu không bao giờ cập nhật được vào giao diện học viên.

### 2.2. Endpoint nộp bài `POST /api/test-submissions` thiếu trường `SubmittedAt`
- Đối tượng trả về của endpoint `POST /api/test-submissions` trước đây là:
  ```csharp
  return Results.Ok(new
  {
      Id = submission.Id,
      StudentName = submission.StudentName,
      Skill = submission.Skill,
      ExamTitle = submission.ExamTitle,
      AttemptNumber = submission.AttemptNumber,
      Status = submission.Status,
      R2StorageKey = submission.R2StorageKey
      // THIẾU: SubmittedAt = submission.SubmittedAt
  });
  ```
- Khi phía client nhận kết quả và gán `submission.SubmittedAt = res.SubmittedAt`, do `res.SubmittedAt` không có trong JSON nên nó nhận giá trị mặc định là `0001-01-01T00:00:00Z` (`DateTime.MinValue`).
- Việc này phá vỡ hoàn toàn thuật toán tìm bản ghi cục bộ `FindLocalMatch`: phép trừ `(l.SubmittedAt - srv.SubmittedAt).TotalMinutes` cho ra khoảng cách hơn 2000 năm (> 24 giờ), khiến hệ thống không thể liên kết bài nộp vừa gửi với dữ liệu từ server.

### 2.3. Thiếu cơ chế ánh xạ chi tiết `DetailsJson` khi đồng bộ về Local
- Khi đồng bộ các bản ghi `serverItems` về `local`, hàm `GetAllAsync()` chỉ đọc `BandScore` và `Status` cơ bản.
- Trường `DetailsJson` của kỹ năng **Listening** và **Reading** (chứa thông tin từng câu trả lời đúng/sai `GradingResultRecord`) không được deserialize thành object `Grading`. Do đó, khi học viên làm mới trang (F5), nút "Xem đáp án" không lấy được dữ liệu chi tiết của bài làm để hiển thị lại bài đã làm.
- Bài làm Writing #39 có `ExamTitle = "IELTS Online Tests"`. Khi không có `MockTestId`, cơ chế so khớp cũ chỉ dựa vào chuỗi tiêu đề bài thi khiến bài làm bị bỏ qua không khớp được vào card của "Practise Test 1".

### 2.4. Trải nghiệm giao diện (UI/UX) và cơ chế cập nhật Real-time đa tab
- Yêu cầu nghiệp vụ:
  - **Reading & Listening**: Hệ thống tự chấm điểm ngay lập tức -> Hiển thị badge `Band X.X` và nút `Làm lại` + `Xem đáp án`.
  - **Writing & Speaking**: Cần giáo viên chấm điểm -> Khi mới nộp phải hiển thị badge màu vàng `Chờ chấm` (kèm icon đồng hồ cát). Khi giáo viên chấm xong mới chuyển thành `Band X.X` và nút `Viết lại` / `Thi lại` + `Xem bài viết` / `Xem bài nói`.
- Trang Tổng hợp (`IeltsMockTestSummary.razor`) trước đây không đăng ký JavaScript Hook theo dõi chuyển đổi tab (`TabVisibility`). Khi giáo viên chấm điểm ở tab khác và học viên chuyển về lại tab tổng hợp thì trang không tự động tải lại dữ liệu mới nếu không bấm F5 thủ công.

---

## 3. CÁC THAY ĐỔI KỸ THUẬT ĐÃ TRIỂN KHAI

### 3.1. Sửa đổi Backend (`backend/src/Backend.Api/Program.cs`)

1. **Bật bỏ qua vòng lặp tham chiếu JSON trên toàn ứng dụng**:
   ```csharp
   builder.Services.ConfigureHttpJsonOptions(options =>
   {
       options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
   });
   ```

2. **Cập nhật endpoint `GET /api/test-submissions/sync`**:
   - Thêm `.AsNoTracking()` để EF Core không theo dõi thực thể và không kích hoạt Relationship Fixup.
   - Sử dụng `.Select(...)` chiếu tường minh sang DTO không chứa navigation property `User`:
   ```csharp
   app.MapGet("/api/test-submissions/sync", async (...) =>
   {
       ...
       var query = dbContext.TestSubmissions.AsNoTracking().AsQueryable();
       ...
       var list = await query
           .OrderByDescending(s => s.SubmittedAt)
           .Take(100)
           .Select(s => new
           {
               s.Id,
               s.UserId,
               s.StudentName,
               s.UserEmail,
               s.SessionId,
               s.Skill,
               s.ExamUrl,
               s.ExamTitle,
               s.AttemptNumber,
               s.BandScore,
               s.CorrectCount,
               s.TotalCount,
               s.DetailsJson,
               s.R2StorageKey,
               s.Status,
               s.TeacherFeedback,
               s.AudioKey,
               s.SubmittedAt,
               s.GradedAt
           })
           .ToListAsync(cancellationToken);

       return Results.Ok(list);
   });
   ```

3. **Cập nhật endpoint `POST /api/test-submissions`**:
   - Trả về kèm `SubmittedAt = submission.SubmittedAt` để phía client lưu chính xác mốc thời gian nộp bài.

4. **Cập nhật `.AsNoTracking()` cho các endpoint liên quan**:
   - `/api/test-submissions/latest`
   - `/api/admin/test-submissions`

---

### 3.2. Sửa đổi Frontend Service (`frontend/src/Frontend.App/Services/ExamSubmissionService.cs`)

1. **Bổ sung hàm chuẩn hoá và so khớp URL linh hoạt (`IsUrlMatch`)**:
   - Hỗ trợ giải mã URL (`Uri.UnescapeDataString`) để tránh sai lệch ký tự mã hóa URL (ví dụ: `%20` so với dấu cách).
   - So khớp được cả URL tương đối trong ứng dụng, URL tuyệt đối từ CDN Cloudflare R2 và tên tệp tin gốc (`Path.GetFileName`):
   ```csharp
   public static bool IsUrlMatch(string? url1, string? url2)
   {
       if (string.IsNullOrWhiteSpace(url1) || string.IsNullOrWhiteSpace(url2)) return false;
       var norm1 = NormalizeUrl(url1);
       var norm2 = NormalizeUrl(url2);
       if (norm1 == norm2) return true;
       if (norm1.Contains(norm2) || norm2.Contains(norm1)) return true;

       try
       {
           var file1 = Path.GetFileName(norm1);
           var file2 = Path.GetFileName(norm2);
           if (!string.IsNullOrEmpty(file1) && !string.IsNullOrEmpty(file2) && file1.Equals(file2, StringComparison.OrdinalIgnoreCase)) return true;
       }
       catch { }

       return false;
   }
   ```

2. **Cập nhật logic đồng bộ `GetAllAsync()`**:
   - Cập nhật đúng các trường `Status`, `BandScore`, `TeacherFeedback`, `CorrectCount`, `TotalQuestions`.
   - Tự động deserialize `GradingResultRecord` từ `DetailsJson` của kỹ năng Listening / Reading để nút "Xem đáp án" luôn có dữ liệu.
   - Tự động deserialize `IeltsScoreReport` từ `DetailsJson` của kỹ năng Writing / Speaking khi giáo viên đã chấm bài.
   - Thêm log chi tiết `Console.WriteLine($"[ExamSubmissionService] Sync error: {ex.Message}");` thay vì nuốt lỗi âm thầm.

3. **Bảo vệ mốc thời gian nộp bài trong các hàm Save**:
   - Trong `SaveReadingSubmissionAsync`, `SaveListeningSubmissionAsync`, `SaveWritingSubmissionAsync`, `SaveSpeakingSubmissionAsync`:
   ```csharp
   if (res != null)
   {
       submission.Id = res.Id.ToString();
       submission.R2StorageKey = res.R2StorageKey;
       if (res.SubmittedAt > DateTime.MinValue) submission.SubmittedAt = res.SubmittedAt;
   }
   ```

4. **Cải tiến `GetMockTestSummaryAsync()`**:
   - Tìm kiếm bài làm từng kỹ năng dựa trên kết hợp: `SessionId`, `MockTestId`, `IsUrlMatch` (với URL từng kỹ năng của Mock Test), `TestTitle` và `ExamTitle`.

---

### 3.3. Sửa đổi Giao diện Luyện đề (`frontend/src/Frontend.App/Pages/Ielts/IeltsMockTests.razor`)

1. **Hiển thị Badge trạng thái chính xác cho từng kỹ năng**:
   - **Listening & Reading**:
     - Nếu đã nộp và có điểm: `<span class="skill-score-badge">Band @sub.BandScore.Value.ToString("0.0")</span>`
     - Nếu đã nộp nhưng chưa có điểm: `<span class="skill-score-badge pending"><i class="bi bi-check-circle"></i> Đã làm</span>`
   - **Writing & Speaking**:
     - Nếu trạng thái là `Graded` hoặc `Scored` (có điểm): `<span class="skill-score-badge">Band @sub.BandScore</span>`
     - Nếu trạng thái là `Pending` / `Submitted`: `<span class="skill-score-badge pending"><i class="bi bi-hourglass-split"></i> Chờ chấm</span>`
2. **Cập nhật hàm `GetLatestSub`**:
   - Sử dụng `ExamSubmissionService.IsUrlMatch` để so khớp bài nộp với URL đề thi của card hiện tại, đảm bảo không bỏ sót bài nộp kể cả khi link đề là link CDN R2.

---

### 3.4. Sửa đổi Giao diện Tổng hợp (`frontend/src/Frontend.App/Pages/Ielts/IeltsMockTestSummary.razor`)

1. **Tích hợp `TabVisibility` Hook và `IAsyncDisposable`**:
   - Tự động gọi `LoadDataAsync(silent: true)` và cập nhật State khi học viên chuyển tab quay lại trang.
   - Thu hồi tài nguyên hook khi component bị Dispose.
2. **Tối ưu hàm `LoadDataAsync()`**:
   - Đảm bảo tải đề thi và bài làm tương ứng với `MockTestId`, `Title`, `CollectionName`.
   - Tính toán chính xác `OverallBandScore` và số kỹ năng đã hoàn thành.

---

## 4. KẾT QUẢ KIỂM THỬ VÀ XÁC MINH THỰC TẾ

### 4.1. Kiểm thử trực tiếp API Backend (`GET /api/test-submissions/sync`)
- **Điều kiện thử**: Gửi HTTP GET request có header `Authorization: Bearer <JWT_Token>` của tài khoản học viên (UserId = 2 - Chiến Phạm).
- **Kết quả trước khi sửa**: Trả về `HTTP 500 Internal Server Error`, nội dung lỗi `Object cycle detected`.
- **Kết quả sau khi sửa**:
  ```text
  StatusCode: OK (200)
  Body length: 20087 bytes
  Items count: 8 bài nộp
  ```

### 4.2. Kiểm thử khớp nối dữ liệu với đề "Practise Test 1" (MockTestId = 1)
Kết quả khớp nối thực tế qua script kiểm tra tự động:
```text
Server returned 8 items.
Listening: Id=38, Band=2.5, Status=Scored
Reading:   Id=, Band=, Status= (Chưa làm)
Writing:   Id=39, Band=1.0, Status=Graded (Đã chấm bởi Admin)
Speaking:  Id=, Band=, Status= (Chưa làm)
Total completed: 2/4 skills
Overall Band Score: 2.0 (Làm tròn theo chuẩn IELTS: trung bình 2.5 và 1.0 = 1.75 -> Band 2.0)
```

### 4.3. Kiểm thử hiển thị trên giao diện người dùng
| Trang | Trước khi sửa | Sau khi sửa |
| :--- | :--- | :--- |
| **`/ielts/luyen-de`** | Tất cả 4 kỹ năng đều hiện nút xanh/tím "LÀM BÀI", không có badge điểm. | - Listening: Badge **Band 2.5**, nút **Làm lại** & **Xem đáp án**.<br>- Reading: Nút **Làm bài**.<br>- Writing: Badge **Band 1.0**, nút **Viết lại** & **Xem bài viết** (nếu chưa chấm sẽ hiện badge vàng **Chờ chấm**).<br>- Speaking: Nút **Làm bài**. |
| **`/ielts/mock-test/summary`** | Overall Band `0,0`, hoàn thành `0/4 kỹ năng`, tất cả 4 card đều ghi "Chưa làm". | - Overall Band **2.0**, hoàn thành **2/4 kỹ năng**.<br>- Listening: Card hiển thị **Band 2.5** (3/40 câu đúng).<br>- Writing: Card hiển thị **Band 1.0** (kèm tiêu chí đánh giá và nhận xét của giáo viên).<br>- Reading & Speaking: Card hiển thị **Chưa làm**.<br>- Tự động làm mới điểm khi giáo viên chấm ở tab khác. |

---

## 5. DANH SÁCH CÁC TỆP TIN ĐÃ CHỈNH SỬA

| STT | Tệp tin | Vị trí | Mục đích thay đổi |
| :---: | :--- | :--- | :--- |
| 1 | `backend/src/Backend.Api/Program.cs` | Backend API | Thêm `IgnoreCycles`, `.AsNoTracking()`, `Select` DTO projection cho sync/latest/admin API, trả về `SubmittedAt`. |
| 2 | `frontend/src/Frontend.App/Services/ExamSubmissionService.cs` | Frontend Core | Bổ sung `IsUrlMatch`, deserialize `Grading` và `ScoreReport`, bảo vệ `SubmittedAt`, sửa `GetMockTestSummaryAsync`. |
| 3 | `frontend/src/Frontend.App/Pages/Ielts/IeltsMockTests.razor` | Frontend UI | Cải tiến badge điểm/chờ chấm cho 4 kỹ năng, tối ưu hàm `GetLatestSub`. |
| 4 | `frontend/src/Frontend.App/Pages/Ielts/IeltsMockTestSummary.razor` | Frontend UI | Bổ sung `TabVisibility` hook, hàm `LoadDataAsync`, hỗ trợ so khớp 4 kỹ năng linh hoạt. |

---

## 6. KHUYẾN NGHỊ VÀ QUY TẮC BẢO TRÌ VỀ SAU

1. **Tránh trả về Entity EF Core trực tiếp qua Minimal API**:
   - Khi trả dữ liệu ra bên ngoài, luôn sử dụng `.AsNoTracking()` và chiếu qua DTO (`.Select(...)`) hoặc anonymous object. Không nên trả về trực tiếp Db Entity có quan hệ lồng nhau hai chiều để ngăn chặn triệt để nguy cơ `Object Cycle`.
2. **Luôn lưu trữ và trả về `SubmittedAt`**:
   - Mọi endpoint tạo bài nộp bắt buộc phải trả về trường `SubmittedAt` (dạng UTC ISO string) để Client đồng bộ đúng thời gian.
3. **Đồng bộ đa tab (Multi-tab synchronization)**:
   - Các trang hiển thị trạng thái động (như Luyện đề, Tổng hợp kết quả, Thông báo điểm) nên duy trì `TabVisibility` hook để người dùng không cần thao tác F5 thủ công khi có sự thay đổi từ tab khác.
