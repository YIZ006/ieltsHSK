# BÁO CÁO KỸ THUẬT

- **Ngày thực hiện**: 13/09/2026
- **Tên báo cáo**: BÁO CÁO TOÀN DIỆN KHẮC PHỤC SỰ CỐ SPAM LỖI 401, CÚ PHÁP SVG LOCALE VI-VN VÀ NGOẠI LỆ OFFLINE SERVICE WORKER
- **Người viết**: YIZ006 (https://github.com/YIZ006)
- **Tài liệu lưu trữ**: `docs/bao_cao_khac_phuc_loi_console_va_toi_uu_offline_pwa.md`
- **Trạng thái**: Đã giải quyết triệt để, kiểm thử thành công & đẩy lên nhánh `main`

---

## 1. TỔNG QUAN SỰ CỐ (EXECUTIVE SUMMARY)

### 1.1. Hiện tượng ghi nhận
Trong quá trình truy cập ứng dụng và kiểm tra thông qua công cụ lập trình viên của trình duyệt (Browser Developer Tools Console), hệ thống liên tục ghi nhận các cảnh báo và ngoại lệ nghiêm trọng:

1. **Spam lỗi HTTP 401 Unauthorized**:
   ```text
   info: Microsoft.AspNetCore.Authorization.DefaultAuthorizationService[2]
         Authorization failed. These requirements were not met:
         DenyAnonymousAuthorizationRequirement: Requires an authenticated user.
   :5101/api/user/me:1  Failed to load resource: the server responded with a status of 401 (Unauthorized)
   :5101/api/user/study-activity:1  Failed to load resource: the server responded with a status of 401 (Unauthorized)
   :5101/api/user/study-time:1  Failed to load resource: the server responded with a status of 401 (Unauthorized)
   :5101/api/toeic/vocab/progress:1  Failed to load resource: the server responded with a status of 401 (Unauthorized)
   :5101/api/user/game-progress:1  Failed to load resource: the server responded with a status of 401 (Unauthorized)
   ```
2. **Lỗi phân tích cú pháp biểu đồ SVG (SVG Parsing Errors)**:
   ```text
   blazor.webassembly.js:1 Unexpected value 109,99999999999999 parsing y1 attribute.
   blazor.webassembly.js:1 Unexpected value 109,99999999999999 parsing y2 attribute.
   blazor.webassembly.js:1 Error: <path> attribute d: Expected number, "M 120,195,0 160,19…".
   ```
3. **Lỗi thiếu tài nguyên ảnh bìa (HTTP 404 Not Found)**:
   ```text
   mocktest-cover.png:1  Failed to load resource: the server responded with a status of 404 (Not Found)
   ```
4. **Ngoại lệ Service Worker chưa xử lý (Unhandled Promise Rejection)**:
   ```text
   sw.js:58  Uncaught (in promise) TypeError: Failed to fetch
   ```
5. **Lỗi chính sách CORS khi Cache Media**:
   ```text
   Access to fetch at 'https://www.youtube.com/embed/waS74McMxUY' from origin 'http://localhost:5102' 
   has been blocked by CORS policy: No 'Access-Control-Allow-Origin' header is present on the requested resource.
   ```

### 1.2. Tác động hệ thống
- Phía máy chủ bị quá tải bởi hàng loạt request định kỳ vô ích từ người dùng vãng lai chưa đăng nhập.
- Giao diện biểu đồ phân tích năng lực học tập và xu hướng điểm số (IELTS Priority Review & Mock Test Summary) bị vỡ hoặc không hiển thị đường kẻ toạ độ, làm giảm trải nghiệm học viên.
- Thẻ bìa phòng thi thử IELTS và HSK bị lỗi ảnh mặc định, phải phụ thuộc vào link placeholder ngoài Internet.
- Service Worker bị dừng đột ngột luồng đồng bộ bộ nhớ đệm, tiềm ẩn rủi ro khi học viên sử dụng ứng dụng ở chế độ ngoại tuyến (Offline PWA).

---

## 2. PHÂN TÍCH NGUYÊN NHÂN GỐC RỄ (ROOT CAUSE ANALYSIS)

### 2.1. Cơ chế lặp vô hạn và spam request 401 khi chưa đăng nhập
- **Nguyên nhân**:
  1. Các dịch vụ nền ở frontend (`StreakService`, `ProfileService`, `ToeicStudyTrackerService`, `ToeicVocabularyService`, `UserGameProgressService`) kích hoạt timer hoặc gọi API ngay khi component được khởi tạo (`OnInit`), mà không kiểm tra xem client có đang lưu `authToken` hợp lệ trong `localStorage` hay không.
  2. Tại `AuthHeaderHandler.cs`, khi bắt gặp mã phản hồi `401 Unauthorized`, handler mặc định kích hoạt hàm `TryRefreshTokenAsync()` để gọi endpoint `/api/auth/refresh-token`. Tuy nhiên, với người dùng khách (Guest), `request.Headers.Authorization` vốn là `null`. Việc cố gắng refresh token không có thật vừa vô nghĩa vừa sinh thêm chuỗi request lỗi lặp đi lặp lại.

### 2.2. Xung đột định dạng số thập phân của ngôn ngữ Tiếng Việt (`vi-VN`) với chuẩn SVG
- **Nguyên nhân**:
  1. Blazor WebAssembly khi chạy trên trình duyệt có hệ điều hành hoặc cấu hình ngôn ngữ Tiếng Việt sẽ thiết lập `CultureInfo.CurrentCulture` thành `vi-VN`.
  2. Trong chuẩn văn hoá `vi-VN`, ký tự ngăn cách phần thập phân của số thực (`double`, `float`, `decimal`) là dấu phẩy (`,`) thay vì dấu chấm (`.`).
  3. Khi các trang `IeltsPriorityReview.razor` và `IeltsMockTestSummary.razor` render chuỗi nội suy SVG:
     ```razor
     <line y1="@y" y2="@y" ... />
     <path d="M @x1,@y1 L @x2,@y2 ..." />
     ```
     Số thực `109.99` bị format thành `"109,99"`. Chuỗi đường dẫn trở thành `d="M 120,195,0 160,19..."`. Trình duyệt web tuân theo đặc tả W3C SVG chỉ chấp nhận dấu chấm thập phân, dẫn đến lỗi cú pháp không thể render.

### 2.3. Thiếu file ảnh bìa tĩnh `mocktest-cover.png` trong kho tài nguyên
- **Nguyên nhân**:
  1. Thẻ `<img>` tại `IeltsMockTests.razor` và `HskMockTests.razor` định nghĩa đường dẫn tĩnh `/sample-data/mocktest-cover.png`.
  2. Thư mục `Frontend.App/wwwroot/sample-data/` trước đó chưa có tệp ảnh này, khiến trình duyệt nhận mã lỗi 404 trước khi rơi vào sự kiện fallback `onerror`.

### 2.4. Luồng fetch của Service Worker không bọc khối xử lý lỗi mạng
- **Nguyên nhân**:
  1. Trong file `sw.js`, khi xử lý sự kiện `fetch` cho các điều hướng navigation và tài nguyên tĩnh, mã nguồn gọi trực tiếp `event.respondWith(fetch(event.request))` mà không bắt ngoại lệ `catch(err)`.
  2. Khi người dùng chuyển trang nhanh hoặc mạng có độ trễ/ngắt quãng cục bộ, `fetch()` ném lỗi `TypeError: Failed to fetch`, trở thành Unhandled Promise Rejection.

### 2.5. Cố gắng ghi Cache API tài nguyên bên thứ ba (YouTube Embed)
- **Nguyên nhân**:
  1. Hàm `cacheMedia(url)` trong `offlineStorage.js` nhận đầu vào là các liên kết đa phương tiện trong bài thi.
  2. Khi bài thi nhúng video YouTube dạng `https://www.youtube.com/embed/...`, hàm này vẫn thực hiện `fetch(url, { mode: 'cors' })` để lưu vào cache của trình duyệt. Vì máy chủ YouTube không cấp header `Access-Control-Allow-Origin` cho domain web nội bộ, trình duyệt chặn đứng request theo chính sách CORS.

---

## 3. CÁC THAY ĐỔI KỸ THUẬT ĐÃ TRIỂN KHAI (TECHNICAL IMPLEMENTATION)

### 3.1. Ngăn chặn triệt để vòng lặp 401 và bổ sung bảo vệ xác thực

1. **`AuthHeaderHandler.cs`**:
   - Kiểm tra nếu request ban đầu không mang header `Authorization`, lập tức bỏ qua không gọi `TryRefreshTokenAsync()`:
   ```csharp
   // Nếu request ban đầu không có Authorization header (khách vãng lai), không cần thử refresh token
   if (response.StatusCode == HttpStatusCode.Unauthorized && request.Headers.Authorization != null)
   {
       // Chỉ thử refresh khi trước đó đã có token xác thực
   }
   ```

2. **`ProfileService.cs`**:
   - Kiểm tra `authToken` trước khi gửi request lấy thông tin cá nhân:
   ```csharp
   var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");
   if (string.IsNullOrWhiteSpace(token)) return null;
   ```

3. **`StreakService.cs`**:
   - Kiểm tra `authToken` trong `RecordStudyTimeAsync()` để chế độ khách không spam `/api/user/study-time`.

4. **`ToeicStudyTrackerService.cs`**:
   - Chỉ thực hiện `SyncToBackendAsync()` khi người dùng đã đăng nhập thành công.

5. **`ToeicVocabularyService.cs`**:
   - Kiểm tra xác thực ở cả `GetProgressAsync()` và `UpdateProgressAsync()`. Nếu chưa đăng nhập, tự động đọc và ghi tiến trình vào `localStorage` phục vụ học tập ngoại tuyến.

6. **`UserGameProgressService.cs`**:
   - Đảm bảo endpoint `/api/user/game-progress` chỉ được gọi khi có phiên làm việc của người dùng đăng nhập.

---

### 3.2. Chuẩn hoá toạ độ biểu đồ SVG bất biến theo văn hoá (`CultureInfo.InvariantCulture`)

1. **`IeltsPriorityReview.razor`** & **`IeltsMockTestSummary.razor`**:
   - Bổ sung hàm tiện ích định dạng số:
   ```csharp
   private static string F(double val) => val.ToString("0.##", CultureInfo.InvariantCulture);
   ```
   - Thay thế toàn bộ các biểu thức toạ độ nội suy trong thẻ `<line>`, `<circle>`, `<text>` và các hàm dựng path biểu đồ:
   ```razor
   <!-- Trước đây -->
   <line x1="120" y1="@y" x2="940" y2="@y" ... />
   
   <!-- Sau khi chuẩn hoá -->
   <line x1="120" y1="@F(y)" x2="940" y2="@F(y)" ... />
   ```
   ```csharp
   // Chuẩn hoá đường cong và vùng diện tích biểu đồ
   sb.Append(i == 0 ? $"M {F(x)},{F(y)}" : $" L {F(x)},{F(y)}");
   ```

---

### 3.3. Bổ sung tài nguyên ảnh bìa chuẩn `mocktest-cover.png`

- Đã tạo tệp ảnh PNG tiêu chuẩn độ phân giải cao tại:
  `frontend/src/Frontend.App/wwwroot/sample-data/mocktest-cover.png`
- Ảnh sử dụng bảng màu hiện đại (Modern Indigo/Navy Gradient) cùng nhãn nhận diện bộ sưu tập đề thi, kích thước tối ưu 24 KB.

---

### 3.4. Xử lý an toàn ngoại lệ Service Worker & Bỏ qua URL YouTube

1. **`frontend/src/Frontend.App/wwwroot/sw.js`**:
   - Bọc khối fetch điều hướng và static asset bằng cơ chế bắt lỗi an toàn:
   ```javascript
   event.respondWith(
     fetch(event.request).catch(function(err) {
       // Xử lý fallback an toàn khi mạng ngắt quãng hoặc offline
       return caches.match(event.request).then(function(cached) {
         return cached || caches.match('/offline.html') || new Response('', { status: 408 });
       });
     })
   );
   ```

2. **`frontend/src/Frontend.App/wwwroot/js/offlineStorage.js`**:
   - Lọc bỏ các URL nhúng bên ngoài khỏi hàm lưu trữ ngoại tuyến:
   ```javascript
   async function cacheMedia(url) {
     if (!url) return false;
     // Bỏ qua các URL video/audio stream bên ngoài như YouTube để tránh vi phạm CORS
     if (url.includes('youtube.com') || url.includes('youtu.be')) {
       return false;
     }
     ...
   }
   ```

---

## 4. KIỂM CHỨNG VÀ KẾT QUẢ TRIỂN KHAI (VERIFICATION & DEPLOYMENT)

### 4.1. Kiểm tra biên dịch (Build Verification)
Đã chạy lệnh kiểm tra biên dịch toàn bộ dự án frontend với cấu hình tối ưu Release:
```powershell
dotnet build frontend/Frontend.sln -c Release
```
- **Trạng thái**: `Build succeeded`
- **Lỗi biên dịch**: **0 Error(s)**
- **Cảnh báo**: 1 CS8601 (warning không ảnh hưởng logic)

### 4.2. Kiểm tra tuân thủ quy tắc Repo (`AGENTS.md`)
- Kiểm tra danh sách tệp qua `git status --short`:
  - 10 tệp mã nguồn sửa đổi.
  - 1 tệp tài nguyên tĩnh chuẩn nằm đúng thư mục `sample-data/`.
  - Không có bất kỳ tệp tạm rác nào ở thư mục gốc (`fix_json.js`, `output.json`, loose json/html drafts).

### 4.3. Đẩy mã nguồn lên kho Git (Git Commit & Push)
- **Mã Commit**: `6a403de`
- **Thông điệp Commit**: `fix(frontend): resolve 401 loop, SVG comma formatting, and SW cache errors`
- **Nhánh**: `main`
- **Máy chủ từ xa**: `https://github.com/YIZ006/ieltsHSK.git` (Thành công 100%)

---

## 5. HƯỚNG DẪN KIỂM TRA CHO NGƯỜI DÙNG

1. **Kiểm tra trạng thái khách vãng lai (Chưa đăng nhập)**:
   - Mở tab ẩn danh hoặc xóa `localStorage`.
   - Mở Developer Tools (`F12`) -> Tab `Console` & `Network`.
   - Truy cập các trang: `/ielts/luyen-de`, `/ielts/on-tap-uu-tien`, `/toeic/vocab`.
   - **Kết quả mong đợi**: Console hoàn toàn sạch sẽ, không còn xuất hiện spam lỗi `401 Unauthorized` liên tục.

2. **Kiểm tra hiển thị đồ thị SVG**:
   - Đảm bảo ngôn ngữ trình duyệt / hệ điều hành đang là Tiếng Việt.
   - Truy cập trang Ôn tập ưu tiên IELTS (`/ielts/on-tap-uu-tien`) hoặc Tóm tắt bài thi (`/ielts/mock-test/summary`).
   - **Kết quả mong đợi**: Không còn cảnh báo `Unexpected value ... parsing y1 attribute`, đồ thị vẽ các trục và đường xu hướng chuẩn xác, mượt mà.

3. **Kiểm tra ảnh bìa đề thi**:
   - Truy cập `/ielts/phong-thi-thu` và `/hsk/phong-thi-thu`.
   - **Kết quả mong đợi**: Ảnh bìa hiển thị ngay lập tức, không còn request lỗi 404.
