# Báo cáo hoàn thành: Thiết lập Khóa Ngoại (Foreign Keys) & Liên kết dữ liệu Người dùng

Chúng tôi đã hoàn thành việc rà soát, thiết lập toàn bộ các quan hệ khóa ngoại (Foreign Keys) và đồng bộ hóa các luồng dữ liệu gắn liền với tài khoản người dùng (`User`) trong cơ sở dữ liệu PostgreSQL.

---

## 1. Các quan hệ Khóa ngoại & Entity đã thiết lập

1. **Bài nộp thi (`TestSubmission`)**:
   - Thêm navigation property `public User? User { get; set; }` trong [`TestSubmission.cs`](file:///e:/tailieu/D%E1%BB%B1%20%C3%A1n/ielstHSK-PostgeSQL/backend/src/Backend.Domain/Entities/TestSubmission.cs).
   - Thiết lập cấu hình quan hệ trong [`AppDbContext.cs`](file:///e:/tailieu/D%E1%BB%B1%20%C3%A1n/ielstHSK-PostgeSQL/backend/src/Backend.Infrastructure/Persistence/AppDbContext.cs):
     ```csharp
     entity.HasOne(s => s.User)
           .WithMany(u => u.TestSubmissions)
           .HasForeignKey(s => s.UserId)
           .OnDelete(DeleteBehavior.SetNull);
     ```
   - Thêm collection `public ICollection<TestSubmission> TestSubmissions { get; set; }` trong [`User.cs`](file:///e:/tailieu/D%E1%BB%B1%20%C3%A1n/ielstHSK-PostgeSQL/backend/src/Backend.Domain/Entities/User.cs).

2. **Tiến độ Từ vựng IELTS (`IeltsVocabularyProgress`)**:
   - Tạo mới entity [`IeltsVocabularyProgress.cs`](file:///e:/tailieu/D%E1%BB%B1%20%C3%A1n/ielstHSK-PostgeSQL/backend/src/Backend.Domain/Entities/IeltsVocabularyProgress.cs) với khóa ngoại tới `User` và `IeltsVocabulary`.
   - Cấu hình quan hệ và ràng buộc Unique `(UserId, VocabularyId)` trong [`AppDbContext.cs`](file:///e:/tailieu/D%E1%BB%B1%20%C3%A1n/ielstHSK-PostgeSQL/backend/src/Backend.Infrastructure/Persistence/AppDbContext.cs).
   - Thêm collection `IeltsVocabularyProgresses` trong [`User.cs`](file:///e:/tailieu/D%E1%BB%B1%20%C3%A1n/ielstHSK-PostgeSQL/backend/src/Backend.Domain/Entities/User.cs).

3. **Video Luyện nghe (`ListenVideo`)**:
   - Thêm `public int? UserId { get; set; }` và `public User? User { get; set; }` trong [`ListenVideo.cs`](file:///e:/tailieu/D%E1%BB%B1%20%C3%A1n/ielstHSK-PostgeSQL/backend/src/Backend.Domain/Entities/ListenVideo.cs).
   - Cấu hình khóa ngoại `HasOne(User).WithMany(ListenVideos).HasForeignKey(UserId)` trong [`AppDbContext.cs`](file:///e:/tailieu/D%E1%BB%B1%20%C3%A1n/ielstHSK-PostgeSQL/backend/src/Backend.Infrastructure/Persistence/AppDbContext.cs).

4. **Tự động di chuyển Schema SQL an toàn (Idempotent Migration)**:
   - Trong [`DependencyInjection.cs`](file:///e:/tailieu/D%E1%BB%B1%20%C3%A1n/ielstHSK-PostgeSQL/backend/src/Backend.Infrastructure/DependencyInjection.cs), hàm `SeedDataAsync` tự động kiểm tra và thêm các ràng buộc khóa ngoại `fk_test_submissions_users_user_id`, `fk_listen_videos_users_user_id` và tạo bảng `ielts_vocabulary_progresses` ngay khi server khởi động.

---

## 2. Nâng cấp API Backend & Đồng bộ Dữ liệu

1. **`POST /api/test-submissions`**:
   - Tự động lấy `UserId` từ JWT Bearer Token.
   - Kiểm tra xác thực `User` trong PostgreSQL: nếu hợp lệ sẽ liên kết `submission.UserId`, cập nhật `StudentName`, `UserEmail`.
   - Tự động cộng **+50 XP** cho tài khoản và cập nhật **Chuỗi học tập (Streak)** cùng `LastActive`.
2. **`GET /api/test-submissions/sync`**:
   - Tự động nhận diện tài khoản người dùng từ Token để đồng bộ các bài thi đã làm của chính người dùng đó về client.
3. **Các API Hồ sơ cá nhân & Chuỗi học**:
   - `GET /api/user/me`: Trả về thông tin đầy đủ của User (XP, Streak, Level, Avatar, Role).
   - `PUT /api/user/profile`: Cập nhật FullName, Avatar, Level vào database.
   - `POST /api/user/streak`: Ghi nhận đăng nhập/hoạt động hàng ngày và cộng điểm XP check-in.
4. **Các API Tiến độ từ vựng IELTS**:
   - `GET /api/ielts/vocab/progress`: Lấy danh sách ID các từ IELTS đã thuộc của user.
   - `POST /api/ielts/vocab/progress/migrate`: Đồng bộ danh sách từ từ local lên DB.
   - `POST /api/ielts/vocab/progress/{vocabularyId}`: Đánh dấu đã học / chưa học.
5. **`POST /api/listen-videos/submit`**:
   - Ghi nhận `UserId` của người nộp video lên hệ thống.

---

## 3. Cập nhật Frontend Services

- [`ProfileService.cs`](file:///e:/tailieu/D%E1%BB%B1%20%C3%A1n/ielstHSK-PostgeSQL/frontend/src/Frontend.App/Services/ProfileService.cs): Tự động lấy và lưu hồ sơ người dùng đồng bộ giữa `localStorage` và API backend.
- [`StreakService.cs`](file:///e:/tailieu/D%E1%BB%B1%20%C3%A1n/ielstHSK-PostgeSQL/frontend/src/Frontend.App/Services/StreakService.cs): Tự động đồng bộ chuỗi ngày học với database backend mỗi khi hoạt động.
- [`Program.cs`](file:///e:/tailieu/D%E1%BB%B1%20%C3%A1n/ielstHSK-PostgeSQL/frontend/src/Frontend.App/Program.cs): Đăng ký `AuthHeaderHandler` cho các service để tự động đính kèm Token khi gọi API.
