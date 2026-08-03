# IELTS & HSK Learning Platform

Hệ thống ứng dụng học và luyện thi chứng chỉ ngoại ngữ IELTS & HSK. Dự án bao gồm Frontend (Blazor WebAssembly) và Backend (.NET Core Web API), sử dụng cơ sở dữ liệu SQL Server.

## 🚀 Hướng dẫn khởi chạy Server & Ứng dụng

Để chạy được dự án này trên máy tính của bạn, hãy làm theo tuần tự các bước dưới đây:

### 1. Yêu cầu hệ thống (Prerequisites)
- Đã cài đặt **.NET 9.0 SDK** (cho Frontend) và **.NET 10.0 SDK** (cho Backend).
- Đã cài đặt và đang chạy **SQL Server** (Mặc định cấu hình đang dùng `localhost\SQLEXPRESS01`).

### 2. Khởi chạy Backend API (Bắt buộc chạy trước)
Backend chứa cơ sở dữ liệu và các API cung cấp dữ liệu cho Frontend.
- Mở cửa sổ Terminal (hoặc CMD).
- Trỏ đường dẫn vào thư mục của Backend Api:
  ```bash
  cd backend/src/Backend.Api
  ```
- Chạy lệnh khởi động:
  ```bash
  dotnet run
  ```
- *Lưu ý: Ngay lần chạy đầu tiên, Backend sẽ tự động cập nhật cơ sở dữ liệu (Database Migrations) và nạp dữ liệu mẫu (Data Seeding).*
- Backend sẽ chạy tại: **http://localhost:5101**

### 3. Khởi chạy Frontend (Blazor WebAssembly)
Sau khi Backend đã báo chạy thành công (`Application started`), hãy tiếp tục bật Frontend.
- Mở một cửa sổ Terminal (hoặc CMD) **mới**.
- Trỏ đường dẫn vào thư mục của Frontend:
  ```bash
  cd frontend/src/Frontend.App
  ```
- Chạy lệnh khởi động:
  ```bash
  dotnet run
  ```
- Frontend sẽ chạy tại: **http://localhost:5102**

---

### 4. Truy cập Ứng dụng
- Mở trình duyệt web (Chrome, Cốc Cốc, Edge...).
- Nhập địa chỉ: [http://localhost:5102](http://localhost:5102)
- Bạn sẽ thấy trang chủ. Bấm vào nút "Explore IELTS" hoặc "Explore HSK" để trải nghiệm các tính năng đã được thiết kế.

### 💡 Xử lý sự cố thường gặp
- **Lỗi không kết nối được Database:** Hãy kiểm tra lại chuỗi kết nối (Connection String) trong file `backend/src/Backend.Api/appsettings.Development.json` xem tên máy chủ SQL Server của bạn có đúng là `localhost\SQLEXPRESS01` không.
- **Lỗi port đã được sử dụng (Address already in use):** Đảm bảo bạn đã tắt các tiến trình `dotnet` cũ (Bấm `Ctrl + C` ở CMD cũ) trước khi chạy lệnh `dotnet run` mới.