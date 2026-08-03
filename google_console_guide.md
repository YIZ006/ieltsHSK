# Hướng dẫn tạo Client ID Đăng nhập Google (Google Console)

Để tích hợp Đăng nhập bằng Google vào dự án của bạn, trước tiên bạn cần tạo thông tin xác thực (Credentials) trên Google Cloud Console. Dưới đây là các bước chi tiết để làm ở môi trường local (localhost).

## Bước 1: Tạo Project trên Google Cloud
1. Truy cập vào [Google Cloud Console](https://console.cloud.google.com/).
2. Đăng nhập bằng tài khoản Google của bạn.
3. Nhìn lên thanh công cụ trên cùng (cạnh chữ Google Cloud), nhấp vào nút chọn Project (hoặc **Select a project**).
4. Bấm vào **New Project** (Dự án mới).
5. Đặt tên dự án (ví dụ: `IeltsHSK-App`) và bấm **Create**. Đợi một lát để Google tạo dự án, sau đó nhớ chọn dự án bạn vừa tạo.

## Bước 2: Cấu hình màn hình chấp thuận OAuth (OAuth consent screen)
1. Ở menu bên trái, đi tới **APIs & Services** > **OAuth consent screen** (Màn hình chấp thuận OAuth).
2. Chọn **External** (Bên ngoài) để bất kỳ ai có tài khoản Google đều có thể đăng nhập. Bấm **Create**.
3. Điền các thông tin bắt buộc:
   - **App name:** `IeltsHSK` (tên hiển thị cho người dùng).
   - **User support email:** (chọn email của bạn).
   - **Developer contact information:** (nhập email của bạn).
4. Bấm **Save and Continue** qua các bước Scopes và Test users (bạn có thể thêm email của chính mình vào Test users nếu ứng dụng đang ở chế độ Testing).

## Bước 3: Tạo thông tin xác thực (Credentials)
1. Chuyển sang menu **Credentials** (Thông tin xác thực) ở bên trái.
2. Bấm vào nút **+ CREATE CREDENTIALS** ở trên cùng và chọn **OAuth client ID**.
3. Ở mục **Application type**, chọn **Web application**.
4. Đặt tên cho client (ví dụ: `IeltsHSK Local`).
5. Ở phần **Authorized JavaScript origins** (Nguồn gốc JavaScript được phép), bấm **ADD URI** và nhập đường dẫn Frontend của bạn:
   - `http://localhost:5102`
   - `https://localhost:7102`
6. Ở phần **Authorized redirect URIs** (URI chuyển hướng được phép), bấm **ADD URI** và nhập đường dẫn Backend của bạn sẽ xử lý callback (sẽ cấu hình sau, ví dụ):
   - `http://localhost:5101/signin-google`
   - `https://localhost:7101/signin-google`
7. Bấm **Create**.

## Bước 4: Lấy Client ID và Client Secret
Sau khi tạo xong, một bảng thông báo sẽ hiện ra chứa **Client ID** và **Client Secret**.
> [!IMPORTANT]
> Hãy copy 2 chuỗi này và lưu lại. Chúng ta sẽ cần dán chúng vào file `appsettings.Development.json` ở Backend API để hệ thống có thể kết nối với Google.

---
**Khi bạn đã lấy được Client ID và Client Secret, hãy báo cho tôi biết để tôi bắt đầu code phần Backend và Frontend tích hợp Google Login cho bạn nhé!**
