# Bảng Tổng Hợp Router Dự Án ieltsHSK

Tài liệu này liệt kê toàn bộ các đường dẫn (routes) hiện có trong dự án, được chia làm 2 phần: Frontend (Giao diện) và Backend (API).

---

## 1. Frontend Routes (Blazor Pages)
Đây là các đường dẫn URL hiển thị trên thanh địa chỉ của trình duyệt. Người dùng truy cập các đường dẫn này để mở các giao diện tương ứng. Các route này được cấu hình thông qua thẻ `@page` trong thư mục `Frontend.App/Pages`.

### 🎓 Dành cho Người dùng (User)
- `/` - Trang chủ (Home)
- `/hsk` - Trang giới thiệu/cổng thông tin HSK
- `/ielts` - Trang giới thiệu/cổng thông tin IELTS
- `/ielts/dashboard` - Bảng điều khiển IELTS của người dùng
- `/ielts/luyen-de` - Trang danh sách các bộ đề thi IELTS
- `/ielts/reading`, `/ielts/reading/preview`, `/ielts/reading/{ExamUrl}` - Trang làm bài thi Đọc IELTS
- `/ielts/listening`, `/ielts/listening/preview`, `/ielts/listening/{ExamUrl}` - Trang làm bài thi Nghe IELTS
- `/ielts/writing`, `/ielts/writing/preview`, `/ielts/writing/{ExamUrl}` - Trang làm bài thi Viết IELTS
- `/ielts/speaking`, `/ielts/speaking/preview`, `/ielts/speaking/{ExamUrl}` - Trang làm bài thi Nói IELTS
- `/toeic/test`, `/toeic/test/{*ExamUrl}` - Trang làm bài thi TOEIC

### ⚙️ Dành cho Quản trị viên (Admin)
- `/admin` - Bảng điều khiển dành cho Admin
- `/admin/mock-tests` - Trang quản lý các bộ đề thi IELTS
- `/admin/reading-tool` - Công cụ tạo/sửa đề thi Đọc IELTS
- `/admin/toeic-mock-tests` - Trang quản lý các bộ đề thi TOEIC
- `/admin/toeic-builder` - Công cụ tạo/sửa/soạn đề thi TOEIC trực quan

---

## 2. Backend Routes (Minimal API)
Đây là các đường dẫn API mà Frontend (hoặc bên thứ 3) gọi tới để lấy dữ liệu, lưu dữ liệu, tải file, xác thực người dùng... Được định nghĩa trong `backend/src/Backend.Api/Program.cs`.

### 🔐 Xác thực & Người dùng (Authentication)
- `POST /api/auth/register` - Đăng ký tài khoản mới
- `POST /api/auth/login` - Đăng nhập tài khoản
- `POST /api/auth/google-login` - Đăng nhập bằng tài khoản Google
- `PUT /api/user/level` - Cập nhật trình độ hiện tại của người dùng

### 📚 Tài nguyên học tập IELTS
- `GET /api/ielts/courses` - Lấy danh sách khóa học IELTS
- `GET /api/ielts/websites` - Lấy danh sách các trang web hữu ích
- `GET /api/ielts/sections` - Lấy danh sách các khu vực/góc học tập
- `GET /api/ielts/exams` - Lấy danh sách bài thi IELTS
- `POST /api/ielts/exams` - Tạo mới bài thi IELTS

### 📝 Quản lý Đề thi Mock Test (IELTS/Chung)
- `GET /api/mock-tests` - Lấy danh sách toàn bộ đề thi (Mock Tests)
- `POST /api/mock-tests` - Thêm mới một bộ đề thi
- `PUT /api/mock-tests/{id}` - Cập nhật thông tin một bộ đề thi
- `DELETE /api/mock-tests/{id}` - Xóa một bộ đề thi (và xóa các file liên quan trên Cloudflare R2)
- `POST /api/mock-tests/upload` - Tải lên một file PDF/Audio đề thi (lưu lên R2)

### 💯 Kết quả làm bài
- `POST /api/test-submissions` - Nộp bài thi và lưu lại kết quả, chi tiết đáp án của người dùng

### 🎯 Quản lý Đề thi TOEIC
- `POST /api/toeic/upload-media` - Tải lên file hình ảnh/âm thanh cho câu hỏi TOEIC (lưu lên R2)
- `POST /api/toeic/save-exam` - Lưu toàn bộ nội dung đề thi TOEIC dưới dạng JSON (lưu lên R2) và tự động tạo Mock Test tương ứng

---
_Lưu ý: URL cơ sở (Base URL) cho API khi chạy ở môi trường phát triển thường được cấu hình trong launchSettings.json._
